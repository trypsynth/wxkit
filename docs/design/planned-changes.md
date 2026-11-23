# Planned changes

These are changes WxKit wants that need code changes, so they wait for 0.2.0, as [Versions and names](building-and-packaging.md#versions-and-names) says. Each is collected here as it's found, and made in 0.2.0's previews, whose notes say what to change. A bug that leaves a member unusable as it's meant to be is the exception, and is fixed in a 0.1.x release, as 0.1.1 fixed the ids that 0.1.0 gave in the wrong place.

## Names

- `MenuItem.Checked` and `Enabled` become `IsChecked` and `IsEnabled`, as `CheckBox.IsChecked` and `Window.IsEnabled` are. `MenuItem` is written by hand, so the API pass didn't reach it.
- `UpdateUIEvent.Checked` and `Enabled` become `IsChecked` and `IsEnabled`, `Is3State()` becomes the property `IsThreeState`, and `SetChecked`, which says whether a handler set the checked state, becomes `IsCheckedSet`, so it doesn't read as a command.

## Types

- `Image.LoadFlags` and `DefaultLoadFlags` become an `ImageLoadFlags` enum rather than an `int`. wxWidgets defines its one flag, `Load_Verbose`, in an enum without a name, which the generator can't name yet.
- `DC.GetLogicalScale` and `GetUserScale`, which give two doubles through `out` parameters, become properties that give a pair, which `SetLogicalScale` and `SetUserScale` set, as `UserScale` and `LogicalScale`.

## Members that have a better form now

- `TextCtrl`, `ComboBox`, and `SearchCtrl` lose `GetSelection(out long from, out long to)`, which `SelectionStart` and `SelectionLength` replace. `SetSelection` stays.
- `Window.ToolTipText`, which can only be read, and `SetToolTip` become one property that can be set, `ToolTipText`.
- `TextCtrl.PositionToXY(long position, out long x, out long y)` and the styled text control's become `TryGetColumnAndLine`, named as .NET's Try methods are, and giving the column and line it means rather than x and y.

## Visual Basic

Visual Basic isn't supported, as [Language support](languages.md) explains, since it can't publish with Native AOT and nothing tests it. 0.2.0 drops what's there only for it:

- The public API rules that are only for Visual Basic. Members can be `required`, a `ReadOnlySpan<T>` overload no longer needs an array or `string` beside it, and names may differ only in case, though WxKit's naming doesn't make any that do.
- The translation extractor's reading of Visual Basic, so it no longer extracts strings from `.vb` files.

## Considered and not planned

- Naming the controls as .NET does, such as `TextBox` for `TextCtrl`. WxKit keeps wxWidgets' class names, so its documentation, and code written for wxWidgets in other languages, map to WxKit by dropping the `wx` prefix.
- Renaming `ClipboardContent` now that drag and drop carries it too. It's content the clipboard and a drag share, and its name says where it came from rather than everything it's for, which is less harm than a rename.
