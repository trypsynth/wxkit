using WxKit.Interop;

namespace WxKit;

/// <summary>
/// A display connected to the system, as it was when it was asked for: its place on the virtual
/// screen, which spans every display, the part windows can use, and its scale. A display can be
/// connected, moved, or rescaled while the application runs, so ask again rather than keep one.
/// </summary>
/// <param name="Index">Its number among <see cref="All"/>.</param>
/// <param name="Name">The system's name for it, which may be empty.</param>
/// <param name="Bounds">Where it is, in pixels, on the virtual screen.</param>
/// <param name="WorkArea">The part of it windows can use, which leaves out a taskbar or a menu bar.</param>
/// <param name="IsPrimary">Whether it is the primary display, which the system shows new windows on.</param>
/// <param name="ScaleFactor">How much larger than at 96 DPI the system draws on it, such as 1.5 for 150%.</param>
public sealed unsafe record Display(int Index, string Name, Rect Bounds, Rect WorkArea, bool IsPrimary, double ScaleFactor) {
	/// <summary>Every display connected to the system.</summary>
	public static IReadOnlyList<Display> All {
		get {
			Application.VerifyAccess();
			int count = WxKitNative.DisplayGetCount();
			Display[] displays = new Display[count];
			for (int index = 0; index < count; index++) displays[index] = At(index);
			return displays;
		}
	}

	/// <summary>The primary display, which the system shows new windows on.</summary>
	public static Display Primary => All.FirstOrDefault(display => display.IsPrimary) ?? All[0];

	/// <summary>The display <paramref name="window"/> is on, or most of it is, or null when it is on none.</summary>
	public static Display? FromWindow(Window window) {
		Application.VerifyAccess();
		ArgumentNullException.ThrowIfNull(window);
		int index = WxKitNative.DisplayGetFromWindow((WxkWindow*)window.Native);
		return index < 0 ? null : At(index);
	}

	/// <summary>The display <paramref name="point"/>, on the virtual screen, is on, or null when it is on none.</summary>
	public static Display? FromPoint(Point point) {
		Application.VerifyAccess();
		int index = WxKitNative.DisplayGetFromPoint(point);
		return index < 0 ? null : At(index);
	}

	private static Display At(int index) => new(
		index,
		NativeString.Take(WxKitNative.DisplayGetName(index)),
		WxKitNative.DisplayGetGeometry(index),
		WxKitNative.DisplayGetClientArea(index),
		WxKitNative.DisplayIsPrimary(index) != 0,
		WxKitNative.DisplayGetScaleFactor(index));
}
