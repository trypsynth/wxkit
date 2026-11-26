namespace WxKit;

/// <summary>
/// What the controls that hold a list of items share: <see cref="Choice"/>, <see cref="ComboBox"/>,
/// <see cref="ListBox"/>, and those derived from them, such as <see cref="CheckListBox"/>. Code
/// that fills or reads a list can take any of them. Wraps wxItemContainer, which each of them
/// derives from in wxWidgets, beside its window class.
/// </summary>
public interface IItemContainer {
	/// <summary>How many items there are.</summary>
	int Count { get; }

	/// <summary>The items' text, in order.</summary>
	IReadOnlyList<string> Strings { get; }

	/// <summary>The index of the selected item, or -1 for none. Setting it raises no event.</summary>
	int Selection { get; set; }

	/// <summary>The selected item's text, or an empty string for none. Setting it selects the item with that text.</summary>
	string StringSelection { get; set; }

	/// <summary>Adds an item at the end, and returns its index, which in a sorted control is where it sorted to.</summary>
	int Append(string item);

	/// <summary>Adds an item at the end, with an object the control keeps with it, and returns its index.</summary>
	int Append(string item, object? data);

	/// <summary>Adds items at the end, and returns the index of the last.</summary>
	int Append(IEnumerable<string> items);

	/// <summary>Adds an item before a position, and returns its index.</summary>
	int Insert(string item, int position);

	/// <summary>Adds an item before a position, with an object the control keeps with it, and returns its index.</summary>
	int Insert(string item, int position, object? data);

	/// <summary>Adds items before a position, and returns the index of the last.</summary>
	int Insert(IEnumerable<string> items, int position);

	/// <summary>Replaces every item.</summary>
	void Set(IEnumerable<string> items);

	/// <summary>Removes an item, and the object kept with it.</summary>
	void Delete(int index);

	/// <summary>Removes every item.</summary>
	void Clear();

	/// <summary>An item's text.</summary>
	string GetString(int index);

	/// <summary>Changes an item's text.</summary>
	void SetString(int index, string text);

	/// <summary>The index of the first item with a text, or -1 for none.</summary>
	int FindString(string text, bool caseSensitive = false);

	/// <summary>The object kept with an item, or null for none.</summary>
	object? GetItemData(int index);

	/// <summary>Keeps an object with an item, in place of any it had.</summary>
	void SetItemData(int index, object? data);
}

public partial class Choice : IItemContainer;

public partial class ComboBox : IItemContainer;

public partial class ListBox : IItemContainer;

public partial class ControlWithItems : IItemContainer;
