using WxKit;

namespace Checklist;

// A checklist that draws itself, rather than using a list control, as a game board or a custom
// list might. Up, Down, Home, and End move between items, Space ticks the current one, and the
// mouse does both. Screen readers see it as a list, through SetAccessibleRows, and hear each item
// as it becomes the current one, through SetAccessibleCurrentRow.
internal sealed class ChecklistControl : Panel {
	private readonly List<ChecklistItem> _items;
	private int _current;

	public ChecklistControl(Window parent, IEnumerable<string> items) : base(parent, style: WindowStyle.WantsChars) {
		_items = [.. items.Select(static text => new ChecklistItem { Text = text })];
		// A window that draws itself has no name of its own, so it takes the label's, which macOS
		// needs given, and Windows also finds as the label before the control.
		if (PreviousSibling is StaticText label) SetAccessibleName(label.LabelText);
		// The control paints all of itself, so wxWidgets need not erase it first.
		BackgroundStyle = BackgroundStyle.Paint;
		MinSize = new Size(240, RowHeight * Math.Max(_items.Count, 4));
		Paint += (_, _) => Draw();
		KeyDown += OnKeyDown;
		LeftDown += (_, e) => {
			SetFocus();
			MoveTo(e.Position.Y / RowHeight);
		};
		LeftDoubleClick += (_, _) => Toggle();
		GotFocus += (_, _) => Refresh();
		LostFocus += (_, _) => Refresh();
		SizeChanged += (_, e) => {
			Refresh();
			Describe();
			e.Skip();
		};
		Describe();
	}

	// Raised when an item is ticked or the current item changes, for the status strip.
	public event EventHandler? Changed;

	public IReadOnlyList<ChecklistItem> Items => _items;

	public int Current => _current;

	private int RowHeight => CharHeight + 8;

	private void OnKeyDown(object? sender, KeyEvent e) {
		switch (e.KeyCode) {
			case KeyCode.Up:
				MoveTo(_current - 1);
				break;
			case KeyCode.Down:
				MoveTo(_current + 1);
				break;
			case KeyCode.Home:
				MoveTo(0);
				break;
			case KeyCode.End:
				MoveTo(_items.Count - 1);
				break;
			case KeyCode.Space:
				Toggle();
				break;
			// The control wants every key, so it passes Tab on itself, as a standard control does.
			case KeyCode.Tab:
				Navigate(e.ShiftDown() ? NavigationFlags.IsBackward | NavigationFlags.FromTab : NavigationFlags.IsForward | NavigationFlags.FromTab);
				break;
			default:
				e.Skip();
				break;
		}
	}

	private void MoveTo(int index) {
		if (_items.Count == 0) return;
		int clamped = Math.Clamp(index, 0, _items.Count - 1);
		if (clamped == _current) return;
		_current = clamped;
		Refresh();
		Describe();
		Changed?.Invoke(this, EventArgs.Empty);
	}

	private void Toggle() {
		if (_items.Count == 0) return;
		_items[_current].IsDone = !_items[_current].IsDone;
		Refresh();
		Describe();
		Changed?.Invoke(this, EventArgs.Empty);
	}

	// Tells screen readers what the control shows: a row for each item, whose text says whether it
	// is done, and which one is current. Every item is shown here, so all of them are given.
	private void Describe() {
		int width = ClientSize.Width;
		IEnumerable<AccessibleRow> rows = _items.Select((item, index) => {
			Rect bounds = new(0, index * RowHeight, width, RowHeight);
			string label = $"{item.Text}, {(item.IsDone ? "done" : "not done")}";
			return new AccessibleRow(index, bounds, [new AccessiblePart(label, bounds)], IsSelected: index == _current);
		});
		SetAccessibleRows(rows, _items.Count);
		SetAccessibleCurrentRow(_items.Count == 0 ? -1 : _current);
	}

	private void Draw() {
		using PaintDC dc = new(this);
		Color background = SystemSettings.GetColor(SystemColor.ListBox) ?? new Color(255, 255, 255);
		Color text = SystemSettings.GetColor(SystemColor.ListBoxText) ?? new Color(0, 0, 0);
		Color highlight = SystemSettings.GetColor(SystemColor.Highlight) ?? new Color(0, 120, 215);
		Color highlightText = SystemSettings.GetColor(SystemColor.HighlightText) ?? new Color(255, 255, 255);
		using Brush backgroundBrush = new(background);
		dc.Background = backgroundBrush;
		dc.Clear();
		using Font? font = Font;
		dc.Font = font;
		int height = RowHeight;
		int box = CharHeight - 2;
		for (int index = 0; index < _items.Count; index++) {
			ChecklistItem item = _items[index];
			int top = index * height;
			bool isCurrent = index == _current && HasFocus;
			using Brush rowBrush = new(isCurrent ? highlight : background);
			using Pen rowPen = new(isCurrent ? highlight : background);
			dc.Brush = rowBrush;
			dc.Pen = rowPen;
			dc.DrawRectangle(new Rect(0, top, ClientSize.Width, height));
			Color ink = isCurrent ? highlightText : text;
			using Pen boxPen = new(ink);
			using Brush boxBrush = new(item.IsDone ? ink : (isCurrent ? highlight : background));
			dc.Pen = boxPen;
			dc.Brush = boxBrush;
			dc.DrawRectangle(new Rect(6, top + (height - box) / 2, box, box));
			dc.TextForeground = ink;
			dc.DrawText(item.Text, 12 + box, top + 4);
		}
	}
}

// An item on the list, which has to be given its text, and starts not done.
internal sealed class ChecklistItem {
	public required string Text { get; init; }

	public bool IsDone { get; set; }
}
