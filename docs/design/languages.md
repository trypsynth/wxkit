# Language support

WxKit is written in C# and designed for C# first. F# is supported too, and Visual Basic should work, but isn't tested.

- C# gets the whole API, as [Managed API](managed-api.md) describes. The samples and docs are in C#.
- F# uses the C# API, and `WxKit.FSharp` adds translation functions and layouts that read as F#. It publishes with Native AOT. The HelloFSharp sample shows it, and CI tests WxKit.FSharp and publishes and runs the sample on every platform.
- Visual Basic can use the API within the rules below, but nothing in CI builds Visual Basic, and it can't use the C#-only source generators that Native AOT relies on, such as System.Text.Json's and `LibraryImport`. The translation extractor reads it.

## Rules for the public API

These rules keep the public `WxKit` namespace usable from F# and Visual Basic, and cost C# little. The internals can use any C# feature.

- No `ref struct` types in public signatures without an alternative. Where a `ReadOnlySpan<T>` overload helps C#, there is also one that takes an array or a `string`.
- No `required` members, which Visual Basic can't set.
- No C# 14 extension members, such as extension properties, which neither language can consume yet. Ordinary extension methods are fine.
- No public API only on a static abstract interface member, which F# consumes with warnings and Visual Basic can't call.
- Events are `EventHandler<T>`, with arguments that derive from `EventArgs`, which Visual Basic's `WithEvents` and `Handles` depend on.
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

## Visual Basic

```vb
Imports WxKit

Module Program
	Private WithEvents greet As Button

	Sub Main()
		Application.Run(Sub()
			Dim frame As New Frame(Nothing, "Hello")
			greet = New Button(frame, "&Greet")
			frame.Show()
		End Sub)
	End Sub

	Private Sub OnGreet(sender As Object, e As CommandEvent) Handles greet.Click
		MessageBox.Show("Hello!", "Greeting")
	End Sub
End Module
```

`_` alone isn't a valid name in Visual Basic, so it translates text with `Translations.Translate` rather than `_`, as [Translating an application](../translating.md) describes.
