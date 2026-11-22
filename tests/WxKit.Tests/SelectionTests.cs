namespace WxKit.Tests;

// Controls with more than one item selected or checked at once, which give them back as arrays.
public class SelectionTests {
	[Fact]
	public Task GivesBackAListBoxsSelections() => Gui.WithFrame(frame => {
		ListBox list = new(frame, ["Milk", "Bread", "Eggs", "Tea"], style: ListBoxStyle.Multiple);
		list.Select(1);
		list.Select(3);
		Assert.Equal([1, 3], list.Selections);
	});

	[Fact]
	public Task GivesBackACheckListBoxsCheckedItems() => Gui.WithFrame(frame => {
		CheckListBox list = new(frame, ["Milk", "Bread", "Eggs"]);
		list.Check(0);
		list.Check(2);
		Assert.Equal([0, 2], list.CheckedItems);
	});

	[Fact]
	public Task GivesBackATreesSelections() => Gui.WithFrame(frame => {
		TreeCtrl tree = new(frame, style: TreeCtrlStyle.Multiple | TreeCtrlStyle.HasButtons);
		TreeItemId root = tree.AddRoot("Library");
		TreeItemId fiction = tree.AppendItem(root, "Fiction");
		TreeItemId poetry = tree.AppendItem(root, "Poetry");
		tree.UnselectAll();
		tree.SelectItem(fiction);
		tree.SelectItem(poetry);
		Assert.Equal(new HashSet<TreeItemId> { fiction, poetry }, tree.Selections.ToHashSet());
	});

	[Fact]
	public Task SelectsDataViewRows() => Gui.WithFrame(frame => {
		DataViewListCtrl list = new(frame, style: DataViewCtrlStyle.Multiple);
		list.AppendTextColumn("Task");
		foreach (string task in (string[])["Milk", "Bread", "Eggs"]) list.AppendItem([task]);
		DataViewItem first = list.RowToItem(0);
		DataViewItem last = list.RowToItem(2);
		list.Selections = [first, last];
		Assert.Equal(new HashSet<DataViewItem> { first, last }, list.Selections.ToHashSet());
	});

	[Fact]
	public Task ChoosesSeveralInADialog() => Gui.WithFrame(frame => {
		using MultiChoiceDialog dialog = new(frame, "Which?", "Choose", ["Milk", "Bread", "Eggs"]);
		dialog.Selections = [0, 2];
		// The dialog gives back what was chosen when OK was pressed, which reads the check boxes.
		Assert.True(dialog.TransferDataFromWindow());
		Assert.Equal([0, 2], dialog.Selections);
	});

	[Fact]
	public Task ReordersListColumns() => Gui.WithFrame(frame => {
		Assert.SkipUnless(OperatingSystem.IsWindows(), "Only the Windows list control can reorder its columns. wxWidgets's own, which Linux and macOS use, cannot.");
		ListView list = new(frame);
		list.InsertColumn(0, "Name");
		list.InsertColumn(1, "Size");
		list.InsertColumn(2, "Kind");
		list.ColumnsOrder = [2, 0, 1];
		Assert.Equal([2, 0, 1], list.ColumnsOrder);
	});
}
