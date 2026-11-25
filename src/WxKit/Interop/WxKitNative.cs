using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WxKit.Interop;

// The imports themselves are generated: the shim's generated functions' in WxKitNative.g.cs, and
// its hand-written functions', from their declarations in wxkit.h, in WxKitNative.Header.g.cs.
// The shim's C interface, one to one, which the types in WxKit wrap safely. It is internal, so the
// shim can change without the API changing.
internal static unsafe partial class WxKitNative {
	public const string Library = "wxkitnative";
}
