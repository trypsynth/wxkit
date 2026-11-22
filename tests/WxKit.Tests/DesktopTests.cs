namespace WxKit.Tests;

// Desktop.Open and OpenUrl aren't run, since they would open a real browser or application.
public class DesktopTests {
	[Fact]
	public Task AsksTheSystemAboutTheKeyboardAndThePointer() => Gui.Run(() => {
		Desktop.Bell();
		// Whatever the person at the computer is pressing, asking works and says something.
		_ = Keyboard.IsKeyDown(KeyCode.Shift);
		_ = Keyboard.IsCapsLockOn;
		_ = Keyboard.IsNumLockOn;
		Point pointer = Mouse.Position;
		Assert.NotNull(Display.FromPoint(pointer));
		Assert.Throws<ArgumentNullException>(() => Desktop.OpenUrl((string)null!));
		Assert.Throws<ArgumentNullException>(() => Desktop.Open(null!));
	});

	[Fact]
	public Task ShowsABusyCursorUntilTheLastIsDisposed() => Gui.Run(() => {
		Assert.False(BusyCursor.IsBusy);
		BusyCursor outer = new();
		Assert.True(BusyCursor.IsBusy);
		using (new BusyCursor()) {
			Assert.True(BusyCursor.IsBusy);
		}
		Assert.True(BusyCursor.IsBusy);
		outer.Dispose();
		outer.Dispose();
		Assert.False(BusyCursor.IsBusy);
	});

	[Fact]
	public Task DisablesEveryWindowButOne() => Gui.WithFrame(frame => {
		Frame other = new(null, "Other");
		try {
			frame.Show();
			other.Show();
			using (new WindowDisabler(frame)) {
				Assert.True(frame.IsEnabled);
				Assert.False(other.IsEnabled);
			}
			Assert.True(other.IsEnabled);
			WindowDisabler all = new();
			Assert.False(frame.IsEnabled);
			all.Dispose();
			all.Dispose();
			Assert.True(frame.IsEnabled);
		} finally {
			other.Destroy();
		}
	});

	[Fact]
	public Task DescribesTheDisplays() => Gui.WithFrame(frame => {
		IReadOnlyList<Display> displays = Display.All;
		Assert.NotEmpty(displays);
		Assert.True(Display.Primary.IsPrimary || displays.All(display => !display.IsPrimary));
		foreach (Display display in displays) {
			Assert.Equal(display, displays[display.Index]);
			Assert.True(display.Bounds.Width > 0 && display.Bounds.Height > 0);
			Assert.True(display.WorkArea.Width > 0 && display.WorkArea.Width <= display.Bounds.Width);
			Assert.True(display.ScaleFactor > 0);
		}
		frame.Show();
		Assert.NotNull(Display.FromWindow(frame));
	});

	[Fact]
	public Task SaysWhetherTheSystemIsDark() => Gui.Run(() => {
		SystemAppearance appearance = SystemSettings.Appearance;
		Assert.NotNull(appearance.Name);
		// Following the system is what macOS and Linux do already, and Windows can always be asked to.
		bool followed = Application.SetAppearance(Appearance.System);
		if (OperatingSystem.IsWindows()) Assert.True(followed);
		Assert.Throws<ArgumentOutOfRangeException>(() => Application.SetAppearance((Appearance)7));
	});
}
