namespace WxKit.Tests;

// Members that fill a rectangle or a list by reference, or take ints after their count.
public class OutValueTests {
	[Fact]
	public Task GivesAStatusBarFieldsWidths() => Gui.WithFrame(frame => {
		StatusBar? bar = frame.CreateStatusBar(3);
		Assert.NotNull(bar);
		frame.SetStatusWidths([-1, 120, 60]);
		bar.SetStatusStyles([0, 0, 0]);
		Assert.Equal(3, bar.FieldsCount);
		Assert.True(bar.TryGetFieldRect(1, out Rect field));
		Assert.True(field.Width >= 0);
		Assert.Throws<ArgumentNullException>(() => frame.SetStatusWidths(null!));
	});

	[Fact]
	public Task GivesAListItemsRectangle() => Gui.WithFrame(frame => {
		ListView list = new(frame);
		list.InsertColumn(0, "Name");
		list.InsertItem(0, "readme.md");
		frame.Show();
		Assert.True(list.TryGetItemRect(0, out Rect item));
		Assert.True(item.Height > 0);
	});

	[Fact]
	public Task GivesAFileDialogsPaths() => Gui.WithFrame(frame => {
		using FileDialog dialog = new(frame, style: FileDialogStyle.Open | FileDialogStyle.Multiple);
		IReadOnlyList<string> paths = dialog.Paths;
		IReadOnlyList<string> filenames = dialog.FileNames;
		Assert.NotNull(paths);
		Assert.Equal(paths.Count, filenames.Count);
	});

	[Fact]
	public Task LimitsATextControlsLength() => Gui.WithFrame(frame => {
		TextCtrl text = new(frame, "hello");
		text.SetMaxLength(10);
		Assert.Equal(5, text.LastPosition);
	});
}
