using WxKit.Interop;

namespace WxKit;

/// <summary>
/// A part of a window that draws itself, which screen readers read as text, such as a field of a
/// status bar. <paramref name="Bounds"/> is where it is, in the window's client coordinates.
/// </summary>
public readonly record struct AccessiblePart(string Label, Rect Bounds);

/// <summary>
/// A row of a window that draws itself as a list or a table. <paramref name="Index"/> is the row's
/// position among all of the rows, not only the shown ones. <paramref name="Cells"/> has one part for
/// each column, or one for a list, and a screen reader reads them together as the row's name.
/// </summary>
public sealed record AccessibleRow(int Index, Rect Bounds, IReadOnlyList<AccessiblePart> Cells, bool IsSelected = false);

public unsafe partial class Window {
	/// <summary>
	/// Lets screen readers see the parts of a window that draws itself, each read as text, as
	/// wxWidgets does a status bar's fields. Call it again whenever the parts change: it replaces the
	/// parts set before, and an empty sequence removes them.
	/// </summary>
	/// <remarks>
	/// On Windows, the window's accessible object answers from the parts, through Microsoft Active
	/// Accessibility, so the window cannot have an <see cref="Accessible"/> of its own as well. On
	/// macOS and Linux, wxWidgets gives them to NSAccessibility and ATK. Name the window itself with
	/// <see cref="SetAccessibleName"/>, such as with the label before it, which Windows also falls
	/// back to.
	/// </remarks>
	public void SetAccessibleParts(IEnumerable<AccessiblePart> parts) {
		ArgumentNullException.ThrowIfNull(parts);
		AccessiblePart[] given = [.. parts];
		foreach (AccessiblePart part in given) ArgumentNullException.ThrowIfNull(part.Label, nameof(parts));
		PartsAccessible? windows = DescribedByParts();
		using NativeStringArray labels = new(given.Select(static part => part.Label));
		Rect[] rects = [.. given.Select(static part => part.Bounds)];
		fixed (Rect* bounds = rects) WxKitNative.WindowSetAccessibleParts(Native, labels.Handle, bounds);
		windows?.SetParts(given);
	}

	/// <summary>
	/// Lets screen readers see a window that draws itself as a list or a table, as wxWidgets does its
	/// own list control. Give only the rows that are shown, since there may be too many to describe,
	/// and the number of rows in all. Call it again whenever the rows, or which are shown, change.
	/// </summary>
	/// <remarks>
	/// On Windows, the window's accessible object answers from the rows, so the window cannot have
	/// an <see cref="Accessible"/> of its own as well.
	/// </remarks>
	public void SetAccessibleRows(IEnumerable<AccessibleRow> rows, int rowCount) {
		ArgumentNullException.ThrowIfNull(rows);
		ArgumentOutOfRangeException.ThrowIfNegative(rowCount);
		AccessibleRow[] given = [.. rows];
		PartsAccessible? windows = DescribedByParts();
		WxkAccessibleRow[] native = new WxkAccessibleRow[given.Length];
		List<AccessiblePart> cells = [];
		for (int index = 0; index < given.Length; index++) {
			AccessibleRow row = given[index] ?? throw new ArgumentNullException(nameof(rows), "The rows cannot hold null.");
			ArgumentNullException.ThrowIfNull(row.Cells, nameof(rows));
			native[index] = new WxkAccessibleRow { Index = row.Index, Bounds = row.Bounds, Selected = row.IsSelected ? (byte)1 : (byte)0, FirstCell = cells.Count, CellCount = row.Cells.Count };
			cells.AddRange(row.Cells);
		}
		using NativeStringArray labels = new(cells.Select(static cell => cell.Label ?? throw new ArgumentNullException(nameof(rows), "A cell's label cannot be null.")));
		Rect[] rects = [.. cells.Select(static cell => cell.Bounds)];
		fixed (WxkAccessibleRow* rowsPointer = native)
		fixed (Rect* bounds = rects) {
			WxKitNative.WindowSetAccessibleRows(Native, rowsPointer, (nuint)native.Length, labels.Handle, bounds, rowCount);
		}
		windows?.SetRows(given);
	}

	/// <summary>
	/// Tells screen readers which row is the current one, by its <see cref="AccessibleRow.Index"/>, or
	/// that none is, with -1. A screen reader announces the row when it changes, which it has no other
	/// way of knowing inside a window that draws itself. The row must be one of the shown ones.
	/// </summary>
	public void SetAccessibleCurrentRow(int index) {
		ArgumentOutOfRangeException.ThrowIfLessThan(index, -1);
		PartsAccessible? windows = DescribedByParts();
		WxKitNative.WindowSetAccessibleCurrentRow(Native, index);
		windows?.SetCurrentRow(index);
	}

	// On Windows, the accessible object that answers from the parts or rows, which is made the first
	// time. Elsewhere wxWidgets answers itself.
	private PartsAccessible? DescribedByParts() {
		_ = Native;
		if (AccessibleObject is PartsAccessible parts) return parts;
		if (AccessibleObject is not null) throw new InvalidOperationException("The window is described by an Accessible of its own, which its parts or rows would replace.");
		return OperatingSystem.IsWindows() ? new PartsAccessible(this) : null;
	}
}

// On Windows, where wxWidgets's parts and rows do nothing, an accessible object answers Microsoft
// Active Accessibility from them, and tells screen readers what changed, as macOS and GTK do.
// Parts are static text, and rows list items, whose names are their cells' labels together.
internal sealed class PartsAccessible(Window window) : Accessible(window) {
	private AccessiblePart[] _parts = [];
	private AccessibleRow[] _rows = [];
	private bool _isTable;
	private int _currentRow = -1;

	public void SetParts(AccessiblePart[] parts) {
		AccessiblePart[] old = _parts;
		bool reordered = _isTable || old.Length != parts.Length;
		_parts = parts;
		_rows = [];
		_isTable = false;
		if (reordered) {
			Notify(AccessibleEvent.ObjectReorder, 0);
			return;
		}
		for (int index = 0; index < parts.Length; index++) {
			if (old[index].Label != parts[index].Label) Notify(AccessibleEvent.ObjectNameChange, index + 1);
		}
	}

	public void SetRows(AccessibleRow[] rows) {
		AccessibleRow[] old = _rows;
		bool reordered = !_isTable || old.Length != rows.Length || rows.Where((row, index) => row.Index != old[index].Index).Any();
		_rows = rows;
		_parts = [];
		_isTable = true;
		if (reordered) {
			Notify(AccessibleEvent.ObjectReorder, 0);
			return;
		}
		for (int index = 0; index < rows.Length; index++) {
			if (Name(old[index]) != Name(rows[index])) Notify(AccessibleEvent.ObjectNameChange, index + 1);
			if (old[index].IsSelected != rows[index].IsSelected) Notify(AccessibleEvent.ObjectStateChange, index + 1);
		}
	}

	public void SetCurrentRow(int index) {
		if (index == _currentRow) return;
		_currentRow = index;
		int position = Array.FindIndex(_rows, row => row.Index == index);
		if (position < 0 || Window.FindFocus() != Window) return;
		Notify(AccessibleEvent.ObjectFocus, position + 1);
		if (_rows[position].IsSelected) Notify(AccessibleEvent.ObjectSelection, position + 1);
	}

	protected override int? GetChildCount() => _isTable ? _rows.Length : _parts.Length;

	protected override AccessibleElement? GetChild(int childId) {
		_ = Bounds(childId);
		return AccessibleElement.Child(childId);
	}

	// The window itself is named by the label just before it, as a standard control is, unless
	// SetAccessibleName named it, which wxWidgets answers before asking.
	protected override string? GetName(int childId) => childId == 0 ? (Window.PreviousSibling as StaticText)?.LabelText : _isTable ? Name(Row(childId)) : Part(childId).Label;

	protected override AccessibleRole? GetRole(int childId) => childId == 0 ? (_isTable ? AccessibleRole.List : null) : _isTable ? AccessibleRole.ListItem : AccessibleRole.StaticText;

	protected override AccessibleStates? GetState(int childId) {
		if (childId == 0) return null;
		if (!_isTable) return AccessibleStates.ReadOnly | StateOf(Part(childId).Bounds);
		AccessibleRow row = Row(childId);
		AccessibleStates state = AccessibleStates.Selectable | AccessibleStates.Focusable | StateOf(row.Bounds);
		if (row.IsSelected) state |= AccessibleStates.Selected;
		if (row.Index == _currentRow && Window.FindFocus() == Window) state |= AccessibleStates.Focused;
		return state;
	}

	protected override Rect? GetLocation(int elementId) {
		if (elementId == 0) return null;
		Rect bounds = Bounds(elementId);
		Point screen = Window.ClientToScreen(bounds.Position);
		return new Rect(screen.X, screen.Y, bounds.Width, bounds.Height);
	}

	protected override AccessibleElement? GetFocus() {
		if (!_isTable || Window.FindFocus() != Window) return null;
		int position = Array.FindIndex(_rows, row => row.Index == _currentRow);
		return position < 0 ? AccessibleElement.Self : AccessibleElement.Child(position + 1);
	}

	protected override AccessibleElement? HitTest(Point point) {
		Point client = Window.ScreenToClient(point);
		int count = _isTable ? _rows.Length : _parts.Length;
		for (int index = 0; index < count; index++) {
			Rect bounds = Bounds(index + 1);
			if (client.X >= bounds.X && client.Y >= bounds.Y && client.X < bounds.X + bounds.Width && client.Y < bounds.Y + bounds.Height) return AccessibleElement.Child(index + 1);
		}
		return null;
	}

	// A part outside the window, scrolled away, is offscreen.
	private AccessibleStates StateOf(Rect bounds) {
		Size client = Window.ClientSize;
		bool shown = bounds.X < client.Width && bounds.Y < client.Height && bounds.X + bounds.Width > 0 && bounds.Y + bounds.Height > 0;
		return shown ? AccessibleStates.None : AccessibleStates.Offscreen | AccessibleStates.Invisible;
	}

	private Rect Bounds(int childId) => _isTable ? Row(childId).Bounds : Part(childId).Bounds;

	private AccessiblePart Part(int childId) => childId >= 1 && childId <= _parts.Length ? _parts[childId - 1] : throw new ArgumentOutOfRangeException(nameof(childId));

	private AccessibleRow Row(int childId) => childId >= 1 && childId <= _rows.Length ? _rows[childId - 1] : throw new ArgumentOutOfRangeException(nameof(childId));

	private static string Name(AccessibleRow row) => string.Join(", ", row.Cells.Select(static cell => cell.Label).Where(static label => label.Length > 0));

	private void Notify(AccessibleEvent eventType, int childId) => NotifyEvent(eventType, Window, AccessibleObject.Client, childId);
}
