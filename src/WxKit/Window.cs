using WxKit.Interop;

namespace WxKit;

// The parts of Window the generator cannot write: the sizer the window owns, which WxKit tracks so
// it knows when wxWidgets destroys it, and the platform's own handle.
/// <remarks>
/// wxWidgets owns every window. A child belongs to its parent and is destroyed with it, and a
/// top-level window is destroyed when it closes. Call <see cref="Destroy"/> to destroy one yourself.
/// </remarks>
public unsafe partial class Window {
	private Sizer? _sizer;

	/// <summary>
	/// The platform's own handle for the window, for calling native APIs: an HWND on Windows, an
	/// NSView* on macOS, and a GtkWidget* on Linux. It is only valid until the window is destroyed.
	/// </summary>
	public nint NativeHandle => (nint)WxKitNative.WindowGetHandle(Native);

	/// <summary>
	/// The sizer that lays out the window's children, or null for none. A sizer belongs to the
	/// window it is set on: replacing it, or destroying the window, destroys it.
	/// </summary>
	public Sizer? Sizer {
		get {
			_ = Native;
			return _sizer;
		}
		set {
			WxkWindow* native = Native;
			if (ReferenceEquals(value, _sizer)) return;
			value?.Attach(this);
			WxKitNative.WindowSetSizer(native, value is null ? null : value.Native, 1);
			_sizer?.MarkDestroyed();
			_sizer = value;
		}
	}

	internal WxkWindow* Native => (WxkWindow*)Handle;

	/// <summary>
	/// Names the window for screen readers, in place of the name the platform gives it, such as
	/// for a button that shows only an icon. Wraps SetAccessibleName, which wxWidgets only
	/// implements on Windows, so on Linux and macOS WxKit sets the name through ATK and
	/// NSAccessibility.
	/// </summary>
	public void SetAccessibleName(string name) {
		ArgumentNullException.ThrowIfNull(name);
		fixed (char* text = name) WxKitNative.WindowSetAccessibleName(Native, text, NativeString.Length(name));
	}

	/// <summary>
	/// Sets the tooltip that shows when the pointer rests on the window. A button that shows only
	/// an icon is named by its tooltip for screen readers, unless <see cref="SetAccessibleName"/>
	/// gave it a name. Wraps SetToolTip.
	/// </summary>
	public void SetToolTip(string text) {
		ArgumentNullException.ThrowIfNull(text);
		fixed (char* chars = text) WxKitNative.WindowSetToolTip(Native, chars, NativeString.Length(text));
	}

	private protected override void OnDestroyed() {
		_sizer?.MarkDestroyed();
		_sizer = null;
	}

	internal static WxkWindow* NativeOf(Window? window) => window is null ? null : window.Native;
}

// The menu bar a frame owns, which WxKit keeps so the getter returns the same object.
public unsafe partial class Frame {
	private MenuBar? _menuBar;

	/// <summary>The frame's menu bar. Setting it gives the menu bar to the frame, which destroys it along with itself.</summary>
	public MenuBar? MenuBar {
		get {
			_ = Native;
			return _menuBar;
		}
		set {
			WxkWindow* native = Native;
			WxKitNative.FrameSetMenuBar(native, NativeOf(value));
			_menuBar = value;
		}
	}
}
