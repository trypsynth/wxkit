# General directors

This is a design for generating more of the C++ that lets .NET override wxWidgets' virtual members, which the shim still writes by hand for most classes. It isn't built yet. [Native layer](native-layer.md) describes the directors the generator writes today.

## Where things stand

The generator writes a director for a virtual member a tweak lists, such as `wxListCtrl::OnGetItemText`. Those directors have three limits:

- The object has to be an event handler, since the callback finds its managed object through the object registry.
- The parameters can only be numbers, enums, and booleans. The result can also be a string or an item id.
- The managed side is generated too, as a `protected virtual` method, so a director can only exist where the generated class is the public one.

Every other override in the shim is written by hand, in a `Managed` class that derives from the wxWidgets class and calls a table of callbacks:

- `ManagedPrintout` in printing.cpp: page numbers in, booleans out, and `GetPageInfo`'s four `int*` out parameters.
- `ManagedAccessible` in accessible.cpp: a child id in, a status out, and the answer through an out parameter, which is a `wxString*`, an `int*`, a `long*`, a `wxAccRole*`, or a child object.
- `ManagedDataViewModel` and `ManagedVirtualListModel` in dataview.cpp: items, rows, and columns in, and values through a `wxVariant&`, children through a `wxDataViewItemArray&`.
- `ManagedDropTarget` in dragdrop.cpp: coordinates and a `wxDragResult` in and out, around a composite data object it builds.
- `ManagedValidator` in validator.cpp: windows in, booleans out, and clones that share state with the original.
- `ManagedTranslationsLoader` in translations.cpp: strings in, a catalog and a string array out.
- The application, the task bar icon, and the object tracker, each with one or two overrides.

Each of these holds a `userdata` pointer, a GC handle to its managed object, rather than looking it up in the registry, and calls an `onFree` callback from its destructor so .NET can free the handle.

## Goals

- Generate the C++ of the `Managed` classes whose overrides fit a small set of patterns: printing, accessibility, the data view models, and the simpler parts of drag and drop.
- Keep the public API as it is. The managed classes that wrap these, such as `Printout` and `Accessible`, stay hand-written in 0.1.x, as `Menu` and `Sizer` did when their C++ became generated.
- Keep the hand-written C++ for what doesn't fit a pattern, such as the validator's clones and the application.

## Design

### Decouple the C++ director from the managed class

A tweak marks a class as one whose C++ director is generated for hand-written managed code, in the same way the `Internal` tweak binds members only for hand-written code. For such a class the generator writes:

- The director in C++, deriving from the wxWidgets class, with an override for each listed member.
- A callback pointer for each override, and an export that sets them once, such as `wxk_Printout_SetDirectors`.
- A constructor export for each constructor, which takes a `userdata` pointer after the constructor's own parameters.
- The struct and the delegate types in the interop imports.

The hand-written managed class supplies the `UnmanagedCallersOnly` callbacks and sets them, as it does now. The generated `protected virtual` path stays for generated classes such as `ListCtrl`.

### Find the managed object through userdata

A general director stores the `userdata` it was created with and passes it to each callback in place of the object's handle. Its destructor calls an `onFree` callback with it. The managed side allocates a GC handle for `userdata` and frees it in `onFree`. This is what the hand-written classes do now, and it works for classes that aren't event handlers, such as `wxPrintout` and `wxDataViewModel`.

Which kind of handle to use, strong or weak, stays the managed class's choice, since it depends on who owns the native object. A printout that wxWidgets deletes needs a strong handle; a model the managed side also holds can use a weak one.

### Convert more kinds of parameters

Each conversion gets a callback form, as `StringConversion` and `ItemIdConversion` have today through `CallbackCType`, `FromCallback`, and `ToCallback`. The new forms are:

- Text in: a `const wxString&` crosses as UTF-16 with a length, borrowed for the call, as a string goes the other way today.
- An object in: a `wxWindow*`, a `wxDC&`, or any bound class crosses as its handle, borrowed for the call. The callback wraps it with the registry, or as a `LentDC` for a drawing context, as `OnPrintPage` does now.
- A number or an enum out: an `int*`, `long*`, or an enum pointer crosses as a pointer to the C type, which the callback writes. `GetPageInfo` and the accessible's role and state use this.
- Text out: a `wxString*` crosses as a pointer to a `wxk_String*`, which the callback sets to a string it allocates and the director takes, as it takes a returned string today.
- An item in: `wxDataViewItem` already crosses as its id.

A `wxVariant&` and a `wxDataViewItemArray&` already cross by handle in the hand-written models, and the `NativeVariant` and item array code on the managed side reads and writes them. The generator would bind them by handle in the same way.

### Defaults when .NET doesn't answer

Before the callbacks are set, and when a callback pointer in the struct is null, the director calls the wxWidgets implementation, as today's directors do. Some classes need another default: an accessible object returns `wxACC_NOT_IMPLEMENTED` so Windows answers from the standard object, and a model's `IsEnabled` defaults to true. A tweak gives such a default as a C++ expression for the member, such as `wxACC_NOT_IMPLEMENTED`.

### Members that call the base around .NET

`ManagedPrintout` calls `wxPrintout::OnBeginDocument` before .NET's and `OnEndDocument` after it, because wxWidgets' own implementation starts and ends the document on the DC. A tweak marks a member as wrapping the base, before or after, and the director calls it there. The managed side then doesn't call the base export for that member.

## What stays hand-written

- `ManagedValidator`, whose clones share state with the original and keep the managed object alive only while a window holds one. The ownership is the point of the class, and a pattern for it would be as long as the class.
- `App`, whose `OnInit` runs the managed startup and whose layout direction comes from the translations.
- `ManagedTranslationsLoader`, which returns a `wxMsgCatalog*` that wxWidgets owns, made from bytes .NET gives it.
- The drop target's constructor, which builds a composite data object from the formats .NET asks for. Its overrides could be generated once the constructor is a separate hand-written function.
- liveregion.cpp, labels.cpp, and accessinfo.cpp, which implement platform accessibility interfaces rather than wxWidgets classes.

## Order of work

1. Userdata identity and the generated callbacks, proven on `wxPrintout`, which needs only numbers, booleans, `int*` out parameters, and the wrap-the-base marker. printing.cpp's director goes, and `Printout` calls the generated exports. Done: the printout's director is generated, and `ListCtrl`'s takes a userdata it doesn't use yet.
2. Text and enum out parameters, and the per-member default, proven on `wxAccessible`. Most of accessible.cpp goes.
3. Variants and item arrays by handle, proven on the two data view models. Most of dataview.cpp goes.
4. Object parameters, and the drop target's overrides.

Each step keeps the public API and the tests as they are, and is a commit of its own.

## One kind of director

General directors replace today's registry-based ones, so the shim has one kind. A generated class such as `ListCtrl` already has a generated managed side, which allocates the GC handle it passes as `userdata` and frees it in `onFree`, where today it looks itself up in the registry. Nothing public changes. This comes after step 1, once the userdata form works, and before step 2, so the later steps build on one kind.

## Open questions

- Whether the managed side of a general director should be generated for the hand-written classes too, as a sealed internal class that the hand-written public class wraps. That would remove the `UnmanagedCallersOnly` boilerplate, at the cost of another layer. The generated classes' managed side, as `ListCtrl` has, shows what it would look like. The decision waits until the printout and the accessible object are done, which shows how much of their managed code repeats.
