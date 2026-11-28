namespace WxKit.Tests;

// Images loaded from and saved to .NET streams, through memory.
public class ImageStreamTests {
	private static readonly byte[] PngSignature = [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A];

	[Fact]
	public Task SavesAndLoadsAPng() => Gui.Run(() => {
		Image.AddPngHandler();
		using Image original = Pattern();
		using MemoryStream saved = new();
		Assert.True(original.SaveFile(saved, BitmapType.Png));
		Assert.Equal(PngSignature, saved.ToArray()[..8]);
		saved.Position = 0;
		using Image? loaded = Image.FromStream(saved);
		Assert.NotNull(loaded);
		Assert.Equal(new Size(4, 3), loaded.Size);
		Assert.Equal((byte)200, loaded.GetRgb(new Point(1, 2)).R);
		Assert.Equal((byte)100, loaded.GetRgb(new Point(3, 0)).G);
		Assert.Equal(saved.Length, saved.Position);
	});

	// A memory stream can be positioned past its end, which leaves nothing to load.
	[Fact]
	public Task LoadsNothingPastAStreamsEnd() => Gui.Run(() => {
		Image.AddPngHandler();
		using Image original = Pattern();
		using MemoryStream saved = new();
		Assert.True(original.SaveFile(saved, BitmapType.Png));
		saved.Position = saved.Length + 1000;
		Assert.Null(Image.FromStream(saved, BitmapType.Png));
	});

	// No test adds the TGA handler, so saving as TGA has none. wxWidgets would open the file first,
	// leaving it empty, and then log a warning, which shows the user a dialog.
	[Fact]
	public Task SavesNothingInAFormatWithoutAHandler() => Gui.Run(() => {
		string folder = Directory.CreateTempSubdirectory("wxkit-save-").FullName;
		try {
			string kept = Path.Combine(folder, "kept.tga");
			File.WriteAllBytes(kept, [1, 2, 3]);
			string missing = Path.Combine(folder, "missing.tga");
			using Image image = Pattern();
			Assert.False(image.SaveFile(kept, BitmapType.Tga));
			Assert.False(image.SaveFile(kept, "image/tga"));
			Assert.False(image.SaveFile(missing, BitmapType.Tga));
			Assert.False(image.SaveFile(missing, "image/tga"));
			Assert.False(image.SaveFile(missing));
			Assert.False(image.SaveFile(Path.Combine(folder, "unknown.extension")));
			using Bitmap bitmap = new(image);
			Assert.False(bitmap.SaveFile(kept, BitmapType.Tga));
			Assert.False(bitmap.SaveFile(missing, BitmapType.Tga));
			Assert.Equal([1, 2, 3], File.ReadAllBytes(kept));
			Assert.False(File.Exists(missing));
			string bmp = Path.Combine(folder, "saved.bmp");
			Assert.True(bitmap.SaveFile(bmp, BitmapType.Bmp));
			Assert.True(image.SaveFile(Path.Combine(folder, "by-extension.bmp")));
			Assert.Equal("BM"u8.ToArray(), File.ReadAllBytes(bmp)[..2]);
		} finally {
			Directory.Delete(folder, recursive: true);
		}
	});

	[Fact]
	public Task SavesABmpWithoutAHandler() => Gui.Run(() => {
		using Image original = Pattern();
		using MemoryStream saved = new();
		Assert.True(original.SaveFile(saved, BitmapType.Bmp));
		Assert.Equal("BM"u8.ToArray(), saved.ToArray()[..2]);
		saved.Position = 0;
		using Image? loaded = Image.FromStream(saved, BitmapType.Bmp);
		Assert.NotNull(loaded);
		Assert.Equal((byte)200, loaded.GetRgb(new Point(1, 2)).R);
	});

	[Fact]
	public Task SaysWhenDataIsNotAnImage() => Gui.Run(() => {
		Image.AddPngHandler();
		Assert.Null(Image.FromStream(new MemoryStream("not an image"u8.ToArray())));
		Assert.Null(Image.FromStream(new MemoryStream()));
	});

	[Fact]
	public Task ReadsFromWhereAStreamIs() => Gui.Run(() => {
		Image.AddPngHandler();
		using Image original = Pattern();
		using MemoryStream png = new();
		original.SaveFile(png, BitmapType.Png);
		// A memory stream partway through a buffer of its own, and a stream that is not in memory.
		using MemoryStream prefixed = new();
		prefixed.Write("junk"u8);
		png.WriteTo(prefixed);
		prefixed.Position = 4;
		using Image? fromMemory = Image.FromStream(prefixed, BitmapType.Png);
		Assert.NotNull(fromMemory);
		using BufferedStream buffered = new(new MemoryStream(png.ToArray()));
		using Image? fromStream = Image.FromStream(buffered);
		Assert.NotNull(fromStream);
		Assert.Equal((byte)100, fromStream.GetRgb(new Point(3, 0)).G);
	});

	[Fact]
	public Task SavesABitmapAsBytesForAnotherLibrary() => Gui.Run(() => {
		Image.AddPngHandler();
		using Image original = Pattern();
		using Bitmap bitmap = new(original);
		using Image? converted = bitmap.ConvertToImage();
		Assert.NotNull(converted);
		using MemoryStream png = new();
		Assert.True(converted.SaveFile(png, BitmapType.Png));
		Assert.Equal(PngSignature, png.ToArray()[..8]);
	});

	private static Image Pattern() {
		Image image = new(4, 3);
		image.SetRgb(new Point(1, 2), new Color(200, 0, 0));
		image.SetRgb(new Point(3, 0), new Color(0, 100, 0));
		return image;
	}
}
