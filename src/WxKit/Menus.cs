using WxKit.Interop;

namespace WxKit;

/// <summary>
/// A menu, on a menu bar or inside another menu, or shown on its own by
/// <see cref="Window.PopupMenu(Menu, Point?)"/>. Wraps wxMenu. A menu bar or a menu owns a menu
/// appended to it, and destroys it. A menu on neither is the application's, to dispose.
/// </summary>
public sealed unsafe class Menu : EvtHandler, IDisposable {
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

/// <summary>An item on a menu. Wraps wxMenuItem. Its menu owns it.</summary>
public sealed unsafe class MenuItem {
	private MenuItem(Menu menu, int id) {
		Menu = menu;
		Id = id;
	}

	public Menu Menu { get; }

	/// <summary>The item's id, which wxWidgets picks when the item was added with <see cref="StandardId.Any"/>.</summary>
	public int Id { get; }

	/// <summary>Whether the user can choose the item. A disabled one is grayed out, and screen readers say it is unavailable.</summary>
	public bool IsEnabled {
		get {
			Application.VerifyAccess();
			return WxKitNative.MenuIsEnabled(Menu.Native, Id) != 0;
		}
		set {
			Application.VerifyAccess();
			WxKitNative.MenuEnable(Menu.Native, Id, value ? (byte)1 : (byte)0);
		}
	}

	/// <summary>Whether a check or radio item is checked.</summary>
	public bool IsChecked {
		get {
			Application.VerifyAccess();
			return WxKitNative.MenuIsChecked(Menu.Native, Id) != 0;
		}
		set {
			Application.VerifyAccess();
			WxKitNative.MenuCheck(Menu.Native, Id, value ? (byte)1 : (byte)0);
		}
	}

	/// <summary>The item's text, with its accelerator's ampersand, and its shortcut after a tab.</summary>
	public string Label {
		get {
			Application.VerifyAccess();
			return NativeString.Take(WxKitNative.MenuGetLabel(Menu.Native, Id));
		}
		set {
			ArgumentNullException.ThrowIfNull(value);
			Application.VerifyAccess();
			fixed (char* text = value) WxKitNative.MenuSetLabel(Menu.Native, Id, text, NativeString.Length(value));
		}
	}

	/// <summary>Raised when the user chooses the item.</summary>
	public event EventHandler<CommandEvent>? Click {
		add => Menu.AddHandler(EventType.Menu, Id, Id, value, this);
		remove => Menu.RemoveHandler(EventType.Menu, Id, Id, value);
	}

	internal static MenuItem Wrap(Menu menu, WxkMenuItem* native) {
		if (native is null) throw new InvalidOperationException("wxWidgets could not add the menu item.");
		return new MenuItem(menu, WxKitNative.MenuItemGetId(native));
	}
}
