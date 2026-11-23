# Language support

WxKit is written in C# and designed for C# first. F# is supported too. Visual Basic isn't.

- C# gets the whole API, as [Managed API](managed-api.md) describes. The samples and docs are in C#.
- F# uses the C# API, and `WxKit.FSharp` adds translation functions and layouts that read as F#. It publishes with Native AOT. The HelloFSharp sample shows it, and CI tests WxKit.FSharp and publishes and runs the sample on every platform.
- Visual Basic isn't supported. It can't use the C#-only source generators that Native AOT relies on, such as System.Text.Json's and `LibraryImport`, so a Visual Basic application can't publish as one native executable, which is much of WxKit's point, and nothing in CI builds or tests it. 0.2.0 drops the rules below that are only for it, and the translation extractor's reading of it, as [Planned changes](planned-changes.md) lists.

## Rules for the public API

These rules keep the public `WxKit` namespace usable from F#, and cost C# little. The internals can use any C# feature.

- No C# 14 extension members, such as extension properties, which F# can't consume yet. Ordinary extension methods are fine.
- No public API only on a static abstract interface member, which F# consumes with warnings.
- Events are `EventHandler<T>`, with arguments that derive from `EventArgs`, as .NET's conventions ask and F#'s events expect.

Until 0.2.0, three more rules keep the API as Visual Basic could use it, so a release in 0.1.x doesn't break anyone who used it anyway:

- No `ref struct` types in public signatures without an alternative. Where a `ReadOnlySpan<T>` overload helps C#, there is also one that takes an array or a `string`.
- No `required` members, which Visual Basic can't set.
- No names that differ only in case, since Visual Basic isn't case sensitive.

## F#

Reference `WxKit.FSharp`, which brings `WxKit` with it, or start from `dotnet new wxkit -lang F#`:

```shell
dotnet add package WxKit.FSharp
```

```fsharp
open System
open WxKit
open WxKit.FSharp

Application.Run(fun () ->
    let frame = new Frame(null, tr "Hello")
    let panel = new Panel(frame)
    let nameLabel = new StaticText(panel, tr "&Name:")
    let name = new TextCtrl(panel)
    let greet = new Button(panel, tr "&Greet")
    let notesLabel = new StaticText(panel, tr "N&otes:")
    let notes = new TextCtrl(panel, style = TextCtrlStyle.MultiLine)
    greet.Click.Add(fun _ -> MessageBox.Show(String.Format(tr "Hello, {0}!", name.Value), tr "Greeting") |> ignore)
    panel.Sizer <-
        column {
            form { field nameLabel name }
            item notesLabel |> border (Direction.Left ||| Direction.Right ||| Direction.Top)
            item notes |> grow
            item greet |> alignRight
        }
    frame.Show() |> ignore)
```

WxKit.FSharp has two parts:

- `tr`, `trn`, `trp`, and `trpn` translate text, as `_`, `_n`, `_p`, and `_pn` do in C#, whose `_` F# can't call. The string extractor finds them with their strings, as in `tr "&Open"` or `trn "{0} file" "{0} files" count`. Format with `String.Format`, since gettext's translations use .NET's placeholders, not printf's.
- `column`, `row`, and `form` build a sizer from the items listed in them, one to a line. A window or a sizer on its own gets the platform's usual border. `item` makes a window an item to change, with `grow`, `fill`, `center`, `alignRight`, `border`, and `noBorder`, and `gap`, `stretch`, `section`, and a form's `field` add the rest. `if` and `for` work in them as in a list.

Events appear as F# events, so `Add`, `Subscribe`, and the `Observable` functions work on them. Avoid `printf` and `sprintf` in an application that publishes with Native AOT: FSharp.Core formats them by reflection, which gives trim warnings. F# files are indented with four spaces, since the compiler rejects tabs.
