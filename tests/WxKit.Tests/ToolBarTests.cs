namespace WxKit.Tests;

public class ToolBarTests {
	[Fact]
	public Task AddsToolsToAFramesToolBar() => Gui.WithFrame(frame => {
		ToolBar? toolBar = frame.CreateToolBar();
		Assert.NotNull(toolBar);
		Assert.Same(toolBar, frame.ToolBar);
		using Bitmap? open = ArtProvider.GetBitmap(ArtId.FileOpen, ArtClient.ToolBar);
		Assert.NotNull(open);
		toolBar.AddTool(StandardId.Open, "Open", open, "Open a file");
		toolBar.AddSeparator();
		toolBar.AddTool(StandardId.Save, "Save", open);
		Assert.True(toolBar.Realize());
		Assert.Equal(3, toolBar.ToolCount);
	});

	[Fact]
	public Task GivesBackItsTools() => Gui.WithFrame(frame => {
		ToolBar toolBar = frame.CreateToolBar()!;
		using Bitmap? open = ArtProvider.GetBitmap(ArtId.FileOpen, ArtClient.ToolBar);
		Assert.NotNull(open);
		ToolBarTool? tool = toolBar.AddTool(StandardId.Open, "Open", open, "Open a file");
		ToolBarTool? separator = toolBar.AddSeparator();
		ToolBarTool? bold = toolBar.AddCheckTool(6100, "Bold", open);
		Assert.True(toolBar.Realize());
		Assert.NotNull(tool);
		Assert.NotNull(separator);
		Assert.NotNull(bold);
		Assert.Same(tool, toolBar.GetToolByPosition(0));
		Assert.Same(bold, toolBar.FindById(6100));
		Assert.Same(toolBar, tool.ToolBar);
		Assert.Equal(StandardId.Open, tool.Id);
		Assert.Equal("Open", tool.Label);
		Assert.True(tool.IsButton);
		Assert.True(separator.IsSeparator);
		Assert.Equal(MenuItemKind.Check, bold.Kind);
		Assert.True(bold.CanBeToggled);
		Assert.Equal("Open a file", tool.ShortHelp);
		tool.ShortHelp = "Open a document";
		Assert.Equal("Open a document", toolBar.GetToolShortHelp(StandardId.Open));
		tool.IsEnabled = false;
		Assert.False(toolBar.GetToolEnabled(StandardId.Open));
		bold.IsToggled = true;
		Assert.True(toolBar.GetToolState(6100));
	});

	[Fact]
	public Task MarksItsToolsDestroyed() => Gui.WithFrame(frame => {
		ToolBar toolBar = frame.CreateToolBar()!;
		using Bitmap? open = ArtProvider.GetBitmap(ArtId.FileOpen, ArtClient.ToolBar);
		Assert.NotNull(open);
		ToolBarTool first = toolBar.AddTool(6200, "First", open)!;
		ToolBarTool second = toolBar.AddTool(6201, "Second", open)!;
		ToolBarTool third = toolBar.AddTool(6202, "Third", open)!;
		Assert.True(toolBar.Realize());
		Assert.True(toolBar.DeleteTool(6201));
		Assert.True(second.IsDisposed);
		Assert.Throws<ObjectDisposedException>(() => second.Label);
		Assert.False(toolBar.DeleteTool(6201));
		Assert.True(toolBar.DeleteToolByPosition(1));
		Assert.True(third.IsDisposed);
		Assert.False(first.IsDisposed);
		toolBar.ClearTools();
		Assert.True(first.IsDisposed);
		ToolBarTool last = toolBar.AddTool(6203, "Last", open)!;
		toolBar.Destroy();
		Assert.True(last.IsDisposed);
	});
}
