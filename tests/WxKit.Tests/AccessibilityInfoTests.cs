namespace WxKit.Tests;

// What screen readers read of standard controls, from each platform's accessibility API: MSAA on
// Windows, NSAccessibility on macOS, and ATK on Linux. A control is named by the label just before
// it, as WxKit's samples and docs make forms.
public partial class AccessibilityInfoTests {
	[Fact]
	public Task ReadsAFormAsAScreenReaderDoes() => Gui.WithFrame(async frame => {
		Panel panel = new(frame);
		StaticText nameLabel = new(panel, "&Name:");
		TextCtrl name = new(panel);
		_ = new StaticText(panel, "N&otes:");
		TextCtrl notes = new(panel, style: TextCtrlStyle.MultiLine);
		Button save = new(panel, "&Save");
		Button later = new(panel, "&Later") { IsEnabled = false };
		CheckBox remember = new(panel, "&Remember me") { IsChecked = true };
		RadioButton small = new(panel, "S&mall");
		_ = new StaticText(panel, "Si&ze:");
		Choice size = new(panel, choices: ["One", "Two"]);
		_ = new StaticText(panel, "&Volume:");
		Slider volume = new(panel, 5, 0, 10);
		_ = new StaticText(panel, "&Items:");
		ListBox items = new(panel, ["A", "B"]);
		Gauge progress = new(panel, 100);
		frame.Show();
		// wxGTK gives labels their controls when it next goes idle.
		await Gui.WaitUntil(() => AccessibilityInfo.Of(name).Name.Length > 0 && AccessibilityInfo.Of(items).Name.Length > 0);
		Expect(nameLabel, AccessibleRole.StaticText, "Name:");
		Expect(name, AccessibleRole.Text, "Name:");
		Expect(notes, AccessibleRole.Text, "Notes:");
		Expect(save, AccessibleRole.PushButton, "Save");
		AccessibilityInfo disabled = Expect(later, AccessibleRole.PushButton, "Later");
		Assert.False(disabled.IsEnabled, disabled.ToString());
		AccessibilityInfo checkedBox = Expect(remember, AccessibleRole.CheckButton, "Remember me");
		Assert.True(checkedBox.IsChecked, checkedBox.ToString());
		Assert.True(checkedBox.IsEnabled, checkedBox.ToString());
		Expect(small, AccessibleRole.RadioButton, "Small");
		Expect(size, AccessibleRole.ComboBox, "Size:");
		Expect(volume, AccessibleRole.Slider, "Volume:");
		Expect(items, AccessibleRole.List, "Items:");
		AccessibilityInfo gauge = AccessibilityInfo.Of(progress);
		Assert.True(gauge.Role == AccessibleRole.ProgressBar, gauge.ToString());
	});

	// A live region's text is announced, not read as a label, so a control after one is named by the
	// label before it.
	[Fact]
	public Task NamesAControlPastALiveRegion() => Gui.WithFrame(async frame => {
		Assert.SkipWhen(OperatingSystem.IsWindows(), "Windows names a control by the static text before it itself, which a live region is.");
		Panel panel = new(frame);
		_ = new StaticText(panel, "&Items:");
		_ = new LiveRegion(panel);
		ListBox items = new(panel, ["A", "B"]);
		frame.Show();
		await Gui.WaitUntil(() => AccessibilityInfo.Of(items).Name.Length > 0);
		Expect(items, AccessibleRole.List, "Items:");
	});

	[Fact]
	public Task SaysWhichControlHasTheFocus() => Gui.WithFrame(async frame => {
		Assert.SkipUnless(OperatingSystem.IsWindows() || OperatingSystem.IsLinux(), "The focus moves only in an active window, which the macOS runner's isn't.");
		Panel panel = new(frame);
		Button first = new(panel, "&First");
		Button second = new(panel, "&Second");
		frame.Show();
		frame.Raise();
		second.SetFocus();
		// Windows reports a control focused only in the window in front, which a CI runner's may not be.
		try {
			await Gui.WaitUntil(() => frame.IsActive && Window.FindFocus() == second && IsInFront(frame), TimeSpan.FromSeconds(5));
		} catch (TimeoutException) {
			Assert.Skip("The system did not put the test window in front.");
		}
		Assert.True(AccessibilityInfo.Of(second).IsFocused, AccessibilityInfo.Of(second).ToString());
		Assert.False(AccessibilityInfo.Of(first).IsFocused, AccessibilityInfo.Of(first).ToString());
	});

	// A button that shows only an icon is named by its tooltip, or by the name an application gives
	// it, which a tooltip set later leaves alone.
	[Fact]
	public Task NamesIconButtons() => Gui.WithFrame(async frame => {
		Panel panel = new(frame);
		BitmapButton add = new(panel, ArtProvider.GetBitmap(ArtId.Plus, ArtClient.Button)!);
		add.ToolTipText = "Add";
		BitmapButton remove = new(panel, ArtProvider.GetBitmap(ArtId.Minus, ArtClient.Button)!);
		remove.SetAccessibleName("Remove item");
		remove.ToolTipText = "Remove";
		BitmapButton help = new(panel, ArtProvider.GetBitmap(ArtId.Help, ArtClient.Button)!);
		help.ToolTipText = "Help";
		help.ToolTipText = "Show help";
		Button labeled = new(panel, "&Save");
		labeled.ToolTipText = "Saves the file";
		frame.Show();
		await Gui.WaitUntil(() => AccessibilityInfo.Of(add).Name.Length > 0);
		Expect(add, AccessibleRole.PushButton, "Add");
		Expect(remove, AccessibleRole.PushButton, "Remove item");
		Expect(help, AccessibleRole.PushButton, "Show help");
		Expect(labeled, AccessibleRole.PushButton, "Save");
	});

	private static bool IsInFront(Frame frame) => !OperatingSystem.IsWindows() || GetForegroundWindow() == frame.NativeHandle;

	[System.Runtime.InteropServices.LibraryImport("user32.dll")]
	private static partial nint GetForegroundWindow();

	private static AccessibilityInfo Expect(Window window, AccessibleRole role, string name) {
		AccessibilityInfo info = AccessibilityInfo.Of(window);
		Assert.True(info.Role == role && info.Name == name, $"{window.GetType().Name} should be {role} \"{name}\", and is {info}.");
		return info;
	}
}
