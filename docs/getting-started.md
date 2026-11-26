# Getting started

This guide builds a small to-do list application with WxKit: a text box, a list, buttons, a menu, and a file to save to. It covers how an application starts, how events and layout work, and how to publish the application as one executable with Native AOT.

## Before you begin

You need the .NET 10 SDK. To run an application, Linux needs GTK 3, which desktop distributions include, and macOS needs version 12 or later. Windows needs nothing more.

Publishing with Native AOT needs a native toolchain, as any Native AOT publish does:

- On Windows, Visual Studio 2022 or later, or its Build Tools, with the C++ tools.
- On Linux, `clang` and zlib's development files, such as Ubuntu's `zlib1g-dev`.
- On macOS, Xcode's command line tools.

This guide uses C#. F# works as well, with the `WxKit.FSharp` package, whose translation functions and layouts read as F#, and the [F# sample](../samples/fsharp/Hello/Program.fs) is the C# Hello sample's counterpart, which CI publishes with Native AOT. Visual Basic isn't supported, as [Language support](design/languages.md) explains.

## Create the project

The WxKit template makes a project that is ready to run and to publish with Native AOT, with a window that greets you by name:

```shell
dotnet new install WxKit.Templates
dotnet new wxkit -n Todo
cd Todo
dotnet run
```

`dotnet new wxkit -lang F#` makes the F# version. This guide starts from a console project instead, to show what each part of the project does:

1. Create a console project, and go into its folder:

   ```shell
   dotnet new console -n Todo
   cd Todo
   ```

2. Add the WxKit package. It brings in WxKit's native library for each system and architecture it supports.

   ```shell
   dotnet add package WxKit
   ```

3. In `Todo.csproj`, change `OutputType` from `Exe` to `WinExe`, so that Windows doesn't open a console window beside the application. On Linux and macOS, it makes no difference.

## Show a window

Replace the contents of `Program.cs` with the following, and run it with `dotnet run`:

```csharp
using WxKit;

return Application.Run(() => {
	Frame frame = new(null, "To do", size: new Size(420, 360));
	frame.Show();
});
```

`Application.Run` starts wxWidgets, calls your startup code, and runs the event loop until the last top-level window closes. It returns the exit code. A `Frame` is a top-level window, which can have a menu bar and a status bar. Its first argument is its parent, which a main window doesn't have.

## Build the to-do list

Replace `Program.cs` with the complete application. The sections after it explain each part.

```csharp
using WxKit;

return Application.Run(() => {
	Frame frame = new(null, "To do", size: new Size(420, 360));
	Panel panel = new(frame);
	StaticText label = new(panel, "&New task:");
	TextCtrl entry = new(panel, style: TextCtrlStyle.ProcessEnter);
	Button add = new(panel, "&Add") {
		IsEnabled = false,
	};
	StaticText tasksLabel = new(panel, "&Tasks:");
	ListBox tasks = new(panel);
	Button remove = new(panel, "&Remove") {
		IsEnabled = false,
	};
	bool saved = true;
	entry.TextChanged += (_, _) => add.IsEnabled = entry.Value.Trim().Length > 0;
	void AddTask() {
		string title = entry.Value.Trim();
		if (title.Length == 0) return;
		tasks.Append(title);
		saved = false;
		entry.Clear();
		entry.SetFocus();
	}
	add.Click += (_, _) => AddTask();
	entry.TextEnter += (_, _) => AddTask();
	tasks.SelectionChanged += (_, _) => remove.IsEnabled = tasks.Selection >= 0;
	remove.Click += (_, _) => {
		if (tasks.Selection < 0) return;
		tasks.Delete(tasks.Selection);
		saved = false;
		remove.IsEnabled = false;
	};
	Menu fileMenu = new();
	MenuItem save = fileMenu.Append(StandardId.Save);
	fileMenu.AppendSeparator();
	fileMenu.Append(StandardId.Exit).Click += (_, _) => frame.Close();
	Menu helpMenu = new();
	helpMenu.Append(StandardId.About).Click += (_, _) => MessageBox.Show("A to-do list, made with WxKit.", "About To do", MessageDialogStyle.Ok | MessageDialogStyle.IconInformation, frame);
	MenuBar menuBar = new();
	menuBar.Append(fileMenu, "&File");
	menuBar.Append(helpMenu, "&Help");
	frame.MenuBar = menuBar;
	frame.CreateStatusBar();
	frame.Closing += (_, e) => {
		if (saved || !e.CanVeto) return;
		if (MessageBox.Show("Quit without saving your tasks?", "To do", MessageDialogStyle.YesNo | MessageDialogStyle.IconQuestion, frame) != StandardId.Yes) e.Veto();
	};
	save.Click += async (_, _) => {
		using FileDialog dialog = new(frame, "Save tasks", defaultFile: "tasks.txt", wildcard: "Text files (*.txt)|*.txt", style: FileDialogStyle.Save | FileDialogStyle.OverwritePrompt);
		if (dialog.ShowModal() != StandardId.Ok) return;
		string[] lines = tasks.Strings;
		try {
			await File.WriteAllLinesAsync(dialog.Path, lines);
		} catch (Exception error) when (error is IOException or UnauthorizedAccessException) {
			MessageBox.Show($"The tasks could not be saved. {error.Message}", "To do", MessageDialogStyle.Ok | MessageDialogStyle.IconError, frame);
			return;
		}
		saved = true;
		frame.SetStatusText($"Saved {lines.Length} tasks.");
	};
	BoxSizer entryRow = new(Orientation.Horizontal);
	entryRow.Add(entry, new SizerFlags(1).Border(Direction.Right));
	entryRow.Add(add);
	BoxSizer sizer = new(Orientation.Vertical);
	sizer.Add(label, SizerFlags.Default.Border(Direction.Left | Direction.Right | Direction.Top));
	sizer.Add(entryRow, SizerFlags.Default.Expand().Border());
	sizer.Add(tasksLabel, SizerFlags.Default.Border(Direction.Left | Direction.Right));
	sizer.Add(tasks, new SizerFlags(1).Expand().Border());
	sizer.Add(remove, SizerFlags.Default.Border());
	panel.Sizer = sizer;
	frame.Show();
	entry.SetFocus();
});
```

## Create controls for the keyboard and screen readers

Controls go on a `Panel` inside the frame, which gives them the right background and moves the focus between them with Tab. Each control's first argument is its parent, which owns it: wxWidgets destroys a window's children with it, so you don't dispose of controls yourself.

Three details make the window work well with the keyboard and with screen readers:

- Each label is created just before the control it names. Screen readers name a control after the label before it, and Tab moves through controls in the order they're created.
- The ampersand in a label marks its access key, so Alt+N moves to the text box, and Alt+A presses Add. On macOS, the ampersand doesn't show.
- These are the platform's own controls, which screen readers already know. A control that draws itself needs more, as [Accessibility](design/managed-api.md#accessibility) describes.

## Lay out controls with sizers

A sizer places a window's children, and moves and resizes them with the window. A `BoxSizer` stacks them in a row or a column, and a sizer can hold another sizer, as the vertical sizer holds the row with the text box and Add. The flags for each control say how it grows:

- `new SizerFlags(1)` gives a control a share of the spare room along the sizer's direction, so the list grows taller with the window, and the text box grows wider in its row. `SizerFlags.Default` doesn't grow.
- `Expand()` fills the space across the sizer's direction, so the list is as wide as the window.
- `Border()` puts the platform's usual space around a control, and `Border(Direction.Left | Direction.Right)` puts it on those sides only.

Setting the panel's `Sizer` gives the sizer to the panel, which owns it from then on. For a grid of labels and fields, use a `FlexGridSizer` or a `GridBagSizer`, as the [settings sample](../samples/csharp/Settings/SettingsDialog.cs) does.

## Handle events

Events are .NET events, whose handlers get the sender and an object with the event's details. The text box sends `TextEnter` when you press Enter in it because it has the `ProcessEnter` style. Without the style, Enter goes to the window, as in a dialog.

A list box can also keep an object with each item, such as a record the item stands for, with `tasks.Append(title, item)` and `tasks.GetItemData(index)`. `Selection` is -1 when nothing is selected.

Handlers run on the thread that runs the event loop, and every member of a control has to be called there, or it throws `InvalidOperationException`. To change a control from another thread, pass the change to `Application.Post`. An `await` in a handler comes back to the event loop's thread, so the code after it can use the controls, as the Save handler does after it writes the file.

Two more rules apply to handlers:

- An event's object only exists while its handlers run, so copy what you need out of it before the first `await`.
- If a handler throws an exception it doesn't catch, WxKit raises `Application.UnhandledException`. If nothing marks it handled there, the event loop ends, and `Application.Run` throws the exception.

[Events](design/managed-api.md#events) and [Threading](design/managed-api.md#threading) in the managed API cover both in more depth.

## Add menus and dialogs

A menu item with a standard id, such as `StandardId.Save`, gets the platform's usual label and shortcut, such as Ctrl+S on Windows and Linux and Command+S on macOS. For a command of your own, pass an id above the ones wxWidgets keeps, such as 6001, and a label, as the [text editor sample](../samples/csharp/Editor/EditorFrame.cs) does. On macOS, wxWidgets moves About and Exit to the application menu, and names Exit Quit.

`MessageBox.Show` and a dialog's `ShowModal` return the id of the button that closed them, such as `StandardId.Yes` or `StandardId.Ok`. Unlike a control, a dialog is disposed of when you're done with it, as the `using` declaration does with the file dialog.

A frame raises `Closing` before it closes. A handler can stop it with `e.Veto()`, unless `CanVeto` is false, as when the window is closed with `frame.Close(force: true)`. If no handler vetoes, the window closes once the handlers return.

## Publish with Native AOT

Native AOT compiles the application, WxKit, and wxWidgets into one executable, which starts quickly and needs no .NET runtime or other files beside it. Add the following to the `PropertyGroup` in `Todo.csproj`:

```xml
<PublishAot>true</PublishAot>
<ApplicationTitle>Todo</ApplicationTitle>
<ApplicationId>com.example.todo</ApplicationId>
```

Then publish for a runtime: `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-arm64`, or `osx-x64`.

```shell
dotnet publish -r win-x64
```

The executable is in `bin/Release/net10.0/win-x64/publish`, and is a few megabytes. Native AOT only compiles for the system it runs on, so publish for Linux on Linux, and for macOS on a Mac. Windows can publish for both of its architectures.

On macOS, publishing also makes an app bundle, `Todo.app`, named after `ApplicationTitle` and known to macOS by `ApplicationId`. It's signed ad hoc, which runs on your own Mac. To give it to others, sign and notarize it, as [macOS app bundles](design/building-and-packaging.md#macos-app-bundles) describes.

On Windows, if publishing fails with "'vswhere.exe' is not recognized", add `%ProgramFiles(x86)%\Microsoft Visual Studio\Installer` to `PATH`. The Visual Studio 2026 Build Tools need it there for Native AOT to find the linker.

## Next steps

- The [Hello sample](../samples/csharp/Hello/Program.cs) adds a timer and an async handler, and the [F# sample](../samples/fsharp/Hello/Program.fs) shows the same API from F#.
- The [text editor sample](../samples/csharp/Editor/EditorFrame.cs) is a frame of its own class, with files opened and saved without blocking, find and replace, fonts, word wrap, and printing.
- The [settings sample](../samples/csharp/Settings/SettingsDialog.cs) is a settings dialog with a notebook of pages, a grid bag layout, pickers, a scrolled page, and a validator. It's translated into German.
- The [checklist sample](../samples/csharp/Checklist/ChecklistControl.cs) is a control that draws itself, and describes itself to screen readers on Windows, macOS, and Linux.
- [Translating an application](translating.md) shows how to translate your text, and have wxWidgets' own text follow the same language.
- The [managed API](design/managed-api.md) explains how WxKit maps wxWidgets to .NET: naming, object lifetimes, events, threading, and accessibility.
- [Building and packaging](design/building-and-packaging.md) covers Native AOT, static linking, and macOS app bundles in depth.
- The [wxWidgets documentation](https://docs.wxwidgets.org/3.3/) describes each class. WxKit's names drop the `wx` prefix, so `wxListBox` is `ListBox`.
