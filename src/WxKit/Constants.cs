namespace WxKit;

/// <summary>
/// The ids wxWidgets gives standard commands, such as wxID_OK. A button or menu item with one of
/// these gets the platform's standard label and shortcut, and a dialog's buttons with them close
/// it. <see cref="Any"/> lets wxWidgets pick an id. The ids are generated from wxStandardID.
/// </summary>
public static partial class StandardId;

/// <summary>How an <see cref="Image"/> loads a file.</summary>
[Flags]
public enum ImageLoadFlags {
	None = 0,
	/// <summary>Logs a warning for a problem that does not keep the image from loading. It is on by default.</summary>
	Verbose = 1,
}

/// <summary>Which way a sizer lays its items out, or which way to center a window.</summary>
[Flags]
public enum Orientation {
	Horizontal = 0x0004,
	Vertical = 0x0008,
	Both = Horizontal | Vertical,
}

/// <summary>The sides of an item that a border applies to.</summary>
[Flags]
public enum Direction {
	Left = 0x0010,
	Right = 0x0020,
	Top = 0x0040,
	Bottom = 0x0080,
	All = Left | Right | Top | Bottom,
}

/// <summary>What kind of item a menu item is.</summary>
public enum MenuItemKind {
	Normal = 0,
	Check = 1,
	Radio = 2,
}
