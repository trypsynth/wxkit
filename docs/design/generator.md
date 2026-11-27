# Generator

WxKit generates most of its bindings, so coverage can grow and follow wxWidgets without hand-writing thousands of functions. This document covers what the generator reads, what it writes, and the rules it applies.

## Input and regeneration

The generator reads the Doxygen XML of the wxWidgets interface headers, the copy of the public API in `native/wxWidgets/interface/wx` that the wxWidgets reference documentation is built from. It does not parse C++. wxPython and wxRuby3 are generated the same way. The XML gives classes and their bases, methods with their parameter types, defaults, constness, and whether they are virtual or static, enums and constants, each class's style table and event table, `@onlyfor` port markers, deprecation, and the brief description of each item.

The generator also reads a few things from the real headers in `native/wxWidgets/include/wx`, which are what the shim compiles against: enum values, which the interface headers sometimes leave out or Doxygen cuts short, the deprecated GUI constants in `wx/defs.h`, such as `wxDEFAULT`, and the art provider's ids in `wx/artprov.h`.

The generated files are checked in, so a normal build needs neither Doxygen nor the generator. `scripts/generate.ps1` downloads the pinned Doxygen, 1.15.0, runs it on the interface headers, and runs the generator, which rewrites every generated file. `-XmlOnly` stops after the XML. The script only fetches Doxygen on Windows so far. The CI job for `win-x64` runs it and fails when the checked-in files differ.

## Binding tweaks

Decisions the XML cannot carry come from tweaks, which are C# rather than a data format, so they get compile-time checking and can share helpers. They are in `src/WxKit.Generator/Tweaks`, a file for each area, such as `Controls.cs` and `Printing.cs`, which `Core.cs` applies in turn. A class is bound only when a tweak names it, so binding a new class starts with a line in its area's file:

```csharp
tweaks.Class("wxButton", button => button.Event("EVT_BUTTON", "Click"));
tweaks.Class("wxListCtrl", list => list
	.ParameterEnum("InsertColumn", "format", "wxListColumnFormat")
	.Director("OnGetItemText", "OnGetItemImage", "OnGetItemColumnImage", "OnGetItemIsChecked"));
```

The methods are in `src/WxKit.Generator/Binding/Tweaks.cs`. They ignore members, rename them, pair properties, give a parameter an enum, name events, mark classes as hand-written, owned, static only, or value objects, make a class from a template, and bind C++ enums or groups of constants. The generator fails when a tweak names a class, member, enum, constant, or event that the headers do not have, so tweaks cannot rot quietly when wxWidgets changes.

## What it writes

The generator loads the XML into a model, drops what WxKit does not bind, shapes the rest with naming rules and tweaks, and writes:

- The shim: declarations in `native/shim/include/wxkit_generated.h`, which `wxkit.h` includes, and in `native/shim/generated` a `.cpp` file for each class, which also has the event types its header declares, `EventTypes.cpp` for wx/event.h's, `checks.cpp`, `directors.h`, `Directors.cpp`, and `includes.h`, which includes every header, converts the handles of classes that aren't wxObjects, and is the precompiled header. [Native layer](native-layer.md) explains why each class has a file of its own.
- The interop layer: the `LibraryImport` declarations for the generated functions in `src/WxKit/Interop/WxKitNative.g.cs`, and for the shim's hand-written functions in `WxKitNative.Header.g.cs`, from their declarations in `native/shim/include/wxkit.h`. A hand-written function is declared there, and nowhere else: the generator fails if the shim's sources define a function the header doesn't declare, or the header declares one they don't define, and the C++ compiler checks that each definition matches its declaration, since the sources include the header. Every hand-written function that calls into C++ runs the call in `wxk::Guard`, so an exception reaches .NET rather than ending the process, or the line before it begins `// Can't throw:` and says why, and the generator fails otherwise. C types map to C# ones by their fixed-width names, `wxk_ItemId`, a tree's or data view's item, becomes `nint`, and the callback typedefs become function pointers.
- The managed API in `src/WxKit/Generated`: a partial class for each class, `EventType.g.cs`, `Styles.g.cs`, `Enums.g.cs`, the constant classes, `ArtIds.g.cs`, and `ObjectRegistry.g.cs`, the table that wraps native objects in their managed classes.
- `artifacts/generator-report.txt`, which lists everything skipped and why.

Each managed type and member gets an XML documentation comment from its wxWidgets brief description. A type's comment ends by naming the class it wraps, as in "Wraps wxButton." Detailed descriptions are left to the wxWidgets reference.

Today 168 classes bind 3303 functions and 260 event types. The report lists about 480 entries, mostly members whose types have no conversion yet, such as C arrays and classes not bound yet, like tool tips and drop targets, and event macros whose event type the headers do not declare. Classes that need more than the generator can express are written by hand beside it, such as `EvtHandler`, `Sizer`, `Menu`, `AboutBox`, `Clipboard`, `Accessible`, and the data view models. [Managed API](managed-api.md) describes them.

## Rules for classes

- A class's handle follows its ancestry: `wxk_Window`, `wxk_Sizer`, `wxk_Menu`, `wxk_Event`, `wxk_DataViewColumn`, `wxk_ToolBarTool`, or `wxk_EvtHandler`. Value objects, which are `wxGDIObject`s and `wxImage`, and drawing contexts, which are `wxDC`s, cross as `wxk_Object`. A static-only class, such as `wxSystemSettings`, becomes a C# static class.
- A class's managed base is its nearest bound ancestor with the same lifetime. WxKit owns value objects, drawing contexts, and classes a tweak marks as owned, such as validators. Owned classes without such an ancestor derive from the hand-written `NativeObject`.
- A secondary base's members, such as `wxTextEntry`'s on `wxTextCtrl`, are bound on the class and called through a cast to that base. A tweak can do the same for unbound primary bases, as `wxDataViewColumn` gets `wxHeaderColumn`'s.
- A class with a pure virtual member gets no constructors. A template class is bound by a tweak that names the class and its argument, as `wxScrolledWindow` is `wxScrolled` over `wxPanel`.
- A virtual member a `Director` tweak lists becomes a `protected virtual` method that C# can override, through a director the shim creates in place of the class. An `InternalDirector` tweak has hand-written code give the callbacks instead, as `Printout` does. [Directors](directors.md) covers both.
- A constructor parameter a tweak keeps alive, such as a find dialog's data, stays referenced by the managed object.

## Rules for members

- Left out: a window's parameterless constructor and its `Create`, since two-step creation is not exposed, copy constructors, destructors, operators, deprecated members, and a constructor whose parameters are the start of another's that has only defaults after them, as `wxBannerWindow(parent, dir)` is.
- A member that a derived class repeats is bound once, in the base, except a static member, which stays with the `new` modifier. Two members with the same managed signature, such as `CentreOnParent` and `CenterOnParent`, are bound once.
- A trailing parameter the generator cannot convert, or whose default it cannot write, is left out with the ones after it, so the C++ default applies. An unconvertible parameter without a default leaves the whole function unbound.
- A static member checks the thread itself, since it has no handle to check through.
- A property and a method with the same name fail the generator until a tweak renames one.
- A value returned by a mutable reference, such as a color dialog's `wxColourData&`, is copied, as one returned by value is.
- In window constructors, an id without a default gets `StandardId.Any` and moves after the label or title. Position and size can always be left out, the name always takes its default, and a top-level window's parent can be null. Required parameters move ahead of optional ones, so `new Choice(parent, items)` works.

## Rules for types and defaults

- A `style` parameter of a class with a style table, and a getter named for the style, take the class's style enum, such as `ButtonStyle`, and defaults are written with its members. A number defaulting to `wxBOTH`, `wxHORIZONTAL`, or `wxVERTICAL` is an `Orientation`. A tweak can give any number parameter or return value an enum.
- A C++ enum becomes a C# enum without its members' shared prefix, and crosses as a 32-bit int, so `0xFFFFFFFF` is -1. `wxOrientation`, `wxDirection`, and `wxItemKind` map to hand-written enums. A tweak can bind an enum no member uses, or a group of constants with a shared prefix, as `wxACC_STATE_SYSTEM_` becomes `AccessibleStates`. An enum a class nests is named with the class in the shim.
- The shim checks every enum value with a `static_assert`, so a value wrong for the platform fails the native build. A constant whose value depends on the platform, such as one inside an `#if`, is left out, and a parameter defaulting to one is nullable, with null passing the C++ default.
- A default of `nullptr`, `NULL`, `0`, `wxDefaultPosition`, `wxDefaultSize`, or a value class's empty object, such as `wxNullFont`, is null. A string parameter defaulting to a wxWidgets string constant, or a color parameter defaulting to a wxWidgets color, is null by default, which passes that constant.
- Special types: `wxVariant` is `object`. `wxClientData*` and `wxTreeItemData*` are any `object`, held by a GC handle that the shim frees when the control deletes the data. `wxArrayInt`, `wxArrayTreeItemIds`, and `wxDataViewItemArray` cross as lists of 64-bit numbers, as arrays coming back and any sequence going in, or `out` arrays when filled by reference. A count followed by ints is one `IEnumerable<int>`. `unsigned long` crosses as `size_t`. `wxDateTime` is a `DateTime?` in local time. `wxGBPosition` and `wxGBSpan` are `GridPosition` and `GridSpan`.

## Rules for names and events

- `GetX` with no parameters becomes the property `X`, and a matching one-parameter `SetX` becomes its setter. `IsX`, `HasX`, and `CanX` returning `bool` keep their names.
- British spellings become American, a leading digit is spelled out, terse parameter names become words, such as `pos` to `position`, and snake_case parameters become camelCase.
- A style enum's members drop `wx` and the short prefix the class's styles share, such as `TE_`, unless a tweak gives a longer one. `wxDEFAULT_FRAME_STYLE` becomes `Default`, and every style enum has `None`. Extra styles in a style table, whose names have `_EX_`, are left out, and `wxWS_EX_` is bound as `WindowExtraStyle`.
- Events get .NET names from a table, such as `SizeChanged` and `GotFocus`, or from their macro, as `EVT_TEXT_ENTER` becomes `TextEnter`, with an `Event` suffix where a name would clash. Only event types the headers declare are bound. One they declare only under a condition, such as `wxEVT_HOTKEY` under `wxUSE_HOTKEY`, is a type of the shim's own where the condition doesn't hold, which no event is sent as. An event class WxKit does not bind is delivered as its nearest bound ancestor.

## Conversions

Each C++ type that crosses the boundary has a conversion in `src/WxKit.Generator/Binding`, in `Conversion.cs` and the files named for a kind, such as `StringConversions.cs`, which knows its C type in the shim, its interop type, its managed type, and the code between them. For example, `const wxString&` is a `char16_t*` and length in C, a `char*` and `nuint` in interop, and a `string` in the managed API. Adding a conversion binds every method that uses the type on the next run.

## Generated code

Generated files start with a comment naming `scripts/generate.ps1`, follow `.editorconfig`, and pass `dotnet format`. Managed classes are partial, so a hand-written file can add members, or replace a generated one that a tweak ignores. Output is stable: everything is sorted by a fixed key, there are no timestamps or absolute paths, and overload suffixes depend only on each overload's own parameter types, so a new wxWidgets method shows as one new method in the diff.

The generator is a `net10.0` console application in `src/WxKit.Generator`. Its tests, in `tests/WxKit.Generator.Tests`, run on small hand-written XML samples, so they need neither wxWidgets nor Doxygen.
