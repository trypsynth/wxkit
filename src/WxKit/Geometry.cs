using WxKit.Interop;

namespace WxKit;

/// <summary>A position in pixels. Wraps wxPoint, and crosses into the shim as it is.</summary>
public readonly record struct Point(int X, int Y) {
	/// <summary>Lets wxWidgets choose. Wraps wxDefaultPosition.</summary>
	public static Point Default { get; } = new(-1, -1);
}

/// <summary>A cell of a <see cref="GridBagSizer"/>, counted from 0. Wraps wxGBPosition.</summary>
public readonly record struct GridPosition(int Row, int Column);

/// <summary>How many rows and columns an item of a <see cref="GridBagSizer"/> spans. Wraps wxGBSpan.</summary>
public readonly record struct GridSpan(int Rows, int Columns) {
	/// <summary>One row and one column. Wraps wxDefaultSpan.</summary>
	public static GridSpan Default { get; } = new(1, 1);
}

/// <summary>A rectangle in pixels. Wraps wxRect, and crosses into the shim as it is.</summary>
public readonly record struct Rect(int X, int Y, int Width, int Height) {
	public Point Position => new(X, Y);

	public Size Size => new(Width, Height);
}

/// <summary>
/// A color, with red, green, blue, and alpha channels from 0 to 255. Wraps wxColour. Where WxKit
/// takes or gives a color, null means none is set, so setting a window's color to null restores
/// its default.
/// </summary>
public readonly record struct Color(byte R, byte G, byte B, byte A = 255) {
	internal static WxkColor ToNative(Color? color) => color is Color value ? new WxkColor(value.R, value.G, value.B, value.A, 1) : default;

	internal static Color? FromNative(WxkColor color) => color.Valid != 0 ? new Color(color.Red, color.Green, color.Blue, color.Alpha) : null;
}

/// <summary>
/// An item in a <see cref="TreeCtrl"/>, which only the control gives out, since it is a pointer
/// the control follows. Wraps wxTreeItemId. Where there is no item, the control gives null.
/// </summary>
public readonly record struct TreeItemId {
	internal TreeItemId(nint value) => Value = value;

	// The item a native call gave, or null when it gave none.
	internal static TreeItemId? OrNull(nint value) => value == 0 ? null : new TreeItemId(value);

	internal nint Value { get; }
}

/// <summary>
/// An item in a <see cref="TreeListCtrl"/>, which only the control gives out, since it is a
/// pointer the control follows. Wraps wxTreeListItem. Where there is no item, the control gives null.
/// </summary>
public readonly record struct TreeListItem {
	internal TreeListItem(nint value) => Value = value;

	// The item a native call gave, or null when it gave none.
	internal static TreeListItem? OrNull(nint value) => value == 0 ? null : new TreeListItem(value);

	internal nint Value { get; }
}

/// <summary>A size in pixels. Wraps wxSize, and crosses into the shim as it is.</summary>
public readonly record struct Size(int Width, int Height) {
	/// <summary>Lets wxWidgets choose. Wraps wxDefaultSize.</summary>
	public static Size Default { get; } = new(-1, -1);
}
