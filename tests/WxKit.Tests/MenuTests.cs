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
		// A menu bar the frame replaces is destroyed, as it would have been with the frame.
		MenuBar next = new();
		next.Append(new Menu(), "&Edit");
		frame.MenuBar = next;
		Assert.True(bar.IsDisposed);
		Assert.Same(next, frame.MenuBar);
	});

	[Fact]
	public Task EnablesChecksAndRelabelsItems() => Gui.WithFrame(frame => {
		Menu menu = new();
		MenuItem open = menu.Append(StandardId.Any, "&Open	Ctrl+O");
		MenuItem wrap = menu.Append(StandardId.Any, "&Wrap", kind: MenuItemKind.Check);
		MenuBar bar = new();
		bar.Append(menu, "&File");
		frame.MenuBar = bar;
		Assert.True(open.IsEnabled);
		open.IsEnabled = false;
		Assert.False(open.IsEnabled);
		Assert.False(wrap.IsChecked);
		wrap.IsChecked = true;
		Assert.True(wrap.IsChecked);
		Assert.Equal("&Open	Ctrl+O", open.Label);
		open.Label = "&Open...	Ctrl+O";
		Assert.Equal("&Open...	Ctrl+O", open.Label);
	});

	// The menu bar reaches an item by its id, which it takes first, as wxWidgets does.
	[Fact]
	public Task ChecksAndEnablesItemsThroughTheMenuBar() => Gui.WithFrame(frame => {
		Menu menu = new();
		MenuItem wrap = menu.Append(StandardId.Any, "&Wrap", kind: MenuItemKind.Check);
		MenuBar bar = new();
		bar.Append(menu, "&View");
		frame.MenuBar = bar;
		bar.Check(wrap.Id, true);
		Assert.True(bar.IsChecked(wrap.Id));
		Assert.True(wrap.IsChecked);
		bar.EnableItem(wrap.Id, false);
		Assert.False(bar.IsItemEnabled(wrap.Id));
		bar.SetLabel(wrap.Id, "&Word wrap");
		Assert.Equal("&Word wrap", bar.GetLabel(wrap.Id));
	});

	// A menu on no menu bar and in no menu is the application's to dispose, and one that is on
	// one is its owner's.
	[Fact]
	public Task FreesAMenuNothingOwns() => Gui.WithFrame(frame => {
		Menu popup = new();
		popup.Append(StandardId.Copy);
		int disposed = 0;
		popup.Disposed += (_, _) => disposed++;
		popup.Dispose();
		Assert.True(popup.IsDisposed);
		Assert.Equal(1, disposed);
		popup.Dispose();
		Menu inner = new();
		Menu file = new();
		file.AppendSubMenu(inner, "&Recent");
		MenuBar bar = new();
		bar.Append(file, "&File");
		frame.MenuBar = bar;
		inner.Dispose();
		file.Dispose();
		Assert.False(inner.IsDisposed);
		Assert.False(file.IsDisposed);
	});

	// A menu on a menu bar is destroyed with it, as its frame is, and raises Disposed once then.
	[Fact]
	public Task RaisesDisposedForAMenuDestroyedWithItsMenuBar() => Gui.Run(async () => {
		Frame frame = new(null, "Menus");
		Menu file = new();
		file.Append(StandardId.Exit);
		MenuBar bar = new();
		bar.Append(file, "&File");
		frame.MenuBar = bar;
		int disposed = 0;
		file.Disposed += (_, _) => disposed++;
		frame.Destroy();
		await Gui.WaitUntil(() => frame.IsDisposed);
		Assert.True(file.IsDisposed);
		Assert.Equal(1, disposed);
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
		empty.IsEnabled = false;
		Assert.False(empty.IsEnabled);
		Assert.False(convert.Append(StandardId.Any, "x").Id == submenu.Id);
		Assert.Equal("&Presets", submenu.Label);
		// An item is the native item, so it says what it is, and two separators are apart.
		Assert.Same(presets, submenu.SubMenu);
		Assert.Null(empty.SubMenu);
		MenuItem line = convert.AppendSeparator();
		Assert.True(line.IsSeparator);
		Assert.False(empty.IsSeparator);
		empty.Help = "Presets you save show here";
		Assert.Equal("Presets you save show here", empty.Help);
		MenuItem check = convert.Append(StandardId.Any, "&Loop", kind: MenuItemKind.Check);
		check.IsChecked = true;
		Assert.True(check.IsChecked);
		Menu popup = new();
		MenuItem copy = popup.Append(StandardId.Copy);
		popup.Dispose();
		Assert.Throws<ObjectDisposedException>(() => copy.IsEnabled);
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
		await Gui.WaitUntil(() => frame.IsDisposed);
		Assert.True(bar.IsDisposed);
		Assert.True(menu.IsDisposed);
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
	public Task IsDisposedOnceDisposed() => Gui.Run(() => {
		Timer timer = new();
		timer.Dispose();
		Assert.True(timer.IsDisposed);
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
		Assert.False(dialog.IsDisposed);
		dialog.Dispose();
		await Gui.WaitUntil(() => dialog.IsDisposed);
		dialog.Dispose();
	});
}
