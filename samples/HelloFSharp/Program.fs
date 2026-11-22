// The Hello sample's counterpart in F#: a counter that keeps a history. It avoids printf and
// sprintf, whose formatting FSharp.Core does by reflection that Native AOT can't keep.
open WxKit

[<EntryPoint>]
let main args =
    // With --smoke, the frame closes as soon as the event loop is running, so CI can check that a
    // published executable starts and ends cleanly.
    let smoke = args = [| "--smoke" |]
    Application.Run(fun () ->
        let frame = new Frame(null, "Hello from F#", size = Size(420, 320))
        let fileMenu = new Menu()
        fileMenu.Append(StandardId.Exit).Click.Add(fun _ -> frame.Close() |> ignore)
        let menuBar = new MenuBar()
        menuBar.Append(fileMenu, "&File") |> ignore
        frame.MenuBar <- menuBar
        // Each label is created just before the control it names, so a screen reader names the
        // control after it, and the tab order follows.
        let panel = new Panel(frame)
        let count = new StaticText(panel, "Count: 0")
        let add = new Button(panel, "&Add One")
        let reset = new Button(panel, "&Reset")
        let historyLabel = new StaticText(panel, "&History:")
        let history = new ListBox(panel)
        let mutable total = 0
        let show () = count.Label <- "Count: " + string total
        add.Click.Add(fun _ ->
            total <- total + 1
            show ()
            history.Append("Added one, to " + string total) |> ignore)
        reset.Click.Add(fun _ ->
            total <- 0
            show ()
            history.Clear())
        let buttons = new BoxSizer(Orientation.Horizontal)
        buttons.Add(add, SizerFlags.Default.Border(Direction.Right))
        buttons.Add(reset)
        let sizer = new BoxSizer(Orientation.Vertical)
        sizer.Add(count, SizerFlags.Default.Border())
        sizer.Add(buttons, SizerFlags.Default.Border())
        sizer.Add(historyLabel, SizerFlags.Default.Border(Direction.Left ||| Direction.Right ||| Direction.Top))
        sizer.Add(history, SizerFlags(1).Expand().Border())
        panel.Sizer <- sizer
        frame.Center()
        frame.Show() |> ignore
        add.SetFocus()
        if smoke then Application.Post(fun () -> frame.Close(force = true) |> ignore))
