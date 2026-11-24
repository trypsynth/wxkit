namespace Checklist

open System
open WxKit
open Checklist.Drawing

/// An item on the list. Ticking it replaces it with a copy that's done.
type ChecklistItem = { Text: string; IsDone: bool }

/// A checklist that draws itself, rather than using a list control, as a game board or a custom
/// list might. Up, Down, Home, and End move between items, Space ticks the current one, and the
/// mouse does both. Screen readers see it as a list, through SetAccessibleRows, and hear each item
/// as it becomes the current one, through SetAccessibleCurrentRow.
type ChecklistControl(parent: Window, texts: string seq) as this =
    inherit Panel(parent, style = WindowStyle.WantsChars)
    let mutable items = [| for text in texts -> { Text = text; IsDone = false } |]
    let mutable current = 0
    let changed = Event<unit>()
    let rowHeight () = this.CharHeight + 8

    // Tells screen readers what the control shows: a row for each item, whose text says whether it
    // is done, and which one is current. Every item is shown here, so all of them are given.
    let describe () =
        let width = this.ClientSize.Width
        let height = rowHeight ()
        let rows =
            items
            |> Array.mapi (fun index item ->
                let bounds = Rect(0, index * height, width, height)
                let label = item.Text + (if item.IsDone then ", done" else ", not done")
                AccessibleRow(index, bounds, [| AccessiblePart(label, bounds) |], IsSelected = (index = current)))
        this.SetAccessibleRows(rows, items.Length)
        this.SetAccessibleCurrentRow(if items.Length = 0 then -1 else current)

    let update () =
        this.Refresh()
        describe ()
        changed.Trigger()

    let moveTo index =
        if items.Length > 0 then
            let clamped = Math.Clamp(index, 0, items.Length - 1)
            if clamped <> current then
                current <- clamped
                update ()

    let toggle () =
        if items.Length > 0 then
            items <- items |> Array.updateAt current { items[current] with IsDone = not items[current].IsDone }
            update ()

    let onKeyDown (e: KeyEvent) =
        match e.KeyCode with
        | KeyCode.Up -> moveTo (current - 1)
        | KeyCode.Down -> moveTo (current + 1)
        | KeyCode.Home -> moveTo 0
        | KeyCode.End -> moveTo (items.Length - 1)
        | KeyCode.Space -> toggle ()
        // The control wants every key, so it passes Tab on itself, as a standard control does.
        | KeyCode.Tab ->
            let direction = if e.ShiftDown() then NavigationFlags.IsBackward else NavigationFlags.IsForward
            this.Navigate(direction ||| NavigationFlags.FromTab) |> ignore
        | _ -> e.Skip()

    let draw () =
        use dc = new PaintDC(this)
        let background = systemColor SystemColor.ListBox (rgb 255 255 255)
        let text = systemColor SystemColor.ListBoxText (rgb 0 0 0)
        let highlight = systemColor SystemColor.Highlight (rgb 0 120 215)
        let highlightText = systemColor SystemColor.HighlightText (rgb 255 255 255)
        use backgroundBrush = new Brush(background)
        dc.Background <- backgroundBrush
        dc.Clear()
        use font = this.Font
        dc.Font <- font
        let height = rowHeight ()
        let box = this.CharHeight - 2
        items
        |> Array.iteri (fun index item ->
            let top = index * height
            let isCurrent = index = current && this.HasFocus
            let fill = if isCurrent then highlight else background
            use rowBrush = new Brush(fill)
            use rowPen = new Pen(fill)
            dc.Brush <- rowBrush
            dc.Pen <- rowPen
            dc.DrawRectangle(Rect(0, top, this.ClientSize.Width, height))
            let ink = if isCurrent then highlightText else text
            use boxPen = new Pen(ink)
            use boxBrush = new Brush(if item.IsDone then ink else fill)
            dc.Pen <- boxPen
            dc.Brush <- boxBrush
            dc.DrawRectangle(Rect(6, top + (height - box) / 2, box, box))
            dc.TextForeground <- ink
            dc.DrawText(item.Text, 12 + box, top + 4))

    do
        // A window that draws itself has no name of its own, so it takes the label's, which macOS
        // needs given, and Windows also finds as the label before the control.
        match this.PreviousSibling with
        | :? StaticText as label -> this.SetAccessibleName label.LabelText
        | _ -> ()
        // The control paints all of itself, so wxWidgets need not erase it first.
        this.BackgroundStyle <- BackgroundStyle.Paint
        this.MinSize <- Size(240, rowHeight () * max items.Length 4)
        this.Paint.Add(fun _ -> draw ())
        this.KeyDown.Add onKeyDown
        this.LeftDown.Add(fun e ->
            this.SetFocus()
            moveTo (e.Position.Y / rowHeight ()))
        this.LeftDoubleClick.Add(fun _ -> toggle ())
        this.GotFocus.Add(fun _ -> this.Refresh())
        this.LostFocus.Add(fun _ -> this.Refresh())
        this.SizeChanged.Add(fun e ->
            this.Refresh()
            describe ()
            e.Skip())
        describe ()

    member _.Items: ChecklistItem list = List.ofArray items

    member _.Current = items[current]

    /// Raised when an item is ticked or the current item changes.
    member _.Changed = changed.Publish
