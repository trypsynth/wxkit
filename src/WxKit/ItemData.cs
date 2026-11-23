using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WxKit.Interop;

namespace WxKit;

// Objects a control keeps with its items, such as a list box's or a tree's. Each crosses as a
// GCHandle, which the shim frees when the control deletes the item, so the object lives as long as
// the item does.
internal static unsafe class ItemData {
	private static bool _freeSet;

	public static nint Alloc(object? value) {
		if (value is null) return 0;
		if (!_freeSet) {
			_freeSet = true;
			WxKitNative.ClientDataSetFree(&Free);
		}
		return GCHandle.ToIntPtr(GCHandle.Alloc(value));
	}

	public static object? Get(nint handle) => handle == 0 ? null : GCHandle.FromIntPtr(handle).Target;

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void Free(void* handle) => GCHandle.FromIntPtr((nint)handle).Free();
}

public unsafe partial class TreeCtrl {
	/// <summary>
	/// Keeps an object with the item, such as what it stands for, or none when it is null. The tree
	/// keeps it until the item is deleted or given another.
	/// </summary>
	public void SetItemData(TreeItemId item, object? data) => WxKitNative.TreeCtrlSetItemData(Native, item.Value, (void*)ItemData.Alloc(data));
}
