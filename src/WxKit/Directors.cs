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

// What a generated class's director is made with: its userdata is a handle to this, which points
// at the managed object once its constructor runs. Until then, and once wxWidgets deletes the
// object, a callback finds nothing, and calls wxWidgets' own member.
internal sealed unsafe class DirectorTarget : IDirected {
	private GCHandle _handle;

	public object? Object { get; set; }

	public void* Userdata {
		get {
			if (!_handle.IsAllocated) _handle = GCHandle.Alloc(this);
			return (void*)GCHandle.ToIntPtr(_handle);
		}
	}

	// A director that couldn't be made never calls onFree, so the handle is freed here.
	public T* Created<T>(T* native) where T : unmanaged {
		if (native is null && _handle.IsAllocated) _handle.Free();
		return native;
	}

	public void OnNativeFreed() => Object = null;

	public static T? Find<T>(void* userdata) where T : class => userdata is not null && GCHandle.FromIntPtr((nint)userdata).Target is DirectorTarget { Object: T found } ? found : null;
}
