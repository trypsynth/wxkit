namespace WxKit.Tests;

// List and tree controls, which most applications show their data in.
public class ListTests {
	// A column added without a width gets a fixed one, since it has no items yet, and one sized to
	// its heading fits it, or on Windows, as the last column, fills the rest of the list.
	[Fact]
	public Task SizesColumnsToWhatTheyShow() => Gui.WithFrame(frame => {
		ListView list = new(frame, size: new Size(400, 200));
		list.AppendColumn("Name");
		Assert.Equal(80, list.GetColumnWidth(0));
		list.AppendColumn("A rather long heading for a column", width: ListColumnWidth.AutoSizeUseHeader);
		int fitted = list.GetColumnWidth(1);
		Assert.True(fitted > 80, $"The heading's column is {fitted} pixels wide.");
		list.InsertItem(0, "an item rather longer than eighty pixels");
		Assert.True(list.SetColumnWidth(0, ListColumnWidth.AutoSize));
		Assert.True(list.GetColumnWidth(0) > 80, $"The item's column is {list.GetColumnWidth(0)} pixels wide.");
		if (OperatingSystem.IsWindows()) Assert.True(list.GetColumnWidth(0) + list.GetColumnWidth(1) >= list.ClientSize.Width - 1, $"The columns are {list.GetColumnWidth(0)} and {list.GetColumnWidth(1)} pixels wide in {list.ClientSize.Width}.");
	});

	[Fact]
	public Task FillsAReportListView() => Gui.WithFrame(frame => {
		ListView list = new(frame);
		list.InsertColumn(0, "Name");
		list.InsertColumn(1, "Size", ListColumnFormat.Right);
		Assert.Equal(2, list.ColumnCount);
		int first = list.InsertItem(0, "readme.md");
		list.SetItem(first, 1, "4 KB");
		list.InsertItem(1, "license");
		Assert.Equal(2, list.ItemCount);
		Assert.Equal("4 KB", list.GetItemText(first, 1));
		list.Select(1);
		Assert.True(list.IsSelected(1));
		Assert.Equal(1, list.FirstSelected);
		// The list gives an item, and a column, as an item it makes.
		using (ListItem? size = list.GetItem(first, 1)) {
			Assert.NotNull(size);
			Assert.Equal("4 KB", size.Text);
		}
		using (ListItem? selected = list.GetItem(1)) {
			Assert.NotNull(selected);
			Assert.True(selected.State.HasFlag(ListItemState.Selected));
		}
		using (ListItem? heading = list.GetColumn(1)) {
			Assert.NotNull(heading);
			Assert.Equal("Size", heading.Text);
		}
		Assert.Null(list.GetItem(99));
		Assert.Null(list.GetColumn(99));
		// An item's data, such as a key into the application's own records, can be read back and found.
		Assert.True(list.SetItemData(1, 42));
		Assert.Equal(42, list.GetItemData(1));
		Assert.Equal(1, list.FindItem(-1, 42));
	});

	[Fact]
	public Task BuildsATree() => Gui.WithFrame(frame => {
		TreeCtrl tree = new(frame);
		TreeItemId root = tree.AddRoot("Library");
		TreeItemId fiction = tree.AppendItem(root, "Fiction");
		TreeItemId novel = tree.AppendItem(fiction, "A novel");
		tree.AppendItem(root, "Poetry");
		Assert.Equal(root, tree.RootItem);
		Assert.Equal(3, tree.GetChildrenCount(root));
		Assert.Equal(2, tree.GetChildrenCount(root, recursively: false));
		Assert.Equal(fiction, tree.GetItemParent(novel));
		Assert.Equal("A novel", tree.GetItemText(novel));
		// Where there is no item, the tree gives null.
		Assert.Null(tree.GetItemParent(root));
		Assert.Null(tree.GetNextSibling(novel));
		// The tree wxWidgets draws itself, on GTK and macOS, selects the root when it is added.
		tree.Unselect();
		Assert.Null(tree.Selection);
	});

	// A tree sorts an item's children by their text, or as a derived class's OnCompareItems says.
	[Fact]
	public Task SortsATreesItems() => Gui.WithFrame(frame => {
		static string[] Sorted(TreeCtrl tree) {
			TreeItemId root = tree.AddRoot("Library");
			foreach (string text in (string[])["Poetry", "Art", "Fiction"]) tree.AppendItem(root, text);
			tree.SortChildren(root);
			List<string> texts = [];
			for (TreeItemId? item = tree.GetLastChild(root); item is TreeItemId child; item = tree.GetPreviousSibling(child)) texts.Insert(0, tree.GetItemText(child));
			return [.. texts];
		}
		Assert.Equal(["Art", "Fiction", "Poetry"], Sorted(new TreeCtrl(frame)));
		Assert.Equal(["Art", "Poetry", "Fiction"], Sorted(new ByLength(frame)));
	});

	private sealed class ByLength(Window parent) : TreeCtrl(parent) {
		protected override int OnCompareItems(TreeItemId item1, TreeItemId item2) => GetItemText(item1).Length.CompareTo(GetItemText(item2).Length);
	}

	[Fact]
	public Task GivesATreeItsImages() => Gui.WithFrame(frame => {
		TreeCtrl tree = new(frame);
		using Bitmap folder = new(16, 16);
		using Bitmap file = new(16, 16);
		tree.SetImages([folder, file]);
		Assert.Equal(2, tree.ImageCount);
		TreeItemId root = tree.AddRoot("Library", image: 0);
		TreeItemId novel = tree.AppendItem(root, "A novel", image: 1);
		Assert.Equal(0, tree.GetItemImage(root));
		Assert.Equal(1, tree.GetItemImage(novel));
		Assert.Throws<ArgumentNullException>(() => tree.SetImages([folder, null!]));
	});

	[Fact]
	public Task GivesAListViewItsImages() => Gui.WithFrame(frame => {
		ListView list = new(frame);
		using Bitmap icon = new(16, 16);
		list.SetSmallImages([icon]);
		list.SetNormalImages([icon]);
		list.InsertColumn(0, "Name");
		Assert.Equal(0, list.InsertItem(0, "readme.md", 0));
		Assert.True(list.SetItemImage(0, 0));
	});
}
