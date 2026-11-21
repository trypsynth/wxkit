namespace WxKit.Tests;

public class ApplicationTests {
	[Fact]
	public void ReportsTheWxWidgetsVersion() => Assert.StartsWith("wxWidgets 3.", Application.WxWidgetsVersion);

	[Fact]
	public async Task RunsTheGuiInASingleThreadedApartmentOnWindows() {
		Assert.SkipUnless(OperatingSystem.IsWindows(), "Apartments only exist on Windows.");
		ApartmentState state = await Gui.Run(() => Thread.CurrentThread.GetApartmentState());
		Assert.Equal(ApartmentState.STA, state);
	}

	// The tests run wxWidgets on the main thread, which on macOS is the only one it can run on.
	[Fact]
	public async Task RunsOnlyOnTheMainThreadOnMacOS() {
		Assert.True(await Gui.Run(() => WxKit.Interop.WxKitNative.AppCanRunOnThisThread() != 0));
		bool elsewhere = await Task.Run(() => WxKit.Interop.WxKitNative.AppCanRunOnThisThread() != 0);
		Assert.Equal(!OperatingSystem.IsMacOS(), elsewhere);
	}

	// Nothing awaits between setting the direction and setting it back, so no other test's windows see it.
	[Fact]
	public async Task LaysOutWindowsWithNoParentInItsDirection() {
		Assert.SkipWhen(OperatingSystem.IsMacOS(), "Cocoa takes its direction from the application's localization.");
		(LayoutDirection frame, LayoutDirection child, LayoutDirection after) = await Gui.Run(() => {
			Application.LayoutDirection = LayoutDirection.RightToLeft;
			try {
				Frame mirrored = new(null, "Mirrored");
				Dialog owned = new(mirrored, "Owned");
				LayoutDirection directions = mirrored.LayoutDirection;
				LayoutDirection ownedDirection = owned.LayoutDirection;
				owned.Destroy();
				mirrored.Destroy();
				Application.LayoutDirection = LayoutDirection.Default;
				Frame plain = new(null, "Plain");
				LayoutDirection plainDirection = plain.LayoutDirection;
				plain.Destroy();
				return (directions, ownedDirection, plainDirection);
			} finally {
				Application.LayoutDirection = LayoutDirection.Default;
			}
		});
		Assert.Equal(LayoutDirection.RightToLeft, frame);
		Assert.Equal(LayoutDirection.RightToLeft, child);
		Assert.NotEqual(LayoutDirection.RightToLeft, after);
		Assert.Equal(LayoutDirection.Default, Application.LayoutDirection);
	}

	[Fact]
	public void RefusesADirectionThatIsNotOne() => Assert.Throws<ArgumentOutOfRangeException>(() => Application.LayoutDirection = (LayoutDirection)7);

	[Fact]
	public async Task KnowsWhichThreadIsTheGuiThread() {
		Assert.False(Application.IsGuiThread);
		Assert.True(await Gui.Run(() => Application.IsGuiThread));
	}

	[Fact]
	public async Task ResumesAwaitsOnTheGuiThread() {
		CancellationToken cancellation = TestContext.Current.CancellationToken;
		bool onGuiThread = await Gui.Run(async () => {
			await Task.Delay(10, cancellation);
			return Application.IsGuiThread;
		});
		Assert.True(onGuiThread);
	}

	[Fact]
	public async Task RunsPostedActionsOnTheGuiThread() {
		TaskCompletionSource<bool> ran = new(TaskCreationOptions.RunContinuationsAsynchronously);
		Application.Post(() => ran.SetResult(Application.IsGuiThread));
		Assert.True(await ran.Task.WaitAsync(Gui.Timeout, TestContext.Current.CancellationToken));
	}

	[Fact]
	public async Task RefusesWindowsFromOtherThreads() {
		Frame frame = await Gui.Run(() => new Frame(null, "Other thread"));
		try {
			Assert.Throws<InvalidOperationException>(() => frame.Title);
			// Each refuses before it creates anything, or the native object would leak on the wrong thread.
			Assert.Throws<InvalidOperationException>(() => new Frame(null, "Not on the GUI thread"));
			Assert.Throws<InvalidOperationException>(() => new MessageDialog(null, "Not on the GUI thread"));
			Assert.Throws<InvalidOperationException>(() => new MenuBar());
			Assert.Throws<InvalidOperationException>(() => new Menu());
			Assert.Throws<InvalidOperationException>(() => new Timer());
			Assert.Throws<InvalidOperationException>(() => new BoxSizer(Orientation.Vertical));
			Assert.Throws<InvalidOperationException>(() => new Button(frame, "Not on the GUI thread"));
		} finally {
			await Gui.Run(() => frame.Destroy());
		}
	}

	[Fact]
	public async Task ReportsExceptionsFromPostedActions() {
		await Gui.Run(async () => {
			Application.Post(() => throw new InvalidOperationException("posted"));
			await Task.Delay(20);
			Exception error = Assert.Single(Gui.TakeUnhandled());
			Assert.Equal("posted", error.Message);
		});
	}
}
