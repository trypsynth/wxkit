using WxKit;

namespace Scrapbook;

// A list of snippets, which come from the clipboard or from text or files dropped on the list, and
// go back to the clipboard, or are dragged out of the list to another application. Copying and
// pasting are the keyboard's way to do what dragging does with the mouse.
internal sealed class ScrapbookFrame : Frame {
	private readonly List<Snippet> _snippets = [];
	private readonly ListBox _list;
	private bool _dragging;

	public ScrapbookFrame() : base(null, "Scrapbook", size: new Size(520, 400)) {
		// With a standard id and no text, an item gets the platform's label and shortcut.
		Menu edit = new();
		edit.Append(StandardId.Paste).Click += (_, _) => Paste();
		edit.Append(StandardId.Copy).Click += (_, _) => Copy();
		edit.Append(StandardId.Delete).Click += (_, _) => RemoveSelected();
		MenuBar menuBar = new();
		menuBar.Append(edit, "&Edit");
		MenuBar = menuBar;
		Panel panel = new(this);
		StaticText label = new(panel, "&Snippets, which you can paste, or drop text or files on:");
		_list = new ListBox(panel);
		Button paste = new(panel, "&Paste");
		Button copy = new(panel, "&Copy");
		Button remove = new(panel, "&Remove");
		paste.Click += (_, _) => Paste();
		copy.Click += (_, _) => Copy();
		remove.Click += (_, _) => RemoveSelected();
		// Copy and Remove only apply while a snippet is selected.
		Bind(EventType.UpdateUi, (_, e) => {
			if (e.Id == copy.Id || e.Id == remove.Id || e.Id == StandardId.Copy || e.Id == StandardId.Delete) e.Enable(_list.Selection >= 0);
		});
		// The list takes files and text dropped on it, and copies them, rather than moving them.
		DropTarget target = new(_list, DataFormat.Files, DataFormat.Text);
		target.DragOver += (_, e) => e.Effect = DragResult.Copy;
		target.Drop += (_, e) => {
			if (e.Files is [_, ..] files) Add(new FilesSnippet { Files = files });
			else if (e.Text is { Length: > 0 } text) Add(new TextSnippet { Text = text });
			e.Effect = DragResult.Copy;
		};
		// Dragging the selected snippet out of the list gives it to wherever it's dropped.
		_list.MouseMoved += (_, e) => {
			if (!e.Dragging() || !e.LeftIsDown() || _dragging || _list.Selection < 0) {
				e.Skip();
				return;
			}
			_dragging = true;
			try {
				DragDrop.DoDragDrop(_list, _snippets[_list.Selection].ToContent());
			} finally {
				_dragging = false;
			}
		};
		BoxSizer buttons = new(Orientation.Horizontal);
		buttons.Add(paste, SizerFlags.Default.Border());
		buttons.Add(copy, SizerFlags.Default.Border());
		buttons.Add(remove, SizerFlags.Default.Border());
		BoxSizer sizer = new(Orientation.Vertical);
		sizer.Add(label, SizerFlags.Default.Border(Direction.Left | Direction.Right | Direction.Top));
		sizer.Add(_list, new SizerFlags(1).Expand().Border());
		sizer.Add(buttons);
		panel.Sizer = sizer;
		CreateStatusBar();
		Center();
	}

	public IReadOnlyList<Snippet> Snippets => _snippets;

	public void Add(Snippet snippet) {
		_snippets.Add(snippet);
		_list.Append(snippet.Describe());
		_list.Selection = _snippets.Count - 1;
		SetStatusText($"{_snippets.Count} snippets");
	}

	public void Paste() {
		if (Snippet.FromClipboard() is Snippet snippet) Add(snippet);
		else SetStatusText("The clipboard has no text or files to paste.");
	}

	public void Copy() {
		int selected = _list.Selection;
		if (selected < 0) return;
		SetStatusText(Clipboard.SetContent(_snippets[selected].ToContent()) ? "Copied" : "The clipboard could not be opened.");
	}

	private void RemoveSelected() {
		int selected = _list.Selection;
		if (selected < 0) return;
		_snippets.RemoveAt(selected);
		_list.Delete(selected);
		if (_snippets.Count > 0) _list.Selection = Math.Min(selected, _snippets.Count - 1);
		SetStatusText($"{_snippets.Count} snippets");
	}
}
