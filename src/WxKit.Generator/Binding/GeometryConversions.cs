namespace WxKit.Generator.Binding;

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
