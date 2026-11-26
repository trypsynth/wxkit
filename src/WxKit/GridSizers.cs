using WxKit.Interop;

namespace WxKit;

// A grid sizer is made with how many columns it has, and has as many rows as its items need.
// wxWidgets has four constructors, whose first number is the rows or the columns by how many
// numbers follow, which reads wrongly as often as not.

public unsafe partial class GridSizer {
	/// <summary>Makes a grid with this many columns, and as many rows as its items need.</summary>
	/// <param name="columns">How many columns the grid has.</param>
	/// <param name="gap">The space between items, across and down, which is none when left out.</param>
	public GridSizer(int columns, Size gap = default) : this(CreateNative(columns, gap)) {
	}

	private static WxkSizer* CreateNative(int columns, Size gap) {
		Application.VerifyAccess();
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(columns);
		return WxKitNative.GridSizerNewIntIntSize(0, columns, gap);
	}
}

public unsafe partial class FlexGridSizer {
	/// <summary>Makes a grid with this many columns, and as many rows as its items need.</summary>
	/// <param name="columns">How many columns the grid has.</param>
	/// <param name="gap">The space between items, across and down, which is none when left out.</param>
	public FlexGridSizer(int columns, Size gap = default) : this(CreateNative(columns, gap)) {
	}

	private static WxkSizer* CreateNative(int columns, Size gap) {
		Application.VerifyAccess();
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(columns);
		return WxKitNative.FlexGridSizerNewIntIntSize(0, columns, gap);
	}
}
