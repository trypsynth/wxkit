namespace WxKit.FSharp

open WxKit

/// <summary>
/// One thing a layout places, with how it grows, aligns, and the border around it: a window, a
/// sizer, a label and its field, or space. Make one with <c>item</c>, <c>section</c>,
/// <c>field</c>, <c>gap</c>, or <c>stretch</c>, and change it with <c>grow</c>, <c>fill</c>,
/// <c>center</c>, <c>alignRight</c>, <c>border</c>, or <c>noBorder</c>.
/// </summary>
[<Sealed>]
type LayoutItem internal (add: Sizer -> SizerFlags -> unit, flags: SizerFlags) =
    member internal _.Flags = flags

    member internal _.With(changed: SizerFlags) = LayoutItem(add, changed)

    member internal _.AddTo(sizer: Sizer) = add sizer flags

/// <summary>The functions that make and change a layout's items.</summary>
[<AutoOpen>]
module Layout =
    let private bordered = SizerFlags.Default.Border()

    /// <summary>A window, at the size it asks for, with the platform's usual border around it.</summary>
    let item (window: Window) =
        LayoutItem((fun sizer flags -> sizer.Add(window, flags)), bordered)

    /// <summary>A sizer inside another, such as a row of buttons in a column. Its own items' borders space it.</summary>
    let section (inner: Sizer) =
        LayoutItem((fun sizer flags -> sizer.Add(inner, flags)), SizerFlags.Default)

    /// <summary>
    /// A label and the field it names, in a form: the label centered beside the field, which fills
    /// its column. Create the label just before the field, so screen readers name the field by it.
    /// </summary>
    let field (label: Window) (control: Window) =
        LayoutItem(
            (fun sizer flags ->
                sizer.Add(label, SizerFlags.Default.CenterVertical().Border())
                sizer.Add(control, flags)),
            bordered.Expand()
        )

    /// <summary>Space of <paramref name="pixels"/> along the sizer.</summary>
    let gap (pixels: int) =
        LayoutItem((fun sizer _ -> sizer.AddSpacer pixels), SizerFlags.Default)

    /// <summary>Space that takes a share of the room left, which pushes what comes after it to the end, such as buttons to the right.</summary>
    let stretch =
        LayoutItem((fun sizer _ -> sizer.AddStretchSpacer 1), SizerFlags.Default)

/// <summary>
/// What the functions that change an item take: an item, or a window or a sizer, which become the
/// <c>item</c> or the <c>section</c> they would be on a line of their own. It is how
/// <c>row { ... } |> fill</c> and <c>list |> grow</c> work, and isn't for calling.
/// </summary>
[<Sealed; AbstractClass>]
type LayoutSource =
    static member Item(source: LayoutItem) = source

    static member Item(source: Window) = item source

    static member Item(source: Sizer) = section source

    static member Change(source: LayoutItem, change: SizerFlags -> SizerFlags) = source.With(change source.Flags)

/// <summary>The functions that change how a layout places an item, a window, or a sizer.</summary>
[<AutoOpen>]
module LayoutChanges =
    /// <summary>Takes a share of the spare room along the sizer, and fills the room across it, as a list does in a column.</summary>
    let inline grow (source: ^Source) =
        LayoutSource.Change(((^Source or LayoutSource): (static member Item: ^Source -> LayoutItem) source), (fun flags -> flags.Proportion(1).Expand()))

    /// <summary>Fills the room across the sizer, as a text box does in a column, without growing along it.</summary>
    let inline fill (source: ^Source) =
        LayoutSource.Change(((^Source or LayoutSource): (static member Item: ^Source -> LayoutItem) source), (fun flags -> flags.Expand()))

    /// <summary>Centers the item across the sizer.</summary>
    let inline center (source: ^Source) =
        LayoutSource.Change(((^Source or LayoutSource): (static member Item: ^Source -> LayoutItem) source), (fun flags -> flags.Center()))

    /// <summary>Puts the item at the right of a column.</summary>
    let inline alignRight (source: ^Source) =
        LayoutSource.Change(((^Source or LayoutSource): (static member Item: ^Source -> LayoutItem) source), (fun flags -> flags.Right()))

    /// <summary>Puts the platform's usual border on the given sides only, such as <c>Direction.Left ||| Direction.Right</c>.</summary>
    let inline border (direction: Direction) (source: ^Source) =
        LayoutSource.Change(((^Source or LayoutSource): (static member Item: ^Source -> LayoutItem) source), (fun flags -> flags.Border(direction)))

    /// <summary>Takes the border away.</summary>
    let inline noBorder (source: ^Source) =
        LayoutSource.Change(((^Source or LayoutSource): (static member Item: ^Source -> LayoutItem) source), (fun flags -> flags.Border(Direction.All, 0)))

/// <summary>
/// Builds a sizer from the items listed in it, one to a line. A window or a sizer on its own is an
/// <c>item</c> or a <c>section</c>, and <c>if</c> and <c>for</c> work as in a list.
/// </summary>
[<Sealed>]
type LayoutBuilder<'Sizer when 'Sizer :> Sizer> internal (make: unit -> 'Sizer) =
    member _.Yield(added: LayoutItem) = [ added ]

    member _.Yield(window: Window) = [ item window ]

    member _.Yield(inner: Sizer) = [ section inner ]

    member _.YieldFrom(items: LayoutItem seq) = List.ofSeq items

    member _.Combine(first: LayoutItem list, rest: LayoutItem list) = first @ rest

    member _.Delay(body: unit -> LayoutItem list) = body ()

    member _.Zero() : LayoutItem list = []

    member _.For(source: 'T seq, body: 'T -> LayoutItem list) = [ for value in source do yield! body value ]

    member _.Run(items: LayoutItem list) : 'Sizer =
        let sizer = make ()
        for added in items do
            added.AddTo sizer
        sizer

/// <summary>The layouts: <c>column</c>, <c>row</c>, and <c>form</c>.</summary>
[<AutoOpen>]
module Layouts =
    /// <summary>Items one above another, as a vertical BoxSizer places them.</summary>
    let column = LayoutBuilder(fun () -> new BoxSizer(Orientation.Vertical))

    /// <summary>Items side by side, as a horizontal BoxSizer places them.</summary>
    let row = LayoutBuilder(fun () -> new BoxSizer(Orientation.Horizontal))

    /// <summary>Labels and fields, each <c>field</c> a row of a two-column FlexGridSizer whose field column grows.</summary>
    let form =
        LayoutBuilder(fun () ->
            let grid = new FlexGridSizer(2)
            grid.AddGrowableColumn(1, 1)
            grid)
