namespace WxKit;

// The selected text's start and length, as WinForms' text boxes give them, from wxTextEntry's
// GetSelection and SetSelection, which take the first position and the one after the last.

public partial class TextCtrl {
	/// <summary>Where the selected text starts, or the insertion point when nothing is selected. Setting it keeps the length.</summary>
	public long SelectionStart {
		get {
			GetSelection(out long from, out _);
			return from;
		}
		set => SetSelection(value, value + SelectionLength);
	}

	/// <summary>How many characters are selected. Setting it keeps the start.</summary>
	public long SelectionLength {
		get {
			GetSelection(out long from, out long to);
			return to - from;
		}
		set => SetSelection(SelectionStart, SelectionStart + value);
	}
}

public partial class ComboBox {
	/// <summary>Where the selected text starts in the text box, or the insertion point when nothing is selected. Setting it keeps the length. <see cref="Selection"/> is the item chosen.</summary>
	public long SelectionStart {
		get {
			GetSelection(out long from, out _);
			return from;
		}
		set => SetSelection(value, value + SelectionLength);
	}

	/// <summary>How many characters are selected in the text box. Setting it keeps the start.</summary>
	public long SelectionLength {
		get {
			GetSelection(out long from, out long to);
			return to - from;
		}
		set => SetSelection(SelectionStart, SelectionStart + value);
	}
}

public partial class SearchCtrl {
	/// <summary>Where the selected text starts, or the insertion point when nothing is selected. Setting it keeps the length.</summary>
	public long SelectionStart {
		get {
			GetSelection(out long from, out _);
			return from;
		}
		set => SetSelection(value, value + SelectionLength);
	}

	/// <summary>How many characters are selected. Setting it keeps the start.</summary>
	public long SelectionLength {
		get {
			GetSelection(out long from, out long to);
			return to - from;
		}
		set => SetSelection(SelectionStart, SelectionStart + value);
	}
}
