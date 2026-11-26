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
		using Image loaded = new();
		Assert.True(loaded.LoadFile(saved));
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
		using Image loaded = new();
		Assert.False(loaded.LoadFile(saved, BitmapType.Png));
	});

	[Fact]
	public Task SavesABmpWithoutAHandler() => Gui.Run(() => {
		using Image original = Pattern();
		using MemoryStream saved = new();
		Assert.True(original.SaveFile(saved, BitmapType.Bmp));
		Assert.Equal("BM"u8.ToArray(), saved.ToArray()[..2]);
		saved.Position = 0;
		using Image loaded = new();
		Assert.True(loaded.LoadFile(saved, BitmapType.Bmp));
		Assert.Equal((byte)200, loaded.GetRgb(new Point(1, 2)).R);
	});

	[Fact]
	public Task SaysWhenDataIsNotAnImage() => Gui.Run(() => {
		Image.AddPngHandler();
		using Image image = new();
		Assert.False(image.LoadFile(new MemoryStream("not an image"u8.ToArray())));
		Assert.False(image.IsOk);
		Assert.False(image.LoadFile(new MemoryStream()));
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
		using Image fromMemory = new();
		Assert.True(fromMemory.LoadFile(prefixed, BitmapType.Png));
		using BufferedStream buffered = new(new MemoryStream(png.ToArray()));
		using Image fromStream = new();
		Assert.True(fromStream.LoadFile(buffered));
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
