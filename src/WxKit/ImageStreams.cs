using WxKit.Interop;

namespace WxKit;

// wxImage's constructors from a file, and its LoadFile and SaveFile overloads that take a
// wxInputStream and a wxOutputStream, over .NET's streams, through memory, such as for an image on
// the clipboard, in a resource, or from another library. Loading gives null rather than an image
// that is not usable.
public unsafe partial class Image {
	/// <summary>
	/// Loads an image from a file in a format whose handler was added, such as with
	/// <see cref="AddPngHandler"/>, and returns null if it could not. BMP needs no handler.
	/// </summary>
	/// <param name="fileName">The file's path.</param>
	/// <param name="type">The format, or <see cref="BitmapType.Any"/> to find it from the file.</param>
	/// <param name="index">For a format with several images in a file, such as ICO, the one to load, or -1 for the default.</param>
	public static Image? FromFile(string fileName, BitmapType type = BitmapType.Any, int index = -1) {
		ArgumentNullException.ThrowIfNull(fileName);
		Application.VerifyAccess();
		using QuietLog quiet = QuietLog.Start();
		fixed (char* characters = fileName) return Loaded(new Image(WxKitNative.ImageNewStringBitmapTypeInt(characters, (nuint)fileName.Length, (int)type, index)));
	}

	/// <summary>Loads an image from a file whose format is given as a MIME type, such as image/png, and returns null if it could not.</summary>
	/// <param name="fileName">The file's path.</param>
	/// <param name="mimeType">The format's MIME type.</param>
	/// <param name="index">For a format with several images in a file, such as ICO, the one to load, or -1 for the default.</param>
	public static Image? FromFile(string fileName, string mimeType, int index = -1) {
		ArgumentNullException.ThrowIfNull(fileName);
		ArgumentNullException.ThrowIfNull(mimeType);
		Application.VerifyAccess();
		using QuietLog quiet = QuietLog.Start();
		fixed (char* characters = fileName, mime = mimeType) return Loaded(new Image(WxKitNative.ImageNewStringStringInt(characters, (nuint)fileName.Length, mime, (nuint)mimeType.Length, index)));
	}

	/// <summary>
	/// Loads an image from a stream that holds a file in a format whose handler was added, such as
	/// with <see cref="AddPngHandler"/>, and returns null if it could not. Reads the stream to its end.
	/// </summary>
	/// <param name="stream">The stream, such as a <see cref="MemoryStream"/> of a PNG file's bytes.</param>
	/// <param name="type">The format, or <see cref="BitmapType.Any"/> to find it from the data.</param>
	/// <param name="index">For a format with several images in a file, such as ICO, the one to load, or -1 for the default.</param>
	public static Image? FromStream(Stream stream, BitmapType type = BitmapType.Any, int index = -1) {
		ArgumentNullException.ThrowIfNull(stream);
		Application.VerifyAccess();
		Image image = Empty();
		if (image.Load(stream, type, index)) return image;
		image.Dispose();
		return null;
	}

	// An image with nothing in it, which is not usable until something is loaded into it.
	internal static Image Empty() => new(WxKitNative.ImageNewDefault());

	private static Image? Loaded(Image image) {
		if (image.IsOk) return image;
		image.Dispose();
		return null;
	}

	private bool Load(Stream stream, BitmapType type, int index) {
		byte[] data;
		int length;
		if (stream is MemoryStream memory && memory.TryGetBuffer(out ArraySegment<byte> buffer)) {
			// A memory stream's own buffer, from where it is to its end, without a copy. A stream can be
			// positioned past its end, which leaves nothing to read.
			data = buffer.Array!;
			int position = (int)Math.Min(memory.Position, buffer.Count);
			int start = buffer.Offset + position;
			length = buffer.Count - position;
			memory.Position = memory.Length;
			fixed (byte* bytes = data) return WxKitNative.ImageLoadData(Native, bytes + start, (nuint)length, (int)type, index) != 0;
		}
		using MemoryStream copy = new();
		stream.CopyTo(copy);
		data = copy.GetBuffer();
		length = (int)copy.Length;
		fixed (byte* bytes = data) return WxKitNative.ImageLoadData(Native, bytes, (nuint)length, (int)type, index) != 0;
	}

	/// <summary>
	/// Saves the image to a stream, as a file in a format whose handler was added, and returns
	/// whether it could. BMP needs no handler.
	/// </summary>
	public bool SaveFile(Stream stream, BitmapType type) {
		ArgumentNullException.ThrowIfNull(stream);
		Application.VerifyAccess();
		nuint size;
		byte* data = WxKitNative.ImageSaveData(Native, (int)type, &size);
		if (data is null) return false;
		try {
			stream.Write(new ReadOnlySpan<byte>(data, checked((int)size)));
		} finally {
			WxKitNative.BytesFree(data);
		}
		return true;
	}

	/// <summary>
	/// Saves the image to a file in a format whose handler was added, such as with
	/// <see cref="AddPngHandler"/>, and returns whether it could. BMP needs no handler. Without the
	/// handler, the file isn't created, or changed when it is there.
	/// </summary>
	public bool SaveFile(string fileName, BitmapType type) {
		ArgumentNullException.ThrowIfNull(fileName);
		WxkObject* native = Native;
		if (WxKitNative.ImageCanSave((int)type, null, 0) == 0) return false;
		using QuietLog quiet = QuietLog.Start();
		fixed (char* text = fileName) return WxKitNative.ImageSaveFileStringBitmapType(native, text, NativeString.Length(fileName), (int)type) != 0;
	}

	/// <summary>
	/// Saves the image to a file in a format given as a MIME type, such as image/png, whose handler
	/// was added, and returns whether it could. Without the handler, the file isn't created, or
	/// changed when it is there.
	/// </summary>
	public bool SaveFile(string fileName, string mimeType) {
		ArgumentNullException.ThrowIfNull(fileName);
		ArgumentNullException.ThrowIfNull(mimeType);
		WxkObject* native = Native;
		fixed (char* mime = mimeType) {
			if (WxKitNative.ImageCanSave(0, mime, NativeString.Length(mimeType)) == 0) return false;
		}
		using QuietLog quiet = QuietLog.Start();
		fixed (char* text = fileName, mime = mimeType) return WxKitNative.ImageSaveFileStringString(native, text, NativeString.Length(fileName), mime, NativeString.Length(mimeType)) != 0;
	}

	/// <summary>
	/// Saves the image to a file in the format its extension names, such as .png, whose handler was
	/// added, and returns whether it could. Without the handler, the file isn't created.
	/// </summary>
	public bool SaveFile(string fileName) {
		ArgumentNullException.ThrowIfNull(fileName);
		WxkObject* native = Native;
		using QuietLog quiet = QuietLog.Start();
		fixed (char* text = fileName) return WxKitNative.ImageSaveFileString(native, text, NativeString.Length(fileName)) != 0;
	}
}
