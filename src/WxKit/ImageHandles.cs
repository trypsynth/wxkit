using WxKit.Interop;

namespace WxKit;

// A bitmap's and an icon's native handles, which each wxWidgets port gives with a call of its own,
// so the generator binds none of them.

public unsafe partial class Bitmap {
	/// <summary>
	/// The platform's own handle for the bitmap, for calling native APIs: an HBITMAP on Windows, the
	/// CGContextRef its pixels are in on macOS, and a GdkPixbuf* on Linux, or 0 when the bitmap has
	/// none. The bitmap keeps it, so don't delete or release it, and it is only valid until the
	/// bitmap is disposed or changed. Wraps GetHBITMAP, and GetPixbuf on Linux.
	/// </summary>
	public nint NativeHandle => (nint)WxKitNative.BitmapGetHandle(Native);
}

public unsafe partial class Icon {
	/// <summary>
	/// The platform's own handle for the icon, for calling native APIs: an HICON on Windows, and on
	/// macOS and Linux, where an icon is a bitmap, the same as <see cref="Bitmap.NativeHandle"/>, or
	/// 0 when the icon has none. The icon keeps it, so don't destroy or release it, and it is only
	/// valid until the icon is disposed. Wraps GetHICON.
	/// </summary>
	public nint NativeHandle => (nint)WxKitNative.IconGetHandle(Native);
}
