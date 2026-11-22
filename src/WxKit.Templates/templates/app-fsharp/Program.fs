open System
open WxKit
open WxKit.FSharp

// Text a user reads goes through tr, so it can be translated. Translating an application:
// https://github.com/trypsynth/wxkit/blob/master/docs/translating.md
[<EntryPoint>]
let main _ =
    Translations.Use "WxKitApp" |> ignore
    Application.Run(fun () ->
        let frame = new Frame(null, tr "WxKitApp", size = Size(480, 320))
        let panel = new Panel(frame)
        // Each label comes just before the control it names, so screen readers read the control
        // by it on every platform. The & marks the letter that Alt and that letter go to.
        let nameLabel = new StaticText(panel, tr "&Name:")
        let name = new TextCtrl(panel, style = TextCtrlStyle.ProcessEnter)
        let greet = new Button(panel, tr "&Greet")
        let greeting = new StaticText(panel, "")
        // Speaks news that no control has the focus for, such as the greeting, to screen readers.
        let announcer = new LiveRegion(panel)
        let sayHello () =
            let text = name.Value.Trim()
            let message = if text.Length = 0 then tr "Type your name first." else String.Format(tr "Hello, {0}!", text)
            greeting.Label <- message
            announcer.Announce message |> ignore
        greet.Click.Add(fun _ -> sayHello ())
        name.TextEnter.Add(fun _ -> sayHello ())
        panel.Sizer <-
            column {
                item nameLabel |> border (Direction.Left ||| Direction.Right ||| Direction.Top)
                item name |> fill
                greet
                item greeting |> fill
            }
        frame.Center()
        frame.Show() |> ignore
        name.SetFocus())
