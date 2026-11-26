# Directors

A director is how .NET overrides a wxWidgets virtual member, such as a virtual list's `OnGetItemText` or a printout's `OnPrintPage`. The generator writes them, from a tweak that lists the members, and this page describes how they work and what is still written by hand.

## What a director is

A director is a C++ class the generator writes, which derives from the wxWidgets class and overrides the members a tweak lists. It takes the wxWidgets class's constructors after a `userdata` pointer, and the class's constructor exports create the director instead of the class, taking the userdata first, as `wxk_Printout_new(void* userdata, ...)` does.

Each override calls a callback, a C function pointer that .NET sets once, before it creates its first object, through an export such as `wxk_Printout_SetDirectors`. The callback takes the object's handle, the director's userdata, and the member's arguments, and returns the member's result. Until the callbacks are set, an override calls the wxWidgets implementation.

These classes have directors:

- `wxListCtrl`, whose `OnGetItemText`, `OnGetItemImage`, `OnGetItemColumnImage`, and `OnGetItemIsChecked` a virtual list overrides, and `wxListView`, which has them through it.
- `wxTreeCtrl`, whose `OnCompareItems` gives the order `SortChildren` sorts in. Its director is a class of its own to wxWidgets, from the `DirectorClassInfo` tweak, since wxMSW only asks `OnCompareItems` of a class that isn't `wxTreeCtrl` itself.
- `wxPrintout`, whose page and document members `Printout` overrides.
- `wxAccessible`, whose sixteen questions `Accessible` answers, only on wxMSW.
- `wxDataViewModel` and `wxDataViewVirtualListModel`, whose values, hierarchy, and rows `DataViewTreeModel` and `DataViewVirtualListModel` give.

## Finding the managed object

The userdata is a GC handle to what finds the managed object, so a director works for any class, whether it's an event handler or not. When wxWidgets deletes a director, its destructor calls the one `onFree` that `wxk_Directors_SetOnFree` sets, with the userdata, and the hand-written `Directors` class in .NET frees the handle there. A handle's target that implements `IDirected` hears about it first, as `Accessible` and the holder a `Printout` makes do, to mark the managed object destroyed.

A generated class, such as `ListCtrl`, makes a `DirectorTarget` for each object it creates, passes a handle to it as the userdata, and points the target at the object once its constructor runs. Before then, and after wxWidgets deletes the object, a callback finds nothing and calls the wxWidgets implementation. A hand-written class makes its own handle, strong or weak as its ownership needs: a printout that wxWidgets deletes holds a strong one, and a data view model that its controls keep alive holds a weak one.

## Generated and hand-written callbacks

A `Director` tweak generates the managed side too: a `protected virtual` method for each member, which defaults to the wxWidgets implementation through an export such as `wxk_ListCtrl_base_OnGetItemText`, and the `UnmanagedCallersOnly` callbacks that call it. The director always calls into .NET, which costs one transition but needs no reflection to see which members a class overrides.

An `InternalDirector` tweak generates only the C++ and the imports, and hand-written C# gives the callbacks, as `Printout`, `Accessible`, and the data view models do. Their managed classes are written by hand, so their public API, such as `Accessible.GetName` returning null for "no answer", can be what suits .NET rather than what wxWidgets' signature says. Such a director has no export of the wxWidgets implementation, since the hand-written class has its own defaults. Generating these classes' managed side as well was considered and isn't planned: each callback has its own rules, such as the accessible object's statuses and which arguments a model may write, and what they share is only a try and catch.

## What a callback can take and give

A generated director's parameters can be numbers, enums, booleans, and item ids, and its result can also be text. A hand-written one's can also be:

- A count, such as the number of children a model's `GetChildren` added, as its C type.
- A point, by value, and a data view item, as its id.
- An object it's lent for the call, by handle, such as the `wxVariant` a model fills with a value, or the `wxDataViewItemArray` it fills with an item's children.
- Something it gives back through a pointer or a reference: text, a `long`, an enum, a rectangle, or an object's handle, such as an accessible object's name, state, role, location, and child. The director passes the callback a `wxk::CallbackOut`, a temporary in the call that converts the value and writes it through wxWidgets' pointer once the callback returns.

An enum that no tweak binds, such as `wxAccStatus`, crosses a hand-written callback as its number, so a director adds no enum to the managed API. The generator rejects a tweak that lists a member whose types it can't convert, and names the parameter it can't.

## When .NET doesn't answer

Before the callbacks are set, an override calls the wxWidgets implementation. A pure virtual member has none, so it returns nothing, and a director that overrides every pure virtual member makes the class one WxKit can create, as the data view models are.

A tweak can have a director call the wxWidgets implementation around .NET's: `DirectorAfterBase` calls it first, and .NET only if it succeeds, and `DirectorBeforeBase` calls .NET first. wxWidgets' `wxPrintout::OnBeginDocument` starts the document and `OnEndDocument` ends it, so the printout's director calls them around .NET's.

## Classes that need more

- A class only some ports have is under a condition, with a `GuardClass` tweak, as `wxAccessible` is under `wxUSE_ACCESSIBILITY`. Its director and functions are under the condition, and the callbacks, which are C, are outside it, so `SetDirectors` links on every port.
- A class that isn't a wxObject crosses as a handle of its own type, with a `Plain` tweak, and converts to and from it in includes.h, so any file can take one, as the data view's generated `AssociateModel` takes a model. Only members a tweak marks internal take or return such a handle.
- A class can share a base's handle, as `wxDataViewVirtualListModel` shares `wxDataViewModel`'s, and leave the base out, with a `NoBase` tweak, when a class between them already overrides the base's directors. `wxDataViewListModel` answers the tree model's questions from rows, so the virtual list model's director only overrides the rows' members, which it gets from that base with `InheritUnboundBases`.

## What stays hand-written

These override virtual members in C++ that the generator doesn't write, because their logic is more than a call into .NET:

- `ManagedValidator`, in validator.cpp, whose clones share state with the original and keep the managed object alive only while a window holds one.
- `App`, in app.cpp, whose `OnInit` runs the managed startup and whose layout direction comes from the translations.
- `ManagedTranslationsLoader`, in translations.cpp, which returns a `wxMsgCatalog*` that wxWidgets owns, made from bytes .NET gives it.
- `ManagedDropTarget`, in dragdrop.cpp, which sorts the format a drag matched into WxKit's kinds of data, reads a named format's name, and keeps a test format that `wxk_DropTarget_TestDrop` sets, which a generated director has nowhere to keep. Generating it would move that logic into C# and bind wxDataFormat to save about 60 lines.
- `ManagedTaskBarIcon`, in taskbar.cpp, whose one override returns the menu .NET gave it, which a callback can't return yet.
- liveregion.cpp, labels.cpp, and accessinfo.cpp, which implement the platforms' accessibility interfaces rather than wxWidgets classes.
