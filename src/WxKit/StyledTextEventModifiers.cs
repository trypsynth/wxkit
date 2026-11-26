using WxKit.Interop;

namespace WxKit;

public unsafe partial class StyledTextEvent {
	/// <summary>Where the pointer was, in the control, for the events of dwelling and of dragging.</summary>
	public Point MousePosition => new(WxKitNative.StyledTextEventGetX(Handle), WxKitNative.StyledTextEventGetY(Handle));

	/// <summary>The modifier keys held when the event happened, such as for a click on a margin or a hotspot.</summary>
	public KeyModifiers Modifiers => FromScintilla(WxKitNative.StyledTextEventGetModifiers(Handle));

	// Scintilla's modifiers are Shift 1, Ctrl 2, Alt 4, Super 8, and Meta 16, where wxWidgets's are
	// Alt 1, Control 2, Shift 4, and Meta 8. Super is the Windows key, which wxWidgets calls Meta.
	// Scintilla's Meta is a Mac's Control key, which wxWidgets gives as 16 there.
	internal static KeyModifiers FromScintilla(int modifiers) {
		KeyModifiers keys = KeyModifiers.None;
		if ((modifiers & 1) != 0) keys |= KeyModifiers.Shift;
		if ((modifiers & 2) != 0) keys |= KeyModifiers.Control;
		if ((modifiers & 4) != 0) keys |= KeyModifiers.Alt;
		if ((modifiers & 8) != 0) keys |= KeyModifiers.Meta;
		if ((modifiers & 16) != 0) keys |= KeyModifiers.RawControl;
		return keys;
	}
}
