using WxKit.Interop;

namespace WxKit;

// A column belongs to its data view, which deletes it when the column is deleted, the columns are
// cleared, or the control is destroyed. wxWidgets says nothing when it does, so the control keeps
// the one managed object of each of its columns, and marks it destroyed then.
public sealed unsafe partial class DataViewColumn {
	private WxkDataViewColumn* _native;

	internal DataViewColumn(WxkDataViewColumn* native) => _native = native;

	/// <summary>Whether wxWidgets has deleted the column, along with the data view or when the data view deleted it.</summary>
	public bool IsDisposed => _native is null;

	internal WxkDataViewColumn* Native {
		get {
			Application.VerifyAccess();
			return _native is not null ? _native : throw new ObjectDisposedException(nameof(DataViewColumn), "wxWidgets has deleted this column, along with the data view or when the data view deleted it.");
		}
	}

	// The managed object of a column wxWidgets hands back, which its control keeps.
	internal static DataViewColumn? Wrap(WxkDataViewColumn* native) {
		if (native is null) return null;
		DataViewCtrl owner = ObjectRegistry.Wrap<DataViewCtrl>(WxKitNative.DataViewColumnGetOwner(native)) ?? throw new InvalidOperationException("The column does not belong to a data view WxKit knows.");
		return owner.ColumnFor(native);
	}

	internal void MarkDestroyed() => _native = null;
}

public unsafe partial class DataViewCtrl {
	private Dictionary<nint, DataViewColumn>? _columns;

	/// <summary>Deletes the column, which is then destroyed. Returns false when the column is not this control's.</summary>
	public bool DeleteColumn(DataViewColumn column) {
		ArgumentNullException.ThrowIfNull(column);
		WxkDataViewColumn* native = column.Native;
		if (_columns is null || !_columns.TryGetValue((nint)native, out DataViewColumn? tracked) || !ReferenceEquals(tracked, column)) return false;
		if (WxKitNative.DataViewCtrlDeleteColumn(Native, native) == 0) return false;
		_columns.Remove((nint)native);
		column.MarkDestroyed();
		return true;
	}

	/// <summary>Deletes every column, which are then destroyed. Returns false when the control could not.</summary>
	public bool ClearColumns() {
		if (WxKitNative.DataViewCtrlClearColumns(Native) == 0) return false;
		MarkColumnsDestroyed();
		return true;
	}

	internal DataViewColumn ColumnFor(WxkDataViewColumn* native) {
		_columns ??= [];
		if (!_columns.TryGetValue((nint)native, out DataViewColumn? column)) _columns[(nint)native] = column = new DataViewColumn(native);
		return column;
	}

	private protected override void OnDestroyed() {
		MarkColumnsDestroyed();
		base.OnDestroyed();
	}

	private void MarkColumnsDestroyed() {
		if (_columns is null) return;
		foreach (DataViewColumn column in _columns.Values) column.MarkDestroyed();
		_columns = null;
	}
}
