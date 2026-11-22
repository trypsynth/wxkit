namespace WxKit.Tests;

// Fonts, bitmaps, images, and the other objects WxKit holds a reference to rather than a window.
public class ValueTests {
	[Fact]
	public Task CreatesAndReadsAFont() => Gui.Run(() => {
		using Font font = new(12, FontFamily.Swiss, FontStyle.Normal, FontWeight.Bold);
		Assert.True(font.IsOk);
		Assert.Equal(12, font.PointSize);
		Assert.Equal(FontWeight.Bold, font.Weight);
	});

	[Fact]
	public Task GivesAWindowsFontAsACopy() => Gui.WithFrame(frame => {
		using Font font = new(15, FontFamily.Modern, FontStyle.Italic, FontWeight.Normal);
		frame.Font = font;
		using Font? copy = frame.Font;
		Assert.NotNull(copy);
		Assert.NotSame(font, copy);
		Assert.Equal(15, copy.PointSize);
		Assert.Equal(FontStyle.Italic, copy.Style);
	});

	[Fact]
	// The default comes back, though not always at the size the frame started with: on macOS, a frame
	// starts with the 13 point system font, and its default afterwards is wxWidgets's normal font.
	public Task RestoresTheDefaultFontWhenSetToNull() => Gui.WithFrame(frame => {
		using Font? original = frame.Font;
		frame.Font = new Font(30, FontFamily.Default, FontStyle.Normal, FontWeight.Normal);
		frame.Font = null;
		using Font? restored = frame.Font;
		Assert.NotNull(restored);
		Assert.NotEqual(30, restored.PointSize);
		if (!OperatingSystem.IsMacOS()) Assert.Equal(original?.PointSize, restored.PointSize);
	});

	[Fact]
	public Task ConvertsBetweenBitmapsAndImages() => Gui.Run(() => {
		using Bitmap bitmap = new(32, 16);
		Assert.Equal(32, bitmap.Width);
		Assert.Equal(16, bitmap.Height);
		using Image? image = bitmap.ConvertToImage();
		Assert.NotNull(image);
		using Image? half = image.Scale(16, 8);
		Assert.Equal(new Size(16, 8), new Size(half!.Width, half.Height));
		using Bitmap back = new(half);
		Assert.Equal(16, back.Width);
	});

	[Fact]
	public Task SavesAndLoadsAPngOnceItsHandlerIsAdded() => Gui.Run(() => {
		Image.AddPngHandler();
		Image.AddPngHandler();
		string path = Path.Combine(Path.GetTempPath(), $"wxkit-{Guid.NewGuid():N}.png");
		try {
			using Image image = new(4, 4);
			image.SetRGB(1, 2, 10, 20, 30);
			Assert.True(image.SaveFile(path, BitmapType.Png));
			using Image loaded = new();
			Assert.True(loaded.LoadFile(path, BitmapType.Png));
			Assert.Equal((byte)20, loaded.GetGreen(1, 2));
		} finally {
			File.Delete(path);
		}
	});

	[Fact]
	public Task GivesAnEmptyValueAsNull() => Gui.Run(() => {
		using Image empty = new();
		Assert.False(empty.IsOk);
		// Copying an empty image out gives nothing, rather than an object that is not usable, and
		// wxWidgets says it was a mistake.
		Assert.Null(empty.Copy());
		Assert.Contains("invalid image", Assert.Single(Gui.TakeAssertions()).Message);
	});

	[Fact]
	public Task RefusesADisposedValue() => Gui.Run(() => {
		Font font = new(10, FontFamily.Default, FontStyle.Normal, FontWeight.Normal);
		font.Dispose();
		Assert.True(font.IsDisposed);
		Assert.Throws<ObjectDisposedException>(() => font.PointSize);
		font.Dispose();
	});

	[Fact]
	public Task FreesValuesTheCollectorFinds() => Gui.Run(async () => {
		Abandon();
		GC.Collect();
		GC.WaitForPendingFinalizers();
		// Each finalizer posts its release to the GUI thread, which runs them once this yields.
		await Task.Delay(50);
		static void Abandon() {
			for (int index = 0; index < 100; index++) _ = new Bitmap(8, 8);
		}
	});
}
