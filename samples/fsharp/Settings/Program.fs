// The Settings sample in F#: a settings dialog, as most applications have, with a notebook of
// pages, a form, pickers for a color, a font, a folder, and a language, a page that scrolls, and OK
// and Cancel, with a validator that keeps the dialog open until the name is filled in. The settings
// are a record, saved to a file in the user's application data. The sample is translated into
// German, from po/de.po, and follows the user's language, or the one they pick.
open WxKit
open Settings

[<EntryPoint>]
let main args =
    // With --smoke, the sample opens the dialog and cancels it as soon as the event loop runs, then
    // closes, so CI can check that a published sample makes every page and ends cleanly.
    let smoke = args = [| "--smoke" |]
    Application.Run(fun () ->
        let settings = AppSettings.load AppSettings.defaultPath
        // Numbers and dates in wxWidgets' controls follow the user's locale, and the text the
        // language, both before any window is made.
        UILocale.UseDefault() |> ignore
        Languages.select settings.Language
        let frame = MainFrame.show settings None
        if smoke then
            Application.Post(fun () ->
                do
                    use dialog = new SettingsDialog(frame, settings)
                    Application.Post(fun () -> dialog.EndModal StandardId.Cancel)
                    dialog.ShowModal() |> ignore
                frame.Close(force = true) |> ignore))
