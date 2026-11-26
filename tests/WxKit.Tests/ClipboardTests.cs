namespace WxKit.Tests;

// The system clipboard, which these tests replace, since the formats can only be read back from it.
public class ClipboardTests {
	[Fact]
	public Task RoundTripsText() => Gui.Run(() => {
		Assert.True(Clipboard.SetText("Copied from WxKit ✓"));
		Assert.True(Clipboard.HasText);
		Assert.Equal("Copied from WxKit ✓", Clipboard.GetText());
	});

	[Fact]
	public Task RoundTripsABitmap() => Gui.Run(() => {
		using Bitmap bitmap = new(8, 8);
		using (MemoryDC dc = new(bitmap)) {
			using Brush red = new(new Color(255, 0, 0));
			dc.Background = red;
			dc.Clear();
		}
		Assert.True(Clipboard.SetBitmap(bitmap));
		Assert.True(Clipboard.HasBitmap);
		Assert.False(Clipboard.HasText);
		using Bitmap? pasted = Clipboard.GetBitmap();
		Assert.NotNull(pasted);
		Assert.Equal(new Size(8, 8), pasted.Size);
		using Image? image = pasted.ConvertToImage();
		Assert.NotNull(image);
		Assert.Equal((byte)255, image.GetRgb(new Point(4, 4)).R);
		Assert.Equal((byte)0, image.GetRgb(new Point(4, 4)).G);
	});

	[Fact]
	public Task RoundTripsFiles() => Gui.Run(() => {
		string[] files = [Path.Combine(Path.GetTempPath(), "WxKit one.txt"), Path.Combine(Path.GetTempPath(), "WxKit two.txt")];
		Assert.True(Clipboard.SetFiles(files));
		Assert.True(Clipboard.HasFiles);
		Assert.Equal(files, Clipboard.GetFiles());
	});

	[Fact]
	public Task RoundTripsHtml() => Gui.Run(() => {
		Assert.True(Clipboard.SetHtml("<b>Bold</b> text"));
		Assert.True(Clipboard.HasHtml);
		Assert.Contains("<b>Bold</b> text", Clipboard.GetHtml());
	});

	[Fact]
	public Task RoundTripsANamedFormat() => Gui.Run(() => {
		byte[] data = [1, 2, 3, 0, 255];
		Assert.True(Clipboard.SetData("WxKit.Tests.Shape", data));
		Assert.True(Clipboard.HasData("WxKit.Tests.Shape"));
		Assert.False(Clipboard.HasData("WxKit.Tests.Other"));
		Assert.Equal(data, Clipboard.GetData("WxKit.Tests.Shape"));
		Assert.Null(Clipboard.GetData("WxKit.Tests.Other"));
	});

	[Fact]
	public Task OffersSeveralFormatsAtOnce() => Gui.Run(() => {
		ClipboardContent content = new() { Text = "Bold text", Html = "<b>Bold</b> text" };
		content.Add("WxKit.Tests.Shape", [42]);
		Assert.True(Clipboard.SetContent(content));
		Assert.Equal("Bold text", Clipboard.GetText());
		Assert.Contains("<b>Bold</b> text", Clipboard.GetHtml());
		Assert.Equal([42], Clipboard.GetData("WxKit.Tests.Shape"));
		Assert.False(Clipboard.HasBitmap);
	});

	[Fact]
	public Task GivesNullForAMissingFormat() => Gui.Run(() => {
		// Only this format is on the clipboard, unless another application copies something meanwhile.
		Assert.True(Clipboard.SetData("WxKit.Tests.Only", [1]));
		Assert.False(Clipboard.HasText);
		Assert.Null(Clipboard.GetText());
		Assert.Null(Clipboard.GetBitmap());
		Assert.Null(Clipboard.GetFiles());
		Assert.Null(Clipboard.GetHtml());
	});

	[Fact]
	public Task Empties() => Gui.Run(() => {
		Assert.True(Clipboard.SetData("WxKit.Tests.Only", [1]));
		Clipboard.Clear();
		Assert.False(Clipboard.HasData("WxKit.Tests.Only"));
	});

	[Fact]
	public Task RefusesEmptyContent() => Gui.Run(() => Assert.Throws<ArgumentException>(() => Clipboard.SetContent(new ClipboardContent())));

	[Fact]
	public void ChecksTheThread() => Assert.Throws<InvalidOperationException>(() => Clipboard.GetText());
}
