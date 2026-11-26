# Planned changes

These are changes WxKit wants that need code changes, so they're for 0.2.0, as [Versions and names](building-and-packaging.md#versions-and-names) says. Each is collected here as it's found, and made in 0.2.0's previews, whose notes say what to change. The bugs the review found, including those whose fix needed a change too, are fixed on `master` already, and in the first preview's notes.

Most of these are patterns, which a change to the generator's rules fixes everywhere at once, rather than a member at a time. A review of the whole API on 2026-11-23 found most of them.

## The generator's naming


## Types


## Members that go

- `Bitmap()` and `Image()`, which make an empty one, if `LoadFile` should become a static factory too, such as `Bitmap.FromFile`.

## Members that have a better form now


## The hand-written classes


## What the F# samples found

Porting the samples to F# found these, which F# makes more of than C# does:

- The samples make their dialogs' buttons themselves, and could use `Dialog.CreateStdDialogButtonSizer`.

## Considered and not planned

- Naming the controls as .NET does, such as `TextBox` for `TextCtrl`. WxKit keeps wxWidgets' class names, so its documentation, and code written for wxWidgets in other languages, map to WxKit by dropping the `wx` prefix.
- Renaming `ClipboardContent` now that drag and drop carries it too. It's content the clipboard and a drag share, and its name says where it came from rather than everything it's for, which is less harm than a rename.
- Taking the zero members out of flags enums that aren't a class's styles, such as `Alignment.Left` and `Top`, `DragFlags.CopyOnly`, `NavigationFlags.Backward`, `SizeFlags.UseExisting`, and `Default` of `WebViewFindFlags` and `WebViewPrintFlags`. Each names the choice a parameter defaults to, which `None` wouldn't say. [The managed API](managed-api.md) says how to test for them.
- Renaming `UpdateUIEvent.Checked`. `IsChecked` is its base's, and means something else.
- Removing a text control's `SetMargins(left, top)`, which returns whether the margins were set, and `ScrolledWindow.SetScrollRate` and `SetScale`, whose two numbers aren't a place or a size.
- Fluent for translations. wxWidgets' own strings stay in gettext's catalogs either way, .NET has no Fluent implementation that's known to work with Native AOT, and gettext's tools and WxKit's extractor already work.
