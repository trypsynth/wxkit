namespace WxKit.Tests;

// The dialogs and controls wxWidgets makes from others that screen readers know: entry dialogs, a
// rich message dialog, a property sheet, a wizard, a tree with columns, and file system choosers.
public class CompositeControlTests {
	[Fact]
	public Task AsksForANumber() => Gui.WithFrame(frame => {
		using NumberEntryDialog dialog = new(frame, "How many copies?", "&Copies:", "Print", 2, 1, 10);
		Assert.Equal(2, dialog.Value);
	});

	[Fact]
	public Task AsksForCredentials() => Gui.WithFrame(frame => {
		using CredentialEntryDialog dialog = new(frame, "Sign in to the server.", "Sign in");
		dialog.UserName = "quin";
		dialog.Password = "pässword";
		Assert.Equal("quin", dialog.UserName);
		Assert.Equal("pässword", dialog.Password);
	});

	[Fact]
	public Task ShowsARichMessage() => Gui.WithFrame(frame => {
		using RichMessageDialog dialog = new(frame, "Delete the file?", "Delete", MessageDialogStyle.YesNo | MessageDialogStyle.IconWarning);
		dialog.ShowCheckBox("&Don't ask again", true);
		dialog.DetailedText = "The file goes to the recycle bin.";
		dialog.FooterText = "You can undo this.";
		Assert.Equal("&Don't ask again", dialog.CheckBoxText);
		Assert.True(dialog.IsCheckBoxChecked);
		Assert.Equal("The file goes to the recycle bin.", dialog.DetailedText);
		Assert.Equal("You can undo this.", dialog.FooterText);
		Assert.True(dialog.SetYesNoLabels("&Delete", "&Keep"));
		Assert.Equal("Delete the file?", dialog.Message);
	});

	[Fact]
	public Task MakesAPropertySheet() => Gui.WithFrame(frame => {
		using PropertySheetDialog dialog = new(frame, "Settings");
		BookCtrlBase book = Assert.IsAssignableFrom<BookCtrlBase>(dialog.BookCtrl);
		book.AddPage(new Panel(book), "General");
		dialog.CreateButtons();
		dialog.LayoutDialog();
		Assert.Equal(1, book.PageCount);
	});

	[Fact]
	public Task ChainsAWizardsPages() => Gui.WithFrame(frame => {
		using Wizard wizard = new(frame, "New project");
		WizardPageSimple first = new(wizard);
		WizardPageSimple second = new(wizard);
		WizardPageSimple.Chain(first, second);
		wizard.FitToPage(first);
		Assert.Same(second, first.Next);
		Assert.Same(first, second.Previous);
		Assert.True(wizard.HasNextPage(first));
		Assert.True(wizard.HasPreviousPage(second));
		Assert.False(wizard.HasNextPage(second));
	});

	[Fact]
	public Task FillsATreeList() => Gui.WithFrame(frame => {
		TreeListCtrl tree = new(frame, style: TreeListCtrlStyle.Multiple | TreeListCtrlStyle.CheckBox);
		tree.AppendColumn("Name");
		tree.AppendColumn("Size");
		TreeListItem folder = tree.AppendItem(tree.RootItem, "Documents");
		TreeListItem file = tree.AppendItem(folder, "Notes.txt");
		tree.SetItemText(file, 1, "2 KB");
		tree.Expand(folder);
		tree.Select(file);
		tree.CheckItem(file, CheckBoxState.Checked);
		// Where there is no item, the tree gives null.
		Assert.Null(tree.GetNextSibling(folder));
		Assert.Equal(folder, tree.GetItemParent(file));
		Assert.Equal("2 KB", tree.GetItemText(file, 1));
		Assert.Equal(CheckBoxState.Checked, tree.GetCheckedState(file));
		Assert.Equal([file], tree.Selections);
		Assert.Equal(2, tree.ColumnCount);
	});

	[Fact]
	public Task EditsAListOfStrings() => Gui.WithFrame(async frame => {
		EditableListBox box = new(frame, "&Folders");
		frame.Show();
		box.Strings = ["One", "Two"];
		Assert.Equal(["One", "Two"], box.Strings);
		Assert.NotNull(box.ListCtrl);
		// Its buttons show only icons, so screen readers name them by their tooltips.
		await Gui.WaitUntil(() => AccessibilityInfo.Of(box.NewButton!).Name.Length > 0);
		foreach ((BitmapButton? button, string name) in (List<(BitmapButton?, string)>)[(box.NewButton, "New item"), (box.EditButton, "Edit item"), (box.DeleteButton, "Delete item"), (box.UpButton, "Move up"), (box.DownButton, "Move down")]) {
			AccessibilityInfo info = AccessibilityInfo.Of(Assert.IsAssignableFrom<BitmapButton>(button));
			Assert.True(info.Role == AccessibleRole.PushButton && info.Name == name, info.ToString());
		}
	});

	[Fact]
	public Task GivesTimesAsTimeSpans() => Gui.Run(() => {
		TimeSpan before = UpdateUIEvent.UpdateInterval;
		try {
			UpdateUIEvent.UpdateInterval = TimeSpan.FromMilliseconds(250);
			Assert.Equal(TimeSpan.FromMilliseconds(250), UpdateUIEvent.UpdateInterval);
			UpdateUIEvent.UpdateInterval = Timeout.InfiniteTimeSpan;
			Assert.Equal(Timeout.InfiniteTimeSpan, UpdateUIEvent.UpdateInterval);
		} finally {
			UpdateUIEvent.UpdateInterval = before;
		}
	});

	[Fact]
	public Task SelectsAListItemByItsState() => Gui.WithFrame(frame => {
		ListCtrl list = new(frame, style: ListCtrlStyle.Report);
		list.AppendColumn("Name");
		list.InsertItem(0, "One");
		list.InsertItem(1, "Two");
		Assert.Equal(-1, list.GetNextItem(-1, ListNextItem.All, ListItemState.Selected));
		Assert.True(list.SetItemState(1, ListItemState.Selected, ListItemState.Selected));
		Assert.Equal(ListItemState.Selected, list.GetItemState(1, ListItemState.Selected));
		Assert.Equal(ListItemState.None, list.GetItemState(0, ListItemState.Selected));
		Assert.Equal(1, list.GetNextItem(-1, ListNextItem.All, ListItemState.Selected));
	});

	[Fact]
	public Task ChoosesFromTheFileSystem() => Gui.WithFrame(frame => {
		string folder = Directory.CreateTempSubdirectory("wxkit-").FullName;
		try {
			File.WriteAllText(Path.Combine(folder, "a.txt"), "a");
			GenericDirCtrl tree = new(frame, folder);
			Assert.NotNull(tree.TreeCtrl);
			// GTK's file chooser gives one empty path when nothing is chosen.
			Assert.DoesNotContain(Path.Combine(folder, "a.txt"), new FileCtrl(frame, folder).Paths);
		} finally {
			Directory.Delete(folder, recursive: true);
		}
	});
}
