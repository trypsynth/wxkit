namespace WxKit.Tests;

public class DrawingTests {
	[Fact]
	public Task DrawsIntoABitmap() => Gui.Run(() => {
		using Bitmap bitmap = new(20, 20);
		using (MemoryDC dc = new(bitmap)) {
			using Brush white = new(new Color(255, 255, 255));
			dc.Background = white;
			dc.Clear();
			using Brush red = new(new Color(255, 0, 0));
			using Pen none = new(new Color(255, 0, 0));
			dc.Brush = red;
			dc.Pen = none;
			dc.DrawRectangle(new Rect(0, 0, 10, 20));
		}
		using Image? image = bitmap.ConvertToImage();
		Assert.NotNull(image);
		Assert.Equal((byte)255, image.GetRed(5, 5));
		Assert.Equal((byte)0, image.GetGreen(5, 5));
		Assert.Equal((byte)255, image.GetGreen(15, 5));
	});

	[Fact]
	// Windows paints during Update, and GTK on its next frame once the window is shown, so the test
	// waits for the paint, running the event loop meanwhile.
	public Task PaintsAWindow() => Gui.WithFrame(async frame => {
		Panel panel = new(frame, size: new Size(50, 50));
		int paints = 0;
		panel.Paint += (_, _) => {
			using PaintDC dc = new(panel);
			dc.DrawLine(0, 0, 50, 50);
			paints++;
		};
		frame.Show();
		panel.Refresh();
		panel.Update();
		await Gui.WaitUntil(() => paints > 0);
	});

	[Fact]
	public Task ScalesWhatItDraws() => Gui.Run(() => {
		using Bitmap bitmap = new(20, 20);
		using MemoryDC dc = new(bitmap);
		Assert.Equal((1, 1), dc.UserScale);
		dc.UserScale = (2, 3);
		Assert.Equal((2, 3), dc.UserScale);
		Assert.Equal(new Point(4, 6), dc.LogicalToDevice(new Point(2, 2)));
		dc.DeviceOrigin = new Point(5, 7);
		Assert.Equal(new Point(5, 7), dc.DeviceOrigin);
	});

	[Fact]
	public Task MeasuresText() => Gui.Run(() => {
		using Bitmap bitmap = new(100, 40);
		using MemoryDC dc = new(bitmap);
		using Font font = new(12, FontFamily.Default, FontStyle.Normal, FontWeight.Normal);
		dc.Font = font;
		using Font? set = dc.Font;
		Assert.Equal(12, set?.PointSize);
		Size wide = dc.GetTextExtent("Wide text");
		Size narrow = dc.GetTextExtent("W");
		Assert.True(wide.Width > narrow.Width);
		Assert.True(dc.CharHeight > 0);
		Assert.Equal(3, dc.GetPartialTextExtents("abc").Length);
	});
}
