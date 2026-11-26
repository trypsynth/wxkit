namespace WxKit.Tests;

// Static members, and classes that only have them.
public class StaticTests {
	[Fact]
	public Task ReadsSystemSettings() => Gui.Run(() => {
		Assert.NotNull(SystemSettings.GetColor(SystemColor.Window));
		using Font? font = SystemSettings.GetFont(SystemFont.DefaultGui);
		Assert.NotNull(font);
		Assert.True(SystemSettings.GetMetric(SystemMetric.ScreenX) > 0);
	});

	[Fact]
	// macOS only gives the keyboard focus to the active application, which a test run is not when
	// nothing activates it, as on a CI runner, so the test waits for the focus, and there skips
	// when it does not come.
	public Task FindsTheFocusedWindow() => Gui.WithFrame(async frame => {
		TextCtrl text = new(frame);
		frame.Show();
		text.SetFocus();
		if (OperatingSystem.IsMacOS()) {
			try {
				await Gui.WaitUntil(() => Window.FindFocus() is not null);
			} catch (TimeoutException) {
				Assert.Skip("macOS gives the keyboard focus only to the active application, which this test run is not.");
			}
		}
		Assert.Same(text, Window.FindFocus());
	});

	[Fact]
	public Task FindsAWindowById() => Gui.WithFrame(frame => {
		Button button = new(frame, "&Found", id: 4242);
		Assert.Same(button, Window.FindWindowById(4242, frame));
		Assert.Null(Window.FindWindowById(4343, frame));
	});

	[Fact]
	public Task GivesStandardArt() => Gui.Run(() => {
		using Bitmap? information = ArtProvider.GetBitmap(ArtId.Information, ArtClient.MessageBox);
		Assert.NotNull(information);
		Assert.True(information.Width > 0);
	});

	[Fact]
	public void ChecksTheThreadInStaticMembers() => Assert.Throws<InvalidOperationException>(() => SystemSettings.GetMetric(SystemMetric.ScreenX));
}
