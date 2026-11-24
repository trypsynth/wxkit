// The Editor sample in F#: a text editor, with menus with the platform's standard labels and
// shortcuts, files opened and saved without blocking the window, find and replace, printing, and a
// prompt before unsaved changes are lost. A file named on the command line opens.
open WxKit
open Editor

[<EntryPoint>]
let main args =
    Application.Run(fun () ->
        let frame = new EditorFrame()
        frame.Show() |> ignore
        match args with
        // With --smoke, the editor makes a print preview, waits for it to lay out and draw a page,
        // and closes, so CI can check that a published editor starts, prints, and ends cleanly.
        | [| "--smoke" |] ->
            Application.Post(fun () ->
                task {
                    do! frame.PreviewAndCloseAsync()
                    frame.Close(force = true) |> ignore
                }
                |> ignore)
        | [| path |] -> frame.OpenAsync path |> ignore
        | _ -> ())
