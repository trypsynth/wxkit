using WxKit.Interop;

namespace WxKit;

// An icon or a cursor from a file, or an icon from a bitmap, as methods that can say they failed.
// wxWidgets' constructors from a file give an object that is not usable when the file can't be
// read, and its empty constructors exist for LoadFile and CopyFromBitmap.
public unsafe partial class Icon {
	/// <summary>
	/// Loads an icon from a file, and returns null if it could not, such as when the file is
	/// missing or is not an icon.
	/// </summary>
	/// <param name="fileName">The file's path.</param>
	/// <param name="type">The format, or null for the platform's default, which is ICO on Windows.</param>
	/// <param name="desiredSize">For a file with several sizes, the size to load, or null for the default.</param>
	public static Icon? FromFile(string fileName, BitmapType? type = null, Size? desiredSize = null) {
		ArgumentNullException.ThrowIfNull(fileName);
		Application.VerifyAccess();
		Size size = desiredSize ?? new Size(-1, -1);
		Icon icon;
		fixed (char* characters = fileName) icon = new(WxKitNative.IconNewStringBitmapTypeIntInt(characters, (nuint)fileName.Length, (int)(type ?? default), (byte)(type is null ? 0 : 1), size.Width, size.Height));
		if (icon.IsOk) return icon;
		icon.Dispose();
		return null;
	}

	/// <summary>Makes an icon from a bitmap, such as one drawn or loaded from a PNG file.</summary>
	public static Icon FromBitmap(Bitmap bitmap) {
		ArgumentNullException.ThrowIfNull(bitmap);
		Application.VerifyAccess();
		Icon icon = new(WxKitNative.IconNewDefault());
		WxKitNative.IconCopyFromBitmap(icon.Native, bitmap.Native);
		return icon;
	}
}

public unsafe partial class Cursor {
	/// <summary>
	/// Loads a cursor from a file, and returns null if it could not, such as when the file is
	/// missing or is not a cursor or an image.
	/// </summary>
	/// <param name="fileName">The file's path.</param>
	/// <param name="type">The format, or null for the platform's default, which is CUR on Windows.</param>
	/// <param name="hotSpot">Where in the cursor it points, for a format that doesn't say, such as PNG.</param>
	public static Cursor? FromFile(string fileName, BitmapType? type = null, Point hotSpot = default) {
		ArgumentNullException.ThrowIfNull(fileName);
		Application.VerifyAccess();
		Cursor cursor;
		fixed (char* characters = fileName) cursor = new(WxKitNative.CursorNewStringBitmapTypeIntInt(characters, (nuint)fileName.Length, (int)(type ?? default), (byte)(type is null ? 0 : 1), hotSpot.X, hotSpot.Y));
		if (cursor.IsOk) return cursor;
		cursor.Dispose();
		return null;
	}
}
