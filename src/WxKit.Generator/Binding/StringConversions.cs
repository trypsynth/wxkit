namespace WxKit.Generator.Binding;

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

	public override string ManagedType(bool nullable) => nullable ? "IEnumerable<string>?" : "IEnumerable<string>";

	public override string ManagedReturnType(bool nullable) => "string[]";

	// A list that can be left out is null, which NativeStringArray makes empty.
	public override string? Check(string name, bool nullable) => nullable ? null : $"ArgumentNullException.ThrowIfNull({name});";

	public override string? Prepare(string name) => $"using NativeStringArray {Native(name)} = new({name});";

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => [$"{Native(name)}.Handle"];

	public override string ManagedReturn(string expression) => $"NativeStringArray.Take({expression})";

	private static string Native(string name) => name.TrimStart('@') + "Native";
}
