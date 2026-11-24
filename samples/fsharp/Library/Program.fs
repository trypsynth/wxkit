// The Library sample in F#: books in a data view whose model, an object expression, gives the
// cells of the rows on screen as the view draws them, filtered as a search is typed, in the order
// chosen, with a check box column the user ticks. Screen readers read it as a list on Windows,
// macOS, and Linux. The books are records, and the order a union.
open WxKit
open Library

[<EntryPoint>]
let main args =
    // With --smoke, the sample searches, marks a book read, and removes it, then closes, so CI can
    // check that a published sample does and ends cleanly.
    let smoke = args = [| "--smoke" |]
    Application.Run(fun () ->
        let frame = new LibraryFrame()
        frame.Show() |> ignore
        if smoke then
            Application.Post(fun () ->
                frame.Exercise()
                frame.Close(force = true) |> ignore))
