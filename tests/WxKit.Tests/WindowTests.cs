using WxKit.Interop;

namespace WxKit.Tests;

public class WindowTests {
	[Fact]
	public Task KeepsUnicodeTextIntact() => Gui.WithFrame(frame => {
		const string Title = "Ünïcödé ✓ 日本語 🎉";
		frame.Title = Title;
		Assert.Equal(Title, frame.Title);
		Button button = new(frame, Title);
		Assert.Equal(Title, button.Label);
	});

	// wxWidgets keeps its own copy of a control's label, so this round trip does not depend on what
	// the native control does with a null character.
	[Fact]
	public Task KeepsTextWithNullCharactersWhole() => Gui.WithFrame(frame => {
		StaticText text = new(frame, "before\0after");
		Assert.Equal("before\0after", text.Label);
	});

	[Fact]
	public Task ReturnsTheSameObjectForTheSameWindow() => Gui.WithFrame(frame => {
		Panel panel = new(frame);
		Button button = new(panel, "&Go");
		Assert.Same(panel, button.Parent);
		Assert.Same(frame, panel.Parent);
		Assert.Null(frame.Parent);
	});

	[Fact]
	public unsafe Task WrapsWindowsItDidNotCreateInTheirOwnClass() => Gui.WithFrame(frame => {
		fixed (char* label = "Native") {
			WxkWindow* native = WxKitNative.ButtonNew(frame.Native, StandardId.Any, label, 6, Point.Default, Size.Default, 0);
			Button? button = ObjectRegistry.Wrap<Button>(native);
			Assert.NotNull(button);
			Assert.Equal("Native", button.Label);
			Assert.Same(button, ObjectRegistry.Wrap<Window>(native));
			Assert.Same(frame, button.Parent);
		}
	});

	[Fact]
	public Task ShowsHidesEnablesAndDisables() => Gui.WithFrame(frame => {
		Button button = new(frame, "&Go");
		Assert.True(button.IsShown);
		Assert.True(button.Hide());
		Assert.False(button.IsShown);
		button.IsEnabled = false;
		Assert.False(button.IsEnabled);
		button.IsEnabled = true;
		Assert.True(button.IsEnabled);
	});

	[Fact]
	public Task GivesEachWindowAnId() => Gui.WithFrame(frame => {
		Button ok = new(frame, id: StandardId.Ok);
		Assert.Equal(StandardId.Ok, ok.Id);
		Assert.NotEqual(StandardId.Any, new Button(frame, "&Other").Id);
	});

	[Fact]
	public Task GivesAStandardButtonItsStandardLabel() => Gui.WithFrame(frame => {
		Button ok = new(frame, id: StandardId.Ok);
		Assert.NotEqual(string.Empty, ok.Label);
	});

	[Fact]
	public Task HasANativeHandle() => Gui.WithFrame(frame => Assert.NotEqual(0, frame.NativeHandle));

	[Fact]
	public Task DestroysChildrenWithTheirParent() => Gui.Run(async () => {
		Frame frame = new(null, "Parent");
		Panel panel = new(frame);
		Button button = new(panel, "&Go");
		TextCtrl text = new(panel);
		frame.Destroy();
		await Gui.WaitUntil(() => frame.IsDestroyed);
		Assert.True(panel.IsDestroyed);
		Assert.True(button.IsDestroyed);
		Assert.True(text.IsDestroyed);
	});

	[Fact]
	public Task ThrowsObjectDisposedExceptionOnceDestroyed() => Gui.Run(async () => {
		Frame frame = new(null, "Destroyed");
		Button button = new(frame, "&Go");
		frame.Destroy();
		await Gui.WaitUntil(() => frame.IsDestroyed);
		Assert.Throws<ObjectDisposedException>(() => button.Label);
		Assert.Throws<ObjectDisposedException>(() => frame.Title);
		Assert.Throws<ObjectDisposedException>(() => button.Click += (_, _) => { });
	});

	[Fact]
	public Task DestroysAChildAtOnce() => Gui.WithFrame(frame => {
		Button button = new(frame, "&Go");
		Assert.True(button.Destroy());
		Assert.True(button.IsDestroyed);
		Assert.False(frame.IsDestroyed);
	});

	[Fact]
	public Task KeepsADerivedWindowAlive() => Gui.Run(async () => {
		WeakReference<LabeledFrame> weak = Create();
		GC.Collect();
		GC.WaitForPendingFinalizers();
		Assert.True(weak.TryGetTarget(out LabeledFrame? frame));
		Assert.Equal("kept", frame.Note);
		frame.Destroy();
		await Gui.WaitUntil(() => frame.IsDestroyed);
		static WeakReference<LabeledFrame> Create() => new(new LabeledFrame { Note = "kept" });
	});

	private sealed class LabeledFrame() : Frame(null, "Derived") {
		public string Note { get; set; } = string.Empty;
	}

	// Code inside a control can name WxKit's types and .NET's, which a member of the same name would hide.
	[Fact]
	public Task NamesTypesFromInsideAControl() => Gui.WithFrame(frame => {
		Board board = new(frame);
		Assert.True(board.Check());
		Assert.NotNull(new Tools(frame).MakeMenu());
	});

	private sealed class Board(Window parent) : Panel(parent) {
		public bool Check() {
			Accessible.NotifyEvent(AccessibleEvent.ObjectFocus, this, AccessibleObjectId.Client, 0);
			HitTestResult result = HitTest(new Point(1, 1));
			// Char, not char, since a member named Char would hide the type.
#pragma warning disable IDE0049
			return Enum.IsDefined(result) && Char.IsDigit('1');
#pragma warning restore IDE0049
		}
	}

	private sealed class Tools(Window parent) : ToolBar(parent) {
		public Menu MakeMenu() => new();
	}
}
