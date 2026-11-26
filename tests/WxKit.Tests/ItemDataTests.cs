using System.Runtime.CompilerServices;

namespace WxKit.Tests;

// Objects controls keep with their items, which live as long as the item does.
public class ItemDataTests {
	private sealed record Book(string Title);

	[Fact]
	public Task KeepsAnObjectWithAListBoxItem() => Gui.WithFrame(frame => {
		ListBox list = new(frame);
		Book novel = new("A novel");
		Assert.Equal(0, list.Append("A novel", novel));
		Assert.Equal(1, list.Append("Untitled", null));
		Assert.Same(novel, list.GetItemData(0));
		// A list box, a choice, and a combo box are item containers alike.
		IItemContainer[] containers = [list, new Choice(frame, ["One"]), new ComboBox(frame, choices: ["One", "Two"])];
		Assert.Equal([2, 1, 2], containers.Select(static container => container.Count));
		Assert.Null(list.GetItemData(1));
		Book poems = new("Poems");
		list.SetItemData(1, poems);
		Assert.Same(poems, list.GetItemData(1));
	});

	[Fact]
	public Task FreesAListBoxItemsObjectWithTheItem() => Gui.WithFrame(frame => {
		ListBox list = new(frame);
		WeakReference replaced = AppendBook(list);
		WeakReference deleted = AppendBook(list);
		list.SetItemData(0, new Book("Another"));
		list.Delete(1);
		Collect();
		Assert.False(replaced.IsAlive);
		Assert.False(deleted.IsAlive);
		Assert.IsType<Book>(list.GetItemData(0));
	});

	[Fact]
	public Task KeepsAnObjectWithATreeItem() => Gui.WithFrame(frame => {
		TreeCtrl tree = new(frame);
		Book novel = new("A novel");
		TreeItemId root = tree.AddRoot("Library");
		TreeItemId item = tree.AppendItem(root, "A novel", data: novel);
		Assert.Same(novel, tree.GetItemData(item));
		Assert.Null(tree.GetItemData(root));
		WeakReference replaced = SetBook(tree, root);
		tree.SetItemData(root, "Library");
		Collect();
		Assert.False(replaced.IsAlive);
		Assert.Equal("Library", tree.GetItemData(root));
		WeakReference deleted = SetBook(tree, item);
		tree.Delete(item);
		Collect();
		Assert.False(deleted.IsAlive);
	});

	[Fact]
	public Task KeepsAnObjectWithADataViewTreeItem() => Gui.WithFrame(frame => {
		DataViewTreeCtrl tree = new(frame);
		Book novel = new("A novel");
		DataViewItem fiction = tree.AppendContainer(default, "Fiction");
		DataViewItem item = tree.AppendItem(fiction, "A novel", data: novel);
		Assert.Same(novel, tree.GetItemData(item));
		Assert.Null(tree.GetItemData(fiction));
	});

	// A data view tree's icons are indexes into the images it's given.
	[Fact]
	public Task ShowsADataViewTreesIcons() => Gui.WithFrame(frame => {
		DataViewTreeCtrl tree = new(frame);
		using Bitmap folder = new(16, 16);
		using Bitmap file = new(16, 16);
		tree.SetImages([folder, file]);
		Assert.True(tree.HasImages);
		Assert.Equal(2, tree.ImageCount);
		DataViewItem fiction = tree.AppendContainer(default, "Fiction", icon: 0);
		DataViewItem novel = tree.AppendItem(fiction, "A novel", icon: 1);
		using Icon? icon = tree.GetItemIcon(novel);
		Assert.True(icon is { IsOk: true });
	});

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference AppendBook(ListBox list) {
		Book book = new("Gone");
		list.Append(book.Title, book);
		return new WeakReference(book);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference SetBook(TreeCtrl tree, TreeItemId item) {
		Book book = new("Gone");
		tree.SetItemData(item, book);
		return new WeakReference(book);
	}

	private static void Collect() {
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
	}
}
