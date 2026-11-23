namespace WxKit.Generator.Binding;

// How a value crosses from managed code through the shim to wxWidgets and back. Each kind of
// type has one conversion, which knows its form in all three places and the code that converts
// between them: the C parameters and return type in the shim, the C# types in the interop layer,
// and the C# type in the managed API. Managed code names WxKit's own types with global::WxKit,
// since a generated member can hide one, as Pen's Color property hides the Color type.
internal abstract class Conversion {
	/// <summary>A short name used to tell overloads apart, such as String or Int.</summary>
	public abstract string Suffix { get; }

	// The shim's side.
	public abstract IEnumerable<string> CParameters(string name);

	public abstract string ToCpp(string name);

	public abstract string CReturnType { get; }

	public abstract string FromCpp(string expression);

	/// <summary>What the shim returns when wxWidgets throws.</summary>
	public abstract string Fallback { get; }

	// The interop layer's side.
	public abstract IEnumerable<string> InteropParameters(string name);

	public abstract string InteropReturnType { get; }

	// The managed side.
	public abstract string ManagedType(bool nullable);

	/// <summary>The managed type of a value coming back, which can be more specific than the type going in.</summary>
	public virtual string ManagedReturnType(bool nullable) => ManagedType(nullable);

	// A director's callback, which wxWidgets calls through an overridden virtual member. Its
	// parameters come from C++ the way a return value does, so they use FromCpp and ManagedReturn,
	// and its result goes back the way a parameter does, through these.

	/// <summary>The C type a director's callback returns this as, or null when a callback cannot return it.</summary>
	public virtual string? CallbackCType => null;

	// A parameter a director's callback takes, as the shim passes it on from the overridden member.
	public virtual string CallbackParameterCType => CReturnType;

	public virtual string CallbackParameterInteropType => InteropReturnType;

	public virtual string ToCallbackArgument(string name) => FromCpp(name);

	public virtual string CallbackInteropType => InteropReturnType;

	/// <summary>C++ that turns what a callback returned into the C++ value.</summary>
	public virtual string FromCallback(string expression) => expression;

	/// <summary>C# that turns the managed value into what a callback returns.</summary>
	public virtual string ToCallback(string expression) => expression;

	/// <summary>A null check, for parameters that cannot be null.</summary>
	public virtual string? Check(string name, bool nullable) => null;

	/// <summary>A fixed statement the call needs, such as pinning a string.</summary>
	public virtual string? Fixed(string name) => null;

	/// <summary>A statement the call needs before it, such as building a native copy of a list.</summary>
	public virtual string? Prepare(string name) => null;

	/// <summary>A statement after the call, such as reading a list the call filled into an out parameter.</summary>
	public virtual string? Finish(string name) => null;

	public abstract IEnumerable<string> InteropArguments(string name, bool nullable);

	public abstract string ManagedReturn(string expression);

	/// <summary>Whether an object of this type can be null, so a default of nullptr can be written as null.</summary>
	public virtual bool AcceptsNull => false;
}

internal sealed class VoidConversion : Conversion {
	public static VoidConversion Instance { get; } = new();

	public override string Suffix => "Void";

	public override string? CallbackCType => "void";

	public override string CReturnType => "void";

	public override string Fallback => string.Empty;

	public override string InteropReturnType => "void";

	public override IEnumerable<string> CParameters(string name) => throw new InvalidOperationException("void is not a parameter type.");

	public override string ToCpp(string name) => throw new InvalidOperationException("void is not a parameter type.");

	public override string FromCpp(string expression) => expression;

	public override IEnumerable<string> InteropParameters(string name) => throw new InvalidOperationException("void is not a parameter type.");

	public override string ManagedType(bool nullable) => "void";

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => throw new InvalidOperationException("void is not a parameter type.");

	public override string ManagedReturn(string expression) => expression;
}

// A C bool's size is not guaranteed to match what .NET expects, so booleans cross as one byte.
internal sealed class BoolConversion : Conversion {
	public static BoolConversion Instance { get; } = new();

	public override string Suffix => "Bool";

	public override string? CallbackCType => "uint8_t";

	public override string FromCallback(string expression) => $"{expression} != 0";

	public override string ToCallback(string expression) => $"{expression} ? (byte)1 : (byte)0";

	public override string CReturnType => "uint8_t";

	public override string Fallback => "0";

	public override string InteropReturnType => "byte";

	public override IEnumerable<string> CParameters(string name) => [$"uint8_t {name}"];

	public override string ToCpp(string name) => $"{name} != 0";

	public override string FromCpp(string expression) => $"({expression}) ? 1 : 0";

	public override IEnumerable<string> InteropParameters(string name) => [$"byte {name}"];

	public override string ManagedType(bool nullable) => "bool";

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => [$"{name} ? (byte)1 : (byte)0"];

	public override string ManagedReturn(string expression) => $"{expression} != 0";
}

/// <summary>A number with the same form in C and C#, such as int32_t and int.</summary>
internal sealed class NumberConversion(string suffix, string cType, string cppType, string csharpType) : Conversion {
	public static NumberConversion Int { get; } = new("Int", "int32_t", "int", "int");

	public static NumberConversion Long { get; } = new("Long", "int64_t", "long", "long");

	public static NumberConversion Double { get; } = new("Double", "double", "double", "double");

	public static NumberConversion Float { get; } = new("Float", "float", "float", "float");

	public static NumberConversion Byte { get; } = new("Byte", "uint8_t", "unsigned char", "byte");

	public override string Suffix => suffix;

	public override string? CallbackCType => cType;

	public override string FromCallback(string expression) => $"static_cast<{cppType}>({expression})";

	public override string CReturnType => cType;

	public override string Fallback => "0";

	public override string InteropReturnType => csharpType;

	public override IEnumerable<string> CParameters(string name) => [$"{cType} {name}"];

	public override string ToCpp(string name) => $"static_cast<{cppType}>({name})";

	public override string FromCpp(string expression) => $"static_cast<{cType}>({expression})";

	public override IEnumerable<string> InteropParameters(string name) => [$"{csharpType} {name}"];

	public override string ManagedType(bool nullable) => csharpType;

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => [name];

	public override string ManagedReturn(string expression) => expression;
}

/// <summary>
/// A number the managed API shows as an enum, such as a style or an orientation. When the C++
/// parameter is an enum itself, <paramref name="cppType"/> names it, so the shim casts to it.
/// </summary>
internal sealed class EnumConversion(string enumName, NumberConversion number, string? cppType = null) : Conversion {
	public string EnumName { get; } = enumName;

	/// <summary>The C++ enum, when the parameter is one, such as wxItemKind.</summary>
	public string? CppType { get; } = cppType;

	public override string Suffix => EnumName;

	public override string? CallbackCType => number.CallbackCType;

	public override string FromCallback(string expression) => CppType is null ? number.FromCallback(expression) : $"static_cast<{CppType}>({expression})";

	public override string ToCallback(string expression) => $"({number.ManagedType(false)}){expression}";

	public override string CReturnType => number.CReturnType;

	public override string Fallback => "0";

	public override string InteropReturnType => number.InteropReturnType;

	public override IEnumerable<string> CParameters(string name) => number.CParameters(name);

	public override string ToCpp(string name) => CppType is null ? number.ToCpp(name) : $"static_cast<{CppType}>({name})";

	public override string FromCpp(string expression) => number.FromCpp(expression);

	public override IEnumerable<string> InteropParameters(string name) => number.InteropParameters(name);

	public override string ManagedType(bool nullable) => EnumName;

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => [$"({number.ManagedType(false)}){name}"];

	public override string ManagedReturn(string expression) => $"({EnumName}){expression}";

	public NumberConversion Number => number;
}

/// <summary>
/// An enum parameter whose C++ default depends on the platform, such as wxTreeCtrl's
/// wxTR_DEFAULT_STYLE. It is nullable in the managed API, and null passes the C++ default.
/// </summary>
/// <summary>
/// A string parameter whose C++ default is one of wxWidgets's string constants, such as
/// wxFileSelectorPromptStr. It is nullable in the managed API, and null, a null pointer in C, passes
/// the constant, so an empty string is still an empty string.
/// </summary>
internal sealed class StringDefaultConversion(string cppDefault) : Conversion {
	public override string Suffix => StringConversion.Instance.Suffix;

	public override string CReturnType => StringConversion.Instance.CReturnType;

	public override string Fallback => StringConversion.Instance.Fallback;

	public override string InteropReturnType => StringConversion.Instance.InteropReturnType;

	public override IEnumerable<string> CParameters(string name) => StringConversion.Instance.CParameters(name);

	public override string ToCpp(string name) => $"({name} != nullptr ? {StringConversion.Instance.ToCpp(name)} : wxString({cppDefault}))";

	public override string FromCpp(string expression) => StringConversion.Instance.FromCpp(expression);

	public override IEnumerable<string> InteropParameters(string name) => StringConversion.Instance.InteropParameters(name);

	public override string ManagedType(bool nullable) => "string?";

	public override string? Fixed(string name) => StringConversion.Instance.Fixed(name);

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => [$"{name.TrimStart('@')}Text", $"{name} is null ? 0 : NativeString.Length({name})"];

	public override string ManagedReturn(string expression) => StringConversion.Instance.ManagedReturn(expression);

	public override bool AcceptsNull => true;
}

internal sealed class PlatformDefaultConversion(EnumConversion inner, string cppDefault) : Conversion {
	public override string Suffix => inner.Suffix;

	public override string CReturnType => inner.CReturnType;

	public override string Fallback => inner.Fallback;

	public override string InteropReturnType => inner.InteropReturnType;

	public override IEnumerable<string> CParameters(string name) => [.. inner.CParameters(name), $"uint8_t {name}Given"];

	public override string ToCpp(string name) => $"({name}Given != 0 ? {inner.ToCpp(name)} : {cppDefault})";

	public override string FromCpp(string expression) => inner.FromCpp(expression);

	public override IEnumerable<string> InteropParameters(string name) => [.. inner.InteropParameters(name), $"byte {name}Given"];

	public override string ManagedType(bool nullable) => inner.EnumName + "?";

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => [$"({inner.Number.ManagedType(false)}){name}.GetValueOrDefault()", $"{name}.HasValue ? (byte)1 : (byte)0"];

	public override string ManagedReturn(string expression) => inner.ManagedReturn(expression);
}

// Text goes in as UTF-16 with a length, pinned rather than copied, and comes out as a wxk_String
// the managed side reads and frees.
internal class StringConversion : Conversion {
	public static StringConversion Instance { get; } = new();

	public override string Suffix => "String";

	public override string? CallbackCType => "wxk_String*";

	public override string FromCallback(string expression) => $"wxk::TakeString({expression})";

	public override string ToCallback(string expression) => $"NativeString.Create({expression})";

	public override string CReturnType => "wxk_String*";

	public override string Fallback => "nullptr";

	public override string InteropReturnType => "WxkString*";

	public override IEnumerable<string> CParameters(string name) => [$"const char16_t* {name}", $"size_t {name}Length"];

	public override string ToCpp(string name) => $"wxk::FromUtf16({name}, {name}Length)";

	public override string FromCpp(string expression) => $"wxk::ToUtf16({expression})";

	public override IEnumerable<string> InteropParameters(string name) => [$"char* {name}", $"nuint {name}Length"];

	public override string ManagedType(bool nullable) => "string";

	public override string? Check(string name, bool nullable) => $"ArgumentNullException.ThrowIfNull({name});";

	public override string? Fixed(string name) => $"fixed (char* {Pinned(name)} = {name})";

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => [Pinned(name), $"NativeString.Length({name})"];

	public override string ManagedReturn(string expression) => $"NativeString.Take({expression})";

	private static string Pinned(string name) => name.TrimStart('@') + "Text";
}

// UTF-8 text a function takes as a const char*, as wxUIActionSimulator::Text does, which .NET
// gives as a string, and the shim converts for the call.
internal sealed class Utf8StringConversion : StringConversion {
	public static new Utf8StringConversion Instance { get; } = new();

	public override string Suffix => "Utf8String";

	public override string ToCpp(string name) => $"wxk::FromUtf16({name}, {name}Length).utf8_str()";
}

// wxPoint and wxSize cross as structs with the same layout on both sides. wxDefaultPosition and
// wxDefaultSize, which are (-1, -1), become null in the managed API.
internal sealed class GeometryConversion(string suffix, string cType, string cppType, string managedType, string? cppDefault) : Conversion {
	public static GeometryConversion Point { get; } = new("Point", "wxk_Point", "wxPoint", "Point", "wxDefaultPosition");

	public static GeometryConversion Size { get; } = new("Size", "wxk_Size", "wxSize", "Size", "wxDefaultSize");

	// A grid bag sizer's cells, and the rows and columns an item spans, whose default is one of each.
	public static GeometryConversion GridPosition { get; } = new("GridPosition", "wxk_GridPosition", "wxGBPosition", "GridPosition", cppDefault: null);

	public static GeometryConversion GridSpan { get; } = new("GridSpan", "wxk_GridSpan", "wxGBSpan", "GridSpan", "wxDefaultSpan");

	public override string Suffix => suffix;

	public override string CReturnType => cType;

	public override string Fallback => $"{cType}{{-1, -1}}";

	public override string InteropReturnType => managedType;

	public override IEnumerable<string> CParameters(string name) => [$"{cType} {name}"];

	public override string ToCpp(string name) => $"wxk::From{suffix}({name})";

	public override string FromCpp(string expression) => $"wxk::To{suffix}({expression})";

	public override IEnumerable<string> InteropParameters(string name) => [$"{managedType} {name}"];

	public override string ManagedType(bool nullable) => nullable ? managedType + "?" : managedType;

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => [nullable ? $"{name} ?? global::WxKit.{managedType}.Default" : name];

	public override string ManagedReturn(string expression) => expression;

	public override bool AcceptsNull => true;

	public bool IsDefault(string value) => value == cppDefault;

	public string CppType => cppType;
}

// A pointer to a class WxKit wraps, which crosses as the class's handle. One coming back is wrapped
// in its managed object, which is made if WxKit has not seen the native one before.
internal sealed class ObjectConversion(BoundClass target) : Conversion {
	public BoundClass Target { get; } = target;

	public override string Suffix => Target.Name;

	public override string CReturnType => $"{Target.Handle.CType}*";

	public override string Fallback => "nullptr";

	public override string InteropReturnType => $"{Target.Handle.InteropType}*";

	public override IEnumerable<string> CParameters(string name) => [$"{Target.Handle.CType}* {name}"];

	public override string ToCpp(string name) => $"static_cast<{Target.CppName}*>(wxk::Unwrap({name}))";

	public override string FromCpp(string expression) => $"wxk::Wrap(static_cast<{Target.Handle.CppBase}*>({expression}))";

	public override IEnumerable<string> InteropParameters(string name) => [$"{Target.Handle.InteropType}* {name}"];

	public override string ManagedType(bool nullable) => nullable ? Target.Name + "?" : Target.Name;

	public override string? Check(string name, bool nullable) => nullable ? null : $"ArgumentNullException.ThrowIfNull({name});";

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => [nullable ? $"{name} is null ? null : ({Target.Handle.InteropType}*){name}.{Target.Handle.Accessor}" : $"({Target.Handle.InteropType}*){name}.{Target.Handle.Accessor}"];

	// A column or a tool comes back as the one managed object its control keeps for it.
	public override string ManagedReturn(string expression) => Target.Handle.IsKeptByOwner ? $"{Target.Name}.Wrap({expression})" : $"ObjectRegistry.Wrap<{Target.Name}>({expression})";

	public override bool AcceptsNull => true;
}

// A wxObject that may or may not be an event handler, such as an event's source. It crosses as
// itself, and comes back wrapped only when it is a class WxKit wraps.
internal sealed class AnyObjectConversion : Conversion {
	public static AnyObjectConversion Instance { get; } = new();

	public override string Suffix => "Object";

	public override string CReturnType => "wxk_Object*";

	public override string Fallback => "nullptr";

	public override string InteropReturnType => "WxkObject*";

	public override IEnumerable<string> CParameters(string name) => throw new InvalidOperationException("wxObject is only bound as a return type.");

	public override string ToCpp(string name) => throw new InvalidOperationException("wxObject is only bound as a return type.");

	public override string FromCpp(string expression) => $"wxk::Wrap({expression})";

	public override IEnumerable<string> InteropParameters(string name) => throw new InvalidOperationException("wxObject is only bound as a return type.");

	public override string ManagedType(bool nullable) => "EvtHandler?";

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => throw new InvalidOperationException("wxObject is only bound as a return type.");

	public override string ManagedReturn(string expression) => $"ObjectRegistry.Wrap<EvtHandler>({expression})";
}

// An unsigned C++ size or count, such as size_t, which the managed API shows as int, the way .NET
// shows counts. A negative value going in, or one too large for int coming back, throws.
internal sealed class CountConversion(string suffix, string cType, string interopType) : Conversion {
	public static CountConversion Size { get; } = new("SizeT", "size_t", "nuint");

	public static CountConversion Unsigned { get; } = new("UInt", "uint32_t", "uint");

	public override string Suffix => suffix;

	public override string CReturnType => cType;

	public override string Fallback => "0";

	public override string InteropReturnType => interopType;

	public override IEnumerable<string> CParameters(string name) => [$"{cType} {name}"];

	public override string ToCpp(string name) => name;

	public override string FromCpp(string expression) => $"static_cast<{cType}>({expression})";

	public override IEnumerable<string> InteropParameters(string name) => [$"{interopType} {name}"];

	public override string ManagedType(bool nullable) => "int";

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => [$"checked(({interopType}){name})"];

	public override string ManagedReturn(string expression) => $"checked((int){expression})";
}

// wxRect crosses as a struct with the same layout on both sides.
internal sealed class RectConversion : Conversion {
	public static RectConversion Instance { get; } = new();

	public override string Suffix => "Rect";

	public override string CReturnType => "wxk_Rect";

	public override string Fallback => "wxk_Rect{0, 0, 0, 0}";

	public override string InteropReturnType => "Rect";

	public override IEnumerable<string> CParameters(string name) => [$"wxk_Rect {name}"];

	public override string ToCpp(string name) => $"wxk::FromRect({name})";

	public override string FromCpp(string expression) => $"wxk::ToRect({expression})";

	public override IEnumerable<string> InteropParameters(string name) => [$"Rect {name}"];

	public override string ManagedType(bool nullable) => "Rect";

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => [name];

	public override string ManagedReturn(string expression) => expression;
}

// A wxColour crosses as its channels and whether it is set. An unset one, wxNullColour, is null in
// the managed API, so setting a window's color to null restores its default.
internal sealed class ColorConversion : Conversion {
	public static ColorConversion Instance { get; } = new();

	public override string Suffix => "Color";

	public override string CReturnType => "wxk_Colour";

	public override string Fallback => "wxk_Colour{0, 0, 0, 0, 0}";

	public override string InteropReturnType => "WxkColor";

	public override IEnumerable<string> CParameters(string name) => [$"wxk_Colour {name}"];

	public override string ToCpp(string name) => $"wxk::FromColour({name})";

	public override string FromCpp(string expression) => $"wxk::ToColour({expression})";

	public override IEnumerable<string> InteropParameters(string name) => [$"WxkColor {name}"];

	public override string ManagedType(bool nullable) => "Color?";

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => [$"global::WxKit.Color.ToNative({name})"];

	public override string ManagedReturn(string expression) => $"global::WxKit.Color.FromNative({expression})";

	public override bool AcceptsNull => true;
}

// A rectangle a call fills by reference, such as a list item's, is an out Rect.
internal sealed class RectOutConversion : Conversion {
	public static RectOutConversion Instance { get; } = new();

	public override string Suffix => "OutRect";

	public override string CReturnType => throw new InvalidOperationException("Output rectangles are only parameters.");

	public override string Fallback => "0";

	public override string InteropReturnType => throw new InvalidOperationException("Output rectangles are only parameters.");

	public override IEnumerable<string> CParameters(string name) => [$"wxk_Rect* {name}"];

	public override string ToCpp(string name) => $"wxk::OutValue<wxRect, wxk_Rect, wxk::ToRect>{{{name}}}";

	public override string FromCpp(string expression) => throw new InvalidOperationException("Output rectangles are only parameters.");

	public override IEnumerable<string> InteropParameters(string name) => [$"Rect* {name}"];

	public override string ManagedType(bool nullable) => "out Rect";

	// An out parameter has to be assigned before its address is taken.
	public override string? Prepare(string name) => $"{name} = default;";

	public override string? Fixed(string name) => $"fixed (Rect* {Pointer(name)} = &{name})";

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => [Pointer(name)];

	public override string ManagedReturn(string expression) => throw new InvalidOperationException("Output rectangles are only parameters.");

	private static string Pointer(string name) => name.TrimStart('@') + "Pointer";
}

// Strings a call fills by reference, such as a file dialog's paths, are an out array.
internal sealed class StringArrayOutConversion : Conversion {
	public static StringArrayOutConversion Instance { get; } = new();

	public override string Suffix => "OutStrings";

	public override string CReturnType => throw new InvalidOperationException("Output lists are only parameters.");

	public override string Fallback => "nullptr";

	public override string InteropReturnType => throw new InvalidOperationException("Output lists are only parameters.");

	public override IEnumerable<string> CParameters(string name) => [$"wxk_StringArray** {name}"];

	public override string ToCpp(string name) => $"wxk::OutValue<wxArrayString, wxk_StringArray*, wxk::CopyStrings>{{{name}}}";

	public override string FromCpp(string expression) => throw new InvalidOperationException("Output lists are only parameters.");

	public override IEnumerable<string> InteropParameters(string name) => [$"WxkStringArray** {name}"];

	public override string ManagedType(bool nullable) => "out string[]";

	public override string? Prepare(string name) => $"WxkStringArray* {Native(name)} = null;";

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => [$"&{Native(name)}"];

	public override string? Finish(string name) => $"{name} = NativeStringArray.Take({Native(name)});";

	public override string ManagedReturn(string expression) => throw new InvalidOperationException("Output lists are only parameters.");

	private static string Native(string name) => name.TrimStart('@') + "Native";
}

// A list that a call fills, as GetSelections(wxArrayInt&) and GetPaths(wxArrayString&) do, which
// the shim gives the call as a list of its own, and returns, as .NET returns a list. The function's
// FilledArgument says where the list goes among the call's arguments.
internal sealed class FilledListConversion(NumberListKind? kind) : Conversion {
	public static FilledListConversion Strings { get; } = new(null);

	public static FilledListConversion Of(NumberListKind kind) => new(kind);

	public const string Argument = "filled";

	public override string Suffix => kind is null ? "Strings" : kind.ElementType + "s";

	public override string CReturnType => kind is null ? "wxk_StringArray*" : "wxk_Numbers*";

	public override string Fallback => "nullptr";

	public override string InteropReturnType => kind is null ? "WxkStringArray*" : "WxkNumbers*";

	public override IEnumerable<string> CParameters(string name) => throw new InvalidOperationException("Filled lists are only return types.");

	public override string ToCpp(string name) => throw new InvalidOperationException("Filled lists are only return types.");

	public override string FromCpp(string expression) => $"[&] {{ {kind?.CppType ?? "wxArrayString"} {Argument}; (void)({expression}); return {(kind is null ? "wxk::CopyStrings" : "wxk::ToNumbers")}({Argument}); }}()";

	public override IEnumerable<string> InteropParameters(string name) => throw new InvalidOperationException("Filled lists are only return types.");

	public override string ManagedType(bool nullable) => kind is null ? "string[]" : kind.ElementType + "[]";

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => throw new InvalidOperationException("Filled lists are only return types.");

	public override string ManagedReturn(string expression) => kind is null ? $"NativeStringArray.Take({expression})" : $"NativeNumbers.{kind.Take}({expression})";
}

// Two ints that a call fills, such as a width and a height, which the shim gives the call as
// locals of its own, named first and second, and returns as a size.
internal sealed class FilledSizeConversion : Conversion {
	public static FilledSizeConversion Instance { get; } = new();

	public override string Suffix => "Size";

	public override string CReturnType => GeometryConversion.Size.CReturnType;

	public override string Fallback => GeometryConversion.Size.Fallback;

	public override string InteropReturnType => GeometryConversion.Size.InteropReturnType;

	public override IEnumerable<string> CParameters(string name) => throw new InvalidOperationException("Filled sizes are only return types.");

	public override string ToCpp(string name) => throw new InvalidOperationException("Filled sizes are only return types.");

	public override string FromCpp(string expression) => GeometryConversion.Size.FromCpp($"[&] {{ int first = 0; int second = 0; (void)({expression}); return wxSize(first, second); }}()");

	public override IEnumerable<string> InteropParameters(string name) => throw new InvalidOperationException("Filled sizes are only return types.");

	public override string ManagedType(bool nullable) => GeometryConversion.Size.ManagedType(nullable);

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => throw new InvalidOperationException("Filled sizes are only return types.");

	public override string ManagedReturn(string expression) => GeometryConversion.Size.ManagedReturn(expression);
}

// Ints after their count, as in SetStatusWidths(int n, const int widths[]), are any sequence of
// them, which passes both, so the count is not a parameter of its own.
internal sealed class CountedIntsConversion : Conversion {
	public static CountedIntsConversion Instance { get; } = new();

	public override string Suffix => "Ints";

	public override string CReturnType => throw new InvalidOperationException("Counted ints are only parameters.");

	public override string Fallback => "nullptr";

	public override string InteropReturnType => throw new InvalidOperationException("Counted ints are only parameters.");

	public override IEnumerable<string> CParameters(string name) => [$"const int32_t* {name}", $"size_t {name}Count"];

	// The count goes first, where the parameter it replaces was.
	public override string ToCpp(string name) => $"static_cast<int>({name}Count), {name}";

	public override string FromCpp(string expression) => throw new InvalidOperationException("Counted ints are only parameters.");

	public override IEnumerable<string> InteropParameters(string name) => [$"int* {name}", $"nuint {name}Count"];

	public override string ManagedType(bool nullable) => "IEnumerable<int>";

	public override string? Check(string name, bool nullable) => $"ArgumentNullException.ThrowIfNull({name});";

	public override string? Prepare(string name) => $"int[] {Values(name)} = [.. {name}];";

	public override string? Fixed(string name) => $"fixed (int* {Pointer(name)} = {Values(name)})";

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => [Pointer(name), $"(nuint){Values(name)}.Length"];

	public override string ManagedReturn(string expression) => throw new InvalidOperationException("Counted ints are only parameters.");

	private static string Values(string name) => name.TrimStart('@') + "Values";

	private static string Pointer(string name) => name.TrimStart('@') + "Pointer";
}

// What a list of numbers holds: ints, as in a wxArrayInt, or item ids, as in a wxArrayTreeItemIds.
// Each crosses as a list of 64-bit numbers, which holds an int or an id alike.
internal sealed record NumberListKind(string CppType, string ElementType, string Take, string? ToCpp) {
	public static NumberListKind Int { get; } = new("wxArrayInt", "int", "TakeInts", "wxk::ToIntArray");

	public static NumberListKind TreeItem { get; } = new("wxArrayTreeItemIds", "TreeItemId", "TakeTreeItems", ToCpp: null);

	public static NumberListKind DataViewItem { get; } = new("wxDataViewItemArray", "DataViewItem", "TakeDataViewItems", "wxk::ToDataViewItems");

	public static NumberListKind TreeListItem { get; } = new("wxTreeListItems", "TreeListItem", "TakeTreeListItems", ToCpp: null);

	public static NumberListKind? For(string cppType) => cppType switch {
		"wxArrayInt" => Int,
		"wxArrayTreeItemIds" => TreeItem,
		"wxDataViewItemArray" => DataViewItem,
		"wxTreeListItems" => TreeListItem,
		_ => null,
	};
}

// A list of numbers coming back is an array, and one going in is any sequence of them.
internal sealed class NumberListConversion(NumberListKind kind) : Conversion {
	public override string Suffix => kind.ElementType + "s";

	public override string CReturnType => "wxk_Numbers*";

	public override string Fallback => "nullptr";

	public override string InteropReturnType => "WxkNumbers*";

	public override IEnumerable<string> CParameters(string name) => [$"const int64_t* {name}", $"size_t {name}Count"];

	public override string ToCpp(string name) => $"{kind.ToCpp ?? throw new InvalidOperationException($"A {kind.CppType} cannot go in.")}({name}, {name}Count)";

	public override string FromCpp(string expression) => $"wxk::ToNumbers({expression})";

	public override IEnumerable<string> InteropParameters(string name) => [$"long* {name}", $"nuint {name}Count"];

	public override string ManagedType(bool nullable) => $"IEnumerable<{kind.ElementType}>";

	public override string ManagedReturnType(bool nullable) => kind.ElementType + "[]";

	public override string? Check(string name, bool nullable) => $"ArgumentNullException.ThrowIfNull({name});";

	public override string? Prepare(string name) => $"long[] {Values(name)} = NativeNumbers.From({name});";

	public override string? Fixed(string name) => $"fixed (long* {Pointer(name)} = {Values(name)})";

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => [Pointer(name), $"(nuint){Values(name)}.Length"];

	public override string ManagedReturn(string expression) => $"NativeNumbers.{kind.Take}({expression})";

	private static string Values(string name) => name.TrimStart('@') + "Values";

	private static string Pointer(string name) => name.TrimStart('@') + "Pointer";
}

// A list a call fills, passed by a reference that can change, such as a list box's selections,
// is an out array. The shim fills a list of its own, and hands a copy back as the call returns.
internal sealed class NumberListOutConversion(NumberListKind kind) : Conversion {
	public NumberListKind Kind => kind;

	public override string Suffix => "Out" + kind.ElementType + "s";

	public override string CReturnType => throw new InvalidOperationException("Output lists are only parameters.");

	public override string Fallback => "nullptr";

	public override string InteropReturnType => throw new InvalidOperationException("Output lists are only parameters.");

	public override IEnumerable<string> CParameters(string name) => [$"wxk_Numbers** {name}"];

	public override string ToCpp(string name) => $"wxk::OutNumbers<{kind.CppType}>{{{name}}}";

	public override string FromCpp(string expression) => throw new InvalidOperationException("Output lists are only parameters.");

	public override IEnumerable<string> InteropParameters(string name) => [$"WxkNumbers** {name}"];

	public override string ManagedType(bool nullable) => $"out {kind.ElementType}[]";

	public override string? Prepare(string name) => $"WxkNumbers* {Native(name)} = null;";

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => [$"&{Native(name)}"];

	public override string? Finish(string name) => $"{name} = NativeNumbers.{kind.Take}({Native(name)});";

	public override string ManagedReturn(string expression) => throw new InvalidOperationException("Output lists are only parameters.");

	private static string Native(string name) => name.TrimStart('@') + "Native";
}

// A wxDateTime is a DateTime in local time, which crosses as its parts. wxInvalidDateTime is null.
internal sealed class DateTimeConversion : Conversion {
	public static DateTimeConversion Instance { get; } = new();

	public override string Suffix => "DateTime";

	public override string CReturnType => "wxk_DateTime";

	public override string Fallback => "wxk_DateTime{0, 0, 0, 0, 0, 0, 0, 0}";

	public override string InteropReturnType => "WxkDateTime";

	public override IEnumerable<string> CParameters(string name) => [$"wxk_DateTime {name}"];

	public override string ToCpp(string name) => $"wxk::FromDateTime({name})";

	public override string FromCpp(string expression) => $"wxk::ToDateTime({expression})";

	public override IEnumerable<string> InteropParameters(string name) => [$"WxkDateTime {name}"];

	public override string ManagedType(bool nullable) => "DateTime?";

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => [$"global::WxKit.NativeDateTime.ToNative({name})"];

	public override string ManagedReturn(string expression) => $"global::WxKit.NativeDateTime.FromNative({expression})";

	public override bool AcceptsNull => true;
}

// A color whose default is one of wxWidgets's colors, such as *wxBLACK, which a null color stands for.
internal sealed class ColorDefaultConversion(string cppDefault) : Conversion {
	public override string Suffix => ColorConversion.Instance.Suffix;

	public override string CReturnType => ColorConversion.Instance.CReturnType;

	public override string Fallback => ColorConversion.Instance.Fallback;

	public override string InteropReturnType => ColorConversion.Instance.InteropReturnType;

	public override IEnumerable<string> CParameters(string name) => ColorConversion.Instance.CParameters(name);

	public override string ToCpp(string name) => $"({name}.valid != 0 ? {ColorConversion.Instance.ToCpp(name)} : wxColour({cppDefault}))";

	public override string FromCpp(string expression) => ColorConversion.Instance.FromCpp(expression);

	public override IEnumerable<string> InteropParameters(string name) => ColorConversion.Instance.InteropParameters(name);

	public override string ManagedType(bool nullable) => ColorConversion.Instance.ManagedType(nullable);

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => ColorConversion.Instance.InteropArguments(name, nullable);

	public override string ManagedReturn(string expression) => ColorConversion.Instance.ManagedReturn(expression);

	public override bool AcceptsNull => true;
}

// A wxArrayString goes in as a native copy of any sequence of strings, made for the call, and
// comes out as a string array.
internal sealed class StringArrayConversion : Conversion {
	public static StringArrayConversion Instance { get; } = new();

	public override string Suffix => "Strings";

	public override string CReturnType => "wxk_StringArray*";

	public override string Fallback => "nullptr";

	public override string InteropReturnType => "WxkStringArray*";

	public override IEnumerable<string> CParameters(string name) => [$"const wxk_StringArray* {name}"];

	public override string ToCpp(string name) => $"*wxk::Unwrap({name})";

	public override string FromCpp(string expression) => $"wxk::Wrap(new wxArrayString({expression}))";

	public override IEnumerable<string> InteropParameters(string name) => [$"WxkStringArray* {name}"];

	public override string ManagedType(bool nullable) => "IEnumerable<string>";

	public override string ManagedReturnType(bool nullable) => "string[]";

	public override string? Check(string name, bool nullable) => $"ArgumentNullException.ThrowIfNull({name});";

	public override string? Prepare(string name) => $"using NativeStringArray {Native(name)} = new({name});";

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => [$"{Native(name)}.Handle"];

	public override string ManagedReturn(string expression) => $"NativeStringArray.Take({expression})";

	private static string Native(string name) => name.TrimStart('@') + "Native";
}

// A value object, such as a font, crosses as a handle to a heap copy the managed side owns. One
// going in can be null, for the class's empty object; one coming back empty is null. A bundle of
// bitmaps or icons is made from one, as C++ would convert it.
internal sealed class ValueConversion(BoundClass target, string? wrapper = null, bool mutable = false) : Conversion {
	public BoundClass Target { get; } = target;

	public override string Suffix => Target.Name;

	public override string CReturnType => "wxk_Object*";

	public override string Fallback => "nullptr";

	public override string InteropReturnType => "WxkObject*";

	public override IEnumerable<string> CParameters(string name) => [mutable ? $"wxk_Object* {name}" : $"const wxk_Object* {name}"];

	public override string ToCpp(string name) => mutable ? $"wxk::ValueRef<{Target.CppName}>({name})" : wrapper is null ? $"wxk::ValueOf<{Target.CppName}>({name})" : $"{wrapper}(wxk::ValueOf<{Target.CppName}>({name}))";

	public override string FromCpp(string expression) => $"wxk::CopyValue<{Target.CppName}>({expression})";

	public override IEnumerable<string> InteropParameters(string name) => [$"WxkObject* {name}"];

	public override string ManagedType(bool nullable) => nullable ? Target.Name + "?" : Target.Name;

	public override string ManagedReturnType(bool nullable) => Target.Name + "?";

	public override string? Check(string name, bool nullable) => nullable && !mutable ? null : $"ArgumentNullException.ThrowIfNull({name});";

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => [nullable && !mutable ? $"{name} is null ? null : {name}.Native" : $"{name}.Native"];

	public override string ManagedReturn(string expression) => $"global::WxKit.{Target.Name}.Take({expression})";

	public override bool AcceptsNull => true;
}

// An object the managed side owns, such as a drawing context, passed by reference or pointer.
internal sealed class OwnedConversion(BoundClass target, bool byReference) : Conversion {
	public override string Suffix => target.Name;

	public override string CReturnType => throw new InvalidOperationException("Owned objects are only bound as parameters.");

	public override string Fallback => "nullptr";

	public override string InteropReturnType => throw new InvalidOperationException("Owned objects are only bound as parameters.");

	public override IEnumerable<string> CParameters(string name) => [$"wxk_Object* {name}"];

	public override string ToCpp(string name) => byReference ? $"*static_cast<{target.CppName}*>(wxk::Unwrap({name}))" : $"static_cast<{target.CppName}*>(wxk::Unwrap({name}))";

	public override string FromCpp(string expression) => throw new InvalidOperationException("Owned objects are only bound as parameters.");

	public override IEnumerable<string> InteropParameters(string name) => [$"WxkObject* {name}"];

	public override string ManagedType(bool nullable) => nullable ? target.Name + "?" : target.Name;

	public override string? Check(string name, bool nullable) => nullable ? null : $"ArgumentNullException.ThrowIfNull({name});";

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => [nullable ? $"{name} is null ? null : {name}.Native" : $"{name}.Native"];

	public override string ManagedReturn(string expression) => throw new InvalidOperationException("Owned objects are only bound as parameters.");

	public override bool AcceptsNull => !byReference;
}

// A number a function writes through a pointer, such as GetSelection's from and to, which the
// managed API gives as an out parameter. A long goes through the shim's OutLong, since a C long is
// 32 bits on Windows.
internal sealed class OutConversion(string suffix, string cType, string csharpType, bool isLong) : Conversion {
	public static OutConversion Int { get; } = new("OutInt", "int32_t", "int", isLong: false);

	public static OutConversion Long { get; } = new("OutLong", "int64_t", "long", isLong: true);

	public static OutConversion Double { get; } = new("OutDouble", "double", "double", isLong: false);

	public override string Suffix => suffix;

	public override string CReturnType => throw new InvalidOperationException("Output pointers are only parameters.");

	public override string Fallback => "0";

	public override string InteropReturnType => throw new InvalidOperationException("Output pointers are only parameters.");

	public override IEnumerable<string> CParameters(string name) => [$"{cType}* {name}"];

	public override string ToCpp(string name) => isLong ? $"wxk::OutLong{{{name}}}" : name;

	public override string FromCpp(string expression) => throw new InvalidOperationException("Output pointers are only parameters.");

	// A director's callback writes through the pointer wxWidgets gave the member.
	public override string CallbackParameterCType => $"{cType}*";

	public override string CallbackParameterInteropType => $"{csharpType}*";

	public override string ToCallbackArgument(string name) => name;

	// A long is 32 bits on Windows, so its pointer can't be passed on as an int64_t's.
	public bool CanCallBack => !isLong;

	public override IEnumerable<string> InteropParameters(string name) => [$"{csharpType}* {name}"];

	public override string ManagedType(bool nullable) => "out " + csharpType;

	// An out parameter has to be assigned before its address is taken.
	public override string? Prepare(string name) => $"{name} = 0;";

	public override string? Fixed(string name) => $"fixed ({csharpType}* {Pointer(name)} = &{name})";

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => [Pointer(name)];

	public override string ManagedReturn(string expression) => throw new InvalidOperationException("Output pointers are only parameters.");

	private static string Pointer(string name) => name.TrimStart('@') + "Pointer";
}

// A function that changes its object and returns it, such as wxFont::MakeBold, returns the same
// managed object, so calls chain.
internal sealed class SelfConversion(BoundClass owner) : Conversion {
	public override string Suffix => "Self";

	public override string CReturnType => "void";

	public override string Fallback => string.Empty;

	public override string InteropReturnType => "void";

	public override IEnumerable<string> CParameters(string name) => throw new InvalidOperationException("Self is only a return type.");

	public override string ToCpp(string name) => throw new InvalidOperationException("Self is only a return type.");

	public override string FromCpp(string expression) => expression;

	public override IEnumerable<string> InteropParameters(string name) => throw new InvalidOperationException("Self is only a return type.");

	public override string ManagedType(bool nullable) => owner.Name;

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => throw new InvalidOperationException("Self is only a return type.");

	public override string ManagedReturn(string expression) => expression;
}

// A list of windows, such as a window's children, which comes back as an array of their managed objects.
internal sealed class WindowListConversion : Conversion {
	public static WindowListConversion Instance { get; } = new();

	public override string Suffix => "Windows";

	public override string CReturnType => "wxk_WindowList*";

	public override string Fallback => "nullptr";

	public override string InteropReturnType => "WxkWindowList*";

	public override IEnumerable<string> CParameters(string name) => throw new InvalidOperationException("Window lists are only return types.");

	public override string ToCpp(string name) => throw new InvalidOperationException("Window lists are only return types.");

	public override string FromCpp(string expression) => $"wxk::ToWindowList({expression})";

	public override IEnumerable<string> InteropParameters(string name) => throw new InvalidOperationException("Window lists are only return types.");

	public override string ManagedType(bool nullable) => "Window[]";

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => throw new InvalidOperationException("Window lists are only return types.");

	public override string ManagedReturn(string expression) => $"global::WxKit.NativeWindowList.Take({expression})";
}

// An item id that wraps one pointer, as wxTreeItemId and wxDataViewItem do, crosses as that
// pointer and becomes a value type that holds it, such as TreeItemId. One whose pointer is typed,
// as wxTreeListItem's is, is made from it after a cast.
internal sealed class ItemIdConversion(string managedName, string cppName, string? pointerType = null) : Conversion {
	public static ItemIdConversion TreeItem { get; } = new("TreeItemId", "wxTreeItemId");

	public static ItemIdConversion DataViewItem { get; } = new("DataViewItem", "wxDataViewItem");

	public static ItemIdConversion TreeListItem { get; } = new("TreeListItem", "wxTreeListItem", "wxTreeListModelNode*");

	public override string Suffix => managedName;

	public override string? CallbackCType => "void*";

	public override string CallbackInteropType => "nint";

	public override string FromCallback(string expression) => Make(expression);

	public override string ToCallback(string expression) => $"{expression}.Value";

	public override string CReturnType => "void*";

	public override string Fallback => "nullptr";

	public override string InteropReturnType => "nint";

	public override IEnumerable<string> CParameters(string name) => [$"void* {name}"];

	public override string ToCpp(string name) => Make(name);

	public override string FromCpp(string expression) => $"static_cast<void*>(({expression}).GetID())";

	public override IEnumerable<string> InteropParameters(string name) => [$"nint {name}"];

	public override string ManagedType(bool nullable) => managedName;

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => [$"{name}.Value"];

	public override string ManagedReturn(string expression) => $"new global::WxKit.{managedName}({expression})";

	private string Make(string pointer) => pointerType is null ? $"{cppName}({pointer})" : $"{cppName}(static_cast<{pointerType}>({pointer}))";
}

// A wxVariant crosses as a handle to one, and is an object in the managed API: null, a string, a
// bool, a long, a double, or a bitmap, icon, or icon text. One going in is built for the call, and
// one coming back is read and freed.
internal sealed class VariantConversion : Conversion {
	public static VariantConversion Instance { get; } = new();

	public override string Suffix => "Variant";

	public override string CReturnType => "wxk_Variant*";

	public override string Fallback => "nullptr";

	public override string InteropReturnType => "WxkVariant*";

	public override IEnumerable<string> CParameters(string name) => [$"const wxk_Variant* {name}"];

	public override string ToCpp(string name) => $"*wxk::Unwrap(const_cast<wxk_Variant*>({name}))";

	public override string FromCpp(string expression) => $"wxk::Wrap(new wxVariant({expression}))";

	public override IEnumerable<string> InteropParameters(string name) => [$"WxkVariant* {name}"];

	public override string ManagedType(bool nullable) => "object?";

	public override string? Prepare(string name) => $"using NativeVariant {Native(name)} = new({name});";

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => [$"{Native(name)}.Handle"];

	public override string ManagedReturn(string expression) => $"NativeVariant.Take({expression})";

	public override bool AcceptsNull => true;

	private static string Native(string name) => name.TrimStart('@') + "Native";
}

// A wxVector<wxVariant>, such as a data view list row's values, is built from the managed values
// for the call.
// A wxClientData* or wxTreeItemData*, the data a control keeps with an item, is any object. It
// crosses as a GCHandle, which the shim wraps in data that frees it when the control deletes it.
// Only members whose control takes the data, and deletes what it replaces, may use it.
internal sealed class ItemDataConversion(string helper) : Conversion {
	public static ItemDataConversion Client { get; } = new("ToClientData");

	public static ItemDataConversion TreeItem { get; } = new("ToTreeItemData");

	public override string Suffix => "Data";

	public override string CReturnType => "void*";

	public override string Fallback => "nullptr";

	public override string InteropReturnType => "nint";

	public override IEnumerable<string> CParameters(string name) => [$"void* {name}"];

	public override string ToCpp(string name) => $"wxk::{helper}({name})";

	public override string FromCpp(string expression) => $"wxk::FromClientData({expression})";

	public override IEnumerable<string> InteropParameters(string name) => [$"nint {name}"];

	public override string ManagedType(bool nullable) => "object?";

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => [$"ItemData.Alloc({name})"];

	public override string ManagedReturn(string expression) => $"ItemData.Get({expression})";

	public override bool AcceptsNull => true;
}

// A wxVector<wxBitmapBundle>, such as a tree's images, is a sequence of bitmaps, which cross as an
// array of their handles that the shim makes bundles of. The bitmaps stay alive for the call.
internal sealed class BitmapListConversion : Conversion {
	public static BitmapListConversion Instance { get; } = new();

	public override string Suffix => "Bitmaps";

	public override string CReturnType => throw new InvalidOperationException("Lists of bitmaps are only parameters.");

	public override string Fallback => "nullptr";

	public override string InteropReturnType => throw new InvalidOperationException("Lists of bitmaps are only parameters.");

	public override IEnumerable<string> CParameters(string name) => [$"wxk_Object* const* {name}", $"size_t {name}Count"];

	public override string ToCpp(string name) => $"wxk::ToBitmapBundles({name}, {name}Count)";

	public override string FromCpp(string expression) => throw new InvalidOperationException("Lists of bitmaps are only parameters.");

	public override IEnumerable<string> InteropParameters(string name) => [$"WxkObject** {name}", $"nuint {name}Count"];

	public override string ManagedType(bool nullable) => "IEnumerable<Bitmap>";

	public override string? Check(string name, bool nullable) => $"ArgumentNullException.ThrowIfNull({name});";

	public override string? Prepare(string name) => $"using NativeBitmapList {Native(name)} = new({name});";

	public override string? Fixed(string name) => $"fixed (nint* {Pointer(name)} = {Native(name)}.Handles)";

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => [$"(WxkObject**){Pointer(name)}", $"(nuint){Native(name)}.Handles.Length"];

	public override string ManagedReturn(string expression) => throw new InvalidOperationException("Lists of bitmaps are only parameters.");

	private static string Native(string name) => name.TrimStart('@') + "Native";

	private static string Pointer(string name) => name.TrimStart('@') + "Pointer";
}

internal sealed class VariantListConversion : Conversion {
	public static VariantListConversion Instance { get; } = new();

	public override string Suffix => "Variants";

	public override string CReturnType => throw new InvalidOperationException("Lists of variants are only parameters.");

	public override string Fallback => "nullptr";

	public override string InteropReturnType => throw new InvalidOperationException("Lists of variants are only parameters.");

	public override IEnumerable<string> CParameters(string name) => [$"const wxk_VariantList* {name}"];

	public override string ToCpp(string name) => $"*wxk::Unwrap(const_cast<wxk_VariantList*>({name}))";

	public override string FromCpp(string expression) => throw new InvalidOperationException("Lists of variants are only parameters.");

	public override IEnumerable<string> InteropParameters(string name) => [$"WxkVariantList* {name}"];

	public override string ManagedType(bool nullable) => "IEnumerable<object?>";

	public override string? Check(string name, bool nullable) => $"ArgumentNullException.ThrowIfNull({name});";

	public override string? Prepare(string name) => $"using NativeVariantList {Native(name)} = new({name});";

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => [$"{Native(name)}.Handle"];

	public override string ManagedReturn(string expression) => throw new InvalidOperationException("Lists of variants are only parameters.");

	private static string Native(string name) => name.TrimStart('@') + "Native";
}
