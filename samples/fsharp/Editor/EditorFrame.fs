namespace Editor

open System
open System.Globalization
open System.IO
open System.Threading.Tasks
open WxKit

module internal Commands =
    let [<Literal>] AppName = "Editor"
    let [<Literal>] Wildcard = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
    // Ids of the commands without a standard one, above the ids wxWidgets keeps for itself.
    let [<Literal>] FindNext = 6001
    let [<Literal>] GoToLine = 6002
    let [<Literal>] WordWrap = 6003

open Commands

/// The editor's window: one text control, its menus, and the file it shows.
type EditorFrame() as this =
    inherit Frame(null, AppName, size = Size(800, 600))
    let findData = new FindReplaceData(FindReplaceFlags.Down)
    let mutable findDialog: (FindReplaceDialog * bool) option = None
    let mutable path: string option = None
    // The line ending the file had, which saving keeps. The control itself only knows "\n".
    let mutable newLine = Environment.NewLine
    let mutable wordWrap = true
    let mutable status = ""
    // The printer and page settings, which the page setup dialog and printing keep between uses.
    let mutable printData = new PrintData()
    let mutable pageSetup = new PageSetupDialogData()
    // A file dropped on the text, which opens, as File > Open does, rather than being inserted.
    let dropped = Event<string>()

    let createText value =
        let wrap = if wordWrap then TextCtrlStyle.WordWrap else TextCtrlStyle.DontWrap ||| TextCtrlStyle.HorizontalScrollBar
        // A rich text control on Windows counts a line break as one character, as the text does,
        // so positions in one are positions in the other. Tab types a tab, as in any editor.
        let style = TextCtrlStyle.MultiLine ||| TextCtrlStyle.Rich2 ||| TextCtrlStyle.ProcessTab ||| wrap
        let text = new TextCtrl(this, value, style = style)
        // Windows and macOS check spelling as you type. Linux, without gspell, does not.
        text.EnableProofCheck() |> ignore
        text.DragAcceptFiles true
        text.DropFiles.Add(fun e -> e.Files |> Seq.tryHead |> Option.iter dropped.Trigger)
        text

    let mutable text = createText ""

    let fileName (file: string) =
        match Path.GetFileName file with
        | null -> file
        | name -> name

    let documentName () =
        path |> Option.map fileName |> Option.defaultValue "Untitled"

    let message text (style: MessageDialogStyle) (parent: Window) =
        MessageBox.Show(text, AppName, MessageDialogStyle.Ok ||| style, parent) |> ignore

    // The find dialog, while it's open, or the frame, as the parent of a message about a search.
    let searchParent () =
        match findDialog with
        | Some(dialog, _) -> dialog :> Window
        | None -> this :> Window

    let updateTitle () =
        let title = (if text.IsModified then "*" else "") + documentName () + " - " + AppName
        if this.Title <> title then
            this.Title <- title

    let updateStatus () =
        let _, column, line = text.PositionToXY text.InsertionPoint
        let shown = $"Line {line + 1}, column {column + 1}"
        if shown <> status then
            status <- shown
            this.SetStatusText shown
        updateTitle ()

    let openFile file =
        task {
            try
                let! contents = File.ReadAllTextAsync file
                newLine <- if contents.Contains "\r\n" then "\r\n" else "\n"
                text.ChangeValue(contents.Replace("\r\n", "\n"))
                text.DiscardEdits()
                text.InsertionPoint <- 0
                path <- Some file
                updateTitle ()
            with
            | :? IOException
            | :? UnauthorizedAccessException as error ->
                message $"{AppName} could not open {file}. {error.Message}" MessageDialogStyle.IconError this
        }

    // Saves to the file the editor shows, or asks where first. False when the user cancels, or
    // saving fails.
    let save saveAs =
        task {
            let target =
                match path with
                | Some file when not saveAs -> Some file
                | _ ->
                    let name = path |> Option.map fileName |> Option.defaultValue "Untitled.txt"
                    let style = FileDialogStyle.Save ||| FileDialogStyle.OverwritePrompt
                    use dialog = new FileDialog(this, "Save As", defaultFile = name, wildcard = Wildcard, style = style)
                    if dialog.ShowModal() = StandardId.Ok then Some dialog.Path else None
            match target with
            | None -> return false
            | Some file ->
                try
                    do! File.WriteAllTextAsync(file, text.Value.Replace("\n", newLine))
                    path <- Some file
                    text.DiscardEdits()
                    updateTitle ()
                    return true
                with
                | :? IOException
                | :? UnauthorizedAccessException as error ->
                    message $"{AppName} could not save {file}. {error.Message}" MessageDialogStyle.IconError this
                    return false
        }

    // Asks what to do with unsaved changes. False when the user cancels, or saving fails.
    let confirmDiscard () =
        task {
            if not text.IsModified then
                return true
            else
                let style = MessageDialogStyle.YesNo ||| MessageDialogStyle.Cancel ||| MessageDialogStyle.IconWarning
                match MessageBox.Show($"Do you want to save the changes to {documentName ()}?", AppName, style, this) with
                | StandardId.Yes -> return! save false
                | StandardId.No -> return true
                | _ -> return false
        }

    // Runs one of the file commands, which wait for the user and the disk, from a menu or an event.
    let start (command: unit -> #Task) = command () |> ignore

    let newFile () =
        task {
            let! confirmed = confirmDiscard ()
            if confirmed then
                text.ChangeValue ""
                text.DiscardEdits()
                path <- None
                newLine <- Environment.NewLine
                updateTitle ()
        }

    let openChosen () =
        task {
            let! confirmed = confirmDiscard ()
            if confirmed then
                let style = FileDialogStyle.Open ||| FileDialogStyle.FileMustExist
                let chosen =
                    use dialog = new FileDialog(this, "Open", wildcard = Wildcard, style = style)
                    if dialog.ShowModal() = StandardId.Ok then Some dialog.Path else None
                match chosen with
                | Some file -> do! openFile file
                | None -> ()
        }

    let openDropped file =
        task {
            let! confirmed = confirmDiscard ()
            if confirmed then
                do! openFile file
        }

    // Find and replace share one modeless dialog, which starts with the selected text, if any.
    let rec showFindDialog replace =
        let selected = text.StringSelection
        if selected <> "" && not (selected.Contains '\n') then
            findData.FindString <- selected
        match findDialog with
        | Some(dialog, replaces) when replaces = replace -> dialog.Raise()
        | open' ->
            open' |> Option.iter (fun (dialog, _) -> dialog.Destroy() |> ignore)
            let style = if replace then FindReplaceDialogStyle.ReplaceDialog else enum 0
            let dialog = new FindReplaceDialog(this, findData, (if replace then "Replace" else "Find"), style)
            dialog.FindRequested.Add(fun e -> findNext e.FindString e.Flags |> ignore)
            dialog.FindNextRequested.Add(fun e -> findNext e.FindString e.Flags |> ignore)
            dialog.ReplaceRequested.Add(fun e -> replaceOne e.FindString e.ReplaceString e.Flags)
            dialog.ReplaceAllRequested.Add(fun e -> replaceAll e.FindString e.ReplaceString e.Flags)
            dialog.FindClosed.Add(fun _ ->
                dialog.Destroy() |> ignore
                findDialog <- None)
            findDialog <- Some(dialog, replace)
            dialog.Show() |> ignore

    // Selects the next match after the selection, or the one before it, going up.
    and findNext needle flags =
        if needle = "" then
            showFindDialog false
            false
        else
            let from = text.SelectionStart
            let last = from + text.SelectionLength
            let start = if flags &&& FindReplaceFlags.Down = FindReplaceFlags.Down then int last else int from - 1
            match Search.find text.Value needle start flags with
            | Some found ->
                text.SetSelection(found, found + needle.Length)
                text.ShowPosition(found)
                true
            | None ->
                message $"{AppName} cannot find \"{needle}\"." MessageDialogStyle.IconInformation (searchParent ())
                false

    and replaceOne needle replacement flags =
        let from = text.SelectionStart
        let last = from + text.SelectionLength
        if last > from && String.Equals(text.GetRange(from, last), needle, Search.comparison flags) then
            text.Replace(from, last, replacement)
            text.SetSelection(from, from + replacement.Length)
        findNext needle flags |> ignore

    and replaceAll needle replacement flags =
        if needle <> "" then
            let replaced, count = Search.replaceAll text.Value needle replacement flags
            if count > 0 then
                text.Value <- replaced
                text.MarkDirty()
            let occurrences = if count = 1 then "occurrence" else "occurrences"
            message $"{AppName} replaced {count} {occurrences} of \"{needle}\"." MessageDialogStyle.IconInformation (searchParent ())

    let deleteSelection () =
        let from = text.SelectionStart
        let last = from + text.SelectionLength
        if last > from then
            text.Remove(from, last)

    let goToLine () =
        let _, _, current = text.PositionToXY text.InsertionPoint
        let lines = text.NumberOfLines
        let answer =
            use dialog = new TextEntryDialog(this, $"Line number, from 1 to {lines}:", "Go To Line", string (current + 1))
            if dialog.ShowModal() = StandardId.Ok then Some dialog.Value else None
        let (|Line|_|) (typed: string) =
            match Int32.TryParse(typed, NumberStyles.Integer, CultureInfo.CurrentCulture) with
            | true, line when line >= 1 && line <= lines -> Some line
            | _ -> None
        match answer with
        | None -> ()
        | Some(Line line) ->
            let position = text.XYToPosition(0, line - 1)
            text.InsertionPoint <- position
            text.ShowPosition position
        | Some _ ->
            let icon = MessageDialogStyle.Ok ||| MessageDialogStyle.IconWarning
            MessageBox.Show($"The line number has to be from 1 to {lines}.", "Go To Line", icon, this) |> ignore

    // A text control wraps lines or not from when it's created, so turning wrapping on or off
    // replaces it with one that does, keeping the text, the selection, the font, and whether it changed.
    let toggleWordWrap () =
        wordWrap <- not wordWrap
        let old = text
        let from = old.SelectionStart
        let last = from + old.SelectionLength
        let modified = old.IsModified
        use font = old.Font
        text <- createText old.Value
        if not (isNull font) then
            text.Font <- font
        text.SetSelection(from, last)
        if modified then
            text.MarkDirty()
        old.Destroy() |> ignore
        this.SendSizeEvent()
        text.SetFocus()

    let setUpPage () =
        pageSetup.PrintData <- printData
        use dialog = new PageSetupDialog(this, pageSetup)
        if dialog.ShowModal() = StandardId.Ok then
            pageSetup <- dialog.PageSetupData |> Option.ofObj |> Option.defaultValue pageSetup
            printData <- pageSetup.PrintData |> Option.ofObj |> Option.defaultValue printData

    let createPrintout () =
        use font = text.Font
        new TextPrintout(documentName (), text.Value, font |> Option.ofObj |> Option.map _.NativeFontInfoDescription)

    // The preview takes its printouts, one to show and one for its Print button.
    let preview () =
        use data = new PrintDialogData(PrintData = printData)
        let title = $"Print Preview of {documentName ()}"
        match PrintPreview.Show(createPrintout (), createPrintout (), this, title, data, Size(700, 800)) with
        | null -> message "There is no printer to preview for." MessageDialogStyle.IconWarning this
        | _ -> ()

    let print () =
        use data = new PrintDialogData(PrintData = printData)
        use printer = new Printer(data)
        use printout = createPrintout ()
        if printer.Print(this, printout) then
            printData <-
                printer.PrintDialogData
                |> Option.ofObj
                |> Option.bind (fun data -> Option.ofObj data.PrintData)
                |> Option.defaultValue printData
        elif Printer.LastError = PrinterError.Error then
            message $"{AppName} could not print {documentName ()}." MessageDialogStyle.IconError this

    let chooseFont () =
        use data = new FontData()
        use current = text.Font
        if not (isNull current) then
            data.InitialFont <- current
        use dialog = new FontDialog(this, data)
        if dialog.ShowModal() = StandardId.Ok then
            match dialog.FontData with
            | null -> ()
            | chosen ->
                use chosen = chosen
                use font = chosen.ChosenFont
                if not (isNull font) then
                    text.Font <- font

    let about () =
        let info =
            AboutDialogInfo(
                Name = AppName,
                Description = $"A text editor, as a WxKit sample in F#, built with {Application.WxWidgetsVersion}.",
                WebSite = "https://github.com/trypsynth/wxkit"
            )
        AboutBox.Show(info, this)

    // The menu items, and the text control, ask whether they apply as the window goes idle.
    let onUpdateUi (e: UpdateUIEvent) =
        match e.Id with
        | StandardId.Undo -> e.Enable text.CanUndo
        | StandardId.Redo -> e.Enable text.CanRedo
        | StandardId.Cut
        | StandardId.Delete -> e.Enable text.CanCut
        | StandardId.Copy -> e.Enable text.CanCopy
        | StandardId.Paste -> e.Enable text.CanPaste
        | WordWrap -> e.Check wordWrap
        | id when id = text.Id -> updateStatus ()
        | _ -> ()

    // A close the user asks for waits for an answer about unsaved changes, which may mean saving,
    // so it's vetoed, and the window closes itself once the answer allows it.
    let onClosing (e: CloseEvent) =
        if e.CanVeto && text.IsModified then
            e.Veto() |> ignore
            start (fun () ->
                task {
                    let! confirmed = confirmDiscard ()
                    if confirmed then
                        this.Close(force = true) |> ignore
                })

    do
        // With a standard id and no text, an item gets the platform's label and shortcut.
        let file = new Menu()
        file.Append(StandardId.New).Click.Add(fun _ -> start newFile)
        file.Append(StandardId.Open).Click.Add(fun _ -> start openChosen)
        file.Append(StandardId.Save).Click.Add(fun _ -> start (fun () -> save false))
        file.Append(StandardId.SaveAs).Click.Add(fun _ -> start (fun () -> save true))
        file.AppendSeparator() |> ignore
        file.Append(StandardId.PageSetup).Click.Add(fun _ -> setUpPage ())
        file.Append(StandardId.Preview).Click.Add(fun _ -> preview ())
        file.Append(StandardId.Print).Click.Add(fun _ -> print ())
        file.AppendSeparator() |> ignore
        file.Append(StandardId.Exit).Click.Add(fun _ -> this.Close() |> ignore)
        let edit = new Menu()
        edit.Append(StandardId.Undo).Click.Add(fun _ -> text.Undo())
        edit.Append(StandardId.Redo).Click.Add(fun _ -> text.Redo())
        edit.AppendSeparator() |> ignore
        edit.Append(StandardId.Cut).Click.Add(fun _ -> text.Cut())
        edit.Append(StandardId.Copy).Click.Add(fun _ -> text.Copy())
        edit.Append(StandardId.Paste).Click.Add(fun _ -> text.Paste())
        edit.Append(StandardId.Delete).Click.Add(fun _ -> deleteSelection ())
        edit.AppendSeparator() |> ignore
        edit.Append(StandardId.Find).Click.Add(fun _ -> showFindDialog false)
        edit.Append(FindNext, "Find &Next\tF3").Click.Add(fun _ -> findNext findData.FindString findData.Flags |> ignore)
        edit.Append(StandardId.Replace).Click.Add(fun _ -> showFindDialog true)
        edit.Append(GoToLine, "&Go To Line...\tCtrl+G").Click.Add(fun _ -> goToLine ())
        edit.AppendSeparator() |> ignore
        edit.Append(StandardId.SelectAll).Click.Add(fun _ -> text.SelectAll())
        let format = new Menu()
        format.Append(WordWrap, "&Word Wrap", kind = MenuItemKind.Check).Click.Add(fun _ -> toggleWordWrap ())
        format.Append(StandardId.SelectFont).Click.Add(fun _ -> chooseFont ())
        let help = new Menu()
        help.Append(StandardId.About).Click.Add(fun _ -> about ())
        let bar = new MenuBar()
        for menu, title in [ file, "&File"; edit, "&Edit"; format, "F&ormat"; help, "&Help" ] do
            bar.Append(menu, title) |> ignore
        this.MenuBar <- bar
        this.CreateStatusBar() |> ignore
        this.Closing.Add onClosing
        this.Bind(EventType.UpdateUi, fun _ e -> onUpdateUi e)
        dropped.Publish.Add(fun file -> start (fun () -> openDropped file))
        updateTitle ()

    /// Opens a file into the editor, after asking about unsaved changes.
    member _.OpenAsync(file: string) : Task = openDropped file

    /// For the smoke test: makes a preview, waits for it to lay out and draw the first page, which
    /// wxWidgets does the first time it shows one, and closes it. Without a printer, some platforms
    /// can't make a preview.
    member _.PreviewAndCloseAsync() : Task =
        task {
            use data = new PrintDialogData(PrintData = printData)
            let printout = createPrintout ()
            match PrintPreview.Show(printout, null, this, "Smoke test", data) with
            | null -> ()
            | shown ->
                let mutable waited = 0
                while not printout.HasPrinted && waited < 200 do
                    do! Task.Delay 50
                    waited <- waited + 1
                shown.Close(force = true) |> ignore
                if not printout.HasPrinted then
                    failwith "The print preview drew no page."
        }
