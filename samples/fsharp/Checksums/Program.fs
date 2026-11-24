// The Checksums sample in F#: work on another thread, with a progress dialog. The sample hashes a
// folder's files on a thread pool thread, so the window keeps responding, and the dialog, updated
// through Progress on the GUI thread, shows how far it is and can cancel it. Each file's hash is a
// Result, which holds why it couldn't be read when it couldn't.
open System
open WxKit
open Checksums

[<EntryPoint>]
let main args =
    // With --smoke, the sample hashes its own folder and closes, so CI can check that a published
    // sample does and ends cleanly.
    let smoke = args = [| "--smoke" |]
    Application.Run(fun () ->
        let frame = new ChecksumsFrame()
        frame.Show() |> ignore
        if smoke then
            Application.Post(fun () ->
                task {
                    do! frame.ComputeAsync AppContext.BaseDirectory
                    if frame.ResultCount = 0 then
                        failwith "No checksums were computed."
                    frame.Close(force = true) |> ignore
                }
                |> ignore))
