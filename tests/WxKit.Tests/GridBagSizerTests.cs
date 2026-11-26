namespace WxKit.Tests;

// Sizers that place items in cells, or wrap them onto more lines.
public class GridBagSizerTests {
	[Fact]
	public Task PlacesItemsInCells() => Gui.WithFrame(frame => {
		Panel panel = new(frame);
		GridBagSizer grid = new(new Size(5, 5));
		StaticText label = new(panel, "&Name:");
		TextCtrl name = new(panel);
		Button ok = new(panel, "OK");
		Assert.True(grid.Add(label, new GridPosition(0, 0)));
		Assert.True(grid.Add(name, new GridPosition(0, 1), flags: SizerFlags.Default.Expand()));
		Assert.True(grid.Add(ok, new GridPosition(1, 0), new GridSpan(1, 2)));
		Assert.Equal(new GridPosition(0, 1), grid.GetItemPosition(name));
		Assert.Equal(new GridSpan(1, 2), grid.GetItemSpan(ok));
		Assert.True(grid.CheckForIntersection(new GridPosition(1, 1), GridSpan.Default));
		Assert.True(grid.AddSpacer(new Size(10, 10), new GridPosition(2, 0)));
		panel.Sizer = grid;
		panel.Layout();
	});

	[Fact]
	public Task RefusesATakenCellAndKeepsTheSizer() => Gui.WithFrame(frame => {
		Panel panel = new(frame);
		GridBagSizer grid = new();
		Assert.True(grid.Add(new StaticText(panel, "Taken"), new GridPosition(0, 0)));
		BoxSizer row = new(Orientation.Horizontal);
		Assert.False(grid.Add(row, new GridPosition(0, 0)));
		Assert.False(row.IsDisposed);
		Assert.True(grid.Add(row, new GridPosition(1, 0)));
		Assert.Throws<InvalidOperationException>(() => grid.Add(row, new GridPosition(2, 0)));
		panel.Sizer = grid;
	});

	[Fact]
	public Task WrapsItems() => Gui.WithFrame(frame => {
		Panel panel = new(frame);
		WrapSizer wrap = new(flags: WrapSizerFlags.ExtendLastOnEachLine);
		for (int index = 0; index < 8; index++) wrap.Add(new Button(panel, $"Tag {index}"));
		panel.Sizer = wrap;
		panel.Layout();
	});
}
