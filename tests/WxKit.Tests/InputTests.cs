namespace WxKit.Tests;

// Real input, sent through the system as a user's is, so these check that events really arrive,
// not only that a handler can be added. The input goes to the active window, and some machines do
// not deliver simulated input at all, as GitHub's Windows runners do not, so each test first sends
// a probe and is skipped, rather than failed, when nothing arrives. Once input is shown to arrive,
// the event the test is for must too. macOS only lets an application send input with a permission
// CI does not have.
public class InputTests {
	// The waits add up to less than a test's own timeout, so a test fails at the wait that did not hold.
	private static readonly TimeSpan ActivateWait = TimeSpan.FromSeconds(2);
	private static readonly TimeSpan ProbeWait = TimeSpan.FromSeconds(2);
	private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);

	[Fact]
	public Task ClicksAButton() => Gui.WithFrame(async frame => {
		Panel panel = new(frame);
		Button button = new(panel, "&Press me", position: new Point(20, 20), size: new Size(120, 40));
		int clicks = 0;
		bool moved = false;
		button.Click += (_, _) => clicks++;
		button.MouseMoved += (_, e) => {
			moved = true;
			e.Skip();
		};
		await Activate(frame, button);
		Size size = button.ClientSize;
		Assert.True(UIActionSimulator.MouseMove(button.ClientToScreen(new Point(size.Width / 2, size.Height / 2))));
		await Probe(() => moved);
		Assert.True(UIActionSimulator.MouseClick());
		await Gui.WaitUntil(() => clicks == 1, Wait);
	});

	[Fact]
	public Task TypesIntoATextControl() => Gui.WithFrame(async frame => {
		TextCtrl text = new(frame);
		List<KeyCode> keys = [];
		text.KeyDown += (_, e) => {
			keys.Add(e.KeyCode);
			e.Skip();
		};
		await Activate(frame, text);
		Assert.True(UIActionSimulator.Text("hello"));
		await Probe(() => keys.Count > 0);
		// Caps Lock, which some screen readers use as their key, can leave it on, and makes it HELLO.
		try {
			await Gui.WaitUntil(() => string.Equals(text.Value, "hello", StringComparison.OrdinalIgnoreCase), Wait);
		} catch (TimeoutException) {
			Assert.Fail($"The text control has \"{text.Value}\", after keys {string.Join(", ", keys)}.");
		}
		Assert.Equal((KeyCode)'H', keys[0]);
	});

	[Fact]
	public Task ChoosesAMenuItemByItsShortcut() => Gui.WithFrame(async frame => {
		Menu menu = new();
		int chosen = 0;
		menu.Append(6100, "&Mark\tCtrl+M").Click += (_, _) => chosen++;
		MenuBar menuBar = new();
		menuBar.Append(menu, "&Edit");
		frame.MenuBar = menuBar;
		TextCtrl text = new(frame);
		bool typed = false;
		text.KeyDown += (_, e) => {
			typed |= e.KeyCode == KeyCode.Right && e.Modifiers.HasFlag(KeyModifiers.Control);
			e.Skip();
		};
		await Activate(frame, text);
		// A key with Ctrl held, which the text control receives, shows that input arrives with its
		// modifiers, as it does not under WSLg, before the shortcut, which the menu bar takes before
		// the control sees it.
		Assert.True(UIActionSimulator.Char(KeyCode.Right, KeyModifiers.Control));
		await Probe(() => typed);
		Assert.True(UIActionSimulator.Char((KeyCode)'M', KeyModifiers.Control));
		await Gui.WaitUntil(() => chosen == 1, Wait);
	});

	// Shows the frame and focuses the window the input is for, or skips the test when the system
	// will not make the frame active, as it only lets the foreground application take it.
	private static async Task Activate(Frame frame, Window focus) {
		Assert.SkipWhen(OperatingSystem.IsMacOS(), "macOS only lets an application send input with the accessibility permission, which the test run does not have.");
		frame.Show();
		frame.Raise();
		focus.SetFocus();
		try {
			await Gui.WaitUntil(() => frame.IsActive && Window.FindFocus() == focus, ActivateWait);
		} catch (TimeoutException) {
			Assert.Skip("The system did not make the test's window active, so input would go elsewhere.");
		}
	}

	// Skips the test when no simulated input arrived at all, as on a machine that does not deliver it.
	private static async Task Probe(Func<bool> arrived) {
		try {
			await Gui.WaitUntil(arrived, ProbeWait);
		} catch (TimeoutException) {
			Assert.Skip("The system did not deliver simulated input to the test's window.");
		}
	}
}
