using WxKit.Interop;

namespace WxKit;

/// <summary>The desktop around the application: its alert sound, and its default browser and applications.</summary>
public static unsafe class Desktop {
	/// <summary>Plays the system's alert sound, as for a mistake the user should notice.</summary>
	public static void Bell() {
		Application.VerifyAccess();
		WxKitNative.DesktopBell();
	}

	/// <summary>Opens <paramref name="url"/> in the default browser, and returns whether it could.</summary>
	public static bool OpenUrl(string url) {
		Application.VerifyAccess();
		ArgumentNullException.ThrowIfNull(url);
		fixed (char* text = url) {
			return WxKitNative.DesktopLaunchDefaultBrowser(text, NativeString.Length(url), 0) != 0;
		}
	}

	/// <summary>Opens <paramref name="url"/> in the default browser, and returns whether it could.</summary>
	public static bool OpenUrl(Uri url) {
		ArgumentNullException.ThrowIfNull(url);
		return OpenUrl(url.IsAbsoluteUri ? url.AbsoluteUri : url.OriginalString);
	}

	/// <summary>
	/// Opens the file at <paramref name="path"/> in the application the system opens its kind of
	/// file with, such as a text editor for a .txt file, and returns whether it could.
	/// </summary>
	public static bool Open(string path) {
		Application.VerifyAccess();
		ArgumentNullException.ThrowIfNull(path);
		fixed (char* text = path) {
			return WxKitNative.DesktopLaunchDefaultApplication(text, NativeString.Length(path), 0) != 0;
		}
	}
}

/// <summary>The keyboard's state, outside of a key event.</summary>
public static class Keyboard {
	/// <summary>
	/// Whether <paramref name="key"/> is held down. For a lock key, such as
	/// <see cref="KeyCode.Capital"/>, Caps Lock, it is whether the lock is on. On Linux under
	/// Wayland, only the modifier keys can be asked about.
	/// </summary>
	public static bool IsKeyDown(KeyCode key) {
		Application.VerifyAccess();
		return WxKitNative.DesktopGetKeyState((int)key) != 0;
	}

	/// <summary>Whether Caps Lock is on.</summary>
	public static bool IsCapsLockOn => IsKeyDown(KeyCode.Capital);

	/// <summary>Whether Num Lock is on.</summary>
	public static bool IsNumLockOn => IsKeyDown(KeyCode.NumLock);
}

/// <summary>The pointer's state, outside of a mouse event.</summary>
public static class Mouse {
	/// <summary>Where the pointer is, on the screen. <see cref="Window.ScreenToClient(Point)"/> gives it in a window.</summary>
	public static Point Position {
		get {
			Application.VerifyAccess();
			return WxKitNative.DesktopGetMousePosition();
		}
	}
}

/// <summary>
/// Shows the busy cursor until it is disposed, as in <c>using (new BusyCursor()) { ... }</c>,
/// over every window of the application. Busy cursors nest, so the cursor comes back once the
/// last one is disposed. Create and dispose it on the GUI thread.
/// </summary>
public sealed class BusyCursor : IDisposable {
	private bool _disposed;

	/// <summary>Shows the busy cursor.</summary>
	public BusyCursor() {
		Application.VerifyAccess();
		WxKitNative.BusyCursorBeginBusyCursor();
	}

	/// <summary>Whether a busy cursor is showing.</summary>
	public static bool IsBusy {
		get {
			Application.VerifyAccess();
			return WxKitNative.BusyCursorIsBusy() != 0;
		}
	}

	/// <summary>Brings the cursor back, unless another busy cursor is still showing.</summary>
	public void Dispose() {
		if (_disposed) return;
		Application.VerifyAccess();
		_disposed = true;
		WxKitNative.BusyCursorEndBusyCursor();
	}
}

/// <summary>
/// Disables every top-level window but one until it is disposed, as in
/// <c>using (new WindowDisabler(progress)) { ... }</c>, so the user can't use the rest of the
/// application while a task runs in a window that doesn't block it. The windows are enabled again,
/// as they were, when it is disposed. Create and dispose it on the GUI thread.
/// </summary>
public sealed unsafe class WindowDisabler : IDisposable {
	private void* _native;

	/// <summary>Disables every top-level window but <paramref name="except"/>, or every one when it is null.</summary>
	public WindowDisabler(Window? except = null) {
		Application.VerifyAccess();
		_native = WxKitNative.WindowDisablerNew(except is null ? null : (WxkWindow*)except.Native);
	}

	/// <summary>Enables the windows again.</summary>
	public void Dispose() {
		if (_native is null) return;
		Application.VerifyAccess();
		WxKitNative.WindowDisablerFree(_native);
		_native = null;
	}
}
