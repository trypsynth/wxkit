using System.Globalization;
using WxKit;

namespace Library;

// The books the list shows, by row, in the order it shows them, which the frame keeps. The data view
// asks for each cell as it draws it, so the model only reads what's on screen, however long the list.
internal sealed class BookModel(Func<IReadOnlyList<Book>> shown, Action<int, bool> markRead) : DataViewVirtualListModel(0) {
	protected override object? GetValue(int row, int column) {
		Book book = shown()[row];
		return (BookColumn)column switch {
			BookColumn.Title => book.Title,
			BookColumn.Author => book.Author,
			BookColumn.Year => book.Year.ToString(CultureInfo.CurrentCulture),
			BookColumn.IsRead => book.IsRead,
			_ => null,
		};
	}

	// The Read column's check box is the one cell the user changes in the list. The data view tells
	// itself the row changed once this returns true.
	protected override bool SetValue(object? value, int row, int column) {
		if ((BookColumn)column != BookColumn.IsRead || value is not bool read) return false;
		markRead(row, read);
		return true;
	}
}
