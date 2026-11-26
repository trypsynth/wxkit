using WxKit.Interop;

namespace WxKit;

// A wxArrayString built from managed strings for the length of one call, and freed after it.
internal readonly unsafe ref struct NativeStringArray {
	// Null is no strings, for a list a caller can leave out. One that can't is checked before this.
	public NativeStringArray(IEnumerable<string>? items) {
		Handle = WxKitNative.StringArrayNew();
		try {
			foreach (string item in items ?? []) {
				ArgumentNullException.ThrowIfNull(item, nameof(items));
				fixed (char* text = item) WxKitNative.StringArrayAdd(Handle, text, NativeString.Length(item));
			}
		} catch {
			WxKitNative.StringArrayFree(Handle);
			throw;
		}
	}

	public WxkStringArray* Handle { get; }

	public void Dispose() => WxKitNative.StringArrayFree(Handle);

	// Takes ownership of an array the shim hands out and frees it.
	public static string[] Take(WxkStringArray* array) {
		if (array is null) return [];
		try {
			string[] items = new string[checked((int)WxKitNative.StringArrayGetCount(array))];
			for (int index = 0; index < items.Length; index++) items[index] = NativeString.Take(WxKitNative.StringArrayItem(array, (nuint)index));
			return items;
		} finally {
			WxKitNative.StringArrayFree(array);
		}
	}
}
