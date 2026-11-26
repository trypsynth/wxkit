namespace WxKit.Tests;

// The common controls, through their generated bindings.
public class ControlTests {
	[Fact]
	public Task FillsAChoiceFromAList() => Gui.WithFrame(frame => {
		Choice choice = new(frame, ["Red", "Green", "Blue"]);
		Assert.Equal(3, choice.Count);
		choice.Selection = 1;
		Assert.Equal(1, choice.Selection);
		Assert.Equal("Green", choice.GetString(1));
		Assert.Equal(3, choice.Append("Cyan"));
		Assert.Equal(["Red", "Green", "Blue", "Cyan"], choice.Strings);
	});

	[Fact]
	public Task ReplacesAListBoxesItems() => Gui.WithFrame(frame => {
		ListBox list = new(frame);
		list.Set(["One", "Two"]);
		Assert.Equal(2, list.Count);
		list.Append(["Three", "Four"]);
		Assert.Equal("Four", list.GetString(3));
	});

	[Fact]
	public Task RoundTripsValues() => Gui.WithFrame(frame => {
		CheckBox check = new(frame, "&Check");
		check.IsChecked = true;
		Assert.True(check.IsChecked);
		SpinCtrl spin = new(frame, max: 50);
		spin.Value = 42;
		Assert.Equal(42, spin.Value);
		Slider slider = new(frame, 10, 0, 100);
		Assert.Equal(10, slider.Value);
		Gauge gauge = new(frame, 200);
		gauge.Value = 150;
		Assert.Equal(150, gauge.Value);
		Assert.Equal(200, gauge.Range);
	});

	[Fact]
	public Task AddsNotebookPages() => Gui.WithFrame(frame => {
		Notebook notebook = new(frame);
		Assert.True(notebook.AddPage(new Panel(notebook), "&First"));
		Assert.True(notebook.AddPage(new Panel(notebook), "&Second", select: true));
		Assert.Equal(2, notebook.PageCount);
		Assert.Equal(1, notebook.Selection);
	});

	[Fact]
	public Task GivesNotebookPagesImages() => Gui.WithFrame(frame => {
		Notebook notebook = new(frame);
		using Bitmap first = new(16, 16);
		using Bitmap second = new(16, 16);
		notebook.SetImages([first, second]);
		Assert.Equal(2, notebook.ImageCount);
		Assert.True(notebook.AddPage(new Panel(notebook), "&First", imageId: 1));
		Assert.Equal(1, notebook.GetPageImage(0));
	});

	[Fact]
	public Task LaysOutWithAFlexGridSizer() => Gui.WithFrame(frame => {
		Panel panel = new(frame);
		FlexGridSizer grid = new(2, new Size(5, 5));
		grid.AddGrowableColumn(1, 1);
		grid.Add(new StaticText(panel, "&Name:"));
		grid.Add(new TextCtrl(panel), SizerFlags.Default.Expand());
		panel.Sizer = grid;
		panel.Layout();
	});

	[Fact]
	public Task CutsAnArraysCountWithIt() => Gui.WithFrame(frame => {
		// wxChoice(parent, id, pos, size, int n, const wxString choices[]) is bound without n.
		Choice empty = new(frame);
		Assert.Equal(0, empty.Count);
	});

	// Windows checks spelling in a rich text control, and macOS in a multi-line one. Linux would
	// need gspell, which WxKit leaves out.
	[Fact]
	public Task ChecksSpellingWhereThePlatformCan() => Gui.WithFrame(frame => {
		TextCtrl text = new(frame, style: TextCtrlStyle.MultiLine | TextCtrlStyle.Rich2);
		Assert.Equal(!OperatingSystem.IsLinux(), text.EnableProofCheck());
	});

	[Fact]
	public Task AcceptsDroppedFiles() => Gui.WithFrame(frame => {
		TextCtrl text = new(frame);
		text.DragAcceptFiles(true);
		text.DropFiles += (_, _) => { };
		text.DragAcceptFiles(false);
	});

	[Fact]
	public Task SelectsTextByStartAndLength() => Gui.WithFrame(frame => {
		TextCtrl text = new(frame, "hello world");
		text.SetSelection(6, 11);
		Assert.Equal(6, text.SelectionStart);
		Assert.Equal(5, text.SelectionLength);
		text.SelectionStart = 0;
		Assert.Equal("hello", text.StringSelection);
		text.SelectionLength = 4;
		Assert.Equal("hell", text.StringSelection);
	});
}
