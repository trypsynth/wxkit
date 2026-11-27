using System.Runtime.InteropServices;

namespace WxKit.Tests;

// Sends windows the messages Windows sends when the user uses a control, so tests can click a
// button or choose a menu item. SendMessage runs the handling before it returns, events included.
internal static partial class Win32 {
	private const uint WmCommand = 0x0111;

	// BM_CLICK only works on a button in the active window, so this sends the BN_CLICKED
	// notification a click makes the button send its parent, which works whether or not it is shown.
	public static void Click(Button button) => SendMessage(button.Parent!.NativeHandle, WmCommand, button.Id & 0xFFFF, button.NativeHandle);

	public static void ChooseMenuItem(Frame frame, MenuItem item) => SendMessage(frame.NativeHandle, WmCommand, item.Id & 0xFFFF, 0);

	// WM_KEYDOWN, with the virtual key code, which for letters is the upper-case letter.
	public static void PressKey(Window window, char key) => SendMessage(window.NativeHandle, 0x0100, key, 0);

	// WM_HOTKEY, which Windows sends the window that registered a hot key when it's pressed, with
	// the hot key's id, and its modifiers, as MOD_ALT, MOD_CONTROL, and MOD_SHIFT, below its virtual key code.
	public static void PressHotKey(Window window, int id, uint modifiers, int virtualKey) => SendMessage(window.NativeHandle, 0x0312, id, (nint)(((uint)virtualKey << 16) | modifiers));

	// Whether Windows has a device to play sounds on, which a machine without speakers or a sound
	// card, such as a CI runner, may not.
	public static bool HasAudioOutput => WaveOutGetNumDevs() > 0;

	// WM_POWERBROADCAST, which Windows sends top-level windows as the system suspends,
	// PBT_APMSUSPEND, and as it resumes after the user woke it, PBT_APMRESUMESUSPEND.
	public static void Suspend(Window window) => SendMessage(window.NativeHandle, 0x0218, 4, 0);

	public static void Resume(Window window) => SendMessage(window.NativeHandle, 0x0218, 7, 0);

	// WM_DISPLAYCHANGE, which Windows sends top-level windows when a display's resolution changes,
	// with the bits per pixel, and the width below the height.
	public static void ChangeDisplay(Window window, int width, int height) => SendMessage(window.NativeHandle, 0x007E, 32, (nint)(((uint)height << 16) | (uint)width));

	// WM_SETTINGCHANGE as Windows sends it when the user changes the cursor size, with the value
	// wxWidgets looks for, 0x2029, which Windows doesn't name.
	public static void ChangeCursorSize(Window window) => SendMessage(window.NativeHandle, 0x001A, 0x2029, 0);

	// WM_SIZING, which Windows sends a window as the user drags its bottom right corner,
	// WMSZ_BOTTOMRIGHT, with the bounds it is to have, which the window can change.
	public static NativeRect Resize(Window window, NativeRect bounds) {
		SendMessage(window.NativeHandle, 0x0214, 8, ref bounds);
		return bounds;
	}

	// Windows's RECT, whose right and bottom are just outside it.
	[StructLayout(LayoutKind.Sequential)]
	public record struct NativeRect(int Left, int Top, int Right, int Bottom);

	[LibraryImport("user32.dll", EntryPoint = "SendMessageW")]
	private static partial nint SendMessage(nint window, uint message, nint wParam, nint lParam);

	[LibraryImport("winmm.dll", EntryPoint = "waveOutGetNumDevs")]
	private static partial uint WaveOutGetNumDevs();

	[LibraryImport("user32.dll", EntryPoint = "SendMessageW")]
	private static partial nint SendMessage(nint window, uint message, nint wParam, ref NativeRect lParam);
}
