namespace WxKit.Tests;

// Virtual members C# overrides, which the shim's directors call.
public class DirectorTests {
	[Fact]
	public Task AsksAVirtualListForItsRows() => Gui.WithFrame(frame => {
		Numbers list = new(frame);
		list.ItemCount = 1_000_000;
		Assert.Equal(1_000_000, list.ItemCount);
		Assert.Equal("Row 123456", list.GetItemText(123456));
		Assert.Equal("123456 squared", list.GetItemText(123456, 1));
		Assert.Contains((123456L, 1L), list.Asked);
	});

	[Fact]
	public Task AsksAVirtualListWhetherARowIsChecked() => Gui.WithFrame(frame => {
		Numbers list = new(frame);
		list.HasCheckBoxes = true;
		Assert.True(list.HasCheckBoxes);
		list.ItemCount = 10;
		Assert.True(list.IsItemChecked(4));
		Assert.False(list.IsItemChecked(5));
	});

	[Fact]
	public Task UsesWxWidgetsOwnWithoutAnOverride() => Gui.WithFrame(frame => {
		ListCtrl list = new(frame, style: ListCtrlStyle.Report | ListCtrlStyle.Virtual);
		list.InsertColumn(0, "Name");
		list.ItemCount = 3;
		Assert.Equal(string.Empty, list.GetItemText(1));
		// A virtual list is meant to override it, so wxWidgets' own says it was called by mistake.
		Assert.Contains("not supposed to be called", Assert.Single(Gui.TakeAssertions()).Message);
	});

	[Fact]
	public Task OverridesThroughADerivedClass() => Gui.WithFrame(frame => {
		Letters list = new(frame);
		list.InsertColumn(0, "Letter");
		list.ItemCount = 26;
		Assert.Equal("C", list.GetItemText(2));
	});

	[Fact]
	public Task ReportsAnExceptionInAnOverride() => Gui.WithFrame(frame => {
		Throwing list = new(frame);
		list.InsertColumn(0, "Name");
		list.ItemCount = 1;
		Assert.Equal(string.Empty, list.GetItemText(0));
		Exception error = Assert.Single(Gui.TakeUnhandled());
		Assert.IsType<InvalidOperationException>(error);
	});

	private sealed class Numbers : ListCtrl {
		public Numbers(Window parent) : base(parent, style: ListCtrlStyle.Report | ListCtrlStyle.Virtual) {
			InsertColumn(0, "Row");
			InsertColumn(1, "Square");
		}

		public List<(long Item, long Column)> Asked { get; } = [];

		protected override string OnGetItemText(int item, int column) {
			Asked.Add((item, column));
			return column == 0 ? $"Row {item}" : $"{item} squared";
		}

		protected override bool OnGetItemIsChecked(int item) => item % 2 == 0;
	}

	private sealed class Letters : ListView {
		public Letters(Window parent) : base(parent, style: ListCtrlStyle.Report | ListCtrlStyle.Virtual) {
		}

		protected override string OnGetItemText(int item, int column) => ((char)('A' + item)).ToString();
	}

	private sealed class Throwing : ListCtrl {
		public Throwing(Window parent) : base(parent, style: ListCtrlStyle.Report | ListCtrlStyle.Virtual) {
		}

		protected override string OnGetItemText(int item, int column) => throw new InvalidOperationException("No rows.");
	}
}
