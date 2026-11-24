// The Checklist sample in F#: controls that draw themselves, described to screen readers on
// Windows, macOS, and Linux. A checklist, which they see as a list whose current item they
// announce, and a status strip, whose fields they read as text.
open WxKit
open WxKit.FSharp
open Checklist

[<EntryPoint>]
let main args =
    // With --smoke, the window closes as soon as the event loop runs, so CI can check that a
    // published sample starts and ends cleanly.
    let smoke = args = [| "--smoke" |]
    Application.Run(fun () ->
        let frame = new Frame(null, "Checklist", size = Size(360, 340))
        let panel = new Panel(frame)
        // The label comes just before the control it names, so screen readers name the control after it.
        let label = new StaticText(panel, "&Shopping list:")
        let list = new ChecklistControl(panel, [ "Milk"; "Bread"; "Eggs"; "Apples"; "Coffee"; "Rice" ])
        let status = new StatusStrip(panel)
        let showStatus () =
            let items = list.Items
            let ticked = items |> List.filter _.IsDone |> List.length
            status.Fields <- [ $"{ticked} of {items.Length} done"; $"Current: {list.Current.Text}" ]
        list.Changed.Add showStatus
        showStatus ()
        panel.Sizer <-
            column {
                item label |> border (Direction.Left ||| Direction.Right ||| Direction.Top)
                item list |> grow
                item status |> fill |> noBorder
            }
        frame.Center()
        frame.Show() |> ignore
        list.SetFocus()
        if smoke then Application.Post(fun () -> frame.Close(force = true) |> ignore))
