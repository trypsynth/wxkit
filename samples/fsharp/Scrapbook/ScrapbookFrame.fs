namespace Scrapbook

open WxKit
open WxKit.FSharp
open Scrapbook.Snippet

/// A list of snippets, which come from the clipboard or from text or files dropped on the list, and
/// go back to the clipboard, or are dragged out of the list to another application. Copying and
/// pasting are the keyboard's way to do what dragging does with the mouse.
type ScrapbookFrame() as this =
    inherit Frame(null, "Scrapbook", size = Size(520, 400))
    let mutable snippets: Snippet list = []
    let mutable dragging = false
    let panel = new Panel(this)
    let label = new StaticText(panel, "&Snippets, which you can paste, or drop text or files on:")
    let list = new ListBox(panel)
    let paste = new Button(panel, "&Paste")
    let copy = new Button(panel, "&Copy")
    let remove = new Button(panel, "&Remove")

    let selected () =
        match list.Selection with
        | -1 -> None
        | index -> Some index

    let showCount () = this.SetStatusText $"{snippets.Length} snippets"

    let add snippet =
        snippets <- snippets @ [ snippet ]
        list.Append(describe snippet) |> ignore
        list.Selection <- snippets.Length - 1
        showCount ()

    let pasteSnippet () =
        match fromClipboard () with
        | Some snippet -> add snippet
        | None -> this.SetStatusText "The clipboard has no text or files to paste."

    let copySelected () =
        selected ()
        |> Option.iter (fun index ->
            let copied = Clipboard.SetContent(toContent snippets[index])
            this.SetStatusText(if copied then "Copied" else "The clipboard could not be opened."))

    let removeSelected () =
        selected ()
        |> Option.iter (fun index ->
            snippets <- List.removeAt index snippets
            list.Delete index
            if not snippets.IsEmpty then
                list.Selection <- min index (snippets.Length - 1)
            showCount ())

    do
        // With a standard id and no text, an item gets the platform's label and shortcut.
        let edit = new Menu()
        edit.Append(StandardId.Paste).Click.Add(fun _ -> pasteSnippet ())
        edit.Append(StandardId.Copy).Click.Add(fun _ -> copySelected ())
        edit.Append(StandardId.Delete).Click.Add(fun _ -> removeSelected ())
        let menuBar = new MenuBar()
        menuBar.Append(edit, "&Edit") |> ignore
        this.MenuBar <- menuBar
        paste.Click.Add(fun _ -> pasteSnippet ())
        copy.Click.Add(fun _ -> copySelected ())
        remove.Click.Add(fun _ -> removeSelected ())
        // Copy and Remove only apply while a snippet is selected.
        let needsSelection = set [ copy.Id; remove.Id; StandardId.Copy; StandardId.Delete ]
        this.Bind(
            EventType.UpdateUi,
            fun _ (e: UpdateUIEvent) ->
                if needsSelection.Contains e.Id then
                    e.Enable(selected () |> Option.isSome)
        )
        // The list takes files and text dropped on it, and copies them, rather than moving them.
        let target = DropTarget(list, DataFormat.Files, DataFormat.Text)
        target.DragOver.Add(fun e -> e.Effect <- DragResult.Copy)
        target.Drop.Add(fun e ->
            from e.Files e.Text |> Option.iter add
            e.Effect <- DragResult.Copy)
        // Dragging the selected snippet out of the list gives it to wherever it's dropped.
        list.MouseMoved.Add(fun e ->
            match selected () with
            | Some index when e.Dragging() && e.LeftIsDown() && not dragging ->
                dragging <- true
                try
                    DragDrop.DoDragDrop(list, toContent snippets[index]) |> ignore
                finally
                    dragging <- false
            | _ -> e.Skip())
        panel.Sizer <-
            column {
                item label |> border (Direction.Left ||| Direction.Right ||| Direction.Top)
                item list |> grow
                row {
                    paste
                    copy
                    remove
                }
            }
        this.CreateStatusBar() |> ignore
        this.Center()

    member _.Snippets = snippets

    member _.Add snippet = add snippet

    member _.Copy() = copySelected ()

    member _.Paste() = pasteSnippet ()
