using System.Runtime.Versioning;

namespace WxKit.Tests;

// Data views, through their own stores, the models C# writes, and the values that cross between.
public class DataViewTests {
	[Fact]
	public Task FillsAList() => Gui.WithFrame(frame => {
		DataViewListCtrl list = new(frame);
		list.AppendTextColumn("Task");
		list.AppendToggleColumn("Done");
		list.AppendItem(["Milk", false]);
		list.AppendItem(["Bread", true]);
		Assert.Equal(2, list.ItemCount);
		Assert.Equal("Bread", list.GetTextValue(1, 0));
		Assert.True(list.GetToggleValue(1, 1));
		list.SetValue("Eggs", 0, 0);
		list.SetToggleValue(true, 0, 1);
		Assert.Equal("Eggs", list.GetTextValue(0, 0));
		Assert.True(list.GetToggleValue(0, 1));
		DataViewItem second = list.RowToItem(1);
		Assert.True(second.IsOk);
		Assert.Equal(1, list.ItemToRow(second));
		list.DeleteItem(0);
		Assert.Equal(1, list.ItemCount);
	});

	[Fact]
	public Task GivesBackItsColumns() => Gui.WithFrame(frame => {
		DataViewListCtrl list = new(frame);
		DataViewColumn? task = list.AppendTextColumn("Task", width: 120);
		DataViewColumn? done = list.AppendToggleColumn("Done");
		Assert.NotNull(task);
		Assert.NotNull(done);
		Assert.Same(task, list.GetColumn(0));
		Assert.Same(list, task.Owner);
		Assert.Equal("Task", task.Title);
		Assert.Equal(0, task.ModelColumn);
		Assert.Equal(1, done.ModelColumn);
		Assert.Equal(1, list.GetColumnPosition(done));
		task.Title = "To do";
		task.Width = 150;
		task.IsSortable = true;
		Assert.Equal("To do", task.Title);
		// GTK gives the width a column is shown at, which is 0 until the control is on screen.
		if (!OperatingSystem.IsLinux()) Assert.Equal(150, task.Width);
		Assert.True(task.IsSortable);
		Assert.True(task.Flags.HasFlag(DataViewColumnFlags.Sortable));
	});

	[Fact]
	public Task MarksItsColumnsDestroyed() => Gui.WithFrame(frame => {
		DataViewListCtrl list = new(frame);
		DataViewColumn task = list.AppendTextColumn("Task")!;
		DataViewColumn done = list.AppendToggleColumn("Done")!;
		DataViewColumn notes = list.AppendTextColumn("Notes")!;
		Assert.True(list.DeleteColumn(done));
		Assert.True(done.IsDestroyed);
		Assert.Throws<ObjectDisposedException>(() => done.Title);
		Assert.Equal(2, list.ColumnCount);
		Assert.Same(notes, list.GetColumn(1));
		DataViewListCtrl other = new(frame);
		DataViewColumn elsewhere = other.AppendTextColumn("Elsewhere")!;
		Assert.False(list.DeleteColumn(elsewhere));
		Assert.False(elsewhere.IsDestroyed);
		Assert.True(list.ClearColumns());
		Assert.True(task.IsDestroyed);
		Assert.True(notes.IsDestroyed);
		Assert.Equal(0, list.ColumnCount);
		other.Destroy();
		Assert.True(elsewhere.IsDestroyed);
	});

	[Fact]
	public Task RefusesAValueItCannotPass() => Gui.WithFrame(frame => {
		DataViewListCtrl list = new(frame);
		list.AppendTextColumn("Task");
		Assert.Throws<ArgumentException>(() => list.AppendItem([new object()]));
		Assert.Equal(0, list.ItemCount);
	});

	[Fact]
	public unsafe Task RoundTripsValues() => Gui.Run(() => {
		using Bitmap bitmap = new(4, 4);
		using DataViewIconText iconText = new("Label");
		object?[] values = [null, "text", true, 42L, 2.5, bitmap, iconText];
		foreach (object? value in values) {
			using NativeVariant variant = new(value);
			object? back = NativeVariant.Get(variant.Handle);
			switch (value) {
				case Bitmap:
					Assert.Equal(new Size(4, 4), Assert.IsType<Bitmap>(back).Size);
					break;
				case DataViewIconText:
					Assert.Equal("Label", Assert.IsType<DataViewIconText>(back).Text);
					break;
				default:
					Assert.Equal(value, back);
					break;
			}
		}
		using NativeVariant number = new(7);
		Assert.Equal(7L, NativeVariant.Get(number.Handle));
	});

	[Fact]
	public Task ShowsAVirtualListModel() => Gui.WithFrame(frame => {
		DataViewCtrl view = new(frame);
		view.AppendTextColumn("Number", 0);
		view.AppendTextColumn("Square", 1);
		Squares model = new(1_000_000);
		view.Model = model;
		Assert.Same(model, view.Model);
		Assert.Equal(1_000_000, model.Count);
		DataViewItem item = model.GetItem(12345);
		Assert.Equal(12345, model.GetRow(item));
		model.NotifyRowValueChanged(12345, 1);
		model.Reset(10);
		Assert.Equal(10, model.Count);
	});

	[Fact]
	public Task ExpandsATreeModel() => Gui.WithFrame(frame => {
		DataViewCtrl view = new(frame);
		view.AppendTextColumn("Name", 0);
		Folder music = new("Music", [new Folder("Jazz", []), new Folder("Folk", [])]);
		List<Folder> top = [music, new Folder("Photos", [])];
		Folders model = new(top);
		view.Model = model;
		DataViewItem musicItem = model.ItemOf(music);
		Assert.Same(music, model.ObjectOf(musicItem));
		view.Expand(musicItem);
		Assert.True(view.IsExpanded(musicItem));
		Assert.Contains(music, model.Opened);
		Folder blues = new("Blues", []);
		music.Children.Add(blues);
		Assert.True(model.NotifyItemAdded(music, blues));
		Assert.True(model.NotifyItemChanged(blues));
		Assert.Same(music, model.ObjectOf(model.ItemOf(music)));
		Assert.Equal(DataViewItem.Root, model.ItemOf(null));
		// Deleting a folder forgets the items under it too.
		DataViewItem bluesItem = model.ItemOf(blues);
		top.Remove(music);
		Assert.True(model.NotifyItemDeleted(null, music));
		Assert.Null(model.ObjectOf(musicItem));
		Assert.Null(model.ObjectOf(bluesItem));
	});

	[Fact]
	public Task SelectsAModelsItem() => Gui.WithFrame(frame => {
		DataViewCtrl view = new(frame);
		view.AppendTextColumn("Name", 0);
		Folder photos = new("Photos", []);
		Folders model = new([new Folder("Music", []), photos]);
		view.Model = model;
		DataViewItem item = model.ItemOf(photos);
		view.Select(item);
		Assert.True(view.IsSelected(item));
		Assert.Equal(item, view.Selection);
	});

	[Fact]
	public Task KeepsItsModelAlive() => Gui.WithFrame(async frame => {
		DataViewCtrl view = new(frame);
		view.AppendTextColumn("Number", 0);
		view.Model = new Squares(5);
		for (int i = 0; i < 3; i++) {
			GC.Collect();
			GC.WaitForPendingFinalizers();
			await Task.Yield();
		}
		Squares model = Assert.IsType<Squares>(view.Model);
		Assert.Equal(5, model.Count);
	});

	// A screen reader hears a row's first column as its name, and the others, with their headings, as its description.
	[Fact]
	[SupportedOSPlatform("windows")]
	public Task DescribesItsRowsToScreenReaders() => Gui.WithFrame(frame => {
		Assert.SkipUnless(OperatingSystem.IsWindows(), "These read the rows through Microsoft Active Accessibility, which is Windows's.");
		DataViewCtrl view = new(frame);
		view.AppendTextColumn("Number", 0);
		view.AppendTextColumn("Square", 1);
		view.Model = new Squares(3);
		IAccessible msaa = Msaa.Of(view);
		Assert.Equal(3, msaa.GetChildCount());
		Assert.Equal("2", msaa.GetName(3));
		Assert.Equal("Square: 4", msaa.GetDescription(3));
	});

	[Fact]
	[SupportedOSPlatform("windows")]
	public Task ReportsAnExceptionInAModel() => Gui.WithFrame(frame => {
		Assert.SkipUnless(OperatingSystem.IsWindows(), "These read the rows through Microsoft Active Accessibility, which is Windows's.");
		DataViewCtrl view = new(frame);
		view.AppendTextColumn("Number", 0);
		view.Model = new Throwing();
		_ = Msaa.Of(view).GetName(1);
		Assert.IsType<InvalidOperationException>(Assert.Single(Gui.TakeUnhandled()));
	});

	private sealed class Throwing : DataViewVirtualListModel {
		public Throwing() : base(1) {
		}

		protected override object? GetValue(int row, int column) => throw new InvalidOperationException("No value.");
	}

	private sealed class Squares : DataViewVirtualListModel {
		public Squares(int count) : base(count) {
		}

		protected override object? GetValue(int row, int column) => column == 0 ? row.ToString(System.Globalization.CultureInfo.InvariantCulture) : (row * (long)row).ToString(System.Globalization.CultureInfo.InvariantCulture);
	}

	private sealed record Folder(string Name, List<Folder> Children);

	private sealed class Folders : DataViewTreeModel<Folder> {
		private readonly List<Folder> _top;

		public Folders(List<Folder> top) => _top = top;

		public List<Folder> Opened { get; } = [];

		protected override object? GetValue(Folder item, int column) => item.Name;

		protected override Folder? GetParent(Folder item) => Find(_top, null, item);

		protected override bool IsContainer(Folder item) => item.Children.Count > 0 || item.Name == "Music";

		protected override IEnumerable<Folder> GetChildren(Folder? item) {
			if (item is null) return _top;
			Opened.Add(item);
			return item.Children;
		}

		private static Folder? Find(List<Folder> folders, Folder? parent, Folder item) {
			foreach (Folder folder in folders) {
				if (ReferenceEquals(folder, item)) return parent;
				if (Find(folder.Children, folder, item) is Folder found) return found;
			}
			return null;
		}
	}
}
