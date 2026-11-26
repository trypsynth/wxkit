namespace WxKit.Generator.Binding;

// A number a function writes through a pointer, such as GetSelection's from and to, which the
// managed API gives as an out parameter. A long goes through the shim's OutLong, since a C long is
// 32 bits on Windows.
internal sealed class OutConversion(string suffix, string cType, string csharpType, bool isLong) : Conversion {
	public static OutConversion Int { get; } = new("OutInt", "int32_t", "int", isLong: false);

	public static OutConversion Long { get; } = new("OutLong", "int64_t", "long", isLong: true);

	// A long that is an index or a position, which the managed API gives as an int, through a long of its own.
	public static OutConversion Index { get; } = new("OutLong", "int64_t", "long", isLong: true) { IsIndex = true };

	private bool IsIndex { get; init; }

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

	public override string ManagedType(bool nullable) => "out " + (IsIndex ? "int" : csharpType);

	// An out parameter has to be assigned before its address is taken.
	public override string? Prepare(string name) => IsIndex ? $"{csharpType} {Wide(name)} = 0;" : $"{name} = 0;";

	public override string? Fixed(string name) => IsIndex ? null : $"fixed ({csharpType}* {Pointer(name)} = &{name})";

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => [IsIndex ? "&" + Wide(name) : Pointer(name)];

	public override string? Finish(string name) => IsIndex ? $"{name} = checked((int){Wide(name)});" : null;

	private static string Wide(string name) => name.TrimStart('@') + "Wide";

	public override string ManagedReturn(string expression) => throw new InvalidOperationException("Output pointers are only parameters.");

	private static string Pointer(string name) => name.TrimStart('@') + "Pointer";
}

// What a director's hand-written callback gives back through a pointer, such as an accessible
// object's name. The callback writes a C value, which the shim's CallbackOut, a temporary in the
// call, converts and writes through wxWidgets' own pointer, or reference, once the callback returns.
internal sealed class CallbackOutConversion(string cppType, string cType, string interopType, bool byReference) : Conversion {
	public override string Suffix => "Out" + interopType.TrimEnd('*');

	public override string CReturnType => throw new InvalidOperationException("Only a director's callback gives a value through a pointer this way.");

	public override string Fallback => "{}";

	public override string InteropReturnType => throw new InvalidOperationException("Only a director's callback gives a value through a pointer this way.");

	public override IEnumerable<string> CParameters(string name) => throw new InvalidOperationException("Only a director's callback gives a value through a pointer this way.");

	public override string ToCpp(string name) => throw new InvalidOperationException("Only a director's callback gives a value through a pointer this way.");

	public override string FromCpp(string expression) => throw new InvalidOperationException("Only a director's callback gives a value through a pointer this way.");

	public override IEnumerable<string> InteropParameters(string name) => throw new InvalidOperationException("Only a director's callback gives a value through a pointer this way.");

	public override string ManagedType(bool nullable) => interopType + "*";

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => throw new InvalidOperationException("Only a director's callback gives a value through a pointer this way.");

	public override string ManagedReturn(string expression) => throw new InvalidOperationException("Only a director's callback gives a value through a pointer this way.");

	public override string CallbackParameterCType => cType + "*";

	public override string CallbackParameterInteropType => interopType + "*";

	public override string ToCallbackArgument(string name) => $"wxk::CallbackOut<{cppType}, {cType}>{{{(byReference ? "&" : string.Empty)}{name}}}";
}

// An object a director's hand-written callback is lent for the call, as its handle, such as the
// variant a model fills with a value, or the array it fills with an item's children.
internal sealed class CallbackLentConversion(string cType, string interopType, string argumentFormat) : Conversion {
	public override string Suffix => interopType.TrimEnd('*');

	public override string CReturnType => throw new InvalidOperationException("Only a director's callback is lent an object this way.");

	public override string Fallback => "nullptr";

	public override string InteropReturnType => throw new InvalidOperationException("Only a director's callback is lent an object this way.");

	public override IEnumerable<string> CParameters(string name) => throw new InvalidOperationException("Only a director's callback is lent an object this way.");

	public override string ToCpp(string name) => throw new InvalidOperationException("Only a director's callback is lent an object this way.");

	public override string FromCpp(string expression) => throw new InvalidOperationException("Only a director's callback is lent an object this way.");

	public override IEnumerable<string> InteropParameters(string name) => throw new InvalidOperationException("Only a director's callback is lent an object this way.");

	public override string ManagedType(bool nullable) => interopType;

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => throw new InvalidOperationException("Only a director's callback is lent an object this way.");

	public override string ManagedReturn(string expression) => throw new InvalidOperationException("Only a director's callback is lent an object this way.");

	public override string CallbackParameterCType => cType;

	public override string CallbackParameterInteropType => interopType;

	public override string ToCallbackArgument(string name) => string.Format(System.Globalization.CultureInfo.InvariantCulture, argumentFormat, name);
}
