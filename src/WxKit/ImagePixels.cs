using WxKit.Interop;

namespace WxKit;

// An image's pixels in bulk, as bytes: three for each pixel's color, red, green, and blue, and one
// for each pixel's alpha, row by row from the top left, as wxWidgets keeps them. The image copies
// the bytes it is given, and gives copies of its own.

public unsafe partial class Image {
	/// <summary>
	/// Makes an image from its pixels' colors, and optionally their alpha. Wraps the wxImage
	/// constructor that takes data.
	/// </summary>
	/// <param name="size">The image's size, in pixels.</param>
	/// <param name="rgb">Three bytes for each pixel, red, green, and blue, row by row from the top left: width × height × 3 in all.</param>
	/// <param name="alpha">One byte for each pixel, from 0, which is see-through, to 255, or empty for an image without alpha.</param>
	public Image(Size size, ReadOnlySpan<byte> rgb, ReadOnlySpan<byte> alpha = default) : this(CreateNative(size, rgb, alpha)) {
	}

	/// <summary>A copy of the pixels' colors, three bytes for each pixel, red, green, and blue, row by row from the top left. Wraps GetData.</summary>
	public byte[] GetRgbData() {
		WxkObject* native = Native;
		int length = checked(PixelCount(native) * 3);
		byte* data = WxKitNative.ImageGetData(native);
		return data is null ? new byte[length] : new ReadOnlySpan<byte>(data, length).ToArray();
	}

	/// <summary>Sets every pixel's color, from three bytes for each pixel, red, green, and blue, row by row from the top left. Wraps SetData.</summary>
	public void SetRgbData(ReadOnlySpan<byte> rgb) {
		WxkObject* native = Native;
		CheckLength(rgb, checked(PixelCount(native) * 3), nameof(rgb));
		fixed (byte* bytes = rgb) WxKitNative.ImageSetData(native, bytes);
	}

	/// <summary>A copy of the pixels' alpha, one byte for each pixel, row by row from the top left, or null when the image has no alpha. Wraps GetAlpha.</summary>
	public byte[]? GetAlphaData() {
		WxkObject* native = Native;
		byte* data = WxKitNative.ImageGetAlphaData(native);
		return data is null ? null : new ReadOnlySpan<byte>(data, PixelCount(native)).ToArray();
	}

	/// <summary>
	/// Sets every pixel's alpha, from one byte for each pixel, row by row from the top left, and
	/// gives the image alpha if it had none. Wraps SetAlpha.
	/// </summary>
	public void SetAlphaData(ReadOnlySpan<byte> alpha) {
		WxkObject* native = Native;
		CheckLength(alpha, PixelCount(native), nameof(alpha));
		fixed (byte* bytes = alpha) WxKitNative.ImageSetAlphaData(native, bytes);
	}

	private static WxkObject* CreateNative(Size size, ReadOnlySpan<byte> rgb, ReadOnlySpan<byte> alpha) {
		Application.VerifyAccess();
		if (size.Width <= 0 || size.Height <= 0) throw new ArgumentOutOfRangeException(nameof(size), size, "An image needs a width and a height of at least one pixel.");
		int pixels = checked(size.Width * size.Height);
		CheckLength(rgb, checked(pixels * 3), nameof(rgb));
		if (!alpha.IsEmpty) CheckLength(alpha, pixels, nameof(alpha));
		fixed (byte* colors = rgb)
		fixed (byte* opacity = alpha) {
			return WxKitNative.ImageNewData(size.Width, size.Height, colors, alpha.IsEmpty ? null : opacity);
		}
	}

	private static int PixelCount(WxkObject* native) => checked(WxKitNative.ImageGetWidth(native) * WxKitNative.ImageGetHeight(native));

	private static void CheckLength(ReadOnlySpan<byte> bytes, int expected, string name) {
		if (bytes.Length != expected) throw new ArgumentException($"There are {bytes.Length} bytes, and the image needs {expected}.", name);
	}
}
