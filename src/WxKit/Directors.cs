using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WxKit.Interop;

namespace WxKit;

// What a director's userdata holds a GC handle to, which the director tells when it is deleted.
internal interface IDirected {
	void OnNativeFreed();
}

// The one onFree every director calls with its userdata when wxWidgets deletes it.
internal static unsafe class Directors {
	private static bool _installed;

	internal static void Install() {
		if (_installed) return;
		_installed = true;
		WxKitNative.DirectorsSetOnFree(&OnFree);
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void OnFree(void* userdata) {
		try {
			GCHandle handle = GCHandle.FromIntPtr((nint)userdata);
			(handle.Target as IDirected)?.OnNativeFreed();
			handle.Free();
		} catch (Exception error) {
			Application.ReportException(error);
		}
	}
}
