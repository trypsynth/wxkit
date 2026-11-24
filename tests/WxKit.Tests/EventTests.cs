namespace WxKit.Tests;

public class EventTests {
	[Fact]
	public Task RaisesTextChangedWhenTheValueIsSet() => Gui.WithFrame(frame => {
		TextCtrl text = new(frame);
		List<(object? Sender, string Value)> changes = [];
		text.TextChanged += (sender, e) => changes.Add((sender, e.String));
		text.Value = "hello";
		var change = Assert.Single(changes);
		Assert.Same(text, change.Sender);
		Assert.Equal("hello", change.Value);
	});

	// A search control's documentation lists only its search events, but it's typed in as a text
	// control is, and says so, for filtering as the user types.
	[Fact]
	public Task RaisesASearchControlsTextChanged() => Gui.WithFrame(frame => {
		SearchCtrl search = new(frame);
		List<(object? Sender, string Value)> changes = [];
		search.TextChanged += (sender, e) => changes.Add((sender, e.String));
		search.Value = "books";
		var change = Assert.Single(changes);
		Assert.Same(search, change.Sender);
		Assert.Equal("books", change.Value);
	});

	[Fact]
	public Task DoesNotRaiseTextChangedForChangeValue() => Gui.WithFrame(frame => {
		TextCtrl text = new(frame);
		int changes = 0;
		text.TextChanged += (_, _) => changes++;
		text.ChangeValue("quiet");
		Assert.Equal(0, changes);
		Assert.Equal("quiet", text.Value);
	});

	[Fact]
	public Task CallsEveryHandlerAndStopsCallingRemovedOnes() => Gui.WithFrame(frame => {
		TextCtrl text = new(frame);
		List<string> calls = [];
		EventHandler<CommandEvent> first = (_, _) => calls.Add("first");
		EventHandler<CommandEvent> second = (_, _) => calls.Add("second");
		text.TextChanged += first;
		text.TextChanged += second;
		text.Value = "one";
		text.TextChanged -= first;
		text.Value = "two";
		text.TextChanged -= second;
		text.Value = "three";
		Assert.Equal(["first", "second", "second"], calls);
	});

	[Fact]
	public Task MakesEventsUnusableAfterTheirHandlersReturn() => Gui.WithFrame(frame => {
		TextCtrl text = new(frame);
		CommandEvent? kept = null;
		text.TextChanged += (_, e) => kept = e;
		text.Value = "kept";
		Assert.NotNull(kept);
		Assert.Throws<ObjectDisposedException>(() => kept.String);
	});

	[Fact]
	public Task ReportsTheObjectAnEventCameFrom() => Gui.WithFrame(frame => {
		TextCtrl text = new(frame);
		EvtHandler? source = null;
		text.TextChanged += (_, e) => source = e.EventObject;
		text.Value = "source";
		Assert.Same(text, source);
	});

	[Fact]
	public Task LetsSkippedCommandEventsReachTheParent() => Gui.WithFrame(frame => {
		Panel panel = new(frame);
		TextCtrl text = new(panel);
		List<string> calls = [];
		text.TextChanged += (_, e) => {
			calls.Add("text");
			if (text.Value == "skip") e.Skip();
		};
		panel.Bind(EventType.Text, (_, _) => calls.Add("panel"));
		text.Value = "keep";
		text.Value = "skip";
		Assert.Equal(["text", "text", "panel"], calls);
	});

	[Fact]
	public Task RaisesClickWhenAButtonIsClicked() => Gui.WithFrame(frame => {
		Assert.SkipUnless(OperatingSystem.IsWindows(), "Clicks are sent as Windows messages.");
		Button button = new(frame, "&Go");
		object? sender = null;
		button.Click += (s, _) => sender = s;
		Win32.Click(button);
		Assert.Same(button, sender);
	});

	[Fact]
	public Task RaisesClickWhenAMenuItemIsChosen() => Gui.WithFrame(frame => {
		Assert.SkipUnless(OperatingSystem.IsWindows(), "Menu commands are sent as Windows messages.");
		Menu menu = new();
		MenuItem item = menu.Append(StandardId.Any, "&Custom");
		MenuBar bar = new();
		bar.Append(menu, "&File");
		frame.MenuBar = bar;
		object? sender = null;
		int id = 0;
		item.Click += (s, e) => {
			sender = s;
			id = e.Id;
		};
		Win32.ChooseMenuItem(frame, item);
		Assert.Same(item, sender);
		Assert.Equal(item.Id, id);
	});

	[Fact]
	public Task GivesKeyEventsTheirKey() => Gui.WithFrame(frame => {
		Assert.SkipUnless(OperatingSystem.IsWindows(), "Keys are sent as Windows messages.");
		TextCtrl text = new(frame);
		KeyCode? keyCode = null;
		text.KeyDown += (_, e) => keyCode = e.KeyCode;
		Win32.PressKey(text, 'A');
		// A letter's code is its character, which KeyCode has no name for.
		Assert.Equal((KeyCode)'A', keyCode);
	});

	[Fact]
	public Task GivesSizeEventsTheNewSize() => Gui.WithFrame(frame => {
		Size? reported = null;
		frame.SizeChanged += (_, e) => {
			reported = e.Size;
			e.Skip();
		};
		frame.Size = new Size(300, 200);
		Assert.Equal(new Size(300, 200), reported);
	});

	[Fact]
	public Task ReportsExceptionsFromHandlers() => Gui.WithFrame(frame => {
		TextCtrl text = new(frame);
		text.TextChanged += (_, _) => throw new InvalidOperationException("handler");
		text.Value = "throw";
		Exception error = Assert.Single(Gui.TakeUnhandled());
		Assert.Equal("handler", error.Message);
	});

	[Fact]
	public Task BindsEventsFromAnIdRange() => Gui.WithFrame(frame => {
		Panel panel = new(frame);
		TextCtrl first = new(panel, id: 100);
		TextCtrl second = new(panel, id: 101);
		TextCtrl outside = new(panel, id: 200);
		List<int> ids = [];
		panel.Bind(EventType.Text, 100, 101, (_, e) => ids.Add(e.Id));
		first.Value = "a";
		second.Value = "b";
		outside.Value = "c";
		Assert.Equal([100, 101], ids);
	});
}

public class CloseTests {
	[Fact]
	public Task ClosesAndDestroysWhenNothingVetoes() => Gui.Run(async () => {
		Frame frame = new(null, "Close");
		int closings = 0;
		frame.Closing += (_, _) => closings++;
		Assert.True(frame.Close());
		await Gui.WaitUntil(() => frame.IsDestroyed);
		Assert.Equal(1, closings);
	});

	[Fact]
	public Task StaysOpenWhenAHandlerVetoes() => Gui.WithFrame(async frame => {
		frame.Closing += (_, e) => e.Veto();
		Assert.False(frame.Close());
		await Task.Delay(20);
		Assert.False(frame.IsDestroyed);
	});

	[Fact]
	public Task CannotVetoAForcedClose() => Gui.Run(async () => {
		Frame frame = new(null, "Forced");
		bool? canVeto = null;
		frame.Closing += (_, e) => {
			canVeto = e.CanVeto;
			Assert.Throws<InvalidOperationException>(() => e.Veto());
		};
		frame.Close(force: true);
		await Gui.WaitUntil(() => frame.IsDestroyed);
		Assert.False(canVeto);
	});

	[Fact]
	public Task ClosesWhenTheLastHandlerIsRemoved() => Gui.Run(async () => {
		Frame frame = new(null, "Removed");
		EventHandler<CloseEvent> veto = (_, e) => e.Veto();
		frame.Closing += veto;
		Assert.False(frame.Close());
		frame.Closing -= veto;
		Assert.True(frame.Close());
		await Gui.WaitUntil(() => frame.IsDestroyed);
	});
}
