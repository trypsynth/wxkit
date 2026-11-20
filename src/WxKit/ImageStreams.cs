using WxKit.Interop;

namespace WxKit;

// wxImage's LoadFile and SaveFile overloads that take a wxInputStream and a wxOutputStream, over
// .NET's streams, through memory, such as for an image on the clipboard, in a resource, or from
// another library.
public unsafe partial class Image {
	/// <summary>
	/// Loads the image from a stream that holds a file in a format whose handler was added, such as
	/// with <see cref="AddPngHandler"/>, and returns whether it could. Reads the stream to its end.
	/// </summary>
	/// <param name="stream">The stream, such as a <see cref="MemoryStream"/> of a PNG file's bytes.</param>
	/// <param name="type">The format, or <see cref="BitmapType.Any"/> to find it from the data.</param>
	/// <param name="index">For a format with several images in a file, such as ICO, the one to load, or -1 for the default.</param>
	public bool LoadFile(Stream stream, BitmapType type = BitmapType.Any, int index = -1) {
		ArgumentNullException.ThrowIfNull(stream);
		Application.VerifyAccess();
		byte[] data;
		int length;
		if (stream is MemoryStream memory && memory.TryGetBuffer(out ArraySegment<byte> buffer)) {
			// A memory stream's own buffer, from where it is to its end, without a copy.
			data = buffer.Array!;
			int start = buffer.Offset + (int)memory.Position;
			length = buffer.Count - (int)memory.Position;
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
}
