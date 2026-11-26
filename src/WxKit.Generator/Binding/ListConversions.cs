namespace WxKit.Generator.Binding;

// An unsigned C++ size or count, such as size_t, which the managed API shows as int, the way .NET
// shows counts. A negative value going in, or one too large for int coming back, throws.
internal sealed class CountConversion(string suffix, string cType, string interopType) : Conversion {
	public static CountConversion Size { get; } = new("SizeT", "size_t", "nuint");

	public static CountConversion Unsigned { get; } = new("UInt", "uint32_t", "uint");

	public override string Suffix => suffix;

	// A callback gives a count as its C type, as a model's GetChildren gives how many children it added.
	public override string? CallbackCType => cType;

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
