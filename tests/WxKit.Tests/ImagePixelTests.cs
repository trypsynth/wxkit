namespace WxKit.Tests;

// An image's pixels in bulk, as bytes, row by row from the top left.
public class ImagePixelTests {
	[Fact]
	public Task MakesAnImageFromItsPixels() => Gui.Run(() => {
		// Two by two: red, green on the top row, and blue, white on the bottom one.
		byte[] rgb = [255, 0, 0, 0, 255, 0, 0, 0, 255, 255, 255, 255];
		using Image image = new(new Size(2, 2), rgb);
		Assert.Equal(new Size(2, 2), image.Size);
		Assert.False(image.HasAlpha);
		Assert.Equal(new Color(0, 255, 0), image.GetRgb(new Point(1, 0)));
		Assert.Equal(new Color(0, 0, 255), image.GetRgb(new Point(0, 1)));
		Assert.Equal(rgb, image.GetRgbData());
		Assert.Null(image.GetAlphaData());
		// The image keeps its own copy.
		rgb[0] = 1;
		Assert.Equal((byte)255, image.GetRgb(new Point(0, 0)).R);
	});

	[Fact]
	public Task MakesAnImageWithAlpha() => Gui.Run(() => {
		byte[] alpha = [0, 64, 128, 255];
		using Image image = new(new Size(2, 2), new byte[12], alpha);
		Assert.True(image.HasAlpha);
		Assert.Equal((byte)64, image.GetAlpha(new Point(1, 0)));
		Assert.Equal(alpha, image.GetAlphaData());
	});

	[Fact]
	public Task SetsAndReadsEveryPixel() => Gui.Run(() => {
		using Image image = new(new Size(3, 1));
		byte[] rgb = [10, 20, 30, 40, 50, 60, 70, 80, 90];
		image.SetRgbData(rgb);
		Assert.Equal(rgb, image.GetRgbData());
		Assert.Equal(new Color(40, 50, 60), image.GetRgb(new Point(1, 0)));
		Assert.False(image.HasAlpha);
		image.SetAlphaData([255, 128, 0]);
		Assert.True(image.HasAlpha);
		Assert.Equal([255, 128, 0], image.GetAlphaData());
		// A pixel set one at a time shows in the bytes too.
		image.SetRgb(new Point(2, 0), new Color(1, 2, 3));
		Assert.Equal([1, 2, 3], image.GetRgbData()[6..]);
	});

	[Fact]
	public Task RefusesBytesOfTheWrongLength() => Gui.Run(() => {
		Assert.Throws<ArgumentException>(() => new Image(new Size(2, 2), new byte[11]));
		Assert.Throws<ArgumentException>(() => new Image(new Size(2, 2), new byte[12], new byte[3]));
		Assert.Throws<ArgumentOutOfRangeException>(() => new Image(new Size(0, 2), ReadOnlySpan<byte>.Empty));
		using Image image = new(new Size(2, 1));
		Assert.Throws<ArgumentException>(() => image.SetRgbData(new byte[5]));
		Assert.Throws<ArgumentException>(() => image.SetAlphaData(new byte[3]));
	});
}
