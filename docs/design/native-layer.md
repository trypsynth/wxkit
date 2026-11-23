# Native layer

This document describes the C interface that WxKit's native shim, in `native/shim`, exposes over wxWidgets. .NET can call C functions but not C++ ones, whose name mangling, virtual tables, overloads, exceptions, and templates `LibraryImport` does not understand, so the shim wraps each wxWidgets function WxKit uses in an `extern "C"` function with only C types in its signature, as wxDragon's `wxd_` functions do for Rust. Most of the shim is generated into `native/shim/generated`, as [Generator](generator.md) describes, and the rest is written by hand in `native/shim/src`. [Overview](overview.md) shows where the shim sits.

## Naming and handles

Every export is named `wxk_<Class>_<Method>`, with the class name minus its `wx` prefix: `wxk_Button_SetLabel` wraps `wxButton::SetLabel`, and `wxk_Frame_new` creates a `wxFrame`. C has no overloads, so the generator gives an overload a suffix from its parameters, such as `wxk_Window_SetSize_Rect` and `wxk_Window_SetSize_IntInt`, which a binding tweak can override.

Handles are incomplete structs declared in `native/shim/include/wxkit.h`, such as `typedef struct wxk_Window wxk_Window;`. In C# they are empty structs used through pointers, such as `WxkWindow*`, so the compiler keeps a window from being passed where a sizer is expected. Handles are coarse: every window class shares `wxk_Window`.

## Types at the boundary

Only fixed-width integers, `float`, `double`, booleans as `uint8_t`, handle pointers, C function pointers, text, and small value structs that both sides lay out the same way, such as `wxk_Point`, `wxk_Size`, `wxk_Rect`, and `wxk_Colour`, cross the boundary. Booleans are `uint8_t` because the size of C's `bool` is not guaranteed to match .NET's. The managed assembly is marked `DisableRuntimeMarshalling`, so a signature the runtime would have to marshal fails to compile, and Native AOT needs no marshalling stubs.

Text going in is a UTF-16 pointer and a length, `const char16_t* text, size_t length`. The managed side pins the string with `fixed`, so a call allocates nothing, and the length keeps an embedded null character intact. On Windows, `wxString` is UTF-16 too, so text crosses without conversion; on macOS and Linux it holds UTF-32 `wchar_t`, so the shim converts on each call. Text coming out is a `wxk_String*`, a heap copy that the managed side reads with `wxk_String_GetData` and frees with `wxk_String_Free`, through one helper that generated code calls. Arrays of strings cross as a `wxk_StringArray*` with a count and an accessor.

## Errors and assertions

An exception that unwinds into .NET ends the process, so every export runs its body through `wxk::Guard`, in `native/shim/src/shim.h`, which catches everything and returns the function's "nothing" value, such as 0 or null. Most wxWidgets failures are `false` returns or null pointers anyway, which the shim passes through for the managed API to interpret.

wxWidgets checks its preconditions with `wxASSERT` and `wxCHECK`, which it recommends keeping in release builds, so the native library is built with debug level 1, the `WXKIT_DEBUG_LEVEL` CMake option, which 0 turns off. The shim installs its own handler with `wxSetAssertHandler` when it loads, so a failure never shows wxWidgets' dialog, on any thread and before the application runs. It goes to `Application.AssertFailed` once the managed side registers, on first use of `Application`, and to the debug output before then. A failure inside the handler is left out rather than recursing.

## Callbacks and events

The shim never stores a .NET delegate. A callback is a C function pointer, which on the managed side is a static `UnmanagedCallersOnly` method, and a `void* userdata`, which is a `GCHandle` to the managed object. This avoids marshalled delegates, which Native AOT supports poorly. Every `UnmanagedCallersOnly` method catches every exception, and [Managed API](managed-api.md) describes where they go.

One function binds every event:

```c
uint8_t wxk_EvtHandler_Bind(wxk_EvtHandler* handler, int32_t eventType, int32_t id, int32_t lastId, wxk_EventCallback callback, void* userdata, wxk_Callback onFree);
```

wxWidgets copies the bound functor, so the copies share their state, and when the last one is deleted, because the handler was unbound or destroyed, `onFree` releases the userdata exactly once, after the last possible call. Event types are runtime values in wxWidgets, so the shim exports a getter for each, such as `wxk_EventType_Activate`, which the managed side reads once.

## Object tracking

So that a call on a destroyed object throws `ObjectDisposedException` instead of crashing, the shim attaches a `wxTrackerNode`, in `native/shim/src/object.cpp`, to each window and event handler the managed side wraps, all of which derive from `wxTrackable`. It calls into managed code when wxWidgets destroys the object, even one WxKit did not create, such as a button inside a standard dialog. Objects that are not trackable, such as sizers, are tracked through the window that owns them, as [Managed API](managed-api.md) describes.

## Overriding virtual methods

For the virtual members a binding tweak lists, such as a virtual list control's `OnGetItemText`, the generator emits a director: a C++ class that derives from the wxWidgets class, overrides those members, and takes the wxWidgets class's constructors after a userdata pointer. The constructor exports create the director instead of the class, and take the userdata first. `ListCtrlDirector`, `ListViewDirector`, and `PrintoutDirector` are the generated directors today.

Each override calls a function pointer that the managed class sets once, before it creates its first object, through an export such as `wxk_ListCtrl_SetDirectors`; until then, the override calls the wxWidgets implementation. The callback takes the object's handle and the director's userdata, a GC handle that finds the managed object. A generated class, such as `ListCtrl`, makes a `DirectorTarget` for each object, which points at the object once its constructor runs. A hand-written one, such as `Printout`, which an `InternalDirector` tweak gives its callbacks, makes its own. When the director is deleted, it calls the one `onFree` that `wxk_Directors_SetOnFree` sets, which frees the handle. Its `protected virtual` counterpart in C# defaults to the wxWidgets implementation, which it reaches through an export such as `wxk_ListCtrl_base_OnGetItemText`, and the callback uses that export too while no managed object exists. The director always calls into .NET, which costs one transition but needs no reflection to detect overrides. A generated director's parameters can only be numbers, enums, and booleans, and its result can also be a string; a hand-written one's can also be pointers to numbers it writes. The generator rejects a tweak that lists anything else. A tweak can also have a director call the wxWidgets implementation before .NET, and .NET only if it succeeds, or after .NET, as the printout's document members do. [General directors](directors.md) is a design for lifting these limits, so the shim's hand-written overrides, such as the printout's, can be generated too.

These classes in `native/shim/src` override virtual methods by hand, each through a table of callbacks into .NET:

- `ManagedAccessible`, in `accessible.cpp`, which answers "not implemented" for whatever .NET leaves to Windows. The window owns it, and its destructor frees the `GCHandle`.
- `ManagedValidator`, in `validator.cpp`. A window clones the validator it is given, so the copies share their callbacks and a weak `GCHandle` through a `shared_ptr`; the clones a window holds make the handle strong while they live, and the last copy frees it.
- `ManagedDataViewModel` and `ManagedVirtualListModel` in `dataview.cpp`, `ManagedPrintout` in `printing.cpp`, `ManagedTaskBarIcon` in `taskbar.cpp`, and `ManagedTranslationsLoader` in `translations.cpp`.

## Announcements

`native/shim/src/liveregion.cpp` exports `wxk_LiveRegion_Prepare` and `wxk_LiveRegion_Announce`, which speak a message through screen readers with a priority of 0, 1, or 2, for low, medium, and high. It raises a UI Automation notification on Windows, posts `AXAnnouncementRequested` on macOS, and emits an ATK signal on Linux. Other ports get stubs that return 0. The announcements section of [Managed API](managed-api.md) describes each backend.

## Linking only what an application uses

A linker loads a static library's object file whole, along with every wxWidgets object file it refers to, even from functions nothing calls. So the generator writes a file for each class, and the hand-written shim keeps features apart the same way, such as a file for each image format in `native/shim/src/handlers`. An application that never creates a data view links no data view code.

Some wxWidgets functions exist on only some ports, such as `wxDialog::IsMainButtonId` on wxMSW. The shim still exports every function on every platform, since a missing symbol would make every Native AOT application that references it fail to link there. On a port that lacks the function, the export does nothing and returns its "nothing" value.

## wxWidgets build options

WxKit builds wxWidgets with a fixed set of options, forced in `native/shim/CMakeLists.txt` with a comment beside each, so the generator knows which features exist. wxWidgets is linked statically, with the static C runtime, as Native AOT needs. `wxUSE_ACCESSIBILITY` is on only on Windows, since `wxAccessible` is MSAA's. `wxBUILD_COMPATIBILITY` is `NONE`, so deprecated functions are not exposed. wxWidgets uses its own copies of zlib, libpng, libjpeg, libtiff, libwebp, PCRE2, and expat. On Linux, GTK and its dependencies come from the system, and features that would need another system library, such as web requests and spell checking, are off. [Building and packaging](building-and-packaging.md) describes the builds themselves.
