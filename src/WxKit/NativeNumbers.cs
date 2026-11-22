using WxKit.Interop;

namespace WxKit;

// Lists of ints or item ids, such as a list box's selections, which cross as 64-bit numbers.
internal static unsafe class NativeNumbers {
	internal static int[] TakeInts(WxkNumbers* numbers) {
		long[] values = Take(numbers);
		int[] result = new int[values.Length];
		for (int index = 0; index < values.Length; index++) result[index] = checked((int)values[index]);
		return result;
	}

	internal static TreeItemId[] TakeTreeItems(WxkNumbers* numbers) {
		long[] values = Take(numbers);
		TreeItemId[] result = new TreeItemId[values.Length];
		for (int index = 0; index < values.Length; index++) result[index] = new TreeItemId((nint)values[index]);
		return result;
	}

	internal static DataViewItem[] TakeDataViewItems(WxkNumbers* numbers) {
		long[] values = Take(numbers);
		DataViewItem[] result = new DataViewItem[values.Length];
		for (int index = 0; index < values.Length; index++) result[index] = new DataViewItem((nint)values[index]);
		return result;
	}

	internal static TreeListItem[] TakeTreeListItems(WxkNumbers* numbers) {
		long[] values = Take(numbers);
		TreeListItem[] result = new TreeListItem[values.Length];
		for (int index = 0; index < values.Length; index++) result[index] = new TreeListItem((nint)values[index]);
		return result;
	}

	internal static long[] From(IEnumerable<int> values) => [.. values.Select(static value => (long)value)];

	internal static long[] From(IEnumerable<DataViewItem> items) => [.. items.Select(static item => (long)item.Value)];

	// Takes ownership of the shim's list and frees it. Null is an empty list.
	private static long[] Take(WxkNumbers* numbers) {
		if (numbers is null) return [];
		try {
			long[] values = new long[checked((int)WxKitNative.NumbersGetCount(numbers))];
			fixed (long* data = values) WxKitNative.NumbersCopyTo(numbers, data);
			return values;
		} finally {
			WxKitNative.NumbersFree(numbers);
		}
	}
}
