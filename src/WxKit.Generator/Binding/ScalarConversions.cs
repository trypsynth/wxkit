namespace WxKit.Generator.Binding;

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

	// A long that counts or numbers things, such as a list's items or a text control's positions,
	// which is an int in the managed API, as .NET's indexes are.
	public static NumberConversion Index { get; } = new("Int", "int32_t", "long", "int");

	public static NumberConversion Double { get; } = new("Double", "double", "double", "double");

	public static NumberConversion Float { get; } = new("Float", "float", "float", "float");

	public static NumberConversion Byte { get; } = new("Byte", "uint8_t", "unsigned char", "byte");

	// Data a list keeps with an item, as wxListCtrl::GetItemData gives it, which SetItemData takes as a long.
	public static NumberConversion ItemData { get; } = new("Long", "int64_t", "wxUIntPtr", "long");

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

/// <summary>A number of milliseconds, which is a TimeSpan in the managed API, and the number everywhere else.</summary>
internal sealed class MillisecondsConversion(Conversion number) : Conversion {
	public override string Suffix => "TimeSpan";

	public override string CReturnType => number.CReturnType;

	public override string Fallback => number.Fallback;

	public override string InteropReturnType => number.InteropReturnType;

	public override IEnumerable<string> CParameters(string name) => number.CParameters(name);

	public override string ToCpp(string name) => number.ToCpp(name);

	public override string FromCpp(string expression) => number.FromCpp(expression);

	public override IEnumerable<string> InteropParameters(string name) => number.InteropParameters(name);

	public override string ManagedType(bool nullable) => "TimeSpan";

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => number.InteropArguments($"checked((long){name}.TotalMilliseconds)", nullable);

	public override string ManagedReturn(string expression) => $"TimeSpan.FromMilliseconds({number.ManagedReturn(expression)})";
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
