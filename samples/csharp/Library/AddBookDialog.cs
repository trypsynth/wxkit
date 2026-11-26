using WxKit;

namespace Library;

// Asks for a book's title, author, and year, and keeps the dialog open until the title and the author
// are filled in.
internal sealed class AddBookDialog : Dialog {
	private readonly TextCtrl _title;
	private readonly TextCtrl _author;
	private readonly SpinCtrl _year;

	public AddBookDialog(Window parent) : base(parent, "Add Book") {
		// Each label is made just before its field, so a screen reader names the field after it.
		FlexGridSizer form = new(2, new Size(10, 5));
		form.AddGrowableColumn(1);
		form.Add(new StaticText(this, "&Title:"), SizerFlags.Default.CenterVertical());
		_title = new TextCtrl(this, size: new Size(260, -1)) { Validator = new Required("a title") };
		form.Add(_title, SizerFlags.Default.Expand());
		form.Add(new StaticText(this, "&Author:"), SizerFlags.Default.CenterVertical());
		_author = new TextCtrl(this) { Validator = new Required("an author") };
		form.Add(_author, SizerFlags.Default.Expand());
		form.Add(new StaticText(this, "&Year:"), SizerFlags.Default.CenterVertical());
		_year = new SpinCtrl(this, min: 1450, max: DateTime.Now.Year, initial: DateTime.Now.Year);
		form.Add(_year);
		BoxSizer sizer = new(Orientation.Vertical);
		sizer.Add(form, SizerFlags.Default.Expand().Border());
		// OK and Cancel, in the order and with the labels the platform uses.
		StdDialogButtonSizer buttons = CreateStdDialogButtonSizer(DialogButtons.Ok | DialogButtons.Cancel);
		sizer.Add(buttons, SizerFlags.Default.Expand().Border(Direction.Left | Direction.Right | Direction.Bottom));
		Sizer = sizer;
		Fit();
		_title.SetFocus();
	}

	// The book, as the dialog has it, once it's closed with OK.
	public Book Book => new() { Title = _title.Value.Trim(), Author = _author.Value.Trim(), Year = _year.Value };

	// Keeps the dialog open, and says what's missing, while a field is empty.
	private sealed class Required(string what) : CustomValidator {
		protected override bool OnValidate(Window window, Window? parent) {
			if (window is TextCtrl text && text.Value.Trim().Length > 0) return true;
			MessageBox.Show($"Enter {what} for the book.", "Add Book", MessageDialogStyle.Ok | MessageDialogStyle.IconWarning, parent);
			window.SetFocus();
			return false;
		}
	}
}
