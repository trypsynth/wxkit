using WxKit.Interop;

namespace WxKit;

/// <summary>
/// Sends mouse and keyboard input as if the user had given it, as a test of an application's
/// user interface does. Wraps wxUIActionSimulator. Each method returns false when the system could
/// not send the input.
/// </summary>
/// <remarks>
/// The input goes where the system sends real input: to the window under the mouse, or the one
/// with the keyboard focus, which must be in a window that is shown and active. It arrives as the
/// event loop runs, not before the method returns. On macOS, the application needs permission to
/// control the computer, in the Privacy and Security settings.
/// </remarks>
public static unsafe class UIActionSimulator {
	/// <summary>Moves the mouse to a point, in screen coordinates.</summary>
	public static bool MouseMove(Point point) => WxKitNative.UIActionSimulatorMouseMovePoint(Simulator, point) != 0;

	public static bool MouseDown(MouseButton button = MouseButton.Left) => WxKitNative.UIActionSimulatorMouseDown(Simulator, (int)button) != 0;

	public static bool MouseUp(MouseButton button = MouseButton.Left) => WxKitNative.UIActionSimulatorMouseUp(Simulator, (int)button) != 0;

	/// <summary>Presses and releases a mouse button where the mouse is.</summary>
	public static bool MouseClick(MouseButton button = MouseButton.Left) => WxKitNative.UIActionSimulatorMouseClick(Simulator, (int)button) != 0;

	public static bool MouseDoubleClick(MouseButton button = MouseButton.Left) => WxKitNative.UIActionSimulatorMouseDblClick(Simulator, (int)button) != 0;

	/// <summary>Presses a key, holding the modifiers, without releasing it.</summary>
	public static bool KeyDown(KeyCode key, KeyModifiers modifiers = KeyModifiers.None) => WxKitNative.UIActionSimulatorKeyDown(Simulator, (int)key, (int)modifiers) != 0;

	public static bool KeyUp(KeyCode key, KeyModifiers modifiers = KeyModifiers.None) => WxKitNative.UIActionSimulatorKeyUp(Simulator, (int)key, (int)modifiers) != 0;

	/// <summary>Presses and releases a key, holding the modifiers, such as Ctrl+S, which a letter's code is the character of, as in <c>(KeyCode)'S'</c>.</summary>
	public static bool Char(KeyCode key, KeyModifiers modifiers = KeyModifiers.None) => WxKitNative.UIActionSimulatorChar(Simulator, (int)key, (int)modifiers) != 0;

	/// <summary>Types text, one key at a time, which can only use the keys of a US keyboard.</summary>
	public static bool Text(string text) {
		ArgumentNullException.ThrowIfNull(text);
		fixed (char* data = text) return WxKitNative.UIActionSimulatorText(Simulator, data, NativeString.Length(text)) != 0;
	}

	/// <summary>Chooses the item with the text in the control with the keyboard focus, such as a choice or combo box, as a user would with the keyboard.</summary>
	public static bool Select(string text) {
		ArgumentNullException.ThrowIfNull(text);
		fixed (char* data = text) return WxKitNative.UIActionSimulatorSelect(Simulator, data, NativeString.Length(text)) != 0;
	}

	// One simulator, made on the GUI thread the first time it's needed, serves every call, since it
	// keeps no state between them that matters.
	private static WxkUIActionSimulator* _simulator;

	private static WxkUIActionSimulator* Simulator {
		get {
			Application.VerifyAccess();
			if (_simulator is null) _simulator = WxKitNative.UIActionSimulatorNew();
			return _simulator is not null ? _simulator : throw new InvalidOperationException("wxWidgets could not make an input simulator.");
		}
	}
}
