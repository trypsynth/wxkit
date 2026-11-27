using WxKit.Interop;

namespace WxKit;

/// <summary>
/// A menu, on a menu bar or inside another menu, or shown on its own by
/// <see cref="Window.PopupMenu(Menu, Point?)"/>. Wraps wxMenu. A menu bar or a menu owns a menu
/// appended to it, and destroys it. A menu on neither is the application's, to dispose.
/// </summary>
public sealed unsafe class Menu : EvtHandler, IDisposable {
	// One object for each item, however it was found, since nothing removes an item from its menu.
	private readonly Dictionary<nint, MenuItem> _items = [];

	public Menu() : base(Create()) {
	}

	internal Menu(WxkMenu* native) : base((WxkEvtHandler*)native) {
	}

	internal WxkMenu* Native => (WxkMenu*)Handle;

	/// <summary>
	/// Adds an item. With a standard id, such as <see cref="StandardId.Exit"/>, and no text, the item
	/// gets the platform's standard text, and on macOS goes where the platform puts it. A tab in the
	/// text starts its keyboard shortcut, as in "&amp;Open\tCtrl+O".
	/// </summary>
	public MenuItem Append(int id, string text = "", string help = "", MenuItemKind kind = MenuItemKind.Normal) {
		ArgumentNullException.ThrowIfNull(text);
		ArgumentNullException.ThrowIfNull(help);
		fixed (char* itemText = text)
		fixed (char* helpText = help) {
			return MenuItem.Wrap(this, WxKitNative.MenuAppendIntStringStringMenuItemKind(Native, id, itemText, NativeString.Length(text), helpText, NativeString.Length(help), (int)kind));
		}
	}

	public MenuItem AppendSeparator() => MenuItem.Wrap(this, WxKitNative.MenuAppendSeparator(Native));

	/// <summary>Adds a submenu, which this menu then owns, as an item that opens it.</summary>
	public MenuItem AppendSubMenu(Menu submenu, string text, string help = "") {
		ArgumentNullException.ThrowIfNull(submenu);
		ArgumentNullException.ThrowIfNull(text);
		ArgumentNullException.ThrowIfNull(help);
		Application.VerifyAccess();
		fixed (char* itemText = text)
		fixed (char* helpText = help) {
			return MenuItem.Wrap(this, WxKitNative.MenuAppendSubMenu(Native, submenu.Native, itemText, NativeString.Length(text), helpText, NativeString.Length(help)));
		}
	}

	/// <summary>How many items the menu has, separators and submenus included, but not the items of its submenus.</summary>
	public int ItemCount => checked((int)WxKitNative.MenuGetMenuItemCount(Native));

	/// <summary>The menu's items, in order, separators and submenus included, as <see cref="Append"/> returned them.</summary>
	public IReadOnlyList<MenuItem> Items {
		get {
			WxkMenu* native = Native;
			int count = checked((int)WxKitNative.MenuGetMenuItemCount(native));
			MenuItem[] items = new MenuItem[count];
			for (int position = 0; position < count; position++) items[position] = Item(WxKitNative.MenuFindItemByPosition(native, (nuint)position));
			return items;
		}
	}

	/// <summary>The item with an id, on this menu or in one of its submenus, or null when there is none.</summary>
	public MenuItem? FindItem(int id) {
		WxkMenuItem* found = WxKitNative.MenuFindItemInt(Native, id);
		return found is null ? null : ItemOf(found);
	}

	/// <summary>
	/// The item with a label, on this menu or in one of its submenus, or null when there is none.
	/// The labels are compared without their accelerator's ampersand and their shortcut, so "Open"
	/// finds "&amp;Open\tCtrl+O". A submenu's item comes before a later item of this menu with the
	/// same label, and a submenu is found by its own label too.
	/// </summary>
	public MenuItem? FindItem(string label) {
		ArgumentNullException.ThrowIfNull(label);
		WxkMenu* native = Native;
		int id;
		fixed (char* text = label) id = WxKitNative.MenuFindItemString(native, text, NativeString.Length(label));
		// wxNOT_FOUND, -1, when no item has the label.
		return id == -1 ? null : FindItem(id);
	}

	// The object for an item of this menu, the same one each time.
	internal MenuItem Item(WxkMenuItem* native) {
		if (native is null) throw new InvalidOperationException("wxWidgets could not add the menu item.");
		if (!_items.TryGetValue((nint)native, out MenuItem? item)) _items[(nint)native] = item = new MenuItem(this, native);
		return item;
	}

	// An item found in this menu or a submenu, whose own menu keeps its object.
	private MenuItem ItemOf(WxkMenuItem* native) => (ObjectRegistry.Wrap<Menu>(WxKitNative.MenuItemGetMenu(native)) ?? this).Item(native);

	/// <summary>
	/// Frees the menu, if it is on no menu bar and in no menu, such as one made to pop up. A menu
	/// that is on one stays, since its owner destroys it.
	/// </summary>
	public void Dispose() {
		if (IsDisposed) return;
		Application.VerifyAccess();
		if (WxKitNative.MenuIsAttached(Native) != 0 || WxKitNative.MenuGetParent(Native) is not null) return;
		WxKitNative.ObjectFree((WxkObject*)Native);
	}

	private static WxkEvtHandler* Create() {
		Application.VerifyAccess();
		return (WxkEvtHandler*)WxKitNative.MenuNewDefault();
	}
}

/// <summary>
/// An item on a menu. Wraps wxMenuItem. Its menu owns it, and destroys it with itself, after which
/// its members throw <see cref="ObjectDisposedException"/>.
/// </summary>
public sealed unsafe class MenuItem {
	private readonly Menu _menu;
	private readonly WxkMenuItem* _native;

	internal MenuItem(Menu menu, WxkMenuItem* native) {
		_menu = menu;
		_native = native;
		Id = WxKitNative.MenuItemGetId(native);
	}

	// A menu deletes its items with itself, and has nothing else that removes one.
	private WxkMenuItem* Native {
		get {
			Application.VerifyAccess();
			return !_menu.IsDisposed ? _native : throw new ObjectDisposedException(nameof(MenuItem), "wxWidgets has destroyed this item, along with its menu.");
		}
	}

	/// <summary>The menu the item is on.</summary>
	public Menu Menu => _menu;

	/// <summary>The item's id, which wxWidgets picks when the item was added with <see cref="StandardId.Any"/>.</summary>
	public int Id { get; }

	/// <summary>Whether the item is a line between items, which can't be chosen.</summary>
	public bool IsSeparator => WxKitNative.MenuItemIsSeparator(Native) != 0;

	/// <summary>The menu the item opens, or null when it opens none.</summary>
	public Menu? SubMenu => ObjectRegistry.Wrap<Menu>(WxKitNative.MenuItemGetSubMenu(Native));

	/// <summary>Whether the user can choose the item. A disabled one is grayed out, and screen readers say it is unavailable.</summary>
	public bool IsEnabled {
		get => WxKitNative.MenuItemIsEnabled(Native) != 0;
		set => WxKitNative.MenuItemEnable(Native, value ? (byte)1 : (byte)0);
	}

	/// <summary>Whether a check or radio item is checked.</summary>
	public bool IsChecked {
		get => WxKitNative.MenuItemIsChecked(Native) != 0;
		set => WxKitNative.MenuItemCheck(Native, value ? (byte)1 : (byte)0);
	}

	/// <summary>The item's text, with its accelerator's ampersand, and its shortcut after a tab.</summary>
	public string Label {
		get => NativeString.Take(WxKitNative.MenuItemGetItemLabel(Native));
		set {
			ArgumentNullException.ThrowIfNull(value);
			WxkMenuItem* native = Native;
			fixed (char* text = value) WxKitNative.MenuItemSetItemLabel(native, text, NativeString.Length(value));
		}
	}

	/// <summary>What a frame's status bar says of the item while it is highlighted.</summary>
	public string Help {
		get => NativeString.Take(WxKitNative.MenuItemGetHelp(Native));
		set {
			ArgumentNullException.ThrowIfNull(value);
			WxkMenuItem* native = Native;
			fixed (char* text = value) WxKitNative.MenuItemSetHelp(native, text, NativeString.Length(value));
		}
	}

	/// <summary>Raised when the user chooses the item.</summary>
	public event EventHandler<CommandEvent>? Click {
		add => Menu.AddHandler(EventType.Menu, Id, Id, value, this);
		remove => Menu.RemoveHandler(EventType.Menu, Id, Id, value);
	}

	internal static MenuItem Wrap(Menu menu, WxkMenuItem* native) => menu.Item(native);
}
