using WxKit.Interop;

namespace WxKit;

public unsafe partial class StatusBar {
	/// <summary>Sets how each field looks, in order, such as flat or raised. Give one for every field.</summary>
	public void SetStatusStyles(IEnumerable<StatusBarFieldStyle> styles) {
		ArgumentNullException.ThrowIfNull(styles);
		int[] values = [.. styles.Select(static style => (int)style)];
		fixed (int* pointer = values) WxKitNative.StatusBarSetStatusStyles(Native, pointer, (nuint)values.Length);
	}
}
