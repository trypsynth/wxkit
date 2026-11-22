# Language support

WxKit is written in C# and designed for C# first. F# is supported too, and Visual Basic should work, but isn't tested.

- C# gets the whole API, as [Managed API](managed-api.md) describes. The samples and docs are in C#.
- F# uses the C# API directly, and publishes with Native AOT. The HelloFSharp sample shows it, and CI publishes and runs it on every platform.
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

```fsharp
open WxKit

Application.Run(fun () ->
    let frame = new Frame(null, "Hello")
    let button = new Button(frame, "&Greet")
    button.Click.Add(fun _ -> MessageBox.Show("Hello!", "Greeting") |> ignore)
    frame.Show() |> ignore)
```

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
