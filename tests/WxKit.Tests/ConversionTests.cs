namespace WxKit.Tests;

// Checks that each kind of value crosses into wxWidgets and back intact.
public class ConversionTests {
	[Fact]
	public Task RoundTripsAColor() => Gui.WithFrame(frame => {
		Panel panel = new(frame);
		Color teal = new(0, 128, 128);
		panel.BackgroundColor = teal;
		Assert.Equal(teal, panel.BackgroundColor);
	});

	[Fact]
	public Task RestoresTheDefaultColorWhenSetToNull() => Gui.WithFrame(frame => {
		Panel panel = new(frame);
		Color? original = panel.BackgroundColor;
		panel.BackgroundColor = new Color(255, 0, 0);
		panel.BackgroundColor = null;
		Assert.Equal(original, panel.BackgroundColor);
	});

	[Fact]
	public Task RoundTripsSizesAndRectangles() => Gui.WithFrame(frame => {
		frame.Size = new Size(320, 240);
		Assert.Equal(new Size(320, 240), frame.Size);
		Rect rect = frame.Rect;
		Assert.Equal(frame.Size, rect.Size);
		Assert.Equal(frame.Position, rect.Position);
	});

	[Fact]
	public Task GivesCountsAsInt() => Gui.WithFrame(frame => {
		MenuBar bar = new();
		bar.Append(new Menu(), "&File");
		bar.Append(new Menu(), "&Edit");
		frame.MenuBar = bar;
		Assert.Equal(2, bar.MenuCount);
	});

	[Fact]
	public Task GivesOutputParametersAsOut() => Gui.WithFrame(frame => {
		TextCtrl text = new(frame, "hello world");
		Assert.True(text.PositionToXY(7, out int column, out int line));
		Assert.Equal((7, 0), (column, line));
		text.SetSelection(2, 7);
		Assert.Equal((2, 5), (text.SelectionStart, text.SelectionLength));
	});

	[Fact]
	public Task ChangesAFontByCopyingIt() => Gui.Run(() => {
		using Font font = new(11, FontFamily.Default, FontStyle.Normal, FontWeight.Normal);
		using Font? bold = font.WithBold();
		Assert.NotNull(bold);
		Assert.NotSame(font, bold);
		Assert.Equal(FontWeight.Bold, bold.Weight);
		Assert.Equal(FontWeight.Normal, font.Weight);
	});

	[Fact]
	public Task GivesAWindowsChildren() => Gui.WithFrame(frame => {
		Panel panel = new(frame);
		Button first = new(panel, "&First");
		Button second = new(panel, "&Second");
		Assert.Equal([first, second], panel.Children);
	});

	[Fact]
	public Task RoundTripsEnums() => Gui.WithFrame(frame => {
		Button button = new(frame, "&Small");
		button.WindowVariant = WindowVariant.Small;
		Assert.Equal(WindowVariant.Small, button.WindowVariant);
		// wxOSX does not mirror windows, so a frame's layout direction stays the default there.
		frame.LayoutDirection = LayoutDirection.RightToLeft;
		Assert.Equal(OperatingSystem.IsMacOS() ? LayoutDirection.Default : LayoutDirection.RightToLeft, frame.LayoutDirection);
	});
}
