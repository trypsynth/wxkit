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
		int changes = 0;
		notebook.PageChanged += (_, _) => changes++;
		notebook.Selection = 0;
		Assert.Equal(0, notebook.Selection);
		Assert.Equal(0, changes);
	});

	[Fact]
	public Task SetsWhatWxWidgetsSetsWithAnotherMember() => Gui.WithFrame(frame => {
		Button button = new(frame, "&Save");
		Assert.Equal(string.Empty, button.ToolTipText);
		button.ToolTipText = "Saves the file";
		Assert.Equal("Saves the file", button.ToolTipText);
		button.ToolTipText = string.Empty;
		Assert.Equal(string.Empty, button.ToolTipText);
		CalendarCtrl calendar = new(frame);
		// A platform's own calendar, such as Windows', keeps no colors, and gives none back.
		Color? background = calendar.HeaderBackgroundColor;
		calendar.HeaderForegroundColor = new Color(10, 20, 30);
		if (calendar.HeaderForegroundColor is Color kept) Assert.Equal(new Color(10, 20, 30), kept);
		Assert.Equal(background, calendar.HeaderBackgroundColor);
		using Image image = new(4, 4);
		Assert.Null(image.MaskColor);
		image.MaskColor = new Color(255, 0, 255);
		Assert.True(image.HasMask);
		Assert.Equal(new Color(255, 0, 255), image.MaskColor);
		image.MaskColor = null;
		Assert.False(image.HasMask);
		// A color is a Color, and a pixel is at a Point.
		image.SetRgb(new Rect(0, 0, 4, 4), new Color(1, 2, 3));
		image.SetRgb(new Point(1, 1), new Color(9, 8, 7));
		Assert.Equal(new Color(9, 8, 7), image.GetRgb(new Point(1, 1)));
		image.Replace(new Color(1, 2, 3), new Color(4, 5, 6));
		Assert.Equal(new Color(4, 5, 6), image.GetRgb(new Point(0, 0)));
		using Image? wider = image.Resized(new Size(6, 4), new Point(0, 0), new Color(50, 60, 70));
		Assert.NotNull(wider);
		Assert.Equal(new Color(50, 60, 70), wider.GetRgb(new Point(5, 0)));
		Assert.False(image.IsTransparent(new Point(0, 0)));
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
		// The list can be left out, so a control made empty can still be given a style.
		ListBox several = new(frame, style: ListBoxStyle.Multiple);
		Assert.True(several.HasMultipleSelection);
		several.Append(["One", "Two"]);
		Assert.Equal(2, several.Count);
		ComboBox combo = new(frame, style: ComboBoxStyle.ReadOnly);
		Assert.Equal(string.Empty, combo.Value);
		ComboBox filled = new(frame, "Two", ["One", "Two"]);
		Assert.Equal(2, filled.Count);
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
