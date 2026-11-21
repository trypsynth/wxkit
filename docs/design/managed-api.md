# Managed API

This document describes the C# API that applications use: how wxWidgets names map to .NET names, who owns which objects, how events work, and how the application starts.

## A first look

This is the program that the first milestone aims for:

```csharp
using WxKit;

Application.Run(() => {
	Frame frame = new(null, "Hello");
	Panel panel = new(frame);
	BoxSizer sizer = new(Orientation.Vertical);
	StaticText label = new(panel, "&Name:");
	TextCtrl name = new(panel);
	Button greet = new(panel, "&Greet");
	greet.Click += (sender, e) => MessageBox.Show($"Hello, {name.Value}!", "Greeting", parent: frame);
	sizer.Add(label, SizerFlags.Default.Border());
	sizer.Add(name, SizerFlags.Default.Expand().Border());
	sizer.Add(greet, SizerFlags.Default.Border());
	panel.Sizer = sizer;
	frame.Show();
});
```

The label is created before the text box, so a screen reader names the text box after it, and the tab order follows. WxKit's samples follow this pattern throughout.

## Naming

WxKit uses the wxWidgets names, adjusted to .NET conventions:

- The `wx` prefix is dropped from types. `wxFrame` becomes `Frame`, and `wxBoxSizer` becomes `BoxSizer`. The `WxKit` namespace keeps them apart from other libraries' types.
- Methods keep their names, which wxWidgets already writes in Pascal case. `wxWindow::Refresh` becomes `Window.Refresh`.
- A getter and setter pair becomes a property. `GetLabel` and `SetLabel` become `Label`. `IsEnabled` and `Enable` become `IsEnabled`, with a setter. A getter without a setter becomes a read-only property, unless it is expensive or has parameters, in which case it stays a method.
- Constants become enums. Style flags become `[Flags]` enums, one per class, such as `ButtonStyle` and `FrameStyle`, holding the styles that wxWidgets documents for that class plus the shared window styles. Standard ids, such as `wxID_OK`, become `int` constants on a static `StandardId` class, such as `StandardId.Ok`. They are not an enum, because ids are plain numbers in wxWidgets, and an application's own ids sit beside the standard ones, so an enum would need a cast everywhere an id is passed.
- Parameters keep their wxWidgets names, converted to camel case, so that named arguments read naturally.

- Names use the American spellings that .NET uses, not the British ones that wxWidgets uses. `wxColour` becomes `Color`, `Centre` becomes `Center`, and `wxEVT_COLOUR_CHANGED` becomes `ColorChanged`. The generator applies a fixed list of these respellings, so a search of the wxWidgets documentation for a WxKit name only needs the British spelling put back. Each type's documentation comment names its wxWidgets class, so the original is always one step away.

Type names that collide with common .NET types, such as `Timer`, `Font`, `Image`, and `Menu`, are kept. The `WxKit` namespace separates them. `Timer` needs care: implicit usings import `System.Threading` into every new project, so a bare `Timer` would be ambiguous there. When a C# project has implicit usings on, WxKit's MSBuild targets add `global using Timer = WxKit.Timer;`. An alias wins over an imported namespace, so the bare name means WxKit's timer. Set the `WxKitTimerAlias` property to `false` to leave the alias out.

## Construction

wxWidgets supports one-step creation, where the constructor creates the native window, and two-step creation, where a default constructor is followed by `Create`. WxKit only exposes one-step creation. Two-step creation exists in C++ mostly for subclassing and for setting extra styles before the window exists, and the director and extra-style support cover those cases.

Constructors use optional parameters with the same defaults as wxWidgets. `wxDefaultPosition` and `wxDefaultSize` become `null`, and the parameter types are `Point?` and `Size?`. The window id defaults to `wxID_ANY`, which most code never needs to change, since WxKit's events are bound to objects rather than ids.

## Value types

Small wxWidgets classes that are values in practice become `readonly record struct` types with the same layout as their shim structs, so they cross the boundary without conversion:

- `Point`, `Size`, and `Rect`.
- `Color`, with its alpha channel.
- `SizerFlags`, which is built on the managed side and only converted to a `wxSizerFlags` when added.

A `wxDateTime`, such as a date picker's value, is a `DateTime?` in local time, and null stands for `wxInvalidDateTime`, no date. A `DateTime` in UTC goes in as the local time it is, and one that comes back is always local. It crosses as its year, month, day, and time of day, so it keeps its milliseconds, but not finer ticks.

A list of positions or items, such as a list box's selections, comes back as an array, such as `int[]` or `TreeItemId[]`, and goes in as any sequence. A member that fills a list, as `wxListBox::GetSelections` does, takes an `out` array and returns the count, as in wxWidgets:

```csharp
int count = list.GetSelections(out int[] selections);
```

## Object lifetime

wxWidgets has three kinds of objects, and each kind gets a different lifetime model in WxKit.

### Windows

wxWidgets owns windows. A child window belongs to its parent and is destroyed with it. A top-level window, such as a frame, is destroyed when it closes, or when you call `Destroy`. You never delete a window directly.

In WxKit, a window object is a proxy for the native window:

- Window classes do not implement `IDisposable`, because disposing a child would conflict with its parent's ownership. To destroy a window, call `Destroy`, or `Close` for a top-level window.
- Dialogs are the exception. wxWidgets recommends creating a modal dialog, showing it, and destroying it when `ShowModal` returns. `Dialog` implements `IDisposable` and calls `Destroy`, so `using MessageDialog dialog = new(...)` does the right thing.
- After the native window is destroyed, every member of its proxy throws `ObjectDisposedException`, except `IsDestroyed`. The shim's object tracking, described in [Native layer](native-layer.md), tells the proxy when this happens.
- A window that is alive keeps its proxy alive. The shim holds a strong `GCHandle` to each proxy, released when the native window is destroyed. This matters when you derive from `Frame` and add your own fields: your object lives exactly as long as the window does, even if nothing on the managed side refers to it.
- There is one proxy per native window. A registry maps native pointers to proxies, so `button.Parent` returns the same `Panel` object you created, not a new wrapper.

When wxWidgets hands WxKit a window it did not create, such as the result of `FindWindowById` or a dialog's own buttons, WxKit creates a proxy of the most specific class it knows. It reads the native object's `wxClassInfo` and walks up its base classes until it finds one that WxKit binds. A generated lookup maps class names to proxy constructors, with no reflection.

### Sizers

A sizer belongs to the window it is set on, or to the sizer it is added to, and is destroyed with it. Setting a window's sizer again destroys the one it had. Sizers are not trackable in wxWidgets, so WxKit tracks them through their owner: when a window is destroyed, its sizer and every sizer inside it are marked as destroyed too. A sizer can only have one owner, and giving it a second throws `InvalidOperationException`.

A `GridBagSizer` places items in cells, at a `GridPosition`, spanning a `GridSpan` of rows and columns. Adding one where an item is already returns false and leaves it out, rather than letting wxWidgets delete a sizer it could not add. A `WrapSizer` wraps its items onto more lines, as text wraps.

A sizer that is never given an owner is freed when its proxy is finalized. The finalizer runs on the finalizer thread, so it posts the free to the GUI thread. Removing or detaching an item from a sizer is not bound yet, and will need the same care.

### Reference-counted objects

Bitmaps, fonts, pens, brushes, images, icons, and cursors are reference counted in wxWidgets, and copying one is cheap. In WxKit, each of these owns a heap copy of the wxWidgets object, which holds one reference to its data, through a `SafeHandle` in the hand-written `NativeObject` base. They implement `IDisposable`.

wxWidgets reference counting is not thread safe, so a reference is only ever released on the GUI thread. `Dispose` on the GUI thread releases at once. A release anywhere else, as by the finalizer, is posted to the GUI thread. That also covers a native call still using the object when the collector finds it: the GUI thread is busy with that call, so the posted release only runs once it returns. Once the application has ended, wxWidgets is gone, and the reference goes with the process.

An object going in can be null, which stands for the class's empty object, as null for a font is `wxNullFont`. Setting a window's font or cursor to null restores its default. An object coming back empty is null rather than an object that cannot be used. An object passed by a reference that can change, such as the bitmap a `MemoryDC` draws into, is the managed object's own copy, so the change shows there.

A parameter that takes a bitmap or icon bundle, which wxWidgets 3.3 uses for every resolution, takes a `Bitmap` or `Icon`, the way C++ converts one.

The images of a tree, list view, or notebook are any sequence of bitmaps, given to `SetImages` or, on a list view, `SetNormalImages` and `SetSmallImages`. An item names its image by its position in the sequence. The control keeps its own bundles, so the bitmaps can be disposed after the call.

### Toolbar tools

Adding a tool to a toolbar returns it as a `ToolBarTool`, and so does finding one, such as with `FindById` or `GetToolByPos`. A toolbar gives back the same object for a tool each time. A tool's enabled and toggled state and its help change through the toolbar, which finds the tool by its id, since that is what shows the change. A tool belongs to its toolbar, which deletes it with `DeleteTool`, `DeleteToolByPos`, or `ClearTools`, or when the toolbar is destroyed, and marks its object destroyed then, as a data view does its columns. A tool is only made by adding it, so the members that make a tool or remove one without deleting it are not bound.

### Item data

A list box, choice, combo box, tree, or data view tree can keep any object with an item, such as what the item stands for, through `Append` and `Insert` with an object, `SetClientObject` and `GetClientObject`, or a tree's `SetItemData` and `GetItemData`. The event a list box, choice, or combo box sends when an item is chosen has its object in `ClientObject`.

The control holds the object through a GC handle, which is freed when the control deletes the item, replaces its object, or is destroyed. Until then, the object stays alive however little else refers to it. Null is no object. A list view's item data is a number, as in wxWidgets, which can be the key of a dictionary of objects.

### Drawing contexts

`DC` and its window, client, paint, memory, and screen classes are objects the application creates, uses, and disposes, which a paint handler needs to do before it returns:

```csharp
panel.Paint += (sender, e) => {
	using PaintDC dc = new(panel);
	dc.DrawLine(0, 0, 50, 50);
};
```

They derive from `NativeObject` too, and their finalizers post the delete to the GUI thread, but a drawing context should not wait for the collector. A context that wxWidgets hands back, such as an erase event's, is not WxKit's to delete, so members that return one are not bound yet.

### Data views

A `DataViewCtrl` shows a model: a list or a tree of items, with a value for each column. A `DataViewListCtrl` has a list of its own to fill, and a `DataViewTreeCtrl` a tree:

```csharp
DataViewListCtrl list = new(panel);
list.AppendTextColumn("Task");
list.AppendToggleColumn("Done");
list.AppendItem(["Buy milk", false]);
```

For data the application has already, a model shows it where it is. `DataViewTreeModel<T>` makes a tree of the application's own objects, and gives each an item id the first time the control asks for it:

```csharp
sealed class Folders : DataViewTreeModel<Folder> {
	protected override object? GetValue(Folder item, int column) => item.Name;
	protected override Folder? GetParent(Folder item) => item.Parent;
	protected override bool IsContainer(Folder item) => item.Children.Count > 0;
	protected override IEnumerable<Folder> GetChildren(Folder? item) => item?.Children ?? _top;
}

view.AppendTextColumn("Name", 0);
view.Model = new Folders(top);
```

A `DataViewVirtualListModel` produces rows as the control shows them, so it can have millions, and `DataViewTreeModel` names its items with ids of its own. A model tells its controls what changed, such as with `ItemAdded` or `RowChanged`, and they ask again. A control keeps its model alive.

A value is an object: a string for a text column, a bool for a toggle column, a number for a progress column, a `DataViewIconText` for an icon and text column, or a `Bitmap`. Numbers come back from wxWidgets as a long or a double, and a value of another type throws `ArgumentException`. An exception in a model goes to `Application.UnhandledException`, and the control gets nothing.

Adding a column returns it as a `DataViewColumn`, whose title, width, alignment, and flags, such as whether it sorts, can change. On Linux, a column's `Width` is the width GTK shows it at, which is 0 until the control is on screen. A control gives back the same object for a column each time, such as from `GetColumn` or `SortingColumn`. A column belongs to its control, which deletes it with `DeleteColumn` or `ClearColumns`, or when the control is destroyed. wxWidgets says nothing when it does, so the control marks the objects of its columns destroyed then, and using one throws `ObjectDisposedException`, as using a destroyed window does.

A data view is drawn by wxWidgets itself on Windows, and describes its rows to screen readers, which hear a row's first column as its name, and the others, with their headings, as its description.

The models are written by hand, as directors that call C#, since their members fill in variants, which a C# member says better with a return value.

### Accessibility

Standard controls are accessible already, since Windows describes them to screen readers itself. A control that draws itself, such as a grid or a game board, is one window to Windows, which cannot see its parts. A class derived from `Accessible` describes it, through Microsoft Active Accessibility:

```csharp
sealed class BoardAccessible : Accessible {
	private readonly Board _board;

	public BoardAccessible(Board board) : base(board) => _board = board;

	protected override string? GetName(int childId) => childId == 0 ? "Board" : _board.CellName(childId);

	protected override AccessibleRole? GetRole(int childId) => childId == 0 ? AccessibleRole.Table : AccessibleRole.Cell;

	protected override int? GetChildCount() => _board.CellCount;

	protected override AccessibleElement? GetFocus() => AccessibleElement.Child(_board.FocusedCell);
}
```

Each member answers for the window, when the id is 0, or for one of its parts, numbered from 1. A member that returns null has no answer, and Windows answers from the window's standard accessible object, so a class only overrides what its control needs. The elements that `GetChild`, `GetFocus`, `HitTest`, and `Navigate` return are an `AccessibleElement`: the window itself, a part, an element with an accessible object of its own, or none, such as for a point outside the window.

A member that throws `ArgumentOutOfRangeException` tells the screen reader that the part does not exist, which is its to handle. Any other exception goes to `Application.UnhandledException`, and the screen reader is told that the question failed. Nothing is swallowed: a missing answer, a missing part, and a bug are each told apart.

Creating the object gives it to the window, which keeps it until the window is destroyed or given another, and `Window.AccessibleObject` is the one it has. When something in the control changes, such as the focus moving to another cell, `Accessible.NotifyEvent` tells screen readers, which then ask again.

`Accessible` is Microsoft Active Accessibility's, as `wxAccessible` is, so on macOS and Linux it does nothing. A control that draws itself can instead describe itself on every platform, through what wxWidgets 3.3.4 added for its own list control and status bar:

- `SetAccessibleParts` gives the window parts that screen readers read as text, each an `AccessiblePart` with a label and its bounds, such as a status bar's fields.
- `SetAccessibleRows` shows the window as a list or table, with the rows that are shown, each an `AccessibleRow` with its index among all the rows, its bounds, its cells, and whether it is selected, and the number of rows in all. A screen reader reads a row's cells together as its name.
- `SetAccessibleCurrentRow` tells screen readers which row is the current one, which they announce as it changes, since they follow the focus but cannot see where it is inside the window.

Each replaces what it set before, so a control calls it whenever its parts or rows change, such as after scrolling. A window that draws itself has no name of its own, so give it one with `SetAccessibleName`, such as its label's, which is what macOS reads. On Windows, a window without one is named by the `StaticText` just before it, as a standard control is. A part of the window that is only read, such as a status strip, is better a `Control` than a `Panel`, with `DisableFocusFromKeyboard`, so Tab does not stop on it: a panel without children takes the focus from the keyboard whatever it is told. On macOS and Linux, wxWidgets gives them to NSAccessibility and ATK, through helpers it keeps private, which may change with a later wxWidgets. On Windows, where those helpers do nothing, an accessible object WxKit gives the window answers Microsoft Active Accessibility from them and tells screen readers what changed, so the window cannot have an `Accessible` of its own as well. Parts are always static text, and rows list items: a control whose parts act, such as buttons, needs an `Accessible`, and does so only on Windows.

```csharp
board.SetAccessibleRows(visibleRows.Select(row => new AccessibleRow(row.Index, row.Bounds, [new AccessiblePart(row.Text, row.Bounds)], row.IsSelected)), allRows.Count);
board.SetAccessibleCurrentRow(currentRow);
```

`wxAccessible` is written by hand, as `CustomValidator` is, since its members answer through a status and output parameters, which a C# member says better with a nullable result. Its enums, such as `AccessibleRole` and `AccessibleStates`, are generated.

### Validators

A validator checks a control's value, and moves data between the control and the application, when a dialog's OK button is pressed or `Window.Validate`, `TransferDataToWindow`, or `TransferDataFromWindow` runs. `TextValidator` is wxWidgets's, with its filters as `TextValidatorStyle`. A validator written in C# derives from `CustomValidator` and overrides the checks it needs:

```csharp
sealed class Required : CustomValidator {
	protected override bool OnValidate(Window window, Window? parent) {
		if (((TextCtrl)window).Value.Length > 0) return true;
		MessageBox.Show("Fill this in first.");
		return false;
	}
}

name.Validator = new Required();
```

A control holds its own copy of the validator, as in wxWidgets, so one validator can serve several controls, and a change to a `TextValidator` after it is set does not reach the control until it is set again. Validators derive from `NativeObject`, though `wxValidator` is an event handler, since WxKit creates and deletes them. A `CustomValidator` stays alive while any control holds a copy of it, and can be collected after that. An exception in one of its checks goes to `Application.UnhandledException`, and the check fails.

### Overriding virtual members

A few wxWidgets classes ask the application for things through virtual members, which a C# class overrides like any other. A virtual list control, for example, asks for each row as it shows it, so it can show a million rows without holding them:

```csharp
sealed class LogView : ListCtrl {
	private readonly List<LogEntry> _entries;

	public LogView(Window parent, List<LogEntry> entries) : base(parent, style: ListCtrlStyle.Report | ListCtrlStyle.Virtual) {
		_entries = entries;
		InsertColumn(0, "Time");
		InsertColumn(1, "Message");
		SetItemCount(entries.Count);
	}

	protected override string OnGetItemText(long item, long column) => column == 0 ? _entries[(int)item].Time : _entries[(int)item].Message;
}
```

A member that is not overridden does what wxWidgets does. Only the members a binding tweak lists can be overridden, and only on objects WxKit creates, not ones wxWidgets creates itself. An exception in an override goes to `Application.UnhandledException`, and wxWidgets gets an empty result. [The native layer](native-layer.md) describes how the calls reach C#.

## Opting in to features

WxKit has no switches for optional features, as wxDragon has Cargo features. Native AOT only compiles the methods an application can reach, so a shim function nothing calls is never referenced, and the linker leaves out its object file and the libraries behind it. Opting in is calling the API.

Each class works this way. A linker loads an object file of a static library whole, along with every wxWidgets object file any of its functions refers to, even functions it then leaves out. So the generated shim has a file for each class, and an application only links the wxWidgets code of the classes it uses. The Hello sample, which uses no list, tree, or data view, is about 5.2 MB. Each event type is made the first time it is used, so an event type an application never uses references nothing either.

Image file formats work this way too. An image can always load and save BMP files, and an application adds the others it uses, such as with `Image.AddPngHandler()`, before it loads images. Each format is a function in a file of its own in the shim, so only the formats an application adds are linked. `Image.AddAllHandlers()` adds every format, which makes a published application about 1.1 MB larger, mostly for JPEG, TIFF, and WebP. PNG costs nothing extra, since wxMSW uses it itself.

Without Native AOT, the shared library has every format in it either way, and the calls only decide which ones images can use.

## The clipboard

`Clipboard` is a static class with a getter, a setter, and a check for each format: text, bitmaps, lists of files, HTML, and formats the application names, which hold bytes:

```csharp
Clipboard.SetFiles([path]);
if (Clipboard.GetData("MyApp.Shape") is byte[] shape) Paste(shape);
```

Each setter replaces everything on the clipboard. To offer one thing in several formats, so that each application takes the one it understands, set a `ClipboardContent`:

```csharp
Clipboard.SetContent(new ClipboardContent { Text = "Bold text", Html = "<b>Bold</b> text" });
```

Each call opens the clipboard and closes it again, so another application can use it in between, and a setter returns false when another application has it open. A getter returns null when the clipboard does not have its format. On Windows, what an application puts on the clipboard goes when the application exits, unless it calls `Clipboard.Flush`.

## The About box

`AboutBox.Show` shows the platform's About box with what an `AboutDialogInfo` gives: the name, version, description, copyright, license, web site, icon, and the people who made the application. It's written by hand, since `wxAboutDialogInfo` is not a `wxObject` and `wxAboutBox` is a function. On Windows and macOS, a license, or anything else the platform's box can't show, makes wxWidgets show its own instead. On Windows, the box is modal, and `Show` returns once it's closed. On Linux and macOS, `Show` returns at once.

```csharp
AboutBox.Show(new AboutDialogInfo {
	Version = "1.0",
	Description = "A to-do list.",
	Developers = { "Ada" },
}, frame);
```

## Notifications

A `NotificationMessage` shows a notification outside the application's windows, such as a toast on Windows, with an icon from `MessageIcon`. It raises `Clicked` when it's clicked, `Dismissed` when it goes away, and `ActionSelected` when one of the buttons `AddAction` adds is pressed. wxWidgets doesn't free a notification on its own, so dispose of it once it's done with, as you do a `Timer`. On Windows, call `NotificationMessage.MSWUseToasts()` before the first one, to show it as a toast rather than a balloon from the taskbar. On macOS, notifications only show from an app bundle.

## Printing

A document prints a page at a time, from a class derived from `Printout`, which draws each page on the DC it is given and says how many pages there are:

```csharp
sealed class TextPrintout(string[] lines) : Printout("My document") {
	private const int LinesPerPage = 50;

	protected override PageRange GetPageInfo() => PageRange.All((lines.Length + LinesPerPage - 1) / LinesPerPage);

	protected override bool OnPrintPage(int page, DC dc) {
		foreach ((string line, int index) in lines.Skip((page - 1) * LinesPerPage).Take(LinesPerPage).Select((line, index) => (line, index))) dc.DrawText(line, 0, index * dc.CharHeight);
		return true;
	}
}

using TextPrintout printout = new(lines);
using Printer printer = new();
printer.Print(frame, printout);
```

`Printer.Print` shows the print dialog first, unless told not to. `PrintPreview.Show` shows a preview in a frame of its own, from one printout to show and, for its Print button, a second of the same document. `PrintDialog` and `PageSetupDialog` show the dialogs on their own, and `PrintData`, `PrintDialogData`, and `PageSetupDialogData` are what they show, which are copied like values.

A printout's virtual members are written by hand, since `GetPageInfo` answers through pointers, and the shim's subclass of `wxPrintout` calls them. `wxPrintout::OnBeginDocument` and `OnEndDocument` start and end the document on the DC, so they run around a printout's own. The DC a page is drawn on is wxWidgets's, which `OnPrintPage` is only lent until it returns, after which using it throws `ObjectDisposedException`. A printout the application prints is the application's to dispose. One given to a preview is the preview's, which deletes it when the preview closes, after which its `IsDisposed` is true.

An application that does not print links none of it, since Native AOT only links the parts of wxWidgets an application uses.

## Taskbar icons

A `TaskBarIcon` shows an icon in the Windows notification area, the macOS menu bar, or a Linux tray, once `SetIcon` gives it one. Its `Menu` is shown when it's right-clicked, or on macOS clicked, and it raises `LeftDoubleClick`, `RightUp`, and the other mouse events where the platform sends them. The menu comes from the shim's own subclass, which returns it to wxWidgets, so it needs no callback into .NET. wxWidgets keeps the application running while an icon exists, so dispose of it when the application is done with it.

```csharp
TaskBarIcon tray = new();
tray.SetIcon(icon, "Backing up");
Menu menu = new();
menu.Append(StandardId.Exit).Click += (_, _) => tray.Dispose();
tray.Menu = menu;
```

## Dropped files

A window that calls `DragAcceptFiles(true)` raises `DropFiles` when files are dropped on it, with their paths in the event's `Files`. A text control that handles it opens nothing on its own, so a handler decides what a dropped file does, as the Editor sample opens it. Dragging other data, and dragging from the application, are not bound yet.

## Announcements

A `LiveRegion` is an empty label that a window has screen readers speak through, for news that has no control of its own to land on, such as a status that changed. `Announce` takes the message and a priority, as in `region.Announce("Preset applied: Blog")`, and returns whether a screen reader could be told. It follows the [live-region](https://github.com/accessware/live-region) crate for wxDragon, which was tested against NVDA, VoiceOver, and Orca:

- On Windows, it raises a UI Automation notification, which a screen reader speaks without moving the focus. Raising one needs a provider of the window's own: the one `UiaHostProviderFromHwnd` gives raises it to no client, and says nothing. So the label's window is subclassed to answer `WM_GETOBJECT` with a provider that leaves every property to the host provider. The priorities are the notification's processing: low is `All`, which queues, medium is `CurrentThenMostRecent`, and high is `ImportantMostRecent`, which interrupts. `Announce` returns false while no UI Automation client is listening.
- On macOS, it posts VoiceOver an `AXAnnouncementRequested`, with the priority, on the next pass of the run loop, so speech the current pass makes does not drown it out. VoiceOver queues announcements unreliably, which the `AnnouncementPriority` documentation describes.
- On Linux, it emits ATK's `notification` signal, polite for low and medium and assertive for high, or before ATK 2.50 its `announcement` signal, which has no priority.

Each message is made one line of at most 512 characters, without control characters, and an empty one is not announced. A UI Automation client, such as one built with .NET's `System.Windows.Automation` listening for `AutomationElement.NotificationEvent`, hears each announcement with its text and processing, which is how the Windows backend was checked. WxKit's own tests can't do that, since they target `net10.0` rather than the Windows desktop runtime that client needs.

## One running copy

`SingleInstance.Claim` keeps an application to one running copy, and hands what a second copy was started with, such as files to open, to the first, as the [doorbell](https://github.com/accessware/doorbell) crate does for Rust. Call it at the start of `Main`, before `Application.Run`:

```csharp
string[] files = [.. args.Select(Path.GetFullPath)];
using SingleInstance? instance = SingleInstance.Claim("myapp", files);
// Another copy has the files, so this one is done.
if (instance is null) return 0;
return Application.Run(() => {
	MainFrame frame = new();
	instance.MessageReceived += (_, e) => {
		if (e.Message.Count == 0) {
			if (frame.IsIconized) frame.Iconize(false);
			frame.Raise();
		}
		foreach (string file in e.Message) frame.Open(file);
	};
	frame.Show();
});
```

`MessageReceived` is raised on the GUI thread, and messages that come before a handler is added wait for one. It builds on `SingleInstanceChecker`, which wraps `wxSingleInstanceChecker`: a named mutex on Windows, and a lock file with its process's ID on Linux and macOS, so a copy that crashed never blocks the next one. On Linux and macOS, wxWidgets counts a lock its own process holds as no other copy, so two checkers in one process do not see each other. The copies talk over a named pipe that only the same user can open on Windows, and a Unix socket in a folder only the user can open elsewhere: `$XDG_RUNTIME_DIR` where there is one, and a folder of WxKit's own in the temporary folder otherwise. A message is a list of strings, up to 1 MB, and a second copy waits a few seconds at most for the first to take it. On Windows, the second copy lets the first bring its window to the front, which Windows otherwise refuses to a process the user is not using.

## Simulated input

`UIActionSimulator` sends mouse and keyboard input as if the user had given it, for an application's own tests of its user interface: `MouseMove`, `MouseClick`, `Char`, such as `Char((KeyCode)'S', KeyModifiers.Control)`, `Text`, and the rest. It wraps `wxUIActionSimulator`, which is not a `wxObject`, so it is written by hand. The input goes where the system sends real input, which must be a window that is shown and active, and arrives as the event loop runs. On macOS, the application needs permission to control the computer.

## Static members

A static member of a wxWidgets class is a static member of its managed class, such as `Window.FindFocus()`. A class with only static members, such as `wxSystemSettings`, is a static class. Static members check the thread first, as instance members do through their handle.

## Events

Each class exposes the events that wxWidgets documents for it as standard .NET events:

```csharp
button.Click += OnClick;
frame.Closing += (sender, e) => {
	if (HasUnsavedChanges && e.CanVeto) e.Veto();
};
textCtrl.TextChanged += (sender, e) => UpdateTitle();
```

Each event uses `EventHandler<T>`, where `T` is the WxKit class for the wxWidgets event, such as `CommandEvent` or `CloseEvent`. These classes derive from `EventArgs`, which keeps the pattern familiar and lets Visual Basic's `Handles` clause work.

The first `+=` for an event type and id range binds one functor in the shim, and later handlers for the same event are added to that functor's list on the managed side. `-=` removes a handler from the list. The functor itself is never unbound: once its list is empty, it skips each event, which leaves wxWidgets to process it as if nothing were bound. The functor is released, and the list with it, when wxWidgets destroys the object. This avoids having to match functors when unbinding, and costs at most one idle functor per event a program ever handled. Event names come from the wxWidgets event macros, converted to .NET style. `EVT_BUTTON` becomes `Click`, `EVT_CLOSE_WINDOW` becomes `Closing`, and so on. A binding tweak sets the name when the conversion reads badly.

For the rare event that a class does not expose, or for an event type the application defines itself, there is a generic method:

```csharp
frame.Bind(EventType.Menu, StandardId.Exit, (sender, e) => frame.Close());
```

### Event objects

A wxWidgets event object only exists while its handlers run. WxKit's event objects follow the same rule: once the handler returns, any member of the event throws `ObjectDisposedException`. Copy what you need out of an event before you start async work.

By default, a handler consumes its event, as in wxWidgets. Call `e.Skip()` to let wxWidgets keep processing it, which some events need. For example, a key-down handler that does not call `Skip` stops the text control from receiving the key.

The close event is the exception. In wxWidgets, a window only closes if its close handler destroys it or skips the event, which is easy to forget. In WxKit, `Closing` works like `FormClosing` in Windows Forms: the window closes once the handlers return, unless one of them calls `e.Veto()`.

A menu item's `Click` is bound on the item's menu, with the item's id. wxWidgets sends a menu event to the menu first, so the handler runs whether or not the menu is on a frame's menu bar, and the sender is the `MenuItem`.

### Exceptions in handlers

An exception cannot unwind through wxWidgets, so WxKit catches every exception a handler throws and raises `Application.UnhandledException`, whose arguments are an `ApplicationExceptionEventArgs`. The same goes for actions posted with `Application.Post` and for `await` continuations. A handler of that event can mark the exception handled, and the application keeps running. If nothing does, WxKit exits the event loop and rethrows the exception from `Application.Run`, with its original stack trace. This keeps an exception from being lost and keeps a broken application from limping on.

## Threading

wxWidgets is single-threaded. Every window must be created and used on the thread that runs the event loop.

- Every member of a window proxy checks that it is on the GUI thread, and throws `InvalidOperationException` if not. The check is one comparison, which costs little next to a native call.
- `Application.Post(Action)` runs an action on the GUI thread later, using `wxApp::CallAfter`. It is safe to call from any thread.
- `Application.Run` installs a `SynchronizationContext` for the GUI thread, so `await` in an event handler resumes on the GUI thread, as it does in Windows Forms and WPF.

On macOS, the GUI thread must be the process's main thread, so `Application.Run` runs the event loop on the thread that calls it, and throws `InvalidOperationException` when that is not the main thread. `Main` starts on the main thread, so calling `Run` from it works.

On Windows, the GUI thread must be a single-threaded apartment, because OLE only works in one, and the clipboard, drag and drop, and accessibility use OLE. .NET starts `Main` in the multithreaded apartment unless it is marked `[STAThread]`, which top-level statements cannot be, and a thread's apartment cannot change once it is set. If wxWidgets runs on such a thread, it fails to start OLE without saying so. So when `Application.Run` is called from a thread that is not a single-threaded apartment, it starts one, runs the event loop there, and waits for it to end. A program whose `Main` is marked `[STAThread]` runs the event loop on its own main thread.

## The application object

`Application.Run` creates the shim's `wxApp` subclass, runs your startup code inside `OnInit`, and runs the event loop until the last top-level window closes. It returns the exit code.

For applications that need more, such as handling files opened from the macOS Finder or customizing `OnExit`, you can derive from `Application` and override its virtual methods:

```csharp
return new MyApp().Run();

sealed class MyApp : Application {
	protected override bool OnInit() {
		new MainFrame().Show();
		return true;
	}

	protected override void MacOpenFiles(IReadOnlyList<string> paths) { ... }
}
```

## Nullability

Every public API carries nullable annotations. The wxWidgets documentation rarely says whether a pointer can be null, so the generator assumes it cannot, and binding tweaks mark the exceptions, such as parent windows and `FindWindow` results.

## Documentation comments

The wxWidgets interface headers contain the reference documentation for every class and method. The generator turns the brief description of each one into an XML documentation comment, so IntelliSense shows it, with C++ names in the text rewritten to their WxKit names. Each type's comment also names its wxWidgets class and links to its page in the wxWidgets reference. Detailed descriptions stay in the wxWidgets reference, because they refer to C++ names and macros throughout.

## The raw interop layer

`WxKit.Interop.WxKitNative` declares every shim function, one to one, and it is public. It is an escape hatch for anything the managed API does not wrap yet. Code that uses it is unsafe and gives up the lifetime and threading checks, so the managed API is always the first choice. The shim can still change between preview releases, and code that calls it directly may need updating when it does.
