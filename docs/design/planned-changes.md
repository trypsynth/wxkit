# Planned changes

These are changes WxKit wants that need code changes, so they're for 0.2.0, as [Versions and names](building-and-packaging.md#versions-and-names) says. Each is collected here as it's found, and made in 0.2.0's previews, whose notes say what to change. The bugs the review found, including those whose fix needed a change too, are fixed on `master` already, and in the first preview's notes.

Most of these are patterns, which a change to the generator's rules fixes everywhere at once, rather than a member at a time. A review of the whole API on 2026-11-23 found most of them.

## The generator's naming

- `UpdateUIEvent.Checked` is named as a boolean, as `IsChecked` is. It can't take that name, which is its base's, `CommandEvent.IsChecked`, and means something else.

## Types

- Numbers where an enum exists or should:
  - `StyledTextEvent.Modifiers`, whose values are Scintilla's, not `KeyModifiers`', so it needs converting, or an enum of its own.
- Each control's style enum has the styles every window takes, such as the borders, so a borderless list doesn't need a cast. The text entry and choice dialogs get style enums that can say what their defaults are.

## Members that go

- `Bitmap()` and `Image()`, which make an empty one, if `LoadFile` should become a static factory too, such as `Bitmap.FromFile`.
- Members that only repeat another:
  - A text control's `SetMargins(left, top)`, beside its `Margins`, which is kept for the result it returns, and `ScrolledWindow.SetScrollRate` and `SetScale`, whose two numbers aren't a place or a size.
  - `KeyEvent.RawControlDown()` and `MouseEvent`'s, once `KeyModifiers` has a member for the Control key of a Mac, where `Control` is Command.
  - The styled text control's `Text` beside `Value`, `TextLength` beside `LastPosition`, and `CurrentPos` beside `InsertionPoint`, if one of each should go. Its `ClearAll` could be `Clear`, as `TextCtrl`'s is.
- Zero members of flags enums that aren't a class's styles, which make `HasFlag` always true, if they should go as the styles' did: `Alignment.Left` and `Top`, `DragFlags.CopyOnly`, `NavigationFlags.Backward`, `SizeFlags.UseExisting`, and `Default` of `WebViewFindFlags` and `WebViewPrintFlags`. `Direction` and `Orientation` have no `None`.

## Members that have a better form now

- A window's style is one property of the class's own style enum, rather than `WindowStyle`, `WindowStyleFlag`, `SetWindowStyle`, and `HasFlag` taking three types.
- The item controls, `Choice`, `ComboBox`, `ListBox`, and `CheckListBox`, have one constructor each, with optional items and a style, and share an `IItemContainer` interface rather than repeating its members. Their item data has one name.

## The hand-written classes

- One lifetime model: everything the application owns is `IDisposable`, says whether it's gone with `IsDisposed`, rather than some with `IsDestroyed`, and is freed when it's disposed. `Menu` gets `Dispose`.
- `MenuItem` wraps the native item, as `ToolBarTool` does, rather than a menu and an id.

## What the F# samples found

Porting the samples to F# found these, which F# makes more of than C# does:

- The samples make their dialogs' buttons themselves, and could use `Dialog.CreateStdDialogButtonSizer`.

## Considered and not planned

- Naming the controls as .NET does, such as `TextBox` for `TextCtrl`. WxKit keeps wxWidgets' class names, so its documentation, and code written for wxWidgets in other languages, map to WxKit by dropping the `wx` prefix.
- Renaming `ClipboardContent` now that drag and drop carries it too. It's content the clipboard and a drag share, and its name says where it came from rather than everything it's for, which is less harm than a rename.
- Fluent for translations. wxWidgets' own strings stay in gettext's catalogs either way, .NET has no Fluent implementation that's known to work with Native AOT, and gettext's tools and WxKit's extractor already work.
