// The Hello sample's counterpart in F#: a counter that keeps a history, with WxKit.FSharp's
// translations and layouts. It avoids printf and sprintf, whose formatting FSharp.Core does by
// reflection that Native AOT can't keep, and formats with String.Format, as gettext's
// translations are formatted.
open System
open WxKit
open WxKit.FSharp

[<EntryPoint>]
let main args =
    // With --smoke, the frame closes as soon as the event loop is running, so CI can check that a
    // published executable starts and ends cleanly.
    let smoke = args = [| "--smoke" |]
    Application.Run(fun () ->
        let frame = new Frame(null, tr "Hello from F#", size = Size(420, 320))
        let fileMenu = new Menu()
        fileMenu.Append(StandardId.Exit).Click.Add(fun _ -> frame.Close() |> ignore)
        let menuBar = new MenuBar()
        menuBar.Append(fileMenu, tr "&File") |> ignore
        frame.MenuBar <- menuBar
        // Each label is created just before the control it names, so a screen reader names the
        // control after it, and the tab order follows.
        let panel = new Panel(frame)
        let count = new StaticText(panel, String.Format(tr "Count: {0}", 0))
        let add = new Button(panel, tr "&Add One")
        let reset = new Button(panel, tr "&Reset")
        let historyLabel = new StaticText(panel, tr "&History:")
        let history = new ListBox(panel)
        let mutable total = 0
        let show () = count.Label <- String.Format(tr "Count: {0}", total)
        add.Click.Add(fun _ ->
            total <- total + 1
            show ()
            history.Append(String.Format(tr "Added one, to {0}", total)) |> ignore)
        reset.Click.Add(fun _ ->
            total <- 0
            show ()
            history.Clear())
        panel.Sizer <-
            column {
                count
                row {
                    add
                    reset
                }
                item historyLabel |> border (Direction.Left ||| Direction.Right ||| Direction.Top)
                item history |> grow
            }
        frame.Center()
        frame.Show() |> ignore
        add.SetFocus()
        if smoke then Application.Post(fun () -> frame.Close(force = true) |> ignore))
