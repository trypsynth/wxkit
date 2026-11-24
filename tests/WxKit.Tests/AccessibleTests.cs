using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace WxKit.Tests;

// A control that draws itself, described to screen readers through Accessible, and read back
// through MSAA as a screen reader reads it, which is Windows's.
[SupportedOSPlatform("windows")]
public class AccessibleTests {
	[Fact]
	public Task DescribesAControlAndItsParts() => Gui.WithFrame(frame => {
		Assert.SkipUnless(OperatingSystem.IsWindows(), "Microsoft Active Accessibility is Windows's.");
		Panel board = new(frame);
		BoardAccessible accessible = new(board);
		Assert.Same(accessible, board.Accessible);
		IAccessible msaa = Msaa.Of(board);
		Assert.Equal("Board", msaa.GetName(0));
		Assert.Equal(Msaa.RoleTable, msaa.GetRole(0));
		Assert.Equal(9, msaa.GetChildCount());
		Assert.Equal("Cell 5", msaa.GetName(5));
		Assert.Equal(Msaa.RoleCell, msaa.GetRole(5));
		Assert.Equal("X", msaa.GetValue(5));
		Assert.NotEqual(0, (int)msaa.GetState(5) & Msaa.StateFocused);
		Assert.Equal(0, (int)msaa.GetState(4) & Msaa.StateFocused);
		Assert.Equal("Mark", msaa.GetDefaultAction(3));
	});

	[Fact]
	public Task DoesAPartsDefaultAction() => Gui.WithFrame(frame => {
		Assert.SkipUnless(OperatingSystem.IsWindows(), "Microsoft Active Accessibility is Windows's.");
		Panel board = new(frame);
		BoardAccessible accessible = new(board);
		Msaa.Of(board).DoDefaultAction(7);
		Assert.Equal(7, accessible.Pressed);
	});

	[Fact]
	public Task ReportsTheFocusedPart() => Gui.WithFrame(frame => {
		Assert.SkipUnless(OperatingSystem.IsWindows(), "Microsoft Active Accessibility is Windows's.");
		Panel board = new(frame);
		_ = new BoardAccessible(board);
		Assert.Equal(5, Msaa.Of(board).GetFocus());
	});

	// A screen reader asks what's at a point, or where navigation leads, and the window itself is an
	// answer, not nothing there.
	[Fact]
	public Task AnswersWithTheWindowItself() => Gui.WithFrame(frame => {
		Assert.SkipUnless(OperatingSystem.IsWindows(), "Microsoft Active Accessibility is Windows's.");
		Panel panel = new(frame);
		_ = new SelfAccessible(panel);
		Point inside = panel.ClientToScreen(new Point(1, 1));
		Assert.Equal(0, Msaa.Of(panel).HitTest(inside.X, inside.Y));
		// wxWidgets gives navigation's answer as the object, which is the window's own.
		Assert.True(Msaa.Of(panel).Navigate(5, 0) is IAccessible);
	});

	[Fact]
	public Task LeavesWhatItDoesNotAnswerToWindows() => Gui.WithFrame(frame => {
		Assert.SkipUnless(OperatingSystem.IsWindows(), "Microsoft Active Accessibility is Windows's.");
		Panel panel = new(frame);
		_ = new NameOnly(panel);
		IAccessible msaa = Msaa.Of(panel);
		Assert.Equal("Only a name", msaa.GetName(0));
		Assert.Equal(Msaa.RoleClient, msaa.GetRole(0));
	});

	[Fact]
	public Task TellsTheScreenReaderAPartDoesNotExist() => Gui.WithFrame(frame => {
		Assert.SkipUnless(OperatingSystem.IsWindows(), "Microsoft Active Accessibility is Windows's.");
		Panel board = new(frame);
		_ = new BoardAccessible(board);
		Assert.ThrowsAny<ArgumentException>(() => Msaa.Of(board).GetName(42));
		Assert.Empty(Gui.TakeUnhandled());
	});

	[Fact]
	public Task ReportsAnExceptionAndFails() => Gui.WithFrame(frame => {
		Assert.SkipUnless(OperatingSystem.IsWindows(), "Microsoft Active Accessibility is Windows's.");
		Panel panel = new(frame);
		_ = new Throwing(panel);
		Assert.ThrowsAny<COMException>(() => Msaa.Of(panel).GetName(0));
		Exception error = Assert.Single(Gui.TakeUnhandled());
		Assert.IsType<InvalidOperationException>(error);
	});

	[Fact]
	public Task ReplacesAWindowsAccessibleObject() => Gui.WithFrame(frame => {
		Assert.SkipUnless(OperatingSystem.IsWindows(), "Microsoft Active Accessibility is Windows's.");
		Panel panel = new(frame);
		_ = new NameOnly(panel);
		BoardAccessible second = new(panel);
		Assert.Same(second, panel.Accessible);
		Assert.Equal("Board", Msaa.Of(panel).GetName(0));
	});

	[Fact]
	public Task LetsGoWhenTheWindowIsDestroyed() => Gui.WithFrame(frame => {
		Assert.SkipUnless(OperatingSystem.IsWindows(), "Microsoft Active Accessibility is Windows's.");
		Panel panel = new(frame);
		_ = new NameOnly(panel);
		Assert.NotNull(panel.Accessible);
		panel.Destroy();
		Assert.Null(panel.Accessible);
	});

	[Fact]
	public Task NotifiesScreenReaders() => Gui.WithFrame(frame => {
		Panel board = new(frame);
		_ = new BoardAccessible(board);
		Accessible.NotifyEvent(AccessibleEvent.ObjectFocus, board, AccessibleObjectId.Client, 5);
		Accessible.NotifyEvent(AccessibleEvent.ObjectNameChange, board, 5);
	});

	// A three by three board, with the middle cell focused and marked.
	private sealed class BoardAccessible : Accessible {
		public BoardAccessible(Window window) : base(window) {
		}

		public int Pressed { get; private set; }

		protected override string? GetName(int childId) => childId switch {
			0 => "Board",
			>= 1 and <= 9 => $"Cell {childId}",
			_ => throw new ArgumentOutOfRangeException(nameof(childId)),
		};

		protected override AccessibleRole? GetRole(int childId) => childId == 0 ? AccessibleRole.Table : AccessibleRole.Cell;

		protected override string? GetValue(int childId) => childId == 5 ? "X" : null;

		protected override AccessibleStates? GetState(int childId) => childId == 5 ? AccessibleStates.Focusable | AccessibleStates.Focused : AccessibleStates.Focusable;

		protected override int? GetChildCount() => 9;

		protected override AccessibleElement? GetChild(int childId) => childId is >= 1 and <= 9 ? AccessibleElement.Child(childId) : AccessibleElement.None;

		protected override AccessibleElement? GetFocus() => AccessibleElement.Child(5);

		protected override string? GetDefaultAction(int childId) => childId == 0 ? string.Empty : "Mark";

		protected override bool? DoDefaultAction(int childId) {
			if (childId == 0) return false;
			Pressed = childId;
			return true;
		}
	}

	private sealed class SelfAccessible : Accessible {
		public SelfAccessible(Window window) : base(window) {
		}

		protected override AccessibleElement? HitTest(Point point) => AccessibleElement.Self;

		protected override AccessibleElement? Navigate(NavigationDirection direction, int fromId) => AccessibleElement.Self;
	}

	private sealed class NameOnly : Accessible {
		public NameOnly(Window window) : base(window) {
		}

		protected override string? GetName(int childId) => childId == 0 ? "Only a name" : null;
	}

	private sealed class Throwing : Accessible {
		public Throwing(Window window) : base(window) {
		}

		protected override string? GetName(int childId) => throw new InvalidOperationException("No name.");
	}
}
