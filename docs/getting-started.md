# Getting started

This guide walks you through a first WxKit application: a small to-do list with a text box, a list, buttons, a menu, and a file to save to. Along the way, it covers how a WxKit application starts, how controls are laid out, how events work, and how to publish the application as one executable. The complete program is at the end.

## Before you begin

You need the .NET 10 SDK. Running an application has a few more requirements, depending on the system:

- On Windows, nothing more.
- On Linux, GTK 3, which desktop Linux distributions include.
- On macOS, version 12 or later.

Publishing with Native AOT, which the last section covers, needs a native toolchain, as any Native AOT publish does:

- On Windows, Visual Studio 2022 or later, or its Build Tools, with the C++ tools.
- On Linux, `clang` and zlib's development files, such as Ubuntu's `zlib1g-dev`.
- On macOS, Xcode's command line tools.

## Create the project

1. Create a console project, and go into its folder:

   ```shell
   dotnet new console -n Todo
   cd Todo
   ```

2. Add the WxKit package. It's a preview, so name the prerelease:

   ```shell
   dotnet add package WxKit --prerelease
   ```

   The package brings in WxKit's native library for each system and architecture it runs on.

3. In `Todo.csproj`, change `OutputType` from `Exe` to `WinExe`, so that Windows doesn't open a console window beside the application. On Linux and macOS, it makes no difference.

## Show a window

Replace the contents of `Program.cs` with the following:

```csharp
using WxKit;

return Application.Run(() => {
	Frame frame = new(null, "To do", size: new Size(420, 360));
	frame.Show();
});
```

Then run it:

```shell
dotnet run
```

`Application.Run` starts wxWidgets, calls your startup code, and runs the event loop until the last top-level window closes. It returns the exit code, which `Program.cs` returns in turn.

A `Frame` is a top-level window, with a title bar, and later a menu bar and a status bar. Its first argument is its parent window. A main window has none, so it's null.

## Add controls

Controls go on a `Panel` inside the frame. A panel gives them the right background, and moves the focus between them with Tab. Replace the line that shows the frame with the following:

```csharp
Panel panel = new(frame);
StaticText label = new(panel, "&New task:");
TextCtrl entry = new(panel, style: TextCtrlStyle.ProcessEnter);
Button add = new(panel, "&Add") {
	IsEnabled = false,
};
StaticText tasksLabel = new(panel, "&Tasks:");
ListBox tasks = new(panel);
StaticText details = new(panel, "");
Button remove = new(panel, "&Remove") {
	IsEnabled = false,
};
frame.Show();
entry.SetFocus();
```

Each control's first argument is its parent, which owns it: wxWidgets destroys a window's children with it, so you don't dispose of controls yourself.

Three details make this window work well with the keyboard and with screen readers:

- Each label is created just before the control it names. Screen readers name a control after the label before it, and the Tab key moves through controls in the order they're created.
- The ampersand in a label marks its access key. Alt+N moves to the text box, and Alt+A presses the Add button. On macOS, the ampersand doesn't show.
- These are the platform's own controls, which screen readers already know. A control your application draws itself needs more, which the [managed API](design/managed-api.md#accessibility) describes.

## Lay out the controls

Without a layout, the controls sit on top of each other. A sizer places a window's children, and moves and resizes them with it. A `BoxSizer` stacks them in a row or a column, and the flags for each control say how it grows and how much space is around it. Add the following before `frame.Show()`:

```csharp
BoxSizer entryRow = new(Orientation.Horizontal);
entryRow.Add(entry, new SizerFlags(1).Border(Direction.Right));
entryRow.Add(add);
BoxSizer sizer = new(Orientation.Vertical);
sizer.Add(label, SizerFlags.Default.Border(Direction.Left | Direction.Right | Direction.Top));
sizer.Add(entryRow, SizerFlags.Default.Expand().Border());
sizer.Add(tasksLabel, SizerFlags.Default.Border(Direction.Left | Direction.Right));
sizer.Add(tasks, new SizerFlags(1).Expand().Border());
sizer.Add(details, SizerFlags.Default.Expand().Border(Direction.Left | Direction.Right));
sizer.Add(remove, SizerFlags.Default.Border());
panel.Sizer = sizer;
```

The flags read as follows:

- `new SizerFlags(1)` gives a control a share of the spare room along the sizer's direction, so the list grows taller with the window, and the text box grows wider in its row. `SizerFlags.Default` doesn't grow.
- `Expand()` fills the space across the sizer's direction, so the list is as wide as the window.
- `Border()` puts the platform's usual space around a control, and `Border(Direction.Left | Direction.Right)` puts it on those sides only.

A sizer can hold another sizer, as the vertical sizer holds the row with the text box and its button. Setting the panel's `Sizer` gives the sizer to the panel, which owns it from then on. For a grid of labels and fields, use a `FlexGridSizer`.

## Handle events

Events are .NET events. Each handler gets the control that sent the event, and an object with the event's details. Add the following before the layout code:

```csharp
entry.TextChanged += (_, _) => add.IsEnabled = entry.Value.Trim().Length > 0;
void AddTask() {
	string title = entry.Value.Trim();
	if (title.Length == 0) return;
	tasks.Append(title);
	entry.Clear();
	entry.SetFocus();
}
add.Click += (_, _) => AddTask();
entry.TextEnter += (_, _) => AddTask();
```

The text box sends `TextEnter` when you press Enter in it, because it has the `ProcessEnter` style. Without the style, Enter is left to the window, as in a dialog.

Every member of a control has to be called on the thread that runs the event loop, which handlers run on. A member called from another thread throws `InvalidOperationException`. To change a control from another thread, give the change to `Application.Post`, which runs it on the event loop's thread. An `await` in a handler comes back to that thread on its own, as the section on saving shows.

## Keep data with list items

A list box shows strings, but it can keep any object with each item, such as what the item stands for. Replace `tasks.Append(title);` in `AddTask` with the following, which keeps a record of when each task was added:

```csharp
tasks.Append(title, new TodoItem(title, DateTime.Now));
```

Then add the record at the end of `Program.cs`, after the code that calls `Application.Run`:

```csharp
internal sealed record TodoItem(string Title, DateTime Added);
```

When the selection changes, show when the task was added, and let it be removed. Add the following after the `TextEnter` handler:

```csharp
tasks.SelectionChanged += (_, _) => {
	int index = tasks.Selection;
	remove.IsEnabled = index >= 0;
	details.Label = index >= 0 && tasks.GetClientObject(index) is TodoItem item ? $"Added at {item.Added:t}" : "";
};
remove.Click += (_, _) => {
	int index = tasks.Selection;
	if (index < 0) return;
	tasks.Delete(index);
	remove.IsEnabled = false;
	details.Label = "";
};
```

`Selection` is -1 when nothing is selected. The list keeps each object for as long as its item exists, and lets go of it when the item is deleted. Trees and choice controls keep objects with their items in the same way.

## Add a menu

A menu item with a standard id, such as `StandardId.Save`, gets the platform's usual label and shortcut, such as Ctrl+S on Windows and Linux, and Command+S on macOS. An item of your own takes a label and an id above the ones wxWidgets uses, such as 6000 and up. Add the following before `frame.Show()`:

```csharp
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
```

On macOS, wxWidgets moves the About and Exit items to the application menu, where Mac applications have them, and names Exit Quit.

`MessageBox.Show` returns the id of the button that was pressed, such as `StandardId.Yes`, which the next section uses.

## Ask before closing

A frame raises `Closing` before it closes, and a handler can stop it with `e.Veto()`. Keep track of whether the list has changed since it was last saved, by adding the following before `AddTask`:

```csharp
bool saved = true;
```

Set `saved = false;` in `AddTask`, after the task is appended, and in the `remove.Click` handler, after the task is deleted. Then add the following after the menu code:

```csharp
frame.Closing += (_, e) => {
	if (saved || !e.CanVeto) return;
	if (MessageBox.Show("Quit without saving your tasks?", "To do", MessageDialogStyle.YesNo | MessageDialogStyle.IconQuestion, frame) != StandardId.Yes) e.Veto();
};
```

`CanVeto` is false when the window is closed by force, such as by `frame.Close(force: true)`, and it closes whatever the handler does. If no handler vetoes, the window closes once the handlers return.

## Save to a file

A file dialog asks where to save, and `ShowModal` returns `StandardId.Ok` when you choose a file. Add the following after the `Closing` handler:

```csharp
async Task SaveAsync() {
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
}
save.Click += async (_, _) => await SaveAsync();
```

The file is written without blocking the window, and the code after `await` runs on the event loop's thread again, so it can use the controls.

Two details matter in async handlers:

- An event's object only exists while its handlers run. Copy what you need out of it before the first `await`.
- A dialog, unlike a control, is disposed of when you're done with it, as the `using` declaration does here.

If a handler throws an exception it doesn't catch, WxKit raises `Application.UnhandledException`. If nothing marks it handled there, the event loop ends, and `Application.Run` throws the exception.

## Publish the application

Native AOT compiles the application, WxKit, and wxWidgets into one executable, which starts quickly and needs no .NET runtime. Add the following to the `PropertyGroup` in `Todo.csproj`:

```xml
<PublishAot>true</PublishAot>
<ApplicationTitle>Todo</ApplicationTitle>
<ApplicationId>com.example.todo</ApplicationId>
```

Then publish for the system and architecture you want, such as `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-arm64`, or `osx-x64`:

```shell
dotnet publish -r win-x64
```

The executable is in `bin/Release/net10.0/win-x64/publish`, and is a few megabytes, since it only links the parts of wxWidgets the application uses. Native AOT builds for the system it runs on, so publish for Linux on Linux, and for macOS on a Mac.

On macOS, publishing makes an app bundle, `Todo.app`, named after `ApplicationTitle` and known to macOS by `ApplicationId`. It's signed ad hoc, which is enough to run it on your own Mac. To give it to others, sign it with your Developer ID and notarize it, as [Building and packaging](design/building-and-packaging.md#signing-and-notarizing) describes.

On Windows, if publishing fails with "'vswhere.exe' is not recognized", add `%ProgramFiles(x86)%\Microsoft Visual Studio\Installer` to `PATH`. The Visual Studio 2026 Build Tools need it there for Native AOT to find the linker.

## The complete program

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
	StaticText details = new(panel, "");
	Button remove = new(panel, "&Remove") {
		IsEnabled = false,
	};
	bool saved = true;
	entry.TextChanged += (_, _) => add.IsEnabled = entry.Value.Trim().Length > 0;
	void AddTask() {
		string title = entry.Value.Trim();
		if (title.Length == 0) return;
		tasks.Append(title, new TodoItem(title, DateTime.Now));
		saved = false;
		entry.Clear();
		entry.SetFocus();
	}
	add.Click += (_, _) => AddTask();
	entry.TextEnter += (_, _) => AddTask();
	tasks.SelectionChanged += (_, _) => {
		int index = tasks.Selection;
		remove.IsEnabled = index >= 0;
		details.Label = index >= 0 && tasks.GetClientObject(index) is TodoItem item ? $"Added at {item.Added:t}" : "";
	};
	remove.Click += (_, _) => {
		int index = tasks.Selection;
		if (index < 0) return;
		tasks.Delete(index);
		saved = false;
		remove.IsEnabled = false;
		details.Label = "";
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
	async Task SaveAsync() {
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
	}
	save.Click += async (_, _) => await SaveAsync();
	BoxSizer entryRow = new(Orientation.Horizontal);
	entryRow.Add(entry, new SizerFlags(1).Border(Direction.Right));
	entryRow.Add(add);
	BoxSizer sizer = new(Orientation.Vertical);
	sizer.Add(label, SizerFlags.Default.Border(Direction.Left | Direction.Right | Direction.Top));
	sizer.Add(entryRow, SizerFlags.Default.Expand().Border());
	sizer.Add(tasksLabel, SizerFlags.Default.Border(Direction.Left | Direction.Right));
	sizer.Add(tasks, new SizerFlags(1).Expand().Border());
	sizer.Add(details, SizerFlags.Default.Expand().Border(Direction.Left | Direction.Right));
	sizer.Add(remove, SizerFlags.Default.Border());
	panel.Sizer = sizer;
	frame.Show();
	entry.SetFocus();
});

internal sealed record TodoItem(string Title, DateTime Added);
```

## Next steps

- The [text editor sample](../samples/Editor/EditorFrame.cs) is a fuller application: a frame of its own class, files opened and saved without blocking, find and replace, fonts, and word wrap.
- The [settings sample](../samples/Settings/SettingsDialog.cs) is a settings dialog: a notebook of pages, a form laid out with a grid bag sizer, color, font, folder, and language pickers, a page that scrolls, and OK and Cancel with a validator. It's translated into German.
- [Translating an application](translating.md) shows how to translate an application's text, and have wxWidgets' own text follow the same language.
- The [checklist sample](../samples/Checklist/ChecklistControl.cs) is a control that draws itself, described to screen readers on Windows, macOS, and Linux.
- The [managed API](design/managed-api.md) explains how WxKit maps wxWidgets to C#: naming, object lifetimes, events, threading, data views, and accessibility.
- [Building and packaging](design/building-and-packaging.md) covers Native AOT, static linking, and macOS app bundles in depth.
- The [wxWidgets documentation](https://docs.wxwidgets.org/3.3/) describes each class. WxKit's names follow it, without the `wx` prefix, such as `wxListBox` as `ListBox`.
