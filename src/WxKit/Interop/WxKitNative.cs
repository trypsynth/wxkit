using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WxKit.Interop;

// The imports themselves are generated: the shim's generated functions' in WxKitNative.g.cs, and
// its hand-written functions', from their declarations in wxkit.h, in WxKitNative.Header.g.cs.
/// <summary>
/// The shim's C interface, one to one. The types in <see cref="WxKit"/> wrap it safely, and calling
/// it directly gives up their lifetime and threading checks. It can change between preview releases.
/// </summary>
public static unsafe partial class WxKitNative {
	public const string Library = "wxkitnative";
}
