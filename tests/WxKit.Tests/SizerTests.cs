namespace WxKit.Tests;

public class SizerTests {
	[Fact]
	public Task GivesASizerToItsWindow() => Gui.WithFrame(frame => {
		Panel panel = new(frame);
		BoxSizer sizer = new(Orientation.Vertical);
		sizer.Add(new Button(panel, "&One"), new SizerFlags(1).Expand().Border());
		sizer.AddSpacer(5);
		sizer.AddStretchSpacer();
		panel.Sizer = sizer;
		Assert.Same(sizer, panel.Sizer);
		panel.Layout();
	});

	[Fact]
	public Task RefusesASecondOwner() => Gui.WithFrame(frame => {
		Panel panel = new(frame);
		BoxSizer sizer = new(Orientation.Vertical);
		panel.Sizer = sizer;
		Assert.Throws<InvalidOperationException>(() => frame.Sizer = sizer);
		BoxSizer outer = new(Orientation.Vertical);
		Assert.Throws<InvalidOperationException>(() => outer.Add(sizer));
		Assert.Throws<ArgumentException>(() => outer.Add(outer));
	});

	[Fact]
	public Task DestroysTheSizerItReplaces() => Gui.WithFrame(frame => {
		BoxSizer first = new(Orientation.Vertical);
		BoxSizer second = new(Orientation.Horizontal);
		frame.Sizer = first;
		frame.Sizer = second;
		Assert.True(first.IsDisposed);
		Assert.False(second.IsDisposed);
		frame.Sizer = null;
		Assert.True(second.IsDisposed);
		Assert.Throws<ObjectDisposedException>(() => second.AddSpacer(1));
	});

	[Fact]
	public Task DestroysNestedSizersWithTheirWindow() => Gui.Run(async () => {
		Frame frame = new(null, "Sizers");
		BoxSizer outer = new(Orientation.Vertical);
		BoxSizer inner = new(Orientation.Horizontal);
		inner.Add(new Button(frame, "&Go"));
		outer.Add(inner, SizerFlags.Default.Right());
		frame.Sizer = outer;
		frame.Destroy();
		await Gui.WaitUntil(() => frame.IsDisposed);
		Assert.True(outer.IsDisposed);
		Assert.True(inner.IsDisposed);
	});

	[Fact]
	public Task FreesASizerNothingOwns() => Gui.Run(async () => {
		Abandon();
		GC.Collect();
		GC.WaitForPendingFinalizers();
		// The finalizer posts the free to the GUI thread, which runs it once this yields.
		await Task.Delay(20);
		static void Abandon() => _ = new BoxSizer(Orientation.Vertical);
	});

	[Fact]
	public void BuildsFlagsWithoutChangingTheOriginal() {
		SizerFlags plain = SizerFlags.Default;
		SizerFlags expanded = plain.Expand().Border(Direction.Left, 4).Proportion(2);
		Assert.Equal(0, plain.FlagsValue);
		Assert.Equal(0x2000 | (int)Direction.Left, expanded.FlagsValue);
		Assert.Equal(4, expanded.BorderValue);
		Assert.Equal(2, expanded.ProportionValue);
		Assert.Equal(-1, plain.Border().BorderValue);
		Assert.Equal(0x0900, plain.Right().Center().FlagsValue);
		Assert.Throws<ArgumentOutOfRangeException>(() => plain.Border(Direction.All, -1));
	}
}
