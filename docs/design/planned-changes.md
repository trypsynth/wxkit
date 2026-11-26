# Planned changes

These are changes WxKit wants that need code changes, so they're for 0.2.0, as [Versions and names](building-and-packaging.md#versions-and-names) says. Each is collected here as it's found, and made in 0.2.0's previews, whose notes say what to change. The bugs the review found, including those whose fix needed a change too, are fixed on `master` already, and in the first preview's notes.

Most of these are patterns, which a change to the generator's rules fixes everywhere at once, rather than a member at a time. A review of the whole API on 2026-11-23 found most of them.

## Found by the second review

A second review of the whole API on 2026-11-26, before the first preview, found these. The earlier review's changes are made, and listed in [the first preview's notes](../releases/0.2.0-preview.1.md).

### Bugs and traps

- F#'s `trn` and `trpn` take the count as an `int64`, so `trn "{0} file" "{0} files" count` doesn't compile with an `int`, which F# doesn't widen.
- `Window.EventHandler` hides `System.EventHandler` in every class derived from a window, as `Window.Char` hid `System.Char`, and setting it is wxWidgets' own plumbing.
- `TextValidatorStyle` has the values of flags without `[Flags]`, and `TextValidator.HasFlag` repeats `Style.HasFlag`.
- `ListItem.Data` is a `long` to read and `SetData(int)` to set, and `ListCtrl.SetItemData` takes an `int` where `GetItemData` gives a `long`.
- `Font.SetSymbolicSizeRelativeTo`'s `base` and `InfoBar.ShowCheckBox`'s `checked` are C# keywords, which a call that names them has to escape.

### Members only wxWidgets calls, or that repeat another

- `Window.SetFocusFromKbd`, `GetMinSizeFromKnownDirection`, `AdjustForLayoutDirection`, `UseBackgroundColor`, `UseForegroundColor`, and `GetBorder(long)`, and `TopLevelWindow.ShouldPreventAppExit` and `TmpDefaultItem`.
- `Event.StopPropagation`, `ResumePropagation`, and `ShouldPropagate`, `Image.Destroy`, `Bitmap.RemoveHandler` and `Image.RemoveHandler`, and `EventType<T>.Value`.
- Repeats: `BitmapToggleButton.Value` beside `IsChecked`, `MouseEvent`'s, `KeyEvent`'s, and `SetCursorEvent`'s `X` and `Y` beside `Position`, `ScrolledWindow.GetVirtualSize`, `DropFilesEvent.NumberOfFiles`, `ListBox.InsertItems`, `RadioBox.Select`, `StaticBox.Enable`, `StyledTextCtrl.SelectedText` beside `StringSelection`, `SearchCtrl.DescriptiveText` beside `Hint`, `TextCtrl.MarkDirty` and `DiscardEdits` beside `IsModified`, and `ToolBar`'s `GetToolEnabled`, `EnableTool`, `GetToolState`, `ToggleTool`, and the help getters and setters beside `ToolBarTool`'s properties.
- Enum members that are bounds or second spellings: `SystemColor.ButtonHilight` and `ThreeDhilight`, `KeyModifiers.Cmd`, `Win`, and `All`, `WindowStyle.BorderDouble`, `MessageDialogStyle.IconHand` and `IconExclamation`, `BrushStyle.FirstHatch` and `LastHatch`, `FontEncoding.MacMin`, and `StandardId.MdiWindowFirst` and `MdiWindowLast`. `CalendarHitTestResult` is used by nothing.

### Loading that can fail

- `Icon` and `Cursor` still load from a file through a constructor, or `Icon()` and `LoadFile`, which give an object that isn't usable when loading fails. They get `FromFile`, as `Bitmap` and `Image` have.
- `TreeItemId` and `TreeListItem` say there is no item with `IsOk`. `TreeCtrl.Selection`, `RootItem`, `GetItemParent`, `GetNextSibling`, and the like could return null instead.

### Getters and setters that aren't properties, and booleans without Is

- Methods that take nothing and return a boolean: `CheckBox.Is3State` and `Is3rdStateAllowedForUser`, `UpdateUIEvent.Is3State`, `ListCtrl.InReportView`, `IdleEvent.MoreRequested`, `Window.InheritsBackgroundColor` and `InheritsForegroundColor`, and `TopLevelWindow.OsxIsModified`. `3State` is `ThreeState`, as the style is.
- A getter with a setter under another name: `DataViewCtrl.AllowMultiColumnSort` with `IsMultiColumnSortAllowed`, `DataViewColumn.SetSortOrder` with `IsSortOrderAscending`, `Bitmap.UseAlpha` with `HasAlpha`, `ListBox.SetFirstItem` with `TopItem`, `Validator.SuppressBellOnError` with `IsSilent`, `SpinCtrl.TextValue` with `SetValue(string)`, `RichMessageDialog.ShowDetailedText` with `DetailedText`, `WizardPageSimple.SetNext` and `SetPrevious`, `PrintData.SetPaperSize`, `MenuBar.MacGetCommonMenuBar` and `MacSetCommonMenuBar`, and `UpdateUIEvent.Check`, `Enable`, and `Show` with `Checked`, `IsEnabled`, and `IsShown`.
- Booleans without `Is`, `Has`, or `Can`: `CollapsiblePaneEvent.Collapsed`, `Button.AuthNeeded`, `Font.Strikethrough`, `ColorData.ChooseFull` and `ChooseAlpha`, `FontData.AllowSymbols`, `EnableEffects`, and `ShowHelp`, `Window.ThemeEnabled` and `AutoLayout`, `TreeCtrl.QuickBestSize`, and `PrintDialogData.AllPages`, `Collate`, `CurrentPage`, `PrintToFile`, `Selection`, and `SpecifiedPages`.
- `DC.MinX`, `MaxX`, `MinY`, and `MaxY` are one `Rect`, and `ListCtrl.TryGetItemRect`, `TryGetSubItemRect`, and `DC.TryGetClippingBox` could return a `Rect?`.

### Numbers that are another type

- `FlexGridSizer.FlexibleDirection` and `Window.GetScrollBarSize`'s parameter are an `Orientation`, `InfoBar.EffectDuration` is a `TimeSpan`, `Window.EnableTouchEvents` takes flags, `RegisterHotKey`'s key is a `KeyCode`, `TreeListCtrl.AppendColumn`'s flags are `DataViewColumnFlags`, `WebView.Find` returns a count as a `long`, and `TextCtrl.LoadFile` and `SaveFile` take a `fileType` that means nothing.
- `SplitterEvent.X` and `Y` are a `Point`, and `GridBagSizer`'s two gaps a `Size`, as `GridSizer`'s are.
- `MessageDialog.MessageDialogStyle` is the dialog's `Style`. `RichMessageDialog.FooterIcon` and `NotificationMessage.SetFlags` take a `MessageIcon`.

### Names

- Abbreviations left: `AlwaysShowSb` in every style enum, and `ListBoxStyle.AlwaysSb` and `NoSb`, `DataViewCtrlStyle.HorizRules` and `VertRules`, `ListCtrlStyle.SingleSel`, `ToolBarStyle.HorzLayout` and `HorzText`, `HitTestResult.WindowHorzScrollBar` and `WindowVertScrollBar`, `StandardId.MdiWindowTileHorz` and `TileVert`, `WizardBitmapPlacement.HalignLeft` and the rest, `CollapsiblePaneStyle.NoTlwResize`, `ArtClient.CmnDialog`, `ArtId.DelBookmark`, `AboutDialogInfo.DocWriters`, `ListItem.Align`, `SystemFont.AnsiVarFont`, `NavigationFlags.WinChange`, and `MouseEvent.ButtonDClick`.
- Redundant words: `WrapSizerFlags.WrapSizerDefaultFlags`, `SystemFont`'s `Font` suffix, `PrinterError.NoError`, and `WebViewNavigationActionFlags` and `WebViewReloadFlags`, which aren't flags.
- One thing under two names: `StringSelection` in the text controls and `SelectedText` in the web view, `ListEvent.Point` and `TreeEvent.Point` where others have `Position`, `SpinEvent.Position` where the controls have `Value`, `ListItem.Id` where the event has `Index`, `FileCtrlEvent.File` where the control has `FileName`, `StatusBar.FieldsCount`, `ToolBar.ToolsCount`, and `TextCtrl.NumberOfLines` where others end in `Count`, `RadioBox.Enable(index)` and `Show(item)` beside `IsItemEnabled`, and `TreeCtrl.Delete` and `SelectItem` beside `TreeListCtrl.DeleteItem` and `Select`.
- `EventType` members that didn't follow their events' new names, such as `DataViewItemStartEditing`, `TreeItemMenu`, `SplitterUnsplit`, `Find`, `FindNext`, and `UpdateUi`.
- Parameters: one letter or an abbreviation, as in `DC.DrawEllipticArc(sa, ea)`, `FromDip(d)`, `SetScale(xs, ys)`, `SetScrollBars`' `noUnitsX` and `xPos`, `SpinCtrlDouble`'s `inc`, and `ToolBar`'s `bitmap1`. A status bar's field is `i`, `field`, and `index`, an image's index is `imageId`, `imageIndex`, and `image`, and a file's name is `name` and `fileName`.

### Constructors

- The style comes before the id in `StatusBar` and `InfoBar`, and the name before the id in `Frame.CreateStatusBar` and `CreateToolBar`.
- `BitmapComboBox` has two constructors, where `ComboBox` has one. `SingleChoiceDialog` has no style or position. `DirPickerCtrl` and `FilePickerCtrl` take a null style. `NumberEntryDialog` takes `long`s and has no defaults.
- `ProgressDialog` and `MessageBox.Show` take their parent last. That's wxWidgets' order, and a parent is seldom given, so it may stay.

### The styled text control

- `Stc` has about 2,900 constants as `int`s, with the words run together that the enums split, such as `CmdCharleftextend`, and a British `Behaviour`. The control takes them as `int`s, as in `Lexer`, `EolMode`, `WrapMode`, and `CmdKeyAssign`. The groups that aren't a lexer's styles could be enums, and the class named `StyledText`, as `EventType` says.
- `Stc.KeymodShift` is 1 and `KeymodAlt` 4, which aren't `KeyModifiers`' values, though the event's `Modifiers` is a `KeyModifiers` now.
- Pairs such as `AnnotationGetVisible` and `AnnotationSetVisible` aren't properties, `Selections` is a count, `SelectionEmpty` and `Overtype` lack `Is`, and `StyledTextEvent` has `LParam`, `WParam`, and `Message`, `X` and `Y`, and `Key` as an `int`.

### F#

- `LayoutSource` is public only for the inline functions, and shows in completion.
- The modules open themselves, so `row`, `item`, `column`, `field`, and `form` are in every file that opens `WxKit.FSharp`, where a parameter of the same name hides them. That may be what's wanted.

## Considered and not planned

- Naming the controls as .NET does, such as `TextBox` for `TextCtrl`. WxKit keeps wxWidgets' class names, so its documentation, and code written for wxWidgets in other languages, map to WxKit by dropping the `wx` prefix.
- Renaming `ClipboardContent` now that drag and drop carries it too. It's content the clipboard and a drag share, and its name says where it came from rather than everything it's for, which is less harm than a rename.
- Taking the zero members out of flags enums that aren't a class's styles, such as `Alignment.Left` and `Top`, `DragFlags.CopyOnly`, `NavigationFlags.Backward`, `SizeFlags.UseExisting`, and `Default` of `WebViewFindFlags` and `WebViewPrintFlags`. Each names the choice a parameter defaults to, which `None` wouldn't say. [The managed API](managed-api.md) says how to test for them.
- Renaming `UpdateUIEvent.Checked`. `IsChecked` is its base's, and means something else.
- Removing a text control's `SetMargins(left, top)`, which returns whether the margins were set, and `ScrolledWindow.SetScrollRate` and `SetScale`, whose two numbers aren't a place or a size.
- Fluent for translations. wxWidgets' own strings stay in gettext's catalogs either way, .NET has no Fluent implementation that's known to work with Native AOT, and gettext's tools and WxKit's extractor already work.
