using WxKit.Interop;

namespace WxKit;

internal static unsafe class NativeString {
	// Takes ownership of the shim's string and frees it. Its text follows its length, so it is read
	// where it is, without a call.
	internal static string Take(WxkString* text) {
		if (text is null) return string.Empty;
		try {
			return new string((char*)(text + 1), 0, checked((int)text->Length));
		} finally {
			WxKitNative.StringFree(text);
		}
	}

	// Hands the shim a copy of the text, which the shim frees, as a director's callback returns it.
	internal static WxkString* Create(string? text) {
		if (string.IsNullOrEmpty(text)) return null;
		fixed (char* data = text) return WxKitNative.StringNew(data, Length(text));
	}

	internal static nuint Length(string text) => (nuint)text.Length;
}
