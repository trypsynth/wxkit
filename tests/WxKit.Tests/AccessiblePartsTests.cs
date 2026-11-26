using System.Runtime.Versioning;

namespace WxKit.Tests;

// The parts and rows of a window that draws itself, which screen readers see on every platform: on
// Windows through the window's accessible object, which these tests read as a screen reader does,
// and on macOS and Linux through wxWidgets's own helpers, which the tests can only call.
public class AccessiblePartsTests {
	[Fact]
	public Task DescribesParts() => Gui.WithFrame(frame => {
		Panel status = new(frame, size: new Size(300, 24));
		status.SetAccessibleParts([new AccessiblePart("Line 3", new Rect(0, 0, 100, 24)), new AccessiblePart("Saved", new Rect(100, 0, 80, 24))]);
		status.SetAccessibleParts([new AccessiblePart("Line 4", new Rect(0, 0, 100, 24)), new AccessiblePart("Saved", new Rect(100, 0, 80, 24))]);
		if (!OperatingSystem.IsWindows()) return;
		IAccessible msaa = Msaa.Of(status);
		Assert.Equal(2, msaa.GetChildCount());
		Assert.Equal("Line 4", msaa.GetName(1));
		Assert.Equal("Saved", msaa.GetName(2));
		Assert.Equal(Msaa.RoleStaticText, msaa.GetRole(2));
		msaa.Location(out int left, out int top, out int width, out int height, 2);
		Point origin = status.ClientToScreen(new Point(100, 0));
		Assert.Equal((origin.X, origin.Y, 80, 24), (left, top, width, height));
		status.SetAccessibleParts([]);
		Assert.Equal(0, msaa.GetChildCount());
	});

	[Fact]
	public Task DescribesRows() => Gui.WithFrame(frame => {
		Panel list = new(frame, size: new Size(300, 200));
		frame.Show();
		list.SetFocus();
		AccessibleRow milk = new(4, new Rect(0, 0, 300, 20), [new AccessiblePart("Milk", new Rect(0, 0, 200, 20)), new AccessiblePart("2", new Rect(200, 0, 100, 20))]);
		AccessibleRow bread = new(5, new Rect(0, 20, 300, 20), [new AccessiblePart("Bread", new Rect(0, 20, 200, 20)), new AccessiblePart("", new Rect(200, 20, 100, 20))], IsSelected: true);
		list.SetAccessibleRows([milk, bread], 100);
		list.SetAccessibleCurrentRow(5);
		if (!OperatingSystem.IsWindows()) return;
		IAccessible msaa = Msaa.Of(list);
		Assert.Equal(Msaa.RoleList, msaa.GetRole(0));
		Assert.Equal(2, msaa.GetChildCount());
		Assert.Equal("Milk, 2", msaa.GetName(1));
		Assert.Equal("Bread", msaa.GetName(2));
		Assert.Equal(Msaa.RoleListItem, msaa.GetRole(1));
		Assert.NotEqual(0, (int)msaa.GetState(2) & Msaa.StateSelected);
		Assert.Equal(0, (int)msaa.GetState(1) & Msaa.StateSelected);
		if (Window.FindFocus() == list) {
			Assert.Equal(2, msaa.GetFocus());
			Assert.NotEqual(0, (int)msaa.GetState(2) & Msaa.StateFocused);
		}
		list.SetAccessibleCurrentRow(-1);
	});

	[Fact]
	[SupportedOSPlatform("windows")]
	public Task NamesTheWindowByItsLabel() => Gui.WithFrame(frame => {
		Assert.SkipUnless(OperatingSystem.IsWindows(), "Microsoft Active Accessibility is Windows's.");
		Panel panel = new(frame);
		_ = new StaticText(panel, "&Shopping list:");
		Panel list = new(panel);
		list.SetAccessibleRows([], 0);
		IAccessible msaa = Msaa.Of(list);
		Assert.Equal("Shopping list:", msaa.GetName(0));
		list.SetAccessibleName("Groceries");
		Assert.Equal("Groceries", msaa.GetName(0));
		// A name given before the parts are survives them.
		Panel named = new(panel);
		named.SetAccessibleName("Named first");
		named.SetAccessibleParts([]);
		Assert.Equal("Named first", Msaa.Of(named).GetName(0));
	});

	// On Windows, parts and rows answer through an accessible object of their own, which would replace
	// the window's Accessible. Elsewhere only wxWidgets shows them, and an Accessible, which only
	// Windows asks, sits beside them, so a window can describe itself to every platform's screen readers.
	[Fact]
	public Task RefusesAWindowWithAnAccessibleOfItsOwnOnWindows() => Gui.WithFrame(frame => {
		Panel panel = new(frame);
		panel.Accessible = new Named();
		if (OperatingSystem.IsWindows()) {
			Assert.Throws<InvalidOperationException>(() => panel.SetAccessibleParts([]));
		} else {
			panel.SetAccessibleRows([], 0);
			panel.SetAccessibleCurrentRow(-1);
		}
		Assert.Throws<ArgumentOutOfRangeException>(() => new Panel(frame).SetAccessibleCurrentRow(-2));
	});

	private sealed class Named : Accessible {
		protected override string? GetName(int childId) => childId == 0 ? "Named" : null;
	}
}
