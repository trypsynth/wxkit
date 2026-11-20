# Open questions

This document lists the design decisions made so far, and the ones still open. Each open question has a recommendation, which the other design documents assume until it is answered.

## Decided

- Bindings are generated from the Doxygen XML of the wxWidgets interface headers, with tweak files in C#, after a small slice is hand-written first. See [Generator](generator.md).
- WxKit tracks the wxWidgets 3.3 series, starting from the `v3.3.4` tag, and moves to 3.4.0 when it comes out.
- Windows comes first, for the hand-written slice and the generator. macOS and Linux follow together.
- Types drop the `wx` prefix and use .NET spellings, such as `Color` and `Center`. Names that collide with common .NET types, such as `Timer` and `Font`, are kept. See the naming section of [Managed API](managed-api.md).
- An exception that an event handler throws raises `Application.UnhandledException`. If nothing marks it handled, `Application.Run` rethrows it.
- wxWidgets assertions stay on in debug builds of the native library, where they raise `Application.AssertFailed`, and are compiled out of release builds. See [Native layer](native-layer.md).
- The raw interop layer, `WxKit.Interop.WxKitNative`, is public, as an escape hatch for anything the managed API does not wrap yet.
- XML documentation comments are generated from the wxWidgets brief descriptions, and each type links to its wxWidgets reference page.
- XRC is the last optional library to be bound.
- `Timer` keeps its name. WxKit's MSBuild targets alias it with `global using Timer = WxKit.Timer;` when implicit usings are on, so it does not clash with `System.Threading.Timer`, and `WxKitTimerAlias=false` turns that off.
- Tests use xUnit v3 with a custom entry point that runs them on the main thread, with a runner of WxKit's own as the fallback. See [Testing](testing.md).
- Translations use gettext through wxWidgets' own `wxTranslations`, so an application's text and wxWidgets' follow one setting, with `Translations` and the shortcuts `_`, `_n`, `_p`, and `_pn`. Catalogs are compiled with GNU gettext's `msgfmt` on each build and embedded in the application, with wxWidgets' catalogs for the languages it has, which the WxKit package carries one copy of. Fuzzy entries are left out, as gettext leaves them out. WxKit's own extractor reads C#, Visual Basic, and F#, which neither `xgettext` nor GetText.NET.Extractor reads all of. See [Translating an application](../translating.md).
- Names follow .NET conventions. Package IDs and namespaces are Pascal case and dotted: `WxKit` for the managed library, and `WxKit.Native.*` packages for native files. The GitHub repository is `trypsynth/wxkit`, and the native library is `wxkitnative`, because on Windows a native `wxkit.dll` would have the same file name as the managed `WxKit.dll`.

## Open

### Order of the optional libraries

Beyond the core libraries, wxWidgets has AUI docking, the styled text control, the property grid, the web view, rich text, the ribbon, OpenGL, the media control, and HTML help. The styled text control is bound, and an application that doesn't use it doesn't link it. Recommendation: AUI next, since tool windows use it most, then the web view and the property grid.

### Custom accessibility on Windows

wxWidgets lets a custom-drawn control describe itself to screen readers on Windows through `wxAccessible`. This needs a director with many virtual methods, and only works on Windows. Recommendation: include it right after virtual list controls, since custom controls are where accessibility usually breaks.

### Package split

The first static build of the base and core libraries for `win-x64` is about 150 megabytes, against nuget.org's limit of 250 megabytes per package, so one package per operating system will not fit. Recommendation: one managed package and one native package per runtime, such as `WxKit.Native.win-x64`, after checking whether the libraries can be made smaller. See [Building and packaging](building-and-packaging.md).

### An F# helper package

Recommendation: decide after the C# API is stable, based on what the F# sample shows is awkward.

### macOS app bundles

A published executable is not an app bundle. Should WxKit provide MSBuild targets that make one, with an `Info.plist` and icon, or leave that to the application?

### Versioning

Recommendation: start at `0.1.0-preview.1`, and keep WxKit's version independent of the wxWidgets version it bundles. The README and package description name the bundled wxWidgets version.
