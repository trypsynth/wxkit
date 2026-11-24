namespace Checklist

open WxKit
open Checklist.Drawing

/// A strip of fields that draws itself, as a status bar does. Screen readers read each field as
/// text, through SetAccessibleParts, as they read a status bar's fields. It is a plain control,
/// not a panel, since a panel takes the focus from the keyboard when it has no children whatever
/// it is told, and a status strip is read, not typed in, so it is no Tab stop.
type StatusStrip(parent: Window) as this =
    inherit Control(parent)
    let mutable fields: string list = []

    // Each field takes an equal share of the width.
    let bounds index =
        let size = this.ClientSize
        let width = if fields.IsEmpty then 0 else size.Width / fields.Length
        Rect(index * width, 0, width, size.Height)

    let describe () =
        this.SetAccessibleParts(fields |> List.mapi (fun index field -> AccessiblePart(field, bounds index)))

    let draw () =
        use dc = new PaintDC(this)
        use background = new Brush(systemColor SystemColor.ButtonFace (rgb 240 240 240))
        dc.Background <- background
        dc.Clear()
        use font = this.Font
        dc.Font <- font
        dc.TextForeground <- systemColor SystemColor.ButtonText (rgb 0 0 0)
        use divider = new Pen(systemColor SystemColor.ButtonShadow (rgb 160 160 160))
        dc.Pen <- divider
        fields
        |> List.iteri (fun index field ->
            let area = bounds index
            dc.DrawText(field, area.X + 6, area.Y + 5)
            if index > 0 then
                dc.DrawLine(area.X, area.Y + 2, area.X, area.Y + area.Height - 2))

    do
        this.BackgroundStyle <- BackgroundStyle.Paint
        this.DisableFocusFromKeyboard()
        this.MinSize <- Size(-1, this.CharHeight + 10)
        this.Paint.Add(fun _ -> draw ())
        this.SizeChanged.Add(fun e ->
            this.Refresh()
            describe ()
            e.Skip())

    /// The fields shown, from left to right.
    member _.Fields
        with get () = fields
        and set value =
            fields <- value
            this.Refresh()
            describe ()
