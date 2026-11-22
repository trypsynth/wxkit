module WxKit.FSharp.Tests.LayoutTests

open WxKit
open WxKit.FSharp
open Xunit

// A panel the size given, laid out by its sizer.
let private laidOut (frame: Frame) (panel: Panel) =
    frame.ClientSize <- Size(300, 400)
    panel.Size <- Size(300, 400)
    panel.Layout() |> ignore

let private right (window: Window) = window.Position.X + window.Size.Width

let private bottom (window: Window) = window.Position.Y + window.Size.Height

[<Fact>]
let ``stacks a column and grows what grows`` () =
    Gui.withFrame (fun frame ->
        let panel = new Panel(frame)
        let first = new Button(panel, "&First")
        let second = new Button(panel, "&Second")
        let third = new Button(panel, "&Third")
        let list = new ListBox(panel)
        let sizer =
            column {
                first
                row {
                    second
                    third
                }
                item list |> grow
            }
        panel.Sizer <- sizer
        laidOut frame panel
        Assert.Equal(Orientation.Vertical, sizer.Orientation)
        Assert.True(first.Position.Y < second.Position.Y)
        Assert.Equal(second.Position.Y, third.Position.Y)
        Assert.True(right second <= third.Position.X)
        Assert.True(bottom second <= list.Position.Y)
        // The list takes the room left, and the column's width, less the borders.
        Assert.True(bottom list > 350, string (bottom list))
        Assert.True(list.Size.Width > 270, string list.Size.Width))

[<Fact>]
let ``aligns, fills, and spaces items`` () =
    Gui.withFrame (fun frame ->
        let panel = new Panel(frame)
        let text = new TextCtrl(panel)
        let ok = new Button(panel, "OK")
        let flush = new Button(panel, "&Flush")
        panel.Sizer <-
            column {
                item text |> fill
                gap 20
                item ok |> alignRight
                item flush |> noBorder
            }
        laidOut frame panel
        Assert.True(text.Size.Width > 270, string text.Size.Width)
        Assert.True(ok.Position.Y >= bottom text + 20)
        Assert.True(right ok > 280, string (right ok))
        Assert.Equal(0, flush.Position.X))

[<Fact>]
let ``lays out a form of labels and fields`` () =
    Gui.withFrame (fun frame ->
        let panel = new Panel(frame)
        let nameLabel = new StaticText(panel, "&Name:")
        let name = new TextCtrl(panel)
        let emailLabel = new StaticText(panel, "&Email:")
        let email = new TextCtrl(panel)
        panel.Sizer <-
            form {
                field nameLabel name
                field emailLabel email
            }
        laidOut frame panel
        Assert.True(right nameLabel <= name.Position.X)
        Assert.Equal(name.Position.X, email.Position.X)
        Assert.True(email.Position.Y >= bottom name)
        // The field column takes the width the labels leave.
        Assert.True(right name > 280, string (right name)))

[<Fact>]
let ``takes items from loops and conditions`` () =
    Gui.withFrame (fun frame ->
        let panel = new Panel(frame)
        let buttons = [ for label in [ "&One"; "&Two"; "T&hree" ] -> new Button(panel, label) ]
        let extra = new Button(panel, "&Extra")
        let showExtra = false
        let before = extra.Position
        panel.Sizer <-
            row {
                for button in buttons do
                    button
                if showExtra then
                    extra
                stretch
            }
        laidOut frame panel
        Assert.True(right buttons[0] <= buttons[1].Position.X)
        Assert.True(right buttons[1] <= buttons[2].Position.X)
        // The button left out stays where it was made.
        Assert.Equal(before, extra.Position))
