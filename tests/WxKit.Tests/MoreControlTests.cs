namespace WxKit.Tests;

// Bitmaps and bitmap buttons, the book controls besides the notebook, and other smaller controls.
public class MoreControlTests {
	// A window-modal dialog, which on macOS is a sheet that isn't waited for, gives what it
	// returned when it closes.
	[Fact]
	public Task SaysWhenAWindowModalDialogCloses() => Gui.WithFrame(async frame => {
		Dialog dialog = new(frame, "Window modal");
		Application.Post(() => dialog.EndModal(StandardId.Ok));
		Assert.Equal(StandardId.Ok, await dialog.ShowWindowModalAsync());
		dialog.Destroy();
	});

	// A progress dialog shows how far a task has got, and says whether the user canceled it.
	[Fact]
	public Task ShowsProgress() => Gui.WithFrame(frame => {
		ProgressDialog progress = new("Copying", "Starting", 10, frame, ProgressDialogStyle.CanAbort);
		try {
			Assert.True(progress.Update(4, "Copying four"));
			Assert.Equal(4, progress.Value);
			Assert.Equal("Copying four", progress.Message);
			Assert.Equal(10, progress.Range);
			Assert.True(progress.Pulse("Still copying"));
			Assert.False(progress.IsCanceled);
			Assert.False(progress.IsSkipped);
		} finally {
			progress.Destroy();
		}
	});

	[Fact]
	public Task ShowsBitmapsAndBitmapButtons() => Gui.WithFrame(frame => {
		using Bitmap picture = new(32, 24);
		StaticBitmap bitmap = new(frame, picture);
		using Bitmap? shown = bitmap.Bitmap;
		Assert.Equal(new Size(32, 24), shown?.DipSize);
		BitmapButton button = new(frame, picture);
		int clicks = 0;
		button.Click += (_, _) => clicks++;
		CommandLinkButton link = new(frame, "&Save", "Keeps your changes.");
		Assert.Equal("&Save", link.MainLabel);
		Assert.Equal("Keeps your changes.", link.Note);
	});

	[Fact]
	public Task AddsPagesToEachBook() => Gui.WithFrame(frame => {
		foreach (BookCtrlBase book in (BookCtrlBase[])[new Listbook(frame), new Choicebook(frame), new Simplebook(frame), new Treebook(frame)]) {
			Assert.True(book.AddPage(new Panel(book), "First"));
			Assert.True(book.AddPage(new Panel(book), "Second", select: true));
			Assert.Equal(2, book.PageCount);
			Assert.Equal(1, book.Selection);
		}
		Treebook tree = new(frame);
		tree.AddPage(new Panel(tree), "Settings");
		Assert.True(tree.AddSubPage(new Panel(tree), "Display"));
		Assert.Equal(0, tree.GetPageParent(1));
	});

	[Fact]
	public Task ShowsAnInfoBar() => Gui.WithFrame(frame => {
		InfoBar bar = new(frame);
		bar.AddButton(StandardId.Undo, "&Undo");
		Assert.True(bar.HasButtonId(StandardId.Undo));
		bar.ShowMessage("The file was deleted.", MessageIcon.Warning);
		bar.RemoveButton(StandardId.Undo);
		Assert.False(bar.HasButtonId(StandardId.Undo));
		bar.Dismiss();
	});

	[Fact]
	public Task RunsAnActivityIndicator() => Gui.WithFrame(frame => {
		ActivityIndicator indicator = new(frame);
		indicator.Start();
		Assert.True(indicator.IsRunning);
		indicator.Stop();
		Assert.False(indicator.IsRunning);
	});

	[Fact]
	public Task GivesComboBoxItemsBitmaps() => Gui.WithFrame(frame => {
		using Bitmap icon = new(16, 16);
		BitmapComboBox combo = new(frame);
		Assert.Equal(0, combo.Append("Red", icon));
		using Bitmap? given = combo.GetItemBitmap(0);
		Assert.Equal(new Size(16, 16), given?.DipSize);
	});

	[Fact]
	public Task RearrangesItems() => Gui.WithFrame(frame => {
		// A complemented position, ~1, is an item that starts unchecked.
		RearrangeList list = new(frame, [2, ~1, 0], ["Name", "Size", "Kind"]);
		Assert.Equal([2, ~1, 0], list.CurrentOrder);
		list.Selection = 0;
		Assert.True(list.MoveCurrentDown());
		Assert.Equal([~1, 2, 0], list.CurrentOrder);
		using RearrangeDialog dialog = new(frame, "Order the columns.", "Columns", [0, 1], ["Name", "Size"]);
		Assert.Equal([0, 1], dialog.Order);
	});

	[Fact]
	public Task ShowsABannerAndAMiniFrame() => Gui.WithFrame(frame => {
		BannerWindow banner = new(frame);
		banner.SetText("Welcome", "Choose where to start.");
		MiniFrame tools = new(frame, "Tools");
		tools.Destroy();
	});

	[Fact]
	public Task ScrollsAPanel() => Gui.WithFrame(frame => {
		ScrolledWindow scrolled = new(frame, size: new Size(200, 100));
		scrolled.SetScrollRate(0, 10);
		BoxSizer sizer = new(Orientation.Vertical);
		for (int index = 0; index < 30; index++) sizer.Add(new CheckBox(scrolled, $"Option {index}"));
		scrolled.Sizer = sizer;
		scrolled.FitInside();
		Assert.Equal(new Size(0, 10), scrolled.ScrollPixelsPerUnit);
		// A child's parent comes back as the same object, of the class WxKit made.
		Assert.Same(scrolled, scrolled.Children[0].Parent);
	});
}
