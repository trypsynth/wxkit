using WxKit.Interop;

namespace WxKit;

public unsafe partial class DropFilesEvent {
	/// <summary>The paths of the files dropped on the window.</summary>
	public IReadOnlyList<string> Files => NativeStringArray.Take(WxKitNative.DropFilesEventGetFiles(Handle));
}
