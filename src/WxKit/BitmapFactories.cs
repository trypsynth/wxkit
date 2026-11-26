using WxKit.Interop;

namespace WxKit;

// A bitmap of a size in other than pixels is made in one step, where wxWidgets makes an empty
// bitmap and then creates it.

public unsafe partial class Bitmap {
	/// <summary>
	/// Makes a bitmap whose size is given in device-independent pixels, for a display of this scale,
	/// so it has that many more pixels. Wraps CreateWithDIPSize.
	/// </summary>
	/// <param name="size">The size, in device-independent pixels.</param>
	/// <param name="scale">The display's scale, such as 2 for one of twice the density, which a window's DpiScaleFactor gives.</param>
	/// <param name="depth">The color depth, which is the screen's when left out.</param>
	public static Bitmap FromDipSize(Size size, double scale, int depth = -1) => Create(size, scale, depth, logical: false);

	/// <summary>
	/// Makes a bitmap whose size is given in logical pixels, for a display of this scale. Logical
	/// pixels are the display's own on Windows, and device-independent ones elsewhere. Wraps
	/// CreateWithLogicalSize.
	/// </summary>
	/// <param name="size">The size, in logical pixels.</param>
	/// <param name="scale">The display's scale, such as 2 for one of twice the density.</param>
	/// <param name="depth">The color depth, which is the screen's when left out.</param>
	public static Bitmap FromLogicalSize(Size size, double scale, int depth = -1) => Create(size, scale, depth, logical: true);

	private static Bitmap Create(Size size, double scale, int depth, bool logical) {
		Application.VerifyAccess();
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size.Width, nameof(size));
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size.Height, nameof(size));
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(scale);
		Bitmap bitmap = new(WxKitNative.BitmapNewDefault());
		byte created = logical ? WxKitNative.BitmapCreateWithLogicalSize(bitmap.Native, size, scale, depth) : WxKitNative.BitmapCreateWithDIPSize(bitmap.Native, size, scale, depth);
		if (created != 0) return bitmap;
		bitmap.Dispose();
		throw new InvalidOperationException("The bitmap could not be made.");
	}
}
