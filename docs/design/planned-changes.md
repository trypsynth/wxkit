# Planned changes

These are changes WxKit wants that need code changes, so they're for 0.2.0, as [Versions and names](building-and-packaging.md#versions-and-names) says. Each is collected here as it's found, and made in 0.2.0's previews, whose notes say what to change. The bugs the review found, including those whose fix needed a change too, are fixed on `master` already, and in the first preview's notes.

Most of these are patterns, which a change to the generator's rules fixes everywhere at once, rather than a member at a time. A review of the whole API on 2026-11-23 found most of them.

## The generator's naming

- Words that run together, which the word tables don't split:
  - About 85 of `EventType`'s members, such as `LeftDclick`, `ToolRclicked`, `CalendarDoubleclicked`, `Togglebutton`, `Mousewheel`, every `Webview` one, which becomes `WebView`, every `Taskbar` one, which becomes `TaskBar`, and every `Stc` one, which becomes `StyledText`. `SetFocus`, `KillFocus`, and `SetCursor` become `GotFocus`, `LostFocus`, and `CursorRequested`.
  - Enum members, such as `ListCtrlStyle.Hrules` and `Autoarrange`, `ComboBoxStyle.TeProcessEnter`, `DatePickerCtrlStyle.Allownone`, `StockCursor.Sizenesw`, `SystemMetric.DclickMsec`, `FontWeight.Extrabold`, `FontEncoding.Macarabic`, and `ToolBarStyle.Nodivider`.
  - Paper sizes whose leading number lost all but its first digit, such as `One0x11` for 10 by 11, and `PrintBin`'s `Onlyone` and `Largefmt`.
- British spellings the respelling table misses: `Cancelled` and `Cancelling` become `Canceled` and `Canceling`, as `SearchCtrl.SearchCanceled` has it, `ConvertToGreyscale` becomes `ConvertToGrayscale`, and `Colourise` becomes `Colorize`.
- One spelling for one word: `ScrollBar` rather than `Scrollbar`, as the class is spelled, `FileName` rather than `Filename`, as .NET spells it, `Ok` rather than `OK`, as `StandardId.Ok` is, and `AutoScroll` rather than `Autoscroll`.
- Abbreviations .NET spells out: `GridSizer.Cols`, `HGap`, and `VGap`, `PrintData.NoCopies`, which becomes `Copies`, `Slider.TickFreq`, `DC.MapMode`, `DC.SizeMM`, `DataViewColumn.IsResizeable`, the `FromPhys` and `ToPhys` members, and the calendar's `Fg` and `Bg` colors.
- Parameter names, which break only named arguments:
  - The rule that makes `col` a `column` makes six color parameters `column` too, such as `DC.FloodFill(Point, Color? column)`. It applies only to numbers now.
  - Hungarian and abbreviated names that are left, such as `nPage`, `colFg`, `ptStart`, `xdest`, `bCase`, `but`, `minW`, and `selImage`, and the same thing named differently across classes, as `FileDialog`'s `defaultDir` and `FileCtrl`'s `defaultDirectory` are.
- Generic names of enums a class nested: `Reason` becomes `ActivationReason`, and `Origin` becomes `HelpOrigin`.
- Booleans named as booleans, as `IsChecked` is: `ActivateEvent.Active`, `CloseEvent.LoggingOff`, `Font.Underlined`, `HyperlinkCtrl.Visited`, `Printer.Abort`, and `UpdateUIEvent.Shown` take `Is`, and `NavigationFlags.IsBackward` and `IsForward` lose it. `MenuItem.Checked` and `Enabled`, and `UpdateUIEvent.Checked` and `Enabled`, become `IsChecked` and `IsEnabled`.
- Members that read as commands: `UpdateUIEvent.SetChecked`, `SetEnabled`, `SetShown`, and `SetText`, which say whether a handler set the state, become `IsCheckedSet` and so on. `FindReplaceDialog.Find`, `Replace`, and their siblings become `FindRequested` and so on, as `SearchRequested` is named, and `StyledTextCtrl.DoDrop` becomes `TextDropping`. `DataViewCtrl.ItemStartEditing` becomes `ItemEditingStarting`. `TextValidator.IsValid`, which returns an error message, becomes `GetError`.

## Types

- Numbers where an enum exists or should:
  - `ListCtrl`'s item states, masks, directions, and arrangement, and `ListItem`'s and `ListEvent`'s, which have no names today, so selecting an item is `SetItemState(i, 4, 4)`. They become enums such as `ListItemState` and `ListNextItem`.
  - `MouseEvent.Button`, which becomes `MouseButton`, `KeyCode` on list and tree events, `Orientation` on the scroll events and on `Window`'s scroll bar members, `StyledTextEvent.Modifiers` and `RegisterHotKey`'s modifiers, which become `KeyModifiers`, and `DC.DrawLabel`'s alignment.
  - `Image.LoadFlags` and `DefaultLoadFlags`, which become an `ImageLoadFlags` enum. wxWidgets defines its one flag in an enum without a name.
  - `KeyEvent.IsKeyInCategory`, the effective icons of the message dialogs, which become `MessageIcon`, `StatusBar.SetStatusStyles`, `FontData.RestrictSelection`, and `FileDialog.AddShortcut`.
- `Alignment` is `[Flags]`, and loses `Not`, `Mask`, and `Invalid`. Columns default to `Alignment.Left`.
- Each control's style enum has the styles every window takes, such as the borders, so a borderless list doesn't need a cast. The text entry and choice dialogs get style enums that can say what their defaults are.
- One integer type for one thing: `ListCtrl`'s items and columns, `TextCtrl`'s and the styled text control's positions and lines, and window ids, which are `int`, as .NET's indexes are.
- Times are `TimeSpan`s: `TimerEvent.Interval`, `UpdateUIEvent.UpdateInterval`, the effects' timeouts, and `NotificationMessage.Show`'s timeout.
- `WeekDay` becomes `DayOfWeek`.
- Lists come back as `IReadOnlyList<T>` rather than arrays, as the hand-written members already do, so a property doesn't copy on every read.
- `DC.GetLogicalScale` and `GetUserScale`, which give two doubles through `out` parameters, become properties that give a pair, `UserScale` and `LogicalScale`, which set them too.

## Events

- An event's setters go, since an application only receives events, except those a handler is meant to use, such as `SplitterEvent.SashPosition` and the data view's drag members. There are about 55, on `Event`, `CommandEvent`, `KeyEvent`, `MouseEvent`, the list, scroll, and size events, and `StyledTextEvent`.
- Duplicate events go: those bound from wxWidgets' `_RANGE` macros, such as `ToolBar.ToolClickedRange`, the 18 `CommandScroll` events of `ScrollBar` and `Slider`, and `BitmapComboBox.Text`.
- Events get the class they're sent with: `Window.ContextMenu`'s is `ContextMenuEvent`, `GenericDirCtrl`'s are `TreeEvent`s, and `Window.SetCursor`'s, renamed `CursorRequested`, is a `SetCursorEvent`.
- `SplitterWindow.UnsplitEvent` becomes `WindowRemoved`, and `NotificationMessage.Clicked` becomes `Click`.

## Members that go

- wxWidgets internals, and members meant to be overridden rather than called:
  - `Window.AddChild`, `RemoveChild`, `SetContainingSizer`, `PushEventHandler`, `PopEventHandler`, `SetNextHandler`, `SetPreviousHandler`, `OnInternalIdle`, `InformFirstDirection`, `CacheBestSize`, and the `AcceptsFocus` family.
  - The sizers' `CalcMin` and `RepositionChildren`, the splitter's `OnSashPositionChange`, `OnDoubleClickSash`, and `OnUnsplit`, the property sheet's `CreateBookCtrl`, `AddBookCtrl`, and `SetInnerSizer`, and `ScrolledWindow.PrepareDC`.
  - `Image` and `Bitmap`'s `CleanUpHandlers` and `InitStandardHandlers`, `Dialog`'s layout adaptation, `DC.StartDoc` and its page members, `MenuBar.Attach` and `Detach`, `Event.EventUserData`, `EventCategory`, and `IsCommandEvent`, and the styled text control's `DoDragEnter` family.
  - `TreeCtrl.OnCompareItems` becomes an override, as `ListCtrl.OnGetItemText` is.
- Two-step creation: `Bitmap()` and its `Create` members become static factories, such as `Bitmap.FromDipSize`, and `Image.Create` goes.
- Members that only repeat another:
  - Subclasses' copies of `Window`'s properties, such as `ListCtrl.SetBackgroundColor`, `InfoBar.SetFont`, and `StaticText.SetLabel`, which the generator misses because the base's became a property.
  - Overloads that take x and y beside one that takes a `Point` or `Size`, and members that take x and y alone take a `Point`.
  - The static `FromDip`, `ToDip`, `FromPhys`, and `ToPhys` that take a window.
  - Setters beside a property, such as `Dialog.SetIcon`, `Brush.SetColor`, and `TopLevelWindow.SetMinSize`.
  - `KeyEvent.AltDown()` and its siblings, which `Modifiers` gives, and `CmdDown()`, which wxWidgets deprecates.
  - The styled text control's twins of its text entry members, such as `Modify` beside `IsModified` and `GetLine` beside `GetLineText`. Its `Clear()`, which clears the selection where `TextCtrl.Clear()` clears everything, becomes `DeleteSelection`.
  - `Font`'s `MakeBold` family, which change a font in place, beside `Bold` and the others that return a changed copy, which are named one way, such as `WithBold`.
- Enum members that are sentinels or aliases, such as `Max`, `First`, `Last`, and `Invalid`, `BitmapType.Tif` beside `Tiff`, and zero members of flags enums, such as `ListBoxStyle.NeededSb`, which make `HasFlag` always true. Flags enums without a `None` get one.
- `TextCtrl.GetSelection(out long from, out long to)`, and the same in `ComboBox`, `SearchCtrl`, and `StyledTextCtrl`, which `SelectionStart` and `SelectionLength` replace. `SetSelection` stays.
- The styled text control's members that do nothing there, since it has the text entry's interface without its implementation: `Margins`, `SetMaxLength`, the `AutoComplete` members, `ForceUpper`, and `Hint`, which can wipe its text.

## Members that have a better form now

- Getter and setter pairs become properties:
  - The styled text control's, which Scintilla names `AutoCompGetAutoHide` and `AutoCompSetAutoHide`, 18 pairs, and its queries such as `CallTipActive()`.
  - `DC.DeviceOrigin` and `LogicalOrigin`, the calendar's colors, `CollapsiblePane.IsCollapsed`, `BookCtrlBase.Selection`, `ListCtrl.HasCheckBoxes`, `SearchCtrl`'s buttons, `SplitterWindow.SashPosition`, `Image.MaskColor`, `MemoryDC.SelectedBitmap`, and the printing dialogs' `Enable` members.
  - `Window.ToolTipText`, which can only be read, beside `SetToolTip`.
  - `MouseEvent.Dragging()` and the others, which become `IsDragging` and so on, with `LeftDClick` and its siblings becoming `IsLeftDoubleClick`.
- `TextCtrl.PositionToXY(long position, out long x, out long y)` and the styled text control's become `TryGetColumnAndLine`, and `XYToPosition` becomes `PositionFromColumnAndLine`.
- A window's style is one property of the class's own style enum, rather than `WindowStyle`, `WindowStyleFlag`, `SetWindowStyle`, and `HasFlag` taking three types.
- The item controls, `Choice`, `ComboBox`, `ListBox`, and `CheckListBox`, have one constructor each, with optional items and a style, and share an `IItemContainer` interface rather than repeating its members. Their item data has one name.
- `ListCtrl.GetItem` and `GetColumn` return a `ListItem` rather than filling one the caller makes.
- `GridSizer` and `FlexGridSizer` take a column count and a `Size` gap, so the first number doesn't mean columns or rows by how many follow.
- `Image.Rescale` and `Resize`, which change the image, return nothing, so they aren't mistaken for `Scale` and `Resized`, which return a copy. Colors given as three bytes are `Color`s.
- `Dialog.ShowWindowModal` becomes `ShowWindowModalAsync`, whose task gives the result.
- `ListCtrl` items, `TreeItemId`, `TreeListItem`, and `DataViewItem` can't be made from a number outside WxKit, which could crash the control.

## The hand-written classes

- One lifetime model: everything the application owns is `IDisposable`, says whether it's gone with `IsDisposed`, rather than some with `IsDestroyed`, and is freed when it's disposed. `Menu` gets `Dispose`.
- `MenuItem` wraps the native item, as `ToolBarTool` does, rather than a menu and an id.
- A data view model's notifications, `ItemChanged`, `ItemAdded`, `Cleared`, and the rest, become `NotifyItemChanged` and so on, so they don't read as events, and stop being virtual.
- `LiveRegion` derives from `Control` rather than `StaticText`, whose label it has no use for.
- A window's accessible object and drop target are set the same way, as `window.DropTarget = new DropTarget(...)` would be, rather than as a side effect of their constructors.
- A `PaintDC` is lent through the paint event, as a printout's DC is, so a handler can't forget to dispose it.

## What the F# samples found

Porting the samples to F# found these, which F# makes more of than C# does:

- `DataViewTreeModel<T>` knows its items by reference, so an F# record, which changes by being copied, is a new item each time it changes. It could take an `IEqualityComparer<T>`, so an item is known by a key, such as an id, and `ItemChanged` with a changed copy keeps the copy.
- A `row` or `column` inside another needs `section` before `fill` or `grow` can change it, as in `section (row { ... }) |> fill`. The item functions could take a sizer too, so it reads `row { ... } |> fill`.
- Flags enums without a `None`, such as `FindReplaceFlags` and `FindReplaceDialogStyle`, which F# can only write as `enum 0`, and C# as `0`, have one.
- `Dialog.CreateStdDialogButtonSizer`, which gives a dialog OK and Cancel, or the other standard buttons, in the platform's order, isn't bound, since it returns a sizer the caller owns until a sizer takes it, so each sample makes the buttons itself. It could give the sizer as a sizer the caller adds.

## Considered and not planned

- Naming the controls as .NET does, such as `TextBox` for `TextCtrl`. WxKit keeps wxWidgets' class names, so its documentation, and code written for wxWidgets in other languages, map to WxKit by dropping the `wx` prefix.
- Renaming `ClipboardContent` now that drag and drop carries it too. It's content the clipboard and a drag share, and its name says where it came from rather than everything it's for, which is less harm than a rename.
- Fluent for translations. wxWidgets' own strings stay in gettext's catalogs either way, .NET has no Fluent implementation that's known to work with Native AOT, and gettext's tools and WxKit's extractor already work.
