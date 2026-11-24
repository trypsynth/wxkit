// The Scrapbook sample in F#: the clipboard and drag and drop. Snippets of text and lists of files,
// a union, pasted or dropped into a list, and copied or dragged back out.
open WxKit
open Scrapbook
open Scrapbook.Snippet

[<EntryPoint>]
let main args =
    // With --smoke, the sample adds a snippet, copies it to the clipboard, and pastes it back, then
    // closes, so CI can check that a published sample does and ends cleanly.
    let smoke = args = [| "--smoke" |]
    Application.Run(fun () ->
        let frame = new ScrapbookFrame()
        frame.Show() |> ignore
        if smoke then
            Application.Post(fun () ->
                frame.Add(Text "Copied by the smoke test")
                frame.Copy()
                frame.Paste()
                match frame.Snippets with
                | [ first; second ] when first = second -> ()
                | _ -> failwith "The snippet didn't come back from the clipboard."
                frame.Close(force = true) |> ignore))
