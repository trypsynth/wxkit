using WxKit.Interop;

namespace WxKit;

// Adding takes SizerFlags, as Sizer.Add does, though a grid bag sizer only uses their flags and
// border, since its cells, not proportions, decide how items grow. wxWidgets asserts when a cell
// is taken, and deletes a sizer it could not add, which WxKit still tracks, so each checks first.
public unsafe partial class GridBagSizer {
	/// <summary>Adds a window at a cell. Returns false when an item is there already, which leaves the window out.</summary>
	public bool Add(Window window, GridPosition position, GridSpan? span = null, SizerFlags flags = default) {
		ArgumentNullException.ThrowIfNull(window);
		GridSpan cells = span ?? GridSpan.Default;
		if (IsTaken(position, cells)) return false;
		WxKitNative.GridBagSizerAddWindowGridPositionGridSpanIntInt(Native, window.Native, position, cells, flags.FlagsValue, Border(flags));
		return true;
	}

	/// <summary>Adds a sizer at a cell, which then belongs to this one. Returns false when an item is there already, which leaves the sizer out.</summary>
	public bool Add(Sizer sizer, GridPosition position, GridSpan? span = null, SizerFlags flags = default) => Adopt(sizer, child => {
		GridSpan cells = span ?? GridSpan.Default;
		if (IsTaken(position, cells)) return false;
		WxKitNative.GridBagSizerAddSizerGridPositionGridSpanIntInt(Native, (WxkSizer*)child, position, cells, flags.FlagsValue, Border(flags));
		return true;
	});

	/// <summary>Adds empty space of a size at a cell. Returns false when an item is there already.</summary>
	public bool AddSpacer(Size size, GridPosition position, GridSpan? span = null, SizerFlags flags = default) {
		GridSpan cells = span ?? GridSpan.Default;
		if (IsTaken(position, cells)) return false;
		WxKitNative.GridBagSizerAddIntIntGridPositionGridSpanIntInt(Native, size.Width, size.Height, position, cells, flags.FlagsValue, Border(flags));
		return true;
	}

	private bool IsTaken(GridPosition position, GridSpan span) => WxKitNative.GridBagSizerCheckForIntersection(Native, position, span) != 0;
}
