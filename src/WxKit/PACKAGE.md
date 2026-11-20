# WxKit

.NET bindings to [wxWidgets](https://wxwidgets.org), for native desktop applications. WxKit works with Native AOT, and links wxWidgets into the executable, so an application ships as one file of a few megabytes, with native controls that screen readers already understand.

WxKit is a preview. It runs on Windows, Linux, and macOS, each on x64 and ARM64, and its API will change before 1.0.

## A first application

```csharp
using WxKit;

return Application.Run(() => {
	Frame frame = new(null, "Hello");
	Panel panel = new(frame);
	StaticText label = new(panel, "&Name:");
	TextCtrl name = new(panel);
	Button greet = new(panel, "&Greet");
	greet.Click += (_, _) => MessageBox.Show($"Hello, {name.Value}!", "Hello", MessageDialogStyle.Ok, frame);
	BoxSizer sizer = new(Orientation.Vertical);
	sizer.Add(label, SizerFlags.Default.Border());
	sizer.Add(name, SizerFlags.Default.Expand().Border());
	sizer.Add(greet, SizerFlags.Default.Border());
	panel.Sizer = sizer;
	frame.Show();
});
```

Run it as it is, or publish it as one executable:

```powershell
dotnet publish -r win-x64 -p:PublishAot=true
```

On Linux, an application needs GTK 3, which desktop Linux has, and runs with `-r linux-x64` or `-r linux-arm64`. A Native AOT publish there needs `clang` and zlib's development files, as any does on Linux, but not GTK's development files.

On macOS, call `Application.Run` from `Main`, since Cocoa only runs a user interface on the main thread. A Native AOT publish needs Xcode's command line tools, as any does on macOS. Publishing for macOS also makes an app bundle, such as `Hello.app`, named after `ApplicationTitle` and identified by `ApplicationId`, as in .NET MAUI, and signed ad hoc, or with your Developer ID in `WxKitCodesignIdentity`. The repository's [building and packaging](https://github.com/trypsynth/wxkit/blob/master/docs/design/building-and-packaging.md#macos-app-bundles) docs describe it, and notarizing it.

On Windows, a Native AOT publish needs Visual Studio's C++ tools, as any Native AOT publish on Windows does. With the Visual Studio 2026 Build Tools, if it fails with "'vswhere.exe' is not recognized", add `%ProgramFiles(x86)%\Microsoft Visual Studio\Installer` to `PATH`, which Native AOT needs there to find the linker.

## What it covers

The bindings are generated from wxWidgets's own headers: windows and the common controls, list, tree, and data view controls, toolbars, sizers, menus, the standard dialogs, drawing, images, validators, and the clipboard, with events and async support. Members C# overrides, such as a virtual list's rows and a self-drawn control's description for screen readers, call into .NET. An application only links the classes it uses.

An application translates its text with gettext, as wxWidgets does its own: mark strings with `_`, `_n`, `_p`, and `_pn` from `WxKit.Gettext`, extract them with `dotnet build -t:WxKitExtract`, and put each language's catalog in `po`, such as `po/de.po`. The build compiles and embeds them, with wxWidgets' own catalogs for the same languages, so stock button labels follow the application's language. The repository's [translating guide](https://github.com/trypsynth/wxkit/blob/master/docs/translating.md) describes it.

## Packages

This package is the managed library. It brings in a native package for each runtime, such as `WxKit.Native.win-x64`, which holds wxWidgets and WxKit's shim, as a shared library and as the static libraries Native AOT links.

## How this was made

WxKit is heavily vibe coded: Claude, an AI model, wrote nearly all of its code and docs, under its author's direction. If AI-written code is a dealbreaker for you, this is not the project for you. The [getting started guide](https://github.com/trypsynth/wxkit/blob/master/docs/getting-started.md) walks through a first application, and the [repository](https://github.com/trypsynth/wxkit) has the design docs, the source, and the issue tracker.

## License

WxKit is MIT licensed. wxWidgets is under the wxWindows Library Licence, which allows applications that link it, statically or not, to use their own terms. The native packages list the libraries wxWidgets builds in, with their licenses.
