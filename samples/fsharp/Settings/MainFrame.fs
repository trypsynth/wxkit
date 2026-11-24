/// The sample's window, which lists the settings and opens the dialog that changes them.
module Settings.MainFrame

open System
open System.IO
open WxKit
open WxKit.FSharp

let private describeFont (description: string) =
    use font = new Font(description)
    String.Format(trn "{0}, {1} point" "{0}, {1} points" (int64 font.PointSize), font.FaceName, font.PointSize)

// The settings as lines of text, in the current language.
let private summary settings =
    let events = AppSettings.events ()
    let notifications =
        match [ for index in settings.Notifications -> events[index] ] with
        | [] -> tr "none"
        | names -> String.Join(", ", names)
    let font = settings.Font |> Option.map describeFont |> Option.defaultValue (tr "the default")
    let language = settings.Language |> Option.map Languages.nativeName |> Option.defaultValue (tr "the system's")
    let accent = settings.Accent
    String.Join(
        '\n',
        [ String.Format(tr "Name: {0}", settings.Name)
          String.Format(tr "Theme: {0}", Theme.name settings.Theme)
          String.Format(tr "Accent color: red {0}, green {1}, blue {2}", accent.R, accent.G, accent.B)
          String.Format(tr "Font: {0}", font)
          String.Format(tr "Recent files remembered: {0}", settings.RecentFiles)
          String.Format(tr "Documents folder: {0}", settings.DocumentsFolder)
          String.Format(tr "Language: {0}", language)
          String.Format(tr "Start when you sign in: {0}", (if settings.StartWithSystem then tr "yes" else tr "no"))
          String.Format(tr "Notify when: {0}", notifications) ]
    )

/// Shows the window for the settings. A window's text is translated when it's made, so a new
/// language takes a new window, which shows where the old one was.
let rec show (settings: AppSettings) (position: Point option) =
    let frame = new Frame(null, tr "Settings sample", size = Size(520, 420))
    if Languages.isRightToLeft () then
        frame.LayoutDirection <- LayoutDirection.RightToLeft
    let current = ref settings
    // The settings are read-only text, which a screen reader can read line by line.
    let panel = new Panel(frame)
    let label = new StaticText(panel, tr "&Current settings:")
    let shown = new TextCtrl(panel, style = (TextCtrlStyle.MultiLine ||| TextCtrlStyle.ReadOnly))
    let change = new Button(panel, tr "&Change settings...")
    let showSummary () = shown.ChangeValue(summary current.Value)
    let save settings =
        try
            AppSettings.save AppSettings.defaultPath settings
        with
        | :? IOException
        | :? UnauthorizedAccessException as error ->
            let icon = MessageDialogStyle.Ok ||| MessageDialogStyle.IconError
            MessageBox.Show(String.Format(tr "The settings could not be saved. {0}", error.Message), tr "Settings sample", icon, frame) |> ignore
    let edit () =
        let edited =
            use dialog = new SettingsDialog(frame, current.Value)
            if dialog.ShowModal() = StandardId.Ok then Some dialog.Edited else None
        match edited with
        | None -> ()
        | Some edited ->
            save edited
            let languageChanged = edited.Language <> current.Value.Language
            current.Value <- edited
            if languageChanged then
                Languages.select edited.Language
                show edited (Some frame.Position) |> ignore
                frame.Close() |> ignore
            else
                showSummary ()
    // On macOS, Preferences goes in the application menu, where Mac applications have it, and Exit
    // becomes Quit there. Exit's label is wxWidgets', which it translates itself.
    let fileMenu = new Menu()
    fileMenu.Append(StandardId.Exit).Click.Add(fun _ -> frame.Close() |> ignore)
    let editMenu = new Menu()
    // TRANSLATORS: Keep the tab and the shortcut after it, which the menu shows and the keyboard uses.
    editMenu.Append(StandardId.Preferences, tr "&Settings...\tCtrl+,").Click.Add(fun _ -> edit ())
    let menuBar = new MenuBar()
    menuBar.Append(fileMenu, tr "&File") |> ignore
    menuBar.Append(editMenu, tr "&Edit") |> ignore
    frame.MenuBar <- menuBar
    change.Click.Add(fun _ -> edit ())
    panel.Sizer <-
        column {
            item label |> border (Direction.Left ||| Direction.Right ||| Direction.Top)
            item shown |> grow
            item change |> border (Direction.Left ||| Direction.Right ||| Direction.Bottom)
        }
    showSummary ()
    match position with
    | Some position -> frame.Position <- position
    | None -> frame.Center()
    frame.Show() |> ignore
    frame
