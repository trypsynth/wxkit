using WxKit.Interop;

namespace WxKit;

// The selected text's start and length, as WinForms' text boxes give them, from wxTextEntry's
// GetSelection and SetSelection, which take the first position and the one after the last.

public unsafe partial class TextCtrl {
	private void GetSelection(out int from, out int to) {
		long first = 0;
		long end = 0;
		WxKitNative.TextCtrlGetSelection(Native, &first, &end);
		from = checked((int)first);
		to = checked((int)end);
	}

	/// <summary>Where the selected text starts, or the insertion point when nothing is selected. Setting it keeps the length.</summary>
	public int SelectionStart {
		get {
			GetSelection(out int from, out _);
			return from;
		}
		set => SetSelection(value, value + SelectionLength);
	}

	/// <summary>How many characters are selected. Setting it keeps the start.</summary>
	public int SelectionLength {
		get {
			GetSelection(out int from, out int to);
			return to - from;
		}
		set => SetSelection(SelectionStart, SelectionStart + value);
	}
}

public unsafe partial class ComboBox {
	private void GetSelection(out int from, out int to) {
		long first = 0;
		long end = 0;
		WxKitNative.ComboBoxGetSelectionOutLongOutLong(Native, &first, &end);
		from = checked((int)first);
		to = checked((int)end);
	}

	/// <summary>Where the selected text starts in the text box, or the insertion point when nothing is selected. Setting it keeps the length. <see cref="Selection"/> is the item chosen.</summary>
	public int SelectionStart {
		get {
			GetSelection(out int from, out _);
			return from;
		}
		set => SetSelection(value, value + SelectionLength);
	}

	/// <summary>How many characters are selected in the text box. Setting it keeps the start.</summary>
	public int SelectionLength {
		get {
			GetSelection(out int from, out int to);
			return to - from;
		}
		set => SetSelection(SelectionStart, SelectionStart + value);
	}
}

public unsafe partial class SearchCtrl {
	private void GetSelection(out int from, out int to) {
		long first = 0;
		long end = 0;
		WxKitNative.SearchCtrlGetSelection(Native, &first, &end);
		from = checked((int)first);
		to = checked((int)end);
	}

	/// <summary>Where the selected text starts, or the insertion point when nothing is selected. Setting it keeps the length.</summary>
	public int SelectionStart {
		get {
			GetSelection(out int from, out _);
			return from;
		}
		set => SetSelection(value, value + SelectionLength);
	}

	/// <summary>How many characters are selected. Setting it keeps the start.</summary>
	public int SelectionLength {
		get {
			GetSelection(out int from, out int to);
			return to - from;
		}
		set => SetSelection(SelectionStart, SelectionStart + value);
	}
}
