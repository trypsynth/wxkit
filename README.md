# WxKit

.NET bindings to [wxWidgets](https://wxwidgets.org), for native desktop applications on Windows, macOS, and Linux. WxKit works with Native AOT, and links wxWidgets into the executable so that an application ships as one file.

WxKit 0.1 is the first release that isn't a preview. Its API follows .NET's names and types, and 0.1.x releases add to it without changes that need code changes, as [Versions and names](docs/design/building-and-packaging.md#versions-and-names) describes. 0.2.0, which `master` is heading for, changes the API where 0.1 got it wrong. Parts of wxWidgets are not bound yet. It runs on Windows, Linux, and macOS, each on x64 and ARM64, from C# or F#. The bindings are generated from wxWidgets's own headers, and cover windows and the common controls, list, tree, and data view controls, the styled text control, the web view, toolbars, sizers, menus, the standard and composite dialogs, wizards, drawing, images, the clipboard, drag and drop, translations, and accessibility, including for controls that draw themselves. Screen readers read its controls, and tests can check what they read, on all three platforms.

To write a first application, follow [Getting started](docs/getting-started.md). The samples show more:

- [Hello](samples/Hello/Program.cs) publishes with Native AOT as one executable of about 6 MB, since an application links only the classes it uses. [HelloFSharp](samples/HelloFSharp/Program.fs) is its F# counterpart.
- [Editor](samples/Editor/EditorFrame.cs) is a text editor, with menus, files, find and replace, fonts, and printing.
- [Settings](samples/Settings/SettingsDialog.cs) is a settings dialog, with pages, a form, pickers, and a validator, translated into German as [Translating an application](docs/translating.md) describes.
- [Checklist](samples/Checklist/ChecklistControl.cs) is a control that draws itself, which screen readers read on all three platforms.
- [Browser](samples/Browser/Program.cs) is a small web browser, with an address, Back, Forward, and Reload, around a web view.

How WxKit is built is in the [design overview](docs/design/overview.md), and what is still undecided in [Open questions](docs/design/open-questions.md).

## Building

The samples build with WxKit's own source and the native libraries `./scripts/build-native.ps1` builds, once there are ones for the machine's runtime. Until then, they use the released WxKit package from nuget.org, so a fresh clone can run one at once, such as with `dotnet run --project samples/Editor`. A sample that uses something newer than the release needs the native build. The tests always do, and fail with a message that says so without it.

On Windows, you need the following:

- The .NET 10 SDK.
- Visual Studio 2022 or newer, or its Build Tools, with the C++ tools. Building for ARM64 also needs their ARM64 build tools, which the Visual Studio Installer lists as "MSVC ARM64/ARM64EC build tools".
- CMake. Without one on `PATH`, the build script uses the one Visual Studio installs with its C++ CMake tools.
- PowerShell 7.

wxWidgets is a git submodule, built with CMake. The build script builds it, and WxKit's native shim, for the runtimes you give it, and defaults to `win-x64`. Give it `-Runtimes win-x64, win-arm64` for both. An x64 machine can build for ARM64, but only an ARM64 machine can run what it builds. The first build of each runtime takes 10 to 20 minutes.

```powershell
git clone --recurse-submodules https://github.com/trypsynth/wxkit
cd wxkit
./scripts/build-native.ps1
dotnet test --project tests/WxKit.Tests
./scripts/test-aot.ps1
```

On Linux, you need CMake, Ninja, PowerShell 7, the .NET 10 SDK, the development files of GTK 3 and WebKitGTK 4.1, such as Ubuntu's `libgtk-3-dev` and `libwebkit2gtk-4.1-dev`, and, for Native AOT, `clang` and zlib's development files, such as `zlib1g-dev`. The same commands build and test, and the build script defaults to the machine's own runtime, such as `linux-x64`.

On macOS, you need Xcode, the .NET 10 SDK, PowerShell 7, and CMake and Ninja, such as from Homebrew. A Mac with Apple Silicon builds both `osx-arm64` and `osx-x64`, and runs the latter through Rosetta.

The tests create real windows, so they need a desktop session. On Linux without one, run them under `xvfb-run`. `test-aot.ps1` publishes the samples with Native AOT and checks that each starts and exits cleanly.

## Packages

WxKit is on nuget.org. To start an application, install the templates and make one, in C# or, with `-lang F#`, in F#:

```shell
dotnet new install WxKit.Templates
dotnet new wxkit -n MyApp
```

To use WxKit in a project you have, add the package, which brings in the native package for each runtime, such as `WxKit.Native.win-x64`:

```shell
dotnet add package WxKit
```

In F#, add `WxKit.FSharp` instead, which brings WxKit with it, and adds translation functions and layouts that read as F#.

[Getting started](docs/getting-started.md) goes from there to a published executable. A tag publishes a new version, as [Building and packaging](docs/design/building-and-packaging.md#releasing) describes.

To try changes of your own from an application, build the packages, which go to `artifacts/packages`:

```powershell
./scripts/build-native.ps1
./scripts/pack.ps1
./scripts/test-package.ps1
```

Give `pack.ps1` `-Runtimes win-x64, win-arm64` to pack both, as CI does. `test-package.ps1` checks the packages the way an application would use them, on the machine's own runtime. Each push to master also builds them in CI, which keeps them for a week as an artifact of the run.

Then add `artifacts/packages` as a package source, and reference the version `pack.ps1` gave them.

An application runs with the shared native library, and a Native AOT publish links wxWidgets into the executable instead. A publish for macOS also makes an app bundle, which [Building and packaging](docs/design/building-and-packaging.md#macos-app-bundles) describes:

```powershell
dotnet publish -r win-x64 -p:PublishAot=true
```

If you publish with Native AOT yourself and it fails with "'vswhere.exe' is not recognized", add `%ProgramFiles(x86)%\Microsoft Visual Studio\Installer` to `PATH`. The Visual Studio 2026 Build Tools need it there for Native AOT to find the linker. `test-aot.ps1` does this for you.

## How this was made

WxKit is heavily vibe coded. Claude, an AI model, wrote nearly all of the code and the docs. I directed the work, reviewed parts of it, and checked that it mostly holds up, but I have not read every line, and you should expect mistakes a careful human author would not make.

If AI-written code is a dealbreaker for you, this is not the project for you, and that's fine. If it isn't, bug reports and pull requests are welcome.

## License

WxKit is MIT licensed. wxWidgets is under the wxWindows Library Licence, which allows applications that link it, statically or not, to use their own terms. The native packages also contain the libraries wxWidgets builds in, such as zlib and libpng, which [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) lists with their licenses.
