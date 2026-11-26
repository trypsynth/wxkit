using WxKit;

namespace Library;

// The library's window: a search box, a choice of order, the list of books, and buttons that add a
// book, remove one, and mark one read. The list shows the books that match the search, in the
// order chosen, which the model reads by row.
internal sealed class LibraryFrame : Frame {
	private readonly List<Book> _books = [
		new() { Title = "Pride and Prejudice", Author = "Jane Austen", Year = 1813, IsRead = true },
		new() { Title = "Frankenstein", Author = "Mary Shelley", Year = 1818 },
		new() { Title = "Moby-Dick", Author = "Herman Melville", Year = 1851 },
		new() { Title = "Middlemarch", Author = "George Eliot", Year = 1871, IsRead = true },
		new() { Title = "The Adventures of Huckleberry Finn", Author = "Mark Twain", Year = 1884 },
		new() { Title = "Dracula", Author = "Bram Stoker", Year = 1897 },
		new() { Title = "The Time Machine", Author = "H. G. Wells", Year = 1895, IsRead = true },
		new() { Title = "Emma", Author = "Jane Austen", Year = 1815 },
	];
	private readonly SearchCtrl _search;
	private readonly Choice _order;
	private readonly DataViewCtrl _list;
	private readonly BookModel _model;
	private List<Book> _shown = [];

	public LibraryFrame() : base(null, "Library", size: new Size(680, 480)) {
		Panel panel = new(this);
		// Each label is made just before the control it names, so a screen reader names the
		// control after it, and the tab order follows.
		StaticText searchLabel = new(panel, "&Search:");
		_search = new SearchCtrl(panel) { DescriptiveText = "Title or author" };
		StaticText orderLabel = new(panel, "&Order by:");
		_order = new Choice(panel, ["Title", "Author", "Year"]) { Selection = 0 };
		StaticText listLabel = new(panel, "&Books:");
		_list = new DataViewCtrl(panel, style: DataViewCtrlStyle.RowLines);
		_model = new BookModel(() => _shown, (row, read) => Replace(row, _shown[row] with { IsRead = read }));
		_list.Model = _model;
		_list.AppendTextColumn("Title", (int)BookColumn.Title, width: 260);
		_list.AppendTextColumn("Author", (int)BookColumn.Author, width: 160);
		_list.AppendTextColumn("Year", (int)BookColumn.Year, width: 60);
		_list.AppendToggleColumn("Read", (int)BookColumn.IsRead, DataViewCellMode.Activatable, width: 60);
		Button add = new(panel, "&Add Book...");
		Button remove = new(panel, "&Remove");
		Button markRead = new(panel, "&Mark Read or Unread");
		_search.TextChanged += (_, _) => ShowBooks();
		_order.SelectionChanged += (_, _) => ShowBooks();
		add.Click += (_, _) => AddBook();
		remove.Click += (_, _) => RemoveSelected();
		markRead.Click += (_, _) => ToggleSelected();
		// The buttons that act on a book only apply while one is selected.
		Bind(EventType.UpdateUI, (_, e) => {
			if (e.Id == remove.Id || e.Id == markRead.Id) e.IsEnabled = SelectedRow >= 0;
		});
		BoxSizer filters = new(Orientation.Horizontal);
		filters.Add(searchLabel, SizerFlags.Default.CenterVertical().Border());
		filters.Add(_search, new SizerFlags(1).CenterVertical().Border());
		filters.Add(orderLabel, SizerFlags.Default.CenterVertical().Border());
		filters.Add(_order, SizerFlags.Default.CenterVertical().Border());
		BoxSizer buttons = new(Orientation.Horizontal);
		buttons.Add(add, SizerFlags.Default.Border());
		buttons.Add(remove, SizerFlags.Default.Border());
		buttons.Add(markRead, SizerFlags.Default.Border());
		BoxSizer sizer = new(Orientation.Vertical);
		sizer.Add(filters, SizerFlags.Default.Expand());
		sizer.Add(listLabel, SizerFlags.Default.Border(Direction.Left | Direction.Right | Direction.Top));
		sizer.Add(_list, new SizerFlags(1).Expand().Border());
		sizer.Add(buttons);
		panel.Sizer = sizer;
		CreateStatusBar();
		ShowBooks();
		Center();
	}

	// The row of the selected book, or -1 when none is.
	private int SelectedRow => _list.HasSelection ? _model.GetRow(_list.Selection) : -1;

	// Shows the books that match the search, in the order chosen, and says how many there are.
	private void ShowBooks() {
		string search = _search.Value.Trim();
		IEnumerable<Book> matching = _books.Where(book => search.Length == 0 || book.Matches(search));
		_shown = [.. _order.Selection switch {
			1 => matching.OrderBy(static book => book.Author, StringComparer.CurrentCulture).ThenBy(static book => book.Year),
			2 => matching.OrderBy(static book => book.Year),
			_ => matching.OrderBy(static book => book.Title, StringComparer.CurrentCulture),
		}];
		_model.Reset(_shown.Count);
		ShowCounts();
	}

	private void ShowCounts() {
		int read = _shown.Count(static book => book.IsRead);
		SetStatusText($"{_shown.Count} of {_books.Count} books shown, {read} of them read");
	}

	// Puts a changed copy of the book in a row in place of the book, in the list and the library.
	private void Replace(int row, Book changed) {
		Book book = _shown[row];
		_books[_books.FindIndex(found => ReferenceEquals(found, book))] = changed;
		_shown[row] = changed;
		ShowCounts();
	}

	private void AddBook() {
		Book book;
		using (AddBookDialog dialog = new(this)) {
			if (dialog.ShowModal() != StandardId.Ok) return;
			book = dialog.Book;
		}
		_books.Add(book);
		// The search is cleared, so the new book is shown, and selected, so a screen reader reads it.
		_search.ChangeValue(string.Empty);
		ShowBooks();
		DataViewItem item = _model.GetItem(_shown.IndexOf(book));
		_list.Select(item);
		_list.EnsureVisible(item);
		_list.SetFocus();
	}

	private void RemoveSelected() {
		int row = SelectedRow;
		if (row < 0) return;
		Book book = _shown[row];
		_books.RemoveAt(_books.FindIndex(found => ReferenceEquals(found, book)));
		_shown.RemoveAt(row);
		_model.NotifyRowDeleted(row);
		ShowCounts();
	}

	private void ToggleSelected() {
		int row = SelectedRow;
		if (row < 0) return;
		Replace(row, _shown[row] with { IsRead = !_shown[row].IsRead });
		_model.NotifyRowChanged(row);
	}

	// For the smoke test: searches, selects the first book shown, marks it read, and removes it.
	internal void Exercise() {
		_search.Value = "Austen";
		if (_shown.Count != 2) throw new InvalidOperationException($"The search showed {_shown.Count} books, not 2.");
		_list.Select(_model.GetItem(0));
		ToggleSelected();
		RemoveSelected();
		if (_books.Count != 7) throw new InvalidOperationException($"The library has {_books.Count} books, not 7.");
	}
}
