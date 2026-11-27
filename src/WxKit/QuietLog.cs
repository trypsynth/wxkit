using WxKit.Interop;

namespace WxKit;

// Keeps wxWidgets from logging while a method that gives null when it can't load a file loads one.
// wxWidgets logs why it couldn't, which shows the user an error dialog, on Linux and macOS when the
// event loop is next idle, or as the application exits, though the caller is told.
internal readonly ref struct QuietLog {
	private readonly bool _wasEnabled;

	private QuietLog(bool wasEnabled) => _wasEnabled = wasEnabled;

	public static QuietLog Start() => new(WxKitNative.LogEnable(0) != 0);

	public void Dispose() => WxKitNative.LogEnable(_wasEnabled ? (byte)1 : (byte)0);
}
