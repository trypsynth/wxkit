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
		Assert.Equal((byte)255, image.GetRgb(new Point(5, 5)).R);
		Assert.Equal((byte)0, image.GetRgb(new Point(5, 5)).G);
		Assert.Equal((byte)255, image.GetRgb(new Point(15, 5)).G);
	});

	[Fact]
	// Windows paints during Update, and GTK on its next frame once the window is shown, so the test
	// waits for the paint, running the event loop meanwhile.
	public Task PaintsAWindow() => Gui.WithFrame(async frame => {
		Panel panel = new(frame, size: new Size(50, 50));
		int paints = 0;
		PaintDC? lent = null;
		panel.Paint += (_, e) => {
			lent = e.DC;
			Assert.Same(lent, e.DC);
			lent.DrawLine(new Point(0, 0), new Point(50, 50));
			paints++;
		};
		frame.Show();
		panel.Refresh();
		panel.Update();
		await Gui.WaitUntil(() => paints > 0);
		// The DC was the event's, which disposed it when the handler returned.
		Assert.True(lent?.IsDisposed);
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
	public Task MakesABitmapForADisplaysScale() => Gui.Run(() => {
		using Bitmap bitmap = Bitmap.FromDipSize(new Size(10, 20), 2);
		Assert.True(bitmap.IsOk);
		Assert.Equal(2, bitmap.ScaleFactor);
		Assert.Equal(new Size(10, 20), bitmap.DipSize);
		Assert.Throws<ArgumentOutOfRangeException>(() => Bitmap.FromLogicalSize(new Size(0, 5), 1));
	});

	// A place is a Point and an extent a Size, in what copies from one drawing to another.
	[Fact]
	public Task CopiesToAPoint() => Gui.Run(() => {
		using Bitmap from = new(10, 10);
		using Bitmap to = new(10, 10);
		using (MemoryDC source = new(from)) {
			using MemoryDC target = new(to);
			using Brush red = new(new Color(255, 0, 0));
			using Brush white = new(new Color(255, 255, 255));
			source.Background = red;
			source.Clear();
			target.Background = white;
			target.Clear();
			Assert.True(target.Blit(new Point(5, 0), new Size(5, 10), source, new Point(0, 0)));
		}
		using Image? image = to.ConvertToImage();
		Assert.NotNull(image);
		Assert.Equal(new Color(255, 255, 255), image.GetRgb(new Point(2, 2)));
		Assert.Equal(new Color(255, 0, 0), image.GetRgb(new Point(7, 2)));
		using Image small = new(2, 2);
		small.SetRgb(new Rect(0, 0, 2, 2), new Color(0, 0, 255));
		image.Paste(small, new Point(1, 1));
		Assert.Equal(new Color(0, 0, 255), image.GetRgb(new Point(2, 2)));
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
