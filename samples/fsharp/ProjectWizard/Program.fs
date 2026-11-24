// The ProjectWizard sample in F#: a wizard that plans a new project, a page at a time, which skips a
// page that doesn't apply and keeps the user on a page until it's filled in. The plan is a record,
// whose kind is a union that holds a desktop application's options. The window shows the last plan.
open WxKit
open WxKit.FSharp
open ProjectWizard

[<EntryPoint>]
let main args =
    // With --smoke, the sample fills the wizard's pages in, checks which follow which, opens it, and
    // cancels it, then closes, so CI can check that a published sample does and ends cleanly.
    let smoke = args = [| "--smoke" |]
    Application.Run(fun () ->
        let frame = new Frame(null, "Project Wizard", size = Size(480, 360))
        let panel = new Panel(frame)
        let start = new Button(panel, "&New Project...")
        let planLabel = new StaticText(panel, "&Last plan:")
        let plan = new TextCtrl(panel, "None yet", style = (TextCtrlStyle.MultiLine ||| TextCtrlStyle.ReadOnly))
        start.Click.Add(fun _ ->
            use wizard = new NewProjectWizard(frame)
            wizard.Run() |> Option.iter (ProjectPlan.describe >> plan.ChangeValue))
        panel.Sizer <-
            column {
                start
                item planLabel |> border (Direction.Left ||| Direction.Right ||| Direction.Top)
                item plan |> grow
            }
        frame.Center()
        frame.Show() |> ignore
        start.SetFocus()
        if smoke then
            Application.Post(fun () ->
                do
                    use wizard = new NewProjectWizard(frame)
                    wizard.Exercise()
                    Application.Post(fun () -> wizard.EndModal StandardId.Cancel)
                    if wizard.Run().IsSome then
                        failwith "The canceled wizard gave a plan."
                frame.Close(force = true) |> ignore))
