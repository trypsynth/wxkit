# Managed API

This document describes the C# API that applications use: how wxWidgets names map to .NET names, who owns which objects, how events and threads work, and how the application starts. [Getting started](../getting-started.md) walks through a first application, and `tests/WxKit.Tests/PublicApi.txt` lists the whole public API.

## Naming

WxKit uses the wxWidgets names, adjusted to .NET conventions. [Generator](generator.md#rules-for-names-and-events) lists the exact rules.

- Types drop the `wx` prefix, so `wxFrame` becomes `Frame`. Methods keep their names, which wxWidgets already writes in Pascal case.
- Names use .NET's American spellings, not wxWidgets's British ones: `wxColour` becomes `Color`, `Centre` becomes `Center`, and `wxEVT_COLOUR_CHANGED` becomes `ColorChanged`. Each type's documentation comment names its wxWidgets class.
- A getter and setter pair becomes a property, so `GetLabel` and `SetLabel` become `Label`, and `IsEnabled` and `Enable` become `IsEnabled`, with a setter.
- Style flags become a `[Flags]` enum for each class, such as `ButtonStyle`, with the styles wxWidgets documents for that class. A class without styles of its own takes `WindowStyle`.
- Other constants become enums. Standard ids, such as `wxID_OK`, are `int` constants on the static class `StandardId`, such as `StandardId.Ok`, because an application's own ids are plain numbers beside them.
- Parameters keep their wxWidgets names in camel case, so named arguments read naturally.
- A static member stays static, such as `Window.FindFocus()`, and a class with only static members, such as `SystemSettings`, is a static class.

Type names that collide with common .NET types, such as `Timer`, `Font`, `Image`, and `Menu`, are kept, and the `WxKit` namespace separates them. Implicit usings import `System.Threading`, which makes a bare `Timer` ambiguous, so when a C# project has implicit usings on, WxKit's MSBuild targets add `global using Timer = WxKit.Timer;`. Set the `WxKitTimerAlias` property to `false` to leave the alias out.

## Construction and value types

WxKit only exposes wxWidgets's one-step creation, where the constructor creates the native window. A class that needs to change what wxWidgets does overrides its virtual members instead. Constructors take optional parameters with the same defaults as wxWidgets. The position and size are `Point?` and `Size?`, where null is `wxDefaultPosition` and `wxDefaultSize`, and the id defaults to `StandardId.Any`, which most code keeps, since events are bound to objects rather than ids.

`Point`, `Size`, `Rect`, and `Color` are `readonly record struct` types laid out like their shim structs, so they cross the boundary without conversion. `SizerFlags` is a `readonly struct` whose methods return a changed copy, as in `new SizerFlags(1).Expand().Border()`, and it becomes a `wxSizerFlags` when an item is added.

A `wxDateTime` is a `DateTime?` in local time, and null is `wxInvalidDateTime`. A UTC time goes in as the local time it is, and it keeps milliseconds but not finer ticks. A list of positions or items, such as selections, comes back as an array, such as `int[]` or `TreeItemId[]`, and goes in as any sequence. A member that fills a list, as `wxListBox::GetSelections` does, returns it, and a getter of one is a property, as in `list.Selections`.

## Object lifetime

### Windows

wxWidgets owns windows. A child is destroyed with its parent, and a top-level window when it closes or when `Destroy` is called. So windows don't implement `IDisposable`. Call `Destroy`, or `Close` for a top-level window. `Dialog` is the exception: it implements `IDisposable` and calls `Destroy`, so `using MessageDialog dialog = new(...)` creates, shows, and destroys a modal dialog the way wxWidgets recommends.

A window object is a proxy for the native window. Once the native window is destroyed, every member of its proxy throws `ObjectDisposedException`, except `IsDestroyed`, as [Native layer](native-layer.md#object-tracking) describes. A registry maps each native window to its one proxy, so `button.Parent` returns the same `Panel` object you created. It holds the proxy strongly until the window is destroyed, so a class derived from `Frame` keeps its fields exactly as long as the window lives. For a window WxKit didn't create, such as the result of `FindWindowById` or a dialog's own buttons, WxKit walks up the native object's `wxClassInfo` to the nearest class it binds, and creates that proxy from a generated table, without reflection.

### Sizers

A sizer belongs to the window it is set on or the sizer it is added to, and is destroyed with it. Setting a window's sizer again destroys the old one. Sizers aren't trackable in wxWidgets, so when a window is destroyed, WxKit marks its sizer and every sizer inside it destroyed. A sizer has one owner, and giving it a second throws `InvalidOperationException`. A sizer that never gets an owner is freed when its proxy is finalized, which posts the free to the GUI thread. Removing or detaching a sizer's items is not bound yet.

A `GridBagSizer` places items at a `GridPosition`, spanning a `GridSpan`, and adding an item where one already is returns false and leaves it out. A `WrapSizer` wraps its items onto more lines.

### Reference-counted objects and drawing contexts

Bitmaps, fonts, pens, brushes, images, icons, and cursors are reference counted in wxWidgets. Each WxKit object holds one reference, through a `SafeHandle` in the hand-written `NativeObject` base class, and implements `IDisposable`. wxWidgets reference counting isn't thread safe, so `Dispose` on the GUI thread releases at once, and a release anywhere else, such as from the finalizer, is posted to the GUI thread.

Null going in is the class's empty object, such as `wxNullFont`, and setting a window's font or cursor to null restores its default. An empty object coming back is null. An object that wxWidgets changes in place, such as the bitmap a `MemoryDC` draws into, is the managed object's own copy, so the change shows there. A parameter that takes a bitmap or icon bundle takes a `Bitmap` or `Icon`. The images of a tree, list view, or notebook are any sequence of bitmaps, given to `SetImages`, or a list view's `SetNormalImages` and `SetSmallImages`, and an item names its image by its position. The control keeps its own copies.

`DC` and its window, client, paint, memory, and screen classes derive from `NativeObject` too. Dispose of one before the handler that made it returns, as in `using ClientDC dc = new(panel);`, rather than waiting for the collector. A `Paint` handler makes none: it paints on the event's `DC`, a `PaintDC` the event makes when first asked for it and disposes when the handlers return. A context that wxWidgets hands out, such as an erase event's, isn't WxKit's to delete, so members that return one aren't bound yet.

### Objects a control owns

A toolbar's `ToolBarTool` objects and a data view's `DataViewColumn` objects work the same way. Adding one returns it, and finding one, such as with `FindById`, `GetToolByPosition`, `GetColumn`, or `SortingColumn`, returns the same object each time. The control deletes it with `DeleteTool`, `DeleteToolByPosition`, `ClearTools`, `DeleteColumn`, or `ClearColumns`, or when the control is destroyed, and then marks it destroyed, so using it throws `ObjectDisposedException`.

A tool's enabled and toggled state and its help change through the toolbar, by the tool's id. Members that make a tool, or remove one without deleting it, aren't bound. A column's title, width, alignment, and flags can change. On Linux, a column's `Width` is 0 until the control is on screen.

### Item data

A list box, choice, combo box, tree, or data view tree can keep any object with an item, through `Append` and `Insert` with an object, `SetClientObject` and `GetClientObject`, or a tree's `SetItemData` and `GetItemData`. The event a list box, choice, or combo box sends when an item is chosen has it in `ClientObject`. The control holds the object through a GC handle until it deletes the item, replaces the object, or is destroyed. A list view's item data is a number, as in wxWidgets, which can key a dictionary.

## Menus

`Menu.Append` returns a `MenuItem`, whose `Enabled`, `Checked`, and `Label` change the item through its menu. An item with a standard id, such as `StandardId.Exit`, and no text gets the platform's standard text, and on macOS goes where the platform puts it. A tab in the text starts the item's shortcut, as in `"&Open\tCtrl+O"`. `AppendSubMenu` adds a submenu, which the menu then owns. A menu bar owns the menus appended to it, and a menu that is never appended isn't freed yet.

A menu item's `Click` is bound on its menu, with its id. wxWidgets sends a menu event to the menu first, so the handler runs whether or not the menu is on a menu bar, and the sender is the `MenuItem`.

## Data views and other overrides

A `DataViewListCtrl` has a list of its own to fill with `AppendItem`, and a `DataViewTreeCtrl` a tree. A `DataViewCtrl` shows a model instead, for data the application already has. `DataViewTreeModel<T>` makes a tree of the application's own objects:

```csharp
sealed class Folders(IReadOnlyList<Folder> top) : DataViewTreeModel<Folder> {
	protected override object? GetValue(Folder item, int column) => item.Name;
	protected override Folder? GetParent(Folder item) => item.Parent;
	protected override bool IsContainer(Folder item) => item.Children.Count > 0;
	protected override IEnumerable<Folder> GetChildren(Folder? item) => item?.Children ?? top;
}

view.AppendTextColumn("Name", 0);
view.Model = new Folders(top);
```

A `DataViewVirtualListModel` produces rows as the control shows them, so it can have millions, and the non-generic `DataViewTreeModel` names its items with ids of its own. A model tells its controls what changed, such as with `NotifyItemAdded` or `NotifyRowChanged`, and they ask again. A control keeps its model alive. A value is a string for a text column, a bool for a toggle column, a number for a progress column, a `DataViewIconText`, or a `Bitmap`. Numbers come back as a long or a double, and a value of another type throws `ArgumentException`. On Windows, wxWidgets draws a data view itself, and screen readers hear a row's first column as its name and the other columns, with their headings, as its description.

Other wxWidgets classes ask the application for things through virtual members the same way. A virtual list control, a `ListCtrl` with `ListCtrlStyle.Virtual`, overrides `OnGetItemText` to give each row as it is shown. A validator derives from `CustomValidator` and overrides `OnValidate`, `OnTransferToWindow`, or `OnTransferFromWindow`, which run when a dialog's OK button is pressed or `Validate`, `TransferDataToWindow`, or `TransferDataFromWindow` is called. A control holds its own copy of a validator, so one can serve several controls, and a `CustomValidator` stays alive while any control holds a copy, and a change to a `TextValidator` doesn't reach a control until it is set again.

A member that isn't overridden does what wxWidgets does. Only members a binding tweak lists can be overridden, and only on objects WxKit creates. An exception in an override goes to `Application.UnhandledException`, and wxWidgets gets an empty or failed result. [Native layer](native-layer.md#overriding-virtual-methods) describes the directors that make the calls, including the hand-written ones behind `Accessible`, `CustomValidator`, the data view models, `Printout`, and `TaskBarIcon`.

## Accessibility

Standard controls are accessible already, and a control is named by the `StaticText` just before it, as Windows has screen readers read it. On Linux, wxGTK gives a label's control its name only through the outer widget of a control made of more than one, such as a list box's scrolled window or a slider's box, so WxKit adds the relation to the widget that gets the focus, which Orca reads.

A button that shows only an icon, such as a `BitmapButton`, is named by its tooltip, so set one with `SetToolTip`. Windows does that itself, and WxKit does it on Linux and macOS. To give a window another name, call `SetAccessibleName`, which a tooltip set later leaves alone. wxWidgets only implements it on Windows, so on Linux and macOS WxKit sets the name through ATK and NSAccessibility.

`AccessibilityInfo.Of(window)` reads a window from the platform's accessibility API, as a screen reader does: its role, its name, the platform's own role, and whether it is enabled, focused, or checked. It asks MSAA on Windows, NSAccessibility on macOS, and ATK on Linux, so an application's own tests can check that each control has the role and the name a user needs, on every platform. On Linux, wxGTK names controls by their labels when it next goes idle, so a test lets the event loop run first.

A control that draws itself, such as a grid or a game board, is one window to the platform, which can't see its parts. WxKit has two ways to describe it.

A class derived from `Accessible` describes it through Microsoft Active Accessibility, so it only works on Windows:

```csharp
sealed class BoardAccessible(Board board) : Accessible(board) {
	protected override string? GetName(int childId) => childId == 0 ? "Board" : board.CellName(childId);
	protected override AccessibleRole? GetRole(int childId) => childId == 0 ? AccessibleRole.Table : AccessibleRole.Cell;
	protected override int? GetChildCount() => board.CellCount;
	protected override AccessibleElement? GetFocus() => AccessibleElement.Child(board.FocusedCell);
}
```

Each member answers for the window when the id is 0, or for a part, numbered from 1. Null means no answer, and Windows answers from the window's standard accessible object. `GetChild`, `GetFocus`, `HitTest`, and `Navigate` return an `AccessibleElement`: `Self`, `Child` for a part, `Of` for an element with an accessible object of its own, or `None`. Throwing `ArgumentOutOfRangeException` tells the screen reader the part doesn't exist, and any other exception goes to `Application.UnhandledException` and tells it the question failed. Creating the object gives it to the window, which keeps it until it is destroyed or given another, and `Window.Accessible` is the one it has. Call `Accessible.NotifyEvent` when something changes, such as the focus moving to another cell.

Window members, which wxWidgets 3.3.4 added for its own list control and status bar, describe the control on every platform:

- `SetAccessibleParts` gives the window parts that screen readers read as text, each an `AccessiblePart` with a label and bounds, such as a status bar's fields.
- `SetAccessibleRows` shows the window as a list or table, from the rows that are shown, each an `AccessibleRow` with its index among all the rows, its bounds, its cells, and whether it's selected, and the total number of rows. A screen reader reads a row's cells together as its name.
- `SetAccessibleCurrentRow` says which row is current, which screen readers announce as it changes.

Each call replaces what was set before, so call it again whenever the parts or rows change, such as after scrolling. Give the window a name with `SetAccessibleName`, which is what macOS reads. On Windows, a window without one is named by the `StaticText` just before it. A read-only part, such as a status strip, is better a `Control` with `DisableFocusFromKeyboard` than a `Panel`, since a panel without children takes the focus from the keyboard regardless.

On macOS and Linux, wxWidgets passes parts and rows to NSAccessibility and ATK through private helpers, which a later wxWidgets may change. On Windows, WxKit gives the window an accessible object that answers from them, so the window can't also have an `Accessible`. Parts are always static text and rows are list items, so a control whose parts act, such as buttons, needs an `Accessible`, and only gets one on Windows.

## Announcements

A `LiveRegion` is an empty label that a window has screen readers speak through, for news that has no control to land on, such as a changed status. `region.Announce("Preset applied: Blog")` speaks a message with an `AnnouncementPriority`, high by default, and returns whether a screen reader could be told. It follows the [live-region](https://github.com/accessware/live-region) crate for wxDragon, which was tested with NVDA, VoiceOver, and Orca.

- On Windows, it raises a UI Automation notification, which a screen reader speaks without moving the focus. The window's default provider raises it to no client, so WxKit subclasses the label's window to answer `WM_GETOBJECT` with a provider of its own that leaves every property to the host provider. Low priority is the processing `All`, which queues, medium is `CurrentThenMostRecent`, and high is `ImportantMostRecent`, which interrupts. `Announce` returns false while no UI Automation client is listening.
- On macOS, it posts VoiceOver an `AXAnnouncementRequested` with the priority on the next pass of the run loop, so speech from the current pass doesn't drown it out. VoiceOver queues announcements unreliably, as the `AnnouncementPriority` documentation describes.
- On Linux, it emits ATK's `notification` signal, polite for low and medium and assertive for high, or before ATK 2.50 its `announcement` signal, which has no priority.

A message is made one line of at most 512 characters without control characters, and an empty one isn't announced. `Prepare` sets the region up early and returns whether announcements can work.

## Linking only what is used

WxKit has no switches for optional features. Native AOT compiles only the methods an application can reach, and the shim has a file for each class, as [Native layer](native-layer.md#linking-only-what-an-application-uses) explains, so the linker leaves out the wxWidgets code of classes an application doesn't use, such as printing in one that doesn't print. Each event type is created the first time it's used. The Hello sample publishes as one executable of about 6 MB.

Image file formats work the same way. Images can always load and save BMP, and an application adds the other formats it uses before it loads images, such as with `Image.AddPngHandler()`. `Image.AddAllHandlers()` adds every format, which makes a published application about 1.1 MB larger, mostly for JPEG, TIFF, and WebP. PNG costs nothing extra, since wxMSW uses it itself. Without Native AOT, the shared library has every format, and the calls only decide which ones images can use. `Image.LoadFile` and `SaveFile` also take a `Stream`, for an image in memory or in a resource.

## The clipboard

`Clipboard` is a static class with a getter, a setter, and a check for each format: text, bitmaps, lists of files, HTML, and formats the application names, which hold bytes, as in `Clipboard.GetData("MyApp.Shape")`. Each setter replaces everything on the clipboard. To offer one thing in several formats, set a `ClipboardContent`, as in `Clipboard.SetContent(new ClipboardContent { Text = "Bold text", Html = "<b>Bold</b> text" })`. Each call opens and closes the clipboard, and a setter returns false when another application has it open. A getter returns null when the clipboard doesn't have its format. On Windows, what an application put on the clipboard goes when it exits, unless it calls `Clipboard.Flush`.

## Drag and drop

`DragDrop.DoDragDrop(source, content, flags)` drags a `ClipboardContent`, the same content the clipboard takes, so each target takes a format it understands. Call it when the mouse moves with a button held, such as from a list's `BeginDrag`. It returns once the content is dropped or the drag is cancelled, with what the target did. When that is `DragResult.Move`, which `DragFlags.AllowMove` permits, remove what was dragged from the source.

A `DropTarget` makes a window take content dropped on it, in the formats it names: `DataFormat.Text`, `Html`, `Bitmap`, `Files`, or `DataFormat.Named("MyApp.Shape")`. Content that has several arrives in the first of these the target takes. Its events are:

- `DragEnter` and `DragOver`, as a drag comes in and moves over the window, with its `Position`, the `Format` it would arrive in, and the `Effect` to show, which a handler sets to `DragResult.None` to refuse it.
- `DragLeave`, when it leaves without a drop.
- `Drop`, with the content as `Text`, `Html`, `Bitmap`, `Files`, or the named format's `Data`, and the `Effect` that says what was done with it.

The window keeps the target until it is destroyed, given another, or `Remove` takes it away. A window that only takes files can use `DragAcceptFiles` and its `DropFiles` event instead. Dragging needs a mouse, so give every drag a keyboard equivalent, such as Cut and Paste, or Move Up and Move Down.

## Composite dialogs and controls

wxWidgets makes some dialogs and controls out of standard controls, which screen readers already read:

- `NumberEntryDialog` asks for a number, and `CredentialEntryDialog` for a `UserName` and a `Password`.
- `RichMessageDialog` is a message box with a check box, such as "Don't ask again", from `ShowCheckBox`, details that can be shown, from `ShowDetailedText`, and a footer. It isn't a `MessageDialog`, since wxWidgets makes it from its own message dialog rather than the platform's, but it has the same members.
- `PropertySheetDialog` is a dialog with a notebook, `BookCtrl`, and the buttons `CreateButtons` makes.
- `Wizard` shows `WizardPageSimple` pages in turn, chained with `WizardPageSimple.Chain`. Call `FitToPage` with the first page to size the wizard for the largest, then `RunWizard`.
- `TreeListCtrl` is a tree with columns, whose items are `TreeListItem` values, and which can have check boxes.
- `GenericDirCtrl` shows the file system as a tree, and `FileCtrl` is a file chooser to put in a window. On Linux, `FileCtrl` is GTK's own, and its `Paths` are one empty path when nothing is chosen.
- `EditableListBox` is a list of strings, `Strings`, with buttons to add, edit, delete, and move them, which screen readers name by their tooltips.

## The About box

`AboutBox.Show` shows the platform's About box from an `AboutDialogInfo`: the name, version, description, copyright, license, web site, icon, and the lists of people, such as `Developers`, to add to. On Windows and macOS, a license, or anything else the platform's box can't show, makes wxWidgets show its own instead. On Windows, the box is modal, and `Show` returns once it's closed. On Linux and macOS, `Show` returns at once.

## Notifications and taskbar icons

A `NotificationMessage` shows a notification outside the application's windows, such as a toast, with an icon from `MessageIcon`. It raises `Clicked`, `Dismissed`, and `ActionSelected`, for the buttons `AddAction` adds. Dispose of it when it's done with, since wxWidgets doesn't free it. On Windows, call `NotificationMessage.MswUseToasts()` before the first one to show toasts rather than taskbar balloons. On macOS, notifications only show from an app bundle.

A `TaskBarIcon` shows an icon in the Windows notification area, the macOS menu bar, or a Linux tray, once `SetIcon` gives it one. Its `Menu` shows when it's right-clicked, or clicked on macOS, and it raises `LeftDoubleClick`, `RightUp`, and the other mouse events where the platform sends them. wxWidgets keeps the application running while an icon exists, so dispose of it when done.

## Printing

A document prints from a class derived from `Printout`. Its `GetPageInfo` returns the pages, such as `PageRange.All(pageCount)`, and its `OnPrintPage(int page, DC dc)` draws a page and returns true. The DC is lent to `OnPrintPage` until it returns, after which using it throws `ObjectDisposedException`. wxWidgets's `OnBeginDocument` and `OnEndDocument` run around a printout's own overrides.

`Printer.Print(parent, printout)` prints, showing the print dialog first unless `prompt` is false. `PrintPreview.Show` shows a preview in a frame of its own, from one printout to show and, for its Print button, a second of the same document. `PrintDialog` and `PageSetupDialog` show the dialogs on their own, and `PrintData`, `PrintDialogData`, and `PageSetupDialogData` are copied like values. A printout the application prints is the application's to dispose. One given to a preview belongs to the preview, which deletes it when it closes, after which its `IsDisposed` is true.

## Dropped files

A window that calls `DragAcceptFiles(true)` raises `DropFiles` when files are dropped on it, with their paths in the event's `Files`. A handler decides what a dropped file does, as the Editor sample opens it. Dragging other data, and dragging from the application, aren't bound yet.

## One running copy

`SingleInstance.Claim` keeps an application to one running copy, and hands what a second copy was started with, such as files to open, to the first, as the [doorbell](https://github.com/accessware/doorbell) crate does for Rust. Call it at the start of `Main`, before `Application.Run`. It returns null when another copy took the message, so this one can exit:

```csharp
using SingleInstance? instance = SingleInstance.Claim("myapp", [.. args.Select(Path.GetFullPath)]);
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

`MessageReceived` is raised on the GUI thread, and messages that come before a handler is added wait for one. A message is a list of strings, up to 1 MB, and a second copy waits a few seconds at most for the first to take it. On Windows, the second copy lets the first bring its window to the front, which Windows otherwise refuses to a process the user isn't using.

It builds on `SingleInstanceChecker`, which wraps `wxSingleInstanceChecker` and needs no running application: a named mutex on Windows, and a lock file with its process's ID on Linux and macOS, so a copy that crashed never blocks the next one. On Linux and macOS, two checkers in one process don't see each other. The copies talk over a named pipe on Windows, and a Unix socket elsewhere, in `$XDG_RUNTIME_DIR` or else a folder of WxKit's own in the temporary folder. Only the same user can open either.

## The desktop

A few of wxWidgets' utilities, where .NET has no way to ask the system:

- `Desktop.Bell` plays the alert sound, `Desktop.OpenUrl` opens a link in the default browser, and `Desktop.Open` opens a file in the application the system opens its kind of file with.
- `Keyboard.IsKeyDown`, `IsCapsLockOn`, and `IsNumLockOn` ask about the keyboard outside a key event, and `Mouse.Position` says where the pointer is on the screen.
- `BusyCursor` and `WindowDisabler` last until they are disposed, as in `using (new BusyCursor()) { ... }`: the busy cursor, which nests, and every top-level window disabled but one, such as a progress window that doesn't block.
- `Display.All`, `Primary`, `FromWindow`, and `FromPoint` describe the displays, like WinForms' `Screen`, with each one's `Bounds`, `WorkArea`, and `ScaleFactor`.
- `Application.SetAppearance` shows the application light, dark, or as the system is, which on Windows it otherwise doesn't follow, and `SystemSettings.Appearance` says whether the system is dark.

Utilities .NET has already, such as timers, processes, and the user's name, aren't bound.

## Web views

`WebView` shows pages with the system's browser engine: Edge's WebView2 on Windows, WebKit on macOS, and WebKitGTK on Linux, whose pages screen readers read as in a browser. An application sets `<WxKitWebView>true</WxKitWebView>`, which becomes a runtime switch the constructor checks, and on Linux gives it the shared library built with the web view and links WebKitGTK when it publishes with Native AOT. `WebView.IsAvailable` says whether the system has the engine, which on Windows is the WebView2 runtime.

- `Navigate`, `NavigateToString`, and `Source` show pages, and `Navigating`, which a handler can veto, `Navigated`, `Loaded`, and `NavigationFailed` say how it goes.
- `ExecuteScriptAsync` runs a script and gives its result as text, or throws `InvalidOperationException` when it fails. `AddScriptMessageHandler("name")` lets a page call `window.name.postMessage(text)`, which raises `ScriptMessageReceived`.
- On Windows, the engine starts after the control is created, and raises `Created` when it is ready. Pages and scripts given before then wait for it, but other members fail until then.
- Edge keeps its data, such as cookies, beside the executable unless told otherwise, so WxKit gives it `%LOCALAPPDATA%\<application>\WebView2`. `WebView.DataFolder` sets another.

## Simulated input

`UIActionSimulator` sends mouse and keyboard input as if the user gave it, for an application's tests of its user interface: `MouseMove`, `MouseClick`, `Char`, as in `Char((KeyCode)'S', KeyModifiers.Control)`, `Text`, and the rest. The input goes where the system sends real input, which must be a window that is shown and active, and arrives as the event loop runs. On macOS, the application needs permission to control the computer. [Testing](testing.md#real-input) describes how WxKit's own tests use it.

## Translations and layout direction

`Gettext._` and its plural and context forms mark and translate an application's strings, `Translations.Use` loads the catalogs for the user's language, and the build compiles and embeds the catalogs in the project's `po` folder. [Translating an application](../translating.md) covers all of it.

`Application.LayoutDirection` sets the direction that new windows with no parent, and message boxes with no window under them, lay out in. A window with a parent takes its parent's, and `Window.LayoutDirection` changes one window's. The default follows the UI locale, so an application whose language is a setting of its own, such as Hebrew on an English system, sets it. It decides the direction on Windows and Linux. On macOS, Cocoa takes it from the languages the application is localized in.

## Events

Each class exposes the events wxWidgets documents for it as standard .NET events:

```csharp
button.Click += OnClick;
frame.Closing += (sender, e) => {
	if (HasUnsavedChanges && e.CanVeto) e.Veto();
};
```

Each event is an `EventHandler<T>`, where `T` is the WxKit class for the wxWidgets event, such as `CommandEvent` or `CloseEvent`. These derive from `EventArgs`, as .NET's conventions ask. Event names come from the wxWidgets event macros, so `EVT_BUTTON` becomes `Click` and `EVT_CLOSE` becomes `Closing`, and a binding tweak names the ones that read badly. For an event a class doesn't expose, `Bind` and `Unbind` take any of the event types on `EventType`, with an optional id or id range, as in `frame.Bind(EventType.Menu, StandardId.Exit, (sender, e) => frame.Close())`.

The first handler for an event type and id range binds one functor in the shim, and later handlers join its list on the managed side. The functor is never unbound: with an empty list, it skips each event, and it's released when wxWidgets destroys the object. That costs at most one idle functor for each event a program ever handled.

### Event objects

An event object only exists while its handlers run. Once they return, every member throws `ObjectDisposedException`, so copy what you need before the first `await`.

A handler consumes its event, as in wxWidgets. Call `e.Skip()` to let wxWidgets keep processing it, which some events need. For example, a key-down handler that doesn't call `Skip` keeps the key from the text control. The close event is the exception. In wxWidgets, a window only closes if its close handler destroys it or skips the event. In WxKit, `Closing` works like `FormClosing` in Windows Forms: the window closes once the handlers return, unless one calls `e.Veto()`.

### Exceptions in handlers

An exception can't unwind through wxWidgets, so WxKit catches every exception that an event handler, a posted action, an `await` continuation, or an override throws, and raises `Application.UnhandledException`. A handler of that event can set `Handled` on its `ApplicationExceptionEventArgs`, and the application keeps running. If nothing does, the event loop ends, and `Application.Run` rethrows the exception with its original stack trace. An exception from `OnInit`, or from the action given to `Run`, ends the application and is rethrown from `Run` directly.

### Failed assertions

wxWidgets checks how it is called, and a check that fails, such as for an index out of range, raises `Application.AssertFailed`, on the thread it failed on, with the file, line, function, condition, and message. wxWidgets then goes on as best it can, often by doing nothing. Unless a handler sets `Handled`, the failure is written to `Trace`, and an attached debugger breaks, so a mistake shows while you develop, and a user never sees a dialog for it.

## Threading

wxWidgets is single threaded. Every window must be created and used on the thread that runs the event loop.

- Every member of a WxKit object checks that it's on the GUI thread, and throws `InvalidOperationException` if not, or if no application is running. `Application.IsGuiThread` says whether the caller is on it. A few members also work before the application runs, such as `Application.LayoutDirection`, `Translations.Use`, and `SingleInstanceChecker`.
- `Application.Post` runs an action on the GUI thread later, through `wxApp::CallAfter`. It can be called from any thread while the application runs.
- `Application.Run` installs a `SynchronizationContext`, so `await` in a handler resumes on the GUI thread, as in Windows Forms and WPF.

On macOS, the GUI thread must be the process's main thread, so `Application.Run` throws `InvalidOperationException` when called from another thread. Call it from `Main`.

On Windows, the GUI thread must be a single-threaded apartment, because OLE, which the clipboard, drag and drop, and accessibility use, only works in one. .NET starts `Main` in the multithreaded apartment unless it's marked `[STAThread]`, which top-level statements can't be. So when `Application.Run` is called on a thread that isn't a single-threaded apartment, it runs the event loop on a new thread that is, and waits for it to end.

## The application object

`Application.Run(Action)` creates the shim's `wxApp` subclass, runs the action inside `OnInit`, and runs the event loop until the last top-level window closes, or until `Application.Exit` is called. It returns the exit code. An application can instead derive from `Application`, override `OnInit`, which returns false to end without running the event loop, and call `new MyApp().Run()`. `Application.Current` is the running application, and `Application.WxWidgetsVersion` is the version of wxWidgets that WxKit is built with.

## Nullability and documentation comments

Every public API carries nullable annotations. The wxWidgets documentation rarely says whether a pointer can be null, so the generator assumes it can't, and binding tweaks mark the exceptions, such as parent windows and `FindWindow` results.

The generator turns the brief description of each class and member in the wxWidgets interface headers into an XML documentation comment, with C++ names rewritten to WxKit names. Each type's comment also names its wxWidgets class and links to its page in the wxWidgets reference, which keeps the detailed descriptions, since they refer to C++ names and macros throughout.

## The raw interop layer

`WxKit.Interop.WxKitNative` declares every shim function, one to one, and it is internal, with the handle types its functions take. It was public in 0.1, as an escape hatch for anything the managed API doesn't wrap yet, but nothing public gave the native object a call needs, so it could reach only what takes none, and it made every shim function part of the API. Something the managed API doesn't wrap is a reason to wrap it.
