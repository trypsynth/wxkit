namespace Settings

open System
open WxKit
open WxKit.FSharp

/// The settings, a page each in a notebook, with OK and Cancel. OK runs the validators on every
/// page first, and the dialog stays open while one fails. Edited gives the settings as changed.
type SettingsDialog(parent: Window, settings: AppSettings) as this =
    inherit Dialog(parent, tr "Settings", style = (DialogStyle.Default ||| DialogStyle.ResizeBorder))

    // Keeps the dialog open, and says why, until the name is filled in.
    let nameRequired () =
        { new CustomValidator() with
            override _.OnValidate(window, parent) =
                match window with
                | :? TextCtrl as text when text.Value.Trim() <> "" -> true
                | _ ->
                    let icon = MessageDialogStyle.Ok ||| MessageDialogStyle.IconWarning
                    MessageBox.Show(tr "Enter a name, which the application shows as yours.", tr "Settings", icon, parent) |> ignore
                    window.SetFocus()
                    false }

    do
        if Languages.isRightToLeft () then
            this.LayoutDirection <- LayoutDirection.RightToLeft

    // The notebook comes before the buttons, so the Tab key reaches the pages first.
    let pages = new Notebook(this)
    // The general page is a form, each label made just before its field, so a screen reader names
    // the field after it.
    let general = new Panel(pages)
    let nameLabel = new StaticText(general, tr "&Name:")
    let name = new TextCtrl(general, settings.Name, Validator = nameRequired ())
    let themeLabel = new StaticText(general, tr "&Theme:")
    let theme =
        new Choice(general, Theme.all |> List.map Theme.name, Selection = List.findIndex ((=) settings.Theme) Theme.all)
    let accentLabel = new StaticText(general, tr "&Accent color:")
    let accent = new ColorPickerCtrl(general, settings.Accent)
    let fontLabel = new StaticText(general, tr "&Font:")
    let font =
        use chosen = settings.Font |> Option.map (fun description -> new Font(description)) |> Option.toObj
        new FontPickerCtrl(general, chosen)
    let recentLabel = new StaticText(general, tr "&Recent files to remember:")
    let recentFiles = new SpinCtrl(general, min = 0, max = 50, initial = settings.RecentFiles)
    // The user's preferred language comes first, and then each one there is a catalog for, named in
    // itself. None stands for the user's preferred language.
    let languages = (None, tr "The system's language") :: [ for code, name in Languages.available () -> Some code, name ]
    let languageLabel = new StaticText(general, tr "&Language:")
    let language =
        let chosen = languages |> List.tryFindIndex (fun (code, _) -> code = settings.Language)
        new Choice(general, List.map snd languages, Selection = Option.defaultValue 0 chosen)
    let startWithSystem = new CheckBox(general, tr "&Start when I sign in", IsChecked = settings.StartWithSystem)
    // The folders page has a picker with a text box, which can be typed in as well as browsed.
    let folders = new Panel(pages)
    let documentsLabel = new StaticText(folders, tr "&Documents folder:")
    let documents =
        let style = DirPickerCtrlStyle.UseTextCtrl ||| DirPickerCtrlStyle.DirectoryMustExist
        new DirPickerCtrl(folders, settings.DocumentsFolder, tr "Choose the documents folder", style = style)
    // The notifications page has more check boxes than fit, so it scrolls, by rows of 10 pixels.
    let notifications = new ScrolledWindow(pages)
    let group = new StaticBoxSizer(Orientation.Vertical, notifications, tr "Notify me when")
    let events =
        AppSettings.events ()
        |> List.mapi (fun index event -> new CheckBox(group.StaticBox, event, IsChecked = settings.Notifications.Contains index))

    do
        general.Sizer <-
            column {
                form {
                    field nameLabel name
                    field themeLabel theme
                    field accentLabel accent
                    field fontLabel font
                    field recentLabel recentFiles
                    field languageLabel language
                }
                |> section
                |> fill
                startWithSystem
            }
        pages.AddPage(general, tr "General") |> ignore
        folders.Sizer <-
            column {
                documentsLabel
                item documents |> fill
            }
        pages.AddPage(folders, tr "Folders") |> ignore
        notifications.SetScrollRate(0, 10)
        for box in events do
            group.Add(box, SizerFlags.Default.Border(Direction.Top ||| Direction.Bottom)) |> ignore
        notifications.Sizer <- column { section group |> grow }
        pages.AddPage(notifications, tr "Notifications") |> ignore
        // OK and Cancel, in the order and with the labels the platform uses, which wxWidgets
        // translates itself.
        let buttons = this.CreateStdDialogButtonSizer(DialogButtons.Ok ||| DialogButtons.Cancel)
        this.Sizer <-
            column {
                item pages |> grow
                section buttons |> fill |> border (Direction.Left ||| Direction.Right ||| Direction.Bottom)
            }
        this.Size <- Size(560, 480)
        name.SetFocus()

    /// The settings as the dialog has them, for when it's closed with OK.
    member _.Edited =
        use chosen = font.SelectedFont
        { settings with
            Name = name.Value.Trim()
            Theme = Theme.all[max theme.Selection 0]
            Accent = accent.Color |> Option.ofNullable |> Option.defaultValue settings.Accent
            Font = chosen |> Option.ofObj |> Option.map _.NativeFontInfoDescription
            RecentFiles = recentFiles.Value
            Language = fst languages[max language.Selection 0]
            StartWithSystem = startWithSystem.IsChecked
            DocumentsFolder = documents.Path
            Notifications = events |> List.indexed |> List.filter (snd >> _.IsChecked) |> List.map fst |> Set.ofList }
