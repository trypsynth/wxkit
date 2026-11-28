using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WxKit.Interop;

namespace WxKit;

// Maps each native event handler WxKit wraps to its one managed object. The map holds the object
// strongly, so a window keeps its managed object alive, with any fields a derived class added, for
// exactly as long as wxWidgets keeps the window. The shim's tracker removes the entry when
// wxWidgets destroys the object. Only the GUI thread touches it.
internal static unsafe partial class ObjectRegistry {
	private static readonly Dictionary<nint, EvtHandler> Handlers = [];

	// Factories, in ObjectRegistry.g.cs, names the managed class to wrap each wxWidgets class in.
	// A native object's class is walked up until a class WxKit knows, and the result is cached by
	// class, since a class's ancestry never changes.
	private static readonly Dictionary<nint, Func<nint, EvtHandler>?> FactoriesByClass = [];

	internal static void Register(EvtHandler handler, WxkEvtHandler* native) {
		if (!Handlers.TryAdd((nint)native, handler)) throw new InvalidOperationException("WxKit already wraps this wxWidgets object.");
		if (WxKitNative.EvtHandlerTrack(native, &OnDestroyed, native) == 0) {
			Handlers.Remove((nint)native);
			throw new InvalidOperationException("wxWidgets could not track the object.");
		}
	}

	internal static EvtHandler? Find(void* native) => native is not null && Handlers.TryGetValue((nint)native, out EvtHandler? handler) ? handler : null;

	// The managed object for a native one, made if WxKit has not seen it before. Null when native
	// is null, or when it is not a class WxKit wraps.
	internal static T? Wrap<T>(void* native) where T : EvtHandler {
		if (native is null) return null;
		if (Find(native) is EvtHandler existing) return existing as T;
		Func<nint, EvtHandler>? factory = FactoryFor(WxKitNative.ObjectGetClassInfo((WxkObject*)native));
		return factory?.Invoke((nint)native) as T;
	}

	// wxWidgets has destroyed every object still here, or never will, so none of them is usable.
	internal static void Clear() {
		foreach (EvtHandler handler in Handlers.Values.ToArray()) handler.OnNativeDestroyed(raise: false);
		Handlers.Clear();
	}

	private static Func<nint, EvtHandler>? FactoryFor(WxkClassInfo* info) {
		if (FactoriesByClass.TryGetValue((nint)info, out Func<nint, EvtHandler>? cached)) return cached;
		Func<nint, EvtHandler>? factory = null;
		for (WxkClassInfo* current = info; current is not null && factory is null; current = WxKitNative.ClassInfoGetBaseClass(current)) {
			Factories.TryGetValue(NativeString.Take(WxKitNative.ClassInfoGetClassName(current)), out factory);
		}
		FactoriesByClass[(nint)info] = factory;
		return factory;
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void OnDestroyed(void* native) {
		if (!Handlers.Remove((nint)native, out EvtHandler? handler)) return;
		try {
			handler.OnNativeDestroyed();
		} catch (Exception error) {
			Application.ReportException(error);
		}
	}
}
