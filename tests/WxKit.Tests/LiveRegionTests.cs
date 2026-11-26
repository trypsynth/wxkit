namespace WxKit.Tests;

// Whether a screen reader hears an announcement needs one running, so these check what WxKit
// controls: the message it sends, and that a live region gets ready and announces without harm.
public class LiveRegionTests {
	[Theory]
	[InlineData("Ready", "Ready")]
	[InlineData("  Converted:\tlogo.png\r\nto JPG  ", "Converted: logo.png to JPG")]
	[InlineData("bell\u0007 and\u0000 nul", "bell and nul")]
	[InlineData("   \n\t ", "")]
	[InlineData("", "")]
	public void CleansAMessageToOneLine(string message, string cleaned) => Assert.Equal(cleaned, LiveRegion.Clean(message));

	[Fact]
	public void CutsALongMessageWithoutSplittingAPair() {
		Assert.Equal(new string('a', LiveRegion.MaxLength), LiveRegion.Clean(new string('a', LiveRegion.MaxLength + 100)));
		// An emoji is two chars, so one that would straddle the limit is left out whole.
		string straddling = new string('a', LiveRegion.MaxLength - 1) + "\U0001F600";
		Assert.Equal(new string('a', LiveRegion.MaxLength - 1), LiveRegion.Clean(straddling));
	}

	[Fact]
	public Task GetsReadyAndAnnounces() => Gui.WithFrame(frame => {
		LiveRegion region = new(frame);
		Assert.Equal(string.Empty, region.LabelText);
		if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux()) Assert.True(region.Prepare());
		// Whether a screen reader was told depends on there being one, but announcing never throws.
		_ = region.Announce("Preset applied: Blog");
		_ = region.Announce("Queued", AnnouncementPriority.Low);
		_ = region.Announce("Converting", AnnouncementPriority.Medium);
		Assert.False(region.Announce("  \n\t "));
		Assert.Equal(string.Empty, region.LabelText);
	});

	[Fact]
	public Task LetsItsWindowGo() => Gui.Run(async () => {
		Frame frame = new(null, "Live region");
		LiveRegion region = new(frame);
		region.Prepare();
		frame.Show();
		frame.Close(force: true);
		await Gui.WaitUntil(() => frame.IsDisposed);
		Assert.True(region.IsDisposed);
	});
}
