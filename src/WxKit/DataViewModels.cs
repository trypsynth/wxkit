using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WxKit.Interop;

namespace WxKit;

/// <summary>An item in a data view: the id its model gave it, or the invisible root when it is 0. Wraps wxDataViewItem.</summary>
public readonly record struct DataViewItem(nint Value) {
	/// <summary>The invisible root, whose children are the top-level items.</summary>
	public static DataViewItem Root => default;

	/// <summary>Whether this is an item, rather than the root or none.</summary>
	public bool IsOk => Value != 0;
}

/// <summary>
/// What a <see cref="DataViewCtrl"/> shows: its items, their values in each column, and, for a tree,
/// their hierarchy. Wraps wxDataViewModel. Derive from <see cref="DataViewTreeModel{T}"/>,
/// <see cref="DataViewTreeModel"/>, or <see cref="DataViewVirtualListModel"/>.
/// </summary>
/// <remarks>
/// <para>
/// A value is an object: null, a string for a text column, a bool for a toggle column, a number for
/// a progress column, a <see cref="DataViewIconText"/> for an icon and text column, or a
/// <see cref="Bitmap"/>. Numbers come back from wxWidgets as a long or a double.
/// </para>
/// <para>
/// A control keeps the model it shows alive. The model tells its controls what changed through its
/// notifying members, such as <see cref="NotifyItemChanged"/>, and they ask for the new values.
/// </para>
/// <para>An exception in a member the control calls goes to <see cref="Application.UnhandledException"/>, and the control gets nothing.</para>
/// </remarks>
public abstract unsafe class DataViewModel {
	private readonly ModelHandle _handle;

	private protected DataViewModel(bool isVirtualList, uint count) {
		Application.VerifyAccess();
		Directors.Install();
		if (isVirtualList) {
			DataViewVirtualListModel.SetCallbacks();
		} else {
			DataViewTreeModel.SetCallbacks();
		}
		// The shim reaches the model through a weak handle, since its controls keep it alive, and
		// Directors frees the handle when the native model goes.
		GCHandle self = GCHandle.Alloc(this, GCHandleType.Weak);
		void* userdata = (void*)GCHandle.ToIntPtr(self);
		WxkDataViewModel* native = isVirtualList ? WxKitNative.DataViewVirtualListModelNew(userdata, count) : WxKitNative.DataViewModelNew(userdata);
		if (native is null) {
			self.Free();
			throw new InvalidOperationException("The data view model could not be created.");
		}
		_handle = new ModelHandle(native);
	}

	internal WxkDataViewModel* Native {
		get {
			Application.VerifyAccess();
			return (WxkDataViewModel*)_handle.DangerousGetHandle();
		}
	}

	/// <summary>Tells the controls that an item's values changed.</summary>
	public bool NotifyItemChanged(DataViewItem item) => WxKitNative.DataViewModelItemChanged(Native, item.Value) != 0;

	/// <summary>Tells the controls that one value of an item changed.</summary>
	public bool NotifyValueChanged(DataViewItem item, int column) => WxKitNative.DataViewModelValueChanged(Native, item.Value, checked((uint)column)) != 0;

	/// <summary>Tells the controls that everything changed, so they ask for all the items again.</summary>
	public bool NotifyCleared() {
		OnClearing();
		return WxKitNative.DataViewModelCleared(Native) != 0;
	}

	// What a model that keeps its items does before the controls ask for them all again.
	private protected virtual void OnClearing() {
	}

	/// <summary>Tells the controls to sort their items again.</summary>
	public void Resort() => WxKitNative.DataViewModelResort(Native);

	internal static T? Of<T>(void* userdata) where T : DataViewModel => GCHandle.FromIntPtr((nint)userdata).Target as T;

	// The model's reference, released on the GUI thread when the model is collected. Its controls
	// hold references of their own.
	private sealed class ModelHandle : SafeHandle {
		public ModelHandle(WxkDataViewModel* native) : base(0, ownsHandle: true) => SetHandle((nint)native);

		public override bool IsInvalid => handle == 0;

		protected override bool ReleaseHandle() {
			nint native = handle;
			if (Application.IsGuiThread) {
				WxKitNative.DataViewModelDecRef((WxkDataViewModel*)native);
			} else {
				Application.TryPost(() => WxKitNative.DataViewModelDecRef((WxkDataViewModel*)native));
			}
			return true;
		}
	}
}

/// <summary>
/// A model whose items form a tree, or a list when no item is a container, and which names its
/// items with ids of its own. <see cref="DataViewTreeModel{T}"/> names them with objects instead.
/// </summary>
public abstract unsafe class DataViewTreeModel : DataViewModel {
	private static bool _callbacksSet;

	protected DataViewTreeModel() : base(isVirtualList: false, 0) {
	}

	/// <summary>Tells the controls that an item was added under a parent, or under <see cref="DataViewItem.Root"/>.</summary>
	public bool NotifyItemAdded(DataViewItem parent, DataViewItem item) => WxKitNative.DataViewModelItemAdded(Native, parent.Value, item.Value) != 0;

	/// <summary>Tells the controls that an item was deleted from under a parent.</summary>
	public bool NotifyItemDeleted(DataViewItem parent, DataViewItem item) {
		bool deleted = WxKitNative.DataViewModelItemDeleted(Native, parent.Value, item.Value) != 0;
		OnItemDeleted(parent, item);
		return deleted;
	}

	// What a model that keeps its items does once the controls know one is gone.
	private protected virtual void OnItemDeleted(DataViewItem parent, DataViewItem item) {
	}

	/// <summary>The item's value in a column.</summary>
	protected abstract object? GetValue(DataViewItem item, int column);

	/// <summary>Changes the item's value in a column, as the user edits it. Returns false when it cannot.</summary>
	protected virtual bool SetValue(object? value, DataViewItem item, int column) => false;

	/// <summary>The item's parent, or <see cref="DataViewItem.Root"/> for a top-level item.</summary>
	protected abstract DataViewItem GetParent(DataViewItem item);

	/// <summary>Whether the item can have children. The root always can.</summary>
	protected abstract bool IsContainer(DataViewItem item);

	/// <summary>The item's children, or the top-level items for <see cref="DataViewItem.Root"/>.</summary>
	protected abstract IEnumerable<DataViewItem> GetChildren(DataViewItem item);

	/// <summary>Whether the item's value in a column can be used, such as edited or toggled.</summary>
	protected virtual bool IsEnabled(DataViewItem item, int column) => true;

	/// <summary>Whether a container shows values in every column, rather than only in the first.</summary>
	protected virtual bool HasContainerColumns(DataViewItem item) => false;

	internal static void SetCallbacks() {
		if (_callbacksSet) return;
		_callbacksSet = true;
		WxKitNative.DataViewModelSetDirectors(&CallGetValue, &CallSetValue, &CallGetParent, &CallIsContainer, &CallGetChildren, &CallIsEnabled, &CallHasContainerColumns);
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void CallGetValue(WxkDataViewModel* self, void* userdata, WxkVariant* value, nint item, uint column) {
		try {
			if (Of<DataViewTreeModel>(userdata) is DataViewTreeModel model) NativeVariant.Set(value, model.GetValue(new DataViewItem(item), (int)column));
		} catch (Exception error) {
			Application.ReportException(error);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static byte CallSetValue(WxkDataViewModel* self, void* userdata, WxkVariant* value, nint item, uint column) {
		try {
			return Of<DataViewTreeModel>(userdata) is DataViewTreeModel model && model.SetValue(NativeVariant.Get(value), new DataViewItem(item), (int)column) ? (byte)1 : (byte)0;
		} catch (Exception error) {
			Application.ReportException(error);
			return 0;
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static nint CallGetParent(WxkDataViewModel* self, void* userdata, nint item) {
		try {
			return Of<DataViewTreeModel>(userdata) is DataViewTreeModel model ? model.GetParent(new DataViewItem(item)).Value : 0;
		} catch (Exception error) {
			Application.ReportException(error);
			return 0;
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static byte CallIsContainer(WxkDataViewModel* self, void* userdata, nint item) {
		try {
			return Of<DataViewTreeModel>(userdata) is DataViewTreeModel model && (item == 0 || model.IsContainer(new DataViewItem(item))) ? (byte)1 : (byte)0;
		} catch (Exception error) {
			Application.ReportException(error);
			return 0;
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static uint CallGetChildren(WxkDataViewModel* self, void* userdata, nint item, WxkDataViewItems* children) {
		// wxWidgets reads how many children were added, which are all of them, even if the model
		// stopped partway through with an exception.
		uint added = 0;
		try {
			if (Of<DataViewTreeModel>(userdata) is not DataViewTreeModel model) return 0;
			foreach (DataViewItem child in model.GetChildren(new DataViewItem(item))) {
				WxKitNative.DataViewItemsAdd(children, child.Value);
				added++;
			}
		} catch (Exception error) {
			Application.ReportException(error);
		}
		return added;
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static byte CallIsEnabled(WxkDataViewModel* self, void* userdata, nint item, uint column) {
		try {
			return Of<DataViewTreeModel>(userdata) is not DataViewTreeModel model || model.IsEnabled(new DataViewItem(item), (int)column) ? (byte)1 : (byte)0;
		} catch (Exception error) {
			Application.ReportException(error);
			return 1;
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static byte CallHasContainerColumns(WxkDataViewModel* self, void* userdata, nint item) {
		try {
			return Of<DataViewTreeModel>(userdata) is DataViewTreeModel model && model.HasContainerColumns(new DataViewItem(item)) ? (byte)1 : (byte)0;
		} catch (Exception error) {
			Application.ReportException(error);
			return 0;
		}
	}
}

/// <summary>
/// A tree model whose items are the application's own objects. It gives each object an id the first
/// time the control sees it, and keeps it until <see cref="NotifyItemDeleted(T, T)"/> or
/// <see cref="DataViewModel.NotifyCleared"/>. The root is null.
/// </summary>
/// <remarks>
/// Two objects are the same item when they are the same object, unless the model is made with a
/// comparer. With one that compares a key, such as an id, an object that changes by being copied,
/// as an F# record does, stays the item it was: give the copy to <see cref="NotifyItemChanged(T)"/>,
/// or return it from <see cref="GetChildren(T)"/>, and the model keeps the copy in place of the
/// object it had.
/// </remarks>
/// <typeparam name="T">The items' type.</typeparam>
public abstract class DataViewTreeModel<T> : DataViewTreeModel where T : class {
	private readonly Dictionary<T, nint> _ids;
	private readonly Dictionary<nint, T> _items = [];
	// The ids of the items under each item, and under 0 for the root, so deleting an item forgets
	// the items under it too, without looking through every item for them.
	private readonly Dictionary<nint, HashSet<nint>> _children = [];
	private nint _lastId;

	/// <summary>Makes a model that knows an object by the object it is.</summary>
	protected DataViewTreeModel() : this(null) {
	}

	/// <summary>Makes a model that knows an object by what a comparer compares, or by the object it is for null.</summary>
	protected DataViewTreeModel(IEqualityComparer<T>? comparer) => _ids = comparer is null ? new(ReferenceEqualityComparer.Instance) : new(comparer);

	/// <summary>
	/// The item for an object, or <see cref="DataViewItem.Root"/> for null. An object equal to one
	/// the model has, by its comparer, takes that one's place.
	/// </summary>
	public DataViewItem ItemOf(T? item) {
		if (item is null) return DataViewItem.Root;
		if (!_ids.TryGetValue(item, out nint id)) {
			id = ++_lastId;
			_ids[item] = id;
			_items[id] = item;
		} else if (!ReferenceEquals(_items[id], item)) {
			// The key is replaced too, so the object it was isn't kept alive.
			_ids.Remove(item);
			_ids[item] = id;
			_items[id] = item;
		}
		return new DataViewItem(id);
	}

	/// <summary>The object an item is, or null for the root or an item the model does not have.</summary>
	public T? ObjectOf(DataViewItem item) => item.IsOk && _items.TryGetValue(item.Value, out T? found) ? found : null;

	/// <summary>Tells the controls that an object was added under a parent, or under the root for null.</summary>
	public bool NotifyItemAdded(T? parent, T item) {
		ArgumentNullException.ThrowIfNull(item);
		DataViewItem parentItem = ItemOf(parent);
		DataViewItem added = ItemOf(item);
		Under(parentItem.Value).Add(added.Value);
		return NotifyItemAdded(parentItem, added);
	}

	/// <summary>Tells the controls that an object was deleted from under a parent, and forgets its id.</summary>
	public bool NotifyItemDeleted(T? parent, T item) {
		ArgumentNullException.ThrowIfNull(item);
		return NotifyItemDeleted(ItemOf(parent), ItemOf(item));
	}

	private protected override void OnItemDeleted(DataViewItem parent, DataViewItem item) {
		if (ObjectOf(item) is not T) return;
		if (_children.TryGetValue(parent.Value, out HashSet<nint>? siblings)) siblings.Remove(item.Value);
		Forget(item.Value);
	}

	/// <summary>Tells the controls that an object's values changed.</summary>
	public bool NotifyItemChanged(T item) => NotifyItemChanged(ItemOf(item));

	/// <summary>Tells the controls that one value of an object changed.</summary>
	public bool NotifyValueChanged(T item, int column) => NotifyValueChanged(ItemOf(item), column);

	private protected override void OnClearing() {
		_ids.Clear();
		_items.Clear();
		_children.Clear();
	}

	/// <summary>The object's value in a column.</summary>
	protected abstract object? GetValue(T item, int column);

	/// <summary>Changes the object's value in a column, as the user edits it. Returns false when it cannot.</summary>
	protected virtual bool SetValue(object? value, T item, int column) => false;

	/// <summary>The object's parent, or null for a top-level one.</summary>
	protected abstract T? GetParent(T item);

	/// <summary>Whether the object can have children. The root, null, always can.</summary>
	protected abstract bool IsContainer(T item);

	/// <summary>The object's children, or the top-level objects for null.</summary>
	protected abstract IEnumerable<T> GetChildren(T? item);

	/// <summary>Whether the object's value in a column can be used, such as edited or toggled.</summary>
	protected virtual bool IsEnabled(T item, int column) => true;

	/// <summary>Whether a container shows values in every column, rather than only in the first.</summary>
	protected virtual bool HasContainerColumns(T item) => false;

	protected sealed override object? GetValue(DataViewItem item, int column) => ObjectOf(item) is T found ? GetValue(found, column) : null;

	protected sealed override bool SetValue(object? value, DataViewItem item, int column) => ObjectOf(item) is T found && SetValue(value, found, column);

	protected sealed override DataViewItem GetParent(DataViewItem item) => ObjectOf(item) is T found ? ItemOf(GetParent(found)) : DataViewItem.Root;

	protected sealed override bool IsContainer(DataViewItem item) => ObjectOf(item) is T found && IsContainer(found);

	protected sealed override IEnumerable<DataViewItem> GetChildren(DataViewItem item) {
		T? parent = ObjectOf(item);
		if (item.IsOk && parent is null) return [];
		DataViewItem[] children = [.. GetChildren(parent).Select(ItemOf)];
		HashSet<nint> under = Under(item.Value);
		foreach (DataViewItem child in children) under.Add(child.Value);
		return children;
	}

	private HashSet<nint> Under(nint parent) {
		if (!_children.TryGetValue(parent, out HashSet<nint>? under)) _children[parent] = under = [];
		return under;
	}

	// Forgets an item's id and those of the items under it, which the control no longer has either.
	private void Forget(nint id) {
		Stack<nint> gone = new([id]);
		while (gone.TryPop(out nint each)) {
			if (_children.Remove(each, out HashSet<nint>? under)) {
				foreach (nint child in under) gone.Push(child);
			}
			if (_items.Remove(each, out T? item)) _ids.Remove(item);
		}
	}

	protected sealed override bool IsEnabled(DataViewItem item, int column) => ObjectOf(item) is not T found || IsEnabled(found, column);

	protected sealed override bool HasContainerColumns(DataViewItem item) => ObjectOf(item) is T found && HasContainerColumns(found);
}

/// <summary>
/// A list whose rows the model produces as the control shows them, so it can have millions. It
/// only keeps how many rows there are. Wraps wxDataViewVirtualListModel.
/// </summary>
public abstract unsafe class DataViewVirtualListModel : DataViewModel {
	private static bool _callbacksSet;

	/// <param name="count">How many rows there are.</param>
	protected DataViewVirtualListModel(int count) : base(isVirtualList: true, checked((uint)count)) {
	}

	/// <summary>How many rows there are.</summary>
	public int Count => (int)WxKitNative.DataViewVirtualListModelGetCount(Native);

	/// <summary>The item a row is.</summary>
	public DataViewItem GetItem(int row) => new(WxKitNative.DataViewVirtualListModelGetItem(Native, checked((uint)row)));

	/// <summary>The row an item is.</summary>
	public int GetRow(DataViewItem item) => (int)WxKitNative.DataViewVirtualListModelGetRow(Native, item.Value);

	/// <summary>Changes how many rows there are, and tells the controls that every row changed.</summary>
	public void Reset(int count) => WxKitNative.DataViewVirtualListModelReset(Native, checked((uint)count));

	/// <summary>Tells the controls that a row was added at the end.</summary>
	public void NotifyRowAppended() => WxKitNative.DataViewVirtualListModelRowAppended(Native);

	/// <summary>Tells the controls that a row was added at the start.</summary>
	public void NotifyRowPrepended() => WxKitNative.DataViewVirtualListModelRowPrepended(Native);

	/// <summary>Tells the controls that a row was added before another.</summary>
	public void NotifyRowInserted(int before) => WxKitNative.DataViewVirtualListModelRowInserted(Native, checked((uint)before));

	/// <summary>Tells the controls that a row was deleted.</summary>
	public void NotifyRowDeleted(int row) => WxKitNative.DataViewVirtualListModelRowDeleted(Native, checked((uint)row));

	/// <summary>Tells the controls that a row's values changed.</summary>
	public void NotifyRowChanged(int row) => WxKitNative.DataViewVirtualListModelRowChanged(Native, checked((uint)row));

	/// <summary>Tells the controls that one value of a row changed.</summary>
	public void NotifyRowValueChanged(int row, int column) => WxKitNative.DataViewVirtualListModelRowValueChanged(Native, checked((uint)row), checked((uint)column));

	/// <summary>The row's value in a column.</summary>
	protected abstract object? GetValue(int row, int column);

	/// <summary>Changes the row's value in a column, as the user edits it. Returns false when it cannot.</summary>
	protected virtual bool SetValue(object? value, int row, int column) => false;

	/// <summary>Whether the row's value in a column can be used, such as edited or toggled.</summary>
	protected virtual bool IsEnabled(int row, int column) => true;

	internal static void SetCallbacks() {
		if (_callbacksSet) return;
		_callbacksSet = true;
		WxKitNative.DataViewVirtualListModelSetDirectors(&CallGetValue, &CallSetValue, &CallIsEnabled);
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void CallGetValue(WxkDataViewModel* self, void* userdata, WxkVariant* value, uint row, uint column) {
		try {
			if (Of<DataViewVirtualListModel>(userdata) is DataViewVirtualListModel model) NativeVariant.Set(value, model.GetValue((int)row, (int)column));
		} catch (Exception error) {
			Application.ReportException(error);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static byte CallSetValue(WxkDataViewModel* self, void* userdata, WxkVariant* value, uint row, uint column) {
		try {
			return Of<DataViewVirtualListModel>(userdata) is DataViewVirtualListModel model && model.SetValue(NativeVariant.Get(value), (int)row, (int)column) ? (byte)1 : (byte)0;
		} catch (Exception error) {
			Application.ReportException(error);
			return 0;
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static byte CallIsEnabled(WxkDataViewModel* self, void* userdata, uint row, uint column) {
		try {
			return Of<DataViewVirtualListModel>(userdata) is not DataViewVirtualListModel model || model.IsEnabled((int)row, (int)column) ? (byte)1 : (byte)0;
		} catch (Exception error) {
			Application.ReportException(error);
			return 1;
		}
	}
}

// The model a data view shows, which the control keeps alive.
public unsafe partial class DataViewCtrl {
	private DataViewModel? _model;

	/// <summary>
	/// The model the control shows, or null for none. The control keeps it alive, and it can be shown
	/// in several controls. A <see cref="DataViewListCtrl"/> or <see cref="DataViewTreeCtrl"/> has a
	/// model of its own already.
	/// </summary>
	public DataViewModel? Model {
		get {
			_ = Native;
			return _model;
		}
		set {
			if (WxKitNative.DataViewCtrlAssociateModel(Native, value is null ? null : value.Native) == 0) throw new InvalidOperationException("The control did not take the model.");
			_model = value;
		}
	}
}
