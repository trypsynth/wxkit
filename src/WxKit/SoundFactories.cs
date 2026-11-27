using WxKit.Interop;

namespace WxKit;

// A sound from a WAV file, or from one's bytes in a stream, as methods that can say they failed.
// wxWidgets' constructors give an object that is not usable when the file can't be read, and log
// why, which would show the user an error dialog.
public unsafe partial class Sound {
	/// <summary>
	/// Loads a sound from a WAV file, and returns null if it could not, such as when the file is
	/// missing. Windows only reads the file as it plays it, so there a file that isn't WAV is found
	/// out when <see cref="Play"/> returns false.
	/// </summary>
	/// <param name="fileName">The file's path.</param>
	public static Sound? FromFile(string fileName) {
		ArgumentNullException.ThrowIfNull(fileName);
		Application.VerifyAccess();
		fixed (char* characters = fileName) return Wrap(WxKitNative.SoundFromFile(characters, (nuint)fileName.Length));
	}

	/// <summary>
	/// Loads a sound from a stream that holds a WAV file, such as a resource, and returns null if it
	/// could not, such as when the stream is empty. Reads the stream to its end, and keeps a copy of
	/// what it read. Windows only reads the sound as it plays it, so there data that isn't WAV is
	/// found out when <see cref="Play"/> returns false.
	/// </summary>
	/// <param name="stream">The stream, such as one from <see cref="System.Reflection.Assembly.GetManifestResourceStream(string)"/>.</param>
	public static Sound? FromStream(Stream stream) {
		ArgumentNullException.ThrowIfNull(stream);
		Application.VerifyAccess();
		byte[] data;
		int start;
		int length;
		if (stream is MemoryStream memory && memory.TryGetBuffer(out ArraySegment<byte> buffer)) {
			// A memory stream's own buffer, from where it is to its end, without a copy. A stream can be
			// positioned past its end, which leaves nothing to read.
			data = buffer.Array!;
			int position = (int)Math.Min(memory.Position, buffer.Count);
			start = buffer.Offset + position;
			length = buffer.Count - position;
			memory.Position = memory.Length;
		} else {
			using MemoryStream copy = new();
			stream.CopyTo(copy);
			data = copy.GetBuffer();
			start = 0;
			length = (int)copy.Length;
		}
		if (length == 0) return null;
		fixed (byte* bytes = data) return Wrap(WxKitNative.SoundFromData(bytes + start, (nuint)length));
	}

	private static Sound? Wrap(WxkObject* native) => native is null ? null : new Sound(native);
}
