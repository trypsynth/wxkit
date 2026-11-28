using WxKit.Interop;

namespace WxKit;

// A list gives what it knows of an item or a column as a ListItem it makes, where wxWidgets fills
// one the caller made and set up with what to ask for.

public unsafe partial class ListCtrl {
	/// <summary>
	/// Gives an item's text, image, data, and state in a column, or null when the list has no such
	/// item. Dispose the item when done with it. Wraps GetItem.
	/// </summary>
	/// <param name="item">The item's index.</param>
	/// <param name="column">The column, which is the first when left out.</param>
	public ListItem? GetItem(int item, int column = 0) {
		// wxWidgets logs an error for an item the list doesn't have, which shows a dialog.
		if (item < 0 || item >= ItemCount || column < 0 || column >= Math.Max(ColumnCount, 1)) return null;
		ListItem info = new() { Index = item, Column = column, Mask = Everything };
		info.SetStateMask(ListItemState.Selected | ListItemState.Focused | ListItemState.DropHighlighted | ListItemState.Cut);
		if (WxKitNative.ListCtrlGetItem(Native, info.Native) != 0) return info;
		info.Dispose();
		return null;
	}

	/// <summary>
	/// Gives a column's heading, alignment, width, and image, or null when the list has no such
	/// column. Dispose the item when done with it. Wraps GetColumn.
	/// </summary>
	public ListItem? GetColumn(int column) {
		if (column < 0 || column >= ColumnCount) return null;
		ListItem info = new() { Mask = Everything };
		if (WxKitNative.ListCtrlGetColumn(Native, column, info.Native) != 0) return info;
		info.Dispose();
		return null;
	}

	private const ListItemMask Everything = ListItemMask.State | ListItemMask.Text | ListItemMask.Image | ListItemMask.Data | ListItemMask.Width | ListItemMask.Format;
}

/// <summary>
/// The widths <see cref="ListCtrl.AppendColumn"/>, <see cref="ListCtrl.InsertColumn(int, string, ListColumnFormat, int)"/>,
/// and <see cref="ListCtrl.SetColumnWidth"/> take besides a number of pixels, which size a column
/// to what it shows.
/// </summary>
public static class ListColumnWidth {
	/// <summary>
	/// Sizes a column to its longest item, as <see cref="ListCtrl.SetColumnWidth"/>'s width. Wraps
	/// wxLIST_AUTOSIZE. It is the default of <see cref="ListCtrl.AppendColumn"/> and
	/// <see cref="ListCtrl.InsertColumn(int, string, ListColumnFormat, int)"/>, which give the column
	/// a fixed width of 80 pixels for it instead, since the column has no items yet.
	/// </summary>
	public const int AutoSize = -1;

	/// <summary>
	/// Sizes a column to its heading, and to its longest item when that is wider. Wraps
	/// wxLIST_AUTOSIZE_USEHEADER. On Windows, the last column is widened to fill the rest of the
	/// list's width too, whether it was added with this width or set to it.
	/// </summary>
	public const int AutoSizeUseHeader = -2;
}
