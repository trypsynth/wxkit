# WxKit design overview

WxKit is a set of .NET bindings to [wxWidgets](https://wxwidgets.org), the C++ library for native desktop user interfaces. This document covers what WxKit is for, how its layers fit together, and what is left to build. The other documents in this folder cover each part in detail.

WxKit 0.1 is on nuget.org, for Windows, Linux, and macOS. The questions still open are in [Open questions](open-questions.md), and the changes that wait for 0.2.0 because they need code changes are in [Planned changes](planned-changes.md).

## Goals

- An API that reads like modern .NET: properties, standard .NET events, enums instead of integer flags, and nullable annotations throughout.
- Native AOT. A published application is one executable with wxWidgets linked into it.
- Broad coverage of the GUI surface a desktop application uses, which a generator makes possible.
- Native controls, so screen readers and other assistive technology work with them. WxKit's samples and defaults model accessible practice.
- C# first, with F# supported. See [Language support](languages.md).

## Non-goals

- The non-GUI parts of wxWidgets that .NET does better, such as `wxString`, `wxDateTime`, `wxFileName`, `wxThread`, `wxSocket`, and the wx containers. WxKit maps these to .NET types at the boundary.
- Mobile platforms, which wxWidgets has no supported port for.
- A visual designer.
- Source compatibility with wxPython, wxRuby, or wxDragon. WxKit borrows their ideas, not their API shapes.

## Platforms

- Windows, through wxMSW, on `win-x64` and `win-arm64`. 32-bit Windows is left out, as Windows 11 has no 32-bit edition. The build script can build `win-x86`, so it would take a package and a CI job.
- macOS, through wxOSX Cocoa, on `osx-arm64` and `osx-x64`.
- Linux, through wxGTK 3, on `linux-x64` and `linux-arm64`.

## Architecture

WxKit has four layers, each of which talks only to the one below it:

1. wxWidgets, a git submodule under `native/wxWidgets`, pinned to a release tag and built with CMake as static libraries.
2. The native shim, under `native/shim`: C++ that exposes wxWidgets as flat C functions with a `wxk_` prefix, since .NET can't call C++. It is mostly generated, and builds with wxWidgets into one library, `wxkitnative`. See [Native layer](native-layer.md).
3. The interop layer, `WxKit.Interop`, which declares each shim function with `LibraryImport`. It is generated.
4. The managed API, `WxKit`, the classes applications use. Each is a generated partial class, with a hand-written partial where it needs help. See [Managed API](managed-api.md).

A call goes down the layers: `button.Label = "Save"` pins the string and calls `WxKitNative.ButtonSetLabel`, which calls `wxk_Button_SetLabel`, which calls `wxButton::SetLabel`. An event goes up: a functor the shim bound with `Bind` calls a function pointer into an `UnmanagedCallersOnly` method, which raises the .NET event.

## Documents in this folder

- [Native layer](native-layer.md): the shim's C conventions, strings, errors, callbacks, and overriding virtual methods.
- [Managed API](managed-api.md): naming, object lifetime, events, threading, and the application model.
- [Generator](generator.md): how bindings come from the wxWidgets interface headers.
- [Building and packaging](building-and-packaging.md): native builds, static linking, NuGet packages, CI, and releases.
- [Language support](languages.md): F#, and why Visual Basic isn't supported.
- [Testing](testing.md): testing GUI code on three platforms.
- [Open questions](open-questions.md): what is still undecided.
- [Planned changes](planned-changes.md): changes that need code changes, which wait for 0.2.0.
- [Directors](directors.md): how .NET overrides wxWidgets' virtual members, and what is still written by hand.

## Conventions

- Text files use LF line endings, which `.editorconfig` and `.gitattributes` enforce. C# is indented with tabs, Markdown and YAML with two spaces, and F# with four spaces.
- Code is formatted with `dotnet format`, and style warnings show in every build. `Directory.Build.props` sets the language version, nullable reference types, implicit usings, and code style for every project.
- Build output goes under `artifacts/`, which git ignores.
- Comments explain why the code does something, not what it does.
- Commit messages start with a lowercase verb for fixes, such as "fix a crash when a frame closes twice", and a capitalized one for new work, such as "Add the tree control".

## Repository layout

- `native/wxWidgets`: the wxWidgets submodule.
- `native/shim`: the shim and its CMake project. Generated sources are in `native/shim/generated`, and hand-written ones in `native/shim/src`.
- `src/WxKit`: the managed library, with `build/WxKit.targets` for applications.
- `src/WxKit.Native`: the native packages.
- `src/WxKit.Generator`: the generator, which is not packed.
- `tests/WxKit.Tests`, `tests/WxKit.Generator.Tests`, and `tests/WxKit.Build.Tests`: the tests of the managed API, the generator, and the MSBuild targets.
- `samples`: in `csharp` and `fsharp`, each in both languages: Hello, a text editor, a translated settings dialog, a checklist control that draws itself, a library of books in a data view, and a browser around a web view.
- `scripts`: building the native libraries, generating, packing, and testing the packages and Native AOT.
- `docs`: the guides, these design documents, and each release's notes, in `docs/releases`.

## What is left

- Optional libraries beyond the styled text control and the web view, in the order [Open questions](open-questions.md) suggests, and XRC last.
- Dragging data other than files.
- Disk images and notarization as part of a macOS publish, which the docs give the commands for today.
