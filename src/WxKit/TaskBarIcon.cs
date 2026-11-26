using WxKit.Interop;

namespace WxKit;

// A taskbar icon is the shim's own class, which shows the menu .NET gives it. wxWidgets keeps the
// application running while one exists, so dispose of it when the application is done with it.
public sealed unsafe partial class TaskBarIcon : IDisposable {
	private Menu? _menu;
	private bool _disposed;

	/// <summary>
	/// Makes an icon, which shows once <see cref="SetIcon"/> gives it one. The type only matters on
	/// macOS, and null is the platform's default, a status item in the menu bar.
	/// </summary>
	public TaskBarIcon(TaskBarIconType? type = null) : this(Create(type)) {
	}

	/// <summary>
	/// The menu shown when the icon is right-clicked, or on macOS clicked, or null for none. The
	/// icon keeps it, and it can be changed while the icon shows.
	/// </summary>
	public Menu? Menu {
		get {
			_ = NativeHandler;
			return _menu;
		}
		set {
			WxKitNative.ManagedTaskBarIconSetMenu(NativeHandler, value is null ? null : value.Native);
			_menu = value;
		}
	}

	/// <summary>Removes the icon, and frees it once the event loop is idle.</summary>
	public void Dispose() {
		if (_disposed || IsDisposed) return;
		_disposed = true;
		WxKitNative.ManagedTaskBarIconDestroy(NativeHandler);
		_menu = null;
	}

	private static WxkEvtHandler* Create(TaskBarIconType? type) {
		Application.VerifyAccess();
		return WxKitNative.ManagedTaskBarIconNew(type is TaskBarIconType given ? (int)given : -1);
	}
}
