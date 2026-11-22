namespace WxKit.Tests;

// What a drop target does with content dragged over it and dropped. The system's own drag loop is
// modal and can't be driven from a test, so TestDrop gives the target the content as the system
// would, through the same enter, over, and drop calls.
public class DragDropTests {
	private static readonly DataFormat Shape = DataFormat.Named("WxKit.Tests.Shape");

	[Fact]
	public Task DropsText() => Gui.WithFrame(frame => {
		Panel panel = new(frame);
		DropTarget target = new(panel, DataFormat.Text);
		List<string> seen = [];
		target.DragEnter += (_, e) => seen.Add($"enter {e.Format} at {e.Position.X},{e.Position.Y}");
		target.DragOver += (_, e) => seen.Add($"over {e.Effect}");
		target.Drop += (_, e) => seen.Add($"drop {e.Text}");
		DragResult result = target.TestDrop(new ClipboardContent { Text = "hello" }, new Point(10, 20));
		Assert.Equal(DragResult.Copy, result);
		Assert.Equal(["enter Text at 10,20", "over Copy", "drop hello"], seen);
	});

	[Fact]
	public Task RefusesADrag() => Gui.WithFrame(frame => {
		Panel panel = new(frame);
		DropTarget target = new(panel, DataFormat.Text);
		bool dropped = false;
		bool left = false;
		target.DragOver += (_, e) => e.Effect = DragResult.None;
		target.DragLeave += (_, _) => left = true;
		target.Drop += (_, _) => dropped = true;
		Assert.Equal(DragResult.None, target.TestDrop(new ClipboardContent { Text = "no" }, new Point(1, 1)));
		Assert.False(dropped);
		Assert.True(left);
	});

	[Fact]
	public Task SaysWhatTheDropDid() => Gui.WithFrame(frame => {
		Panel panel = new(frame);
		DropTarget target = new(panel, DataFormat.Text);
		target.Drop += (_, e) => e.Effect = DragResult.Move;
		Assert.Equal(DragResult.Move, target.TestDrop(new ClipboardContent { Text = "moved" }, new Point(1, 1)));
	});

	[Fact]
	public Task IgnoresFormatsItDoesNotTake() => Gui.WithFrame(frame => {
		Panel panel = new(frame);
		DropTarget target = new(panel, DataFormat.Text);
		bool entered = false;
		target.DragEnter += (_, _) => entered = true;
		Assert.Equal(DragResult.None, target.TestDrop(new ClipboardContent { Files = [Path.GetTempPath()] }, new Point(1, 1)));
		Assert.False(entered);
	});

	[Fact]
	public Task TakesTheFirstFormatItAccepts() => Gui.WithFrame(frame => {
		Panel panel = new(frame);
		DropTarget target = new(panel, DataFormat.Files, DataFormat.Text);
		DropEventArgs? drop = null;
		target.Drop += (_, e) => drop = e;
		target.TestDrop(new ClipboardContent { Text = "words", Files = [Path.GetTempPath()] }, new Point(1, 1));
		Assert.NotNull(drop);
		Assert.Equal(DataFormat.Text, drop.Format);
		Assert.Equal("words", drop.Text);
		Assert.Null(drop.Files);
	});

	[Fact]
	public Task DropsFilesHtmlAndNamedData() => Gui.WithFrame(frame => {
		Panel panel = new(frame);
		DropTarget target = new(panel, DataFormat.Files, DataFormat.Html, Shape);
		List<DropEventArgs> drops = [];
		target.Drop += (_, e) => drops.Add(e);
		string file = Path.Combine(Path.GetTempPath(), "wxkit-drop.txt");
		target.TestDrop(new ClipboardContent { Files = [file] }, new Point(1, 1));
		target.TestDrop(new ClipboardContent { Html = "<b>bold</b>" }, new Point(1, 1));
		ClipboardContent shape = new();
		shape.Add(Shape.Name!, [1, 2, 3, 250]);
		target.TestDrop(shape, new Point(1, 1));
		Assert.Equal(3, drops.Count);
		Assert.Equal([file], drops[0].Files!);
		Assert.Contains("<b>bold</b>", drops[1].Html, StringComparison.Ordinal);
		Assert.Equal(Shape, drops[2].Format);
		Assert.Equal([1, 2, 3, 250], drops[2].Data);
	});

	[Fact]
	public Task DropsABitmap() => Gui.WithFrame(frame => {
		Panel panel = new(frame);
		DropTarget target = new(panel, DataFormat.Bitmap);
		Bitmap? dropped = null;
		bool raised = false;
		target.Drop += (_, e) => {
			raised = true;
			dropped = e.Bitmap;
		};
		DragResult result = target.TestDrop(new ClipboardContent { Bitmap = new Bitmap(new Image(16, 12)) }, new Point(1, 1));
		Assert.True(raised, result.ToString());
		Assert.NotNull(dropped);
		Assert.Equal(new Size(16, 12), dropped.Size);
	});

	[Fact]
	public Task StopsTakingDrops() => Gui.WithFrame(frame => {
		Panel panel = new(frame);
		DropTarget target = new(panel, DataFormat.Text);
		target.Remove();
		Assert.Equal(DragResult.Error, target.TestDrop(new ClipboardContent { Text = "gone" }, new Point(1, 1)));
	});
}
