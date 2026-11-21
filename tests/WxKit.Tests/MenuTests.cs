namespace WxKit.Tests;

public class MenuTests {
	[Fact]
	public Task KeepsStandardIdsAndPicksOthers() => Gui.WithFrame(frame => {
		Menu menu = new();
		MenuItem exit = menu.Append(StandardId.Exit);
		MenuItem custom = menu.Append(StandardId.Any, "&Custom", "Does something");
		MenuItem check = menu.Append(StandardId.Any, "&Check", kind: MenuItemKind.Check);
		menu.AppendSeparator();
		Assert.Equal(StandardId.Exit, exit.Id);
		Assert.NotEqual(StandardId.Any, custom.Id);
		Assert.NotEqual(custom.Id, check.Id);
		Assert.Same(menu, exit.Menu);
		MenuBar bar = new();
		bar.Append(menu, "&File");
		frame.MenuBar = bar;
	});

	[Fact]
	public Task EnablesChecksAndRelabelsItems() => Gui.WithFrame(frame => {
		Menu menu = new();
		MenuItem open = menu.Append(StandardId.Any, "&Open	Ctrl+O");
		MenuItem wrap = menu.Append(StandardId.Any, "&Wrap", kind: MenuItemKind.Check);
		MenuBar bar = new();
		bar.Append(menu, "&File");
		frame.MenuBar = bar;
		Assert.True(open.Enabled);
		open.Enabled = false;
		Assert.False(open.Enabled);
		Assert.False(wrap.Checked);
		wrap.Checked = true;
		Assert.True(wrap.Checked);
		Assert.Equal("&Open	Ctrl+O", open.Label);
		open.Label = "&Open...	Ctrl+O";
		Assert.Equal("&Open...	Ctrl+O", open.Label);
	});

	[Fact]
	public Task ReachesItemsInASubmenu() => Gui.WithFrame(frame => {
		Menu presets = new();
		MenuItem empty = presets.Append(StandardId.Any, "No presets yet");
		Menu convert = new();
		MenuItem submenu = convert.AppendSubMenu(presets, "&Presets");
		MenuBar bar = new();
		bar.Append(convert, "&Convert");
		frame.MenuBar = bar;
		empty.Enabled = false;
		Assert.False(empty.Enabled);
		Assert.False(convert.Append(StandardId.Any, "x").Id == submenu.Id);
		Assert.Equal("&Presets", submenu.Label);
	});

	[Fact]
	public Task GivesTheMenuBarToItsFrame() => Gui.Run(async () => {
		Frame frame = new(null, "Menus");
		Menu menu = new();
		MenuBar bar = new();
		bar.Append(menu, "&File");
		frame.MenuBar = bar;
		Assert.Same(bar, frame.MenuBar);
		Assert.Same(frame, bar.Parent);
		frame.Destroy();
		await Gui.WaitUntil(() => frame.IsDestroyed);
		Assert.True(bar.IsDestroyed);
		Assert.True(menu.IsDestroyed);
	});
}

public class TimerTests {
	[Fact]
	public Task TicksOnTheGuiThread() => Gui.Run(async () => {
		using Timer timer = new();
		TaskCompletionSource<bool> ticked = new();
		timer.Tick += (_, _) => ticked.TrySetResult(Application.IsGuiThread);
		Assert.True(timer.Start(TimeSpan.FromMilliseconds(10), oneShot: true));
		Assert.True(await ticked.Task.WaitAsync(Gui.Timeout));
	});

	[Fact]
	public Task TicksRepeatedlyUntilStopped() => Gui.Run(async () => {
		using Timer timer = new();
		int ticks = 0;
		timer.Tick += (_, _) => ticks++;
		timer.Start(TimeSpan.FromMilliseconds(10));
		Assert.True(timer.IsRunning);
		await Gui.WaitUntil(() => ticks >= 3);
		timer.Stop();
		Assert.False(timer.IsRunning);
	});

	[Fact]
	public Task IsDestroyedOnceDisposed() => Gui.Run(() => {
		Timer timer = new();
		timer.Dispose();
		Assert.True(timer.IsDestroyed);
		Assert.Throws<ObjectDisposedException>(() => timer.Start(TimeSpan.FromSeconds(1)));
		timer.Dispose();
	});

	[Fact]
	public Task RefusesAnIntervalOfZero() => Gui.Run(() => {
		using Timer timer = new();
		Assert.Throws<ArgumentOutOfRangeException>(() => timer.Start(TimeSpan.Zero));
	});
}

public class DialogTests {
	// A string that defaults to one of wxWidgets's constants is null by default, which passes the
	// constant, and an empty string stays empty.
	[Fact]
	public Task GivesADialogItsStringsOrWxWidgetsDefaults() => Gui.WithFrame(frame => {
		using FileDialog given = new(frame, "Open a note", wildcard: "Notes (*.txt)|*.txt", style: FileDialogStyle.Open | FileDialogStyle.FileMustExist);
		Assert.Equal("Open a note", given.Message);
		Assert.Equal("Notes (*.txt)|*.txt", given.Wildcard);
		using FileDialog defaults = new(frame);
		Assert.Equal("Select a file", defaults.Message);
		using FileDialog empty = new(frame, string.Empty);
		Assert.Equal(string.Empty, empty.Message);
		using TextEntryDialog entry = new(frame, "Line number:", "Go To Line", "12");
		Assert.Equal("Go To Line", entry.Title);
		Assert.Equal("12", entry.Value);
	});

	[Fact]
	public Task CreatesAndDisposesAMessageDialog() => Gui.WithFrame(async frame => {
		MessageDialog dialog = new(frame, "Never shown", "Caption", MessageDialogStyle.YesNo | MessageDialogStyle.IconQuestion);
		Assert.False(dialog.IsDestroyed);
		dialog.Dispose();
		await Gui.WaitUntil(() => dialog.IsDestroyed);
		dialog.Dispose();
	});
}
