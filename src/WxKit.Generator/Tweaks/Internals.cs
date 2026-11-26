namespace WxKit.Generator.Tweaks;

// Members wxWidgets has for itself, or for a class derived in C++ to override, which an application
// has no use for calling, and some of which leave a window in a state it can't work in.
internal static class Internals {
	public static void Apply(Binding.Tweaks tweaks) {
		// A window's children, sizer, and chain of handlers are wxWidgets' to keep.
		tweaks.Class("wxWindow").Ignore(
			"AddChild", "RemoveChild", "SetContainingSizer", "PushEventHandler", "PopEventHandler", "RemoveEventHandler", "SetNextHandler", "SetPreviousHandler",
			"OnInternalIdle", "InformFirstDirection", "CacheBestSize", "AcceptsFocus", "AcceptsFocusFromKeyboard", "AcceptsFocusRecursively");
		// What a sizer written in C++ overrides to lay out its items.
		foreach (string sizer in (string[])["wxBoxSizer", "wxGridSizer", "wxFlexGridSizer", "wxStaticBoxSizer", "wxStdDialogButtonSizer", "wxWrapSizer", "wxGridBagSizer"]) tweaks.Class(sizer).Ignore("CalcMin", "RepositionChildren");
		tweaks.Class("wxBoxSizer").Ignore("InformFirstDirection");
		tweaks.Class("wxWrapSizer").Ignore("InformFirstDirection");
		tweaks.Class("wxPanel").Ignore("AcceptsFocus");
		tweaks.Class("wxSplitterWindow").Ignore("OnSashPositionChange", "OnDoubleClickSash", "OnUnsplit");
		tweaks.Class("wxImage").Ignore("CleanUpHandlers", "InitStandardHandlers");
		tweaks.Class("wxBitmap").Ignore("CleanUpHandlers", "InitStandardHandlers");
		// A printout's pages are begun and ended by the printing it is given to.
		tweaks.Class("wxDC").Ignore("StartDoc", "EndDoc", "StartPage", "EndPage");
		// A frame attaches the menu bar it is given.
		tweaks.Class("wxMenuBar").Ignore("Attach", "Detach");
		tweaks.Class("wxEvent").Ignore("GetEventUserData", "GetEventCategory", "IsCommandEvent");
		tweaks.Class("wxDialog").Ignore(
			"CanDoLayoutAdaptation", "DoLayoutAdaptation", "EnableLayoutAdaptation", "IsLayoutAdaptationEnabled", "GetLayoutAdaptationDone", "SetLayoutAdaptationDone",
			"GetLayoutAdaptationLevel", "SetLayoutAdaptationLevel", "GetLayoutAdaptationMode", "SetLayoutAdaptationMode");
		// What the styled text control's drop target calls.
		tweaks.Class("wxStyledTextCtrl").Ignore("DoDragEnter", "DoDragOver", "DoDragLeave", "DoDropText");
		tweaks.Class("wxScrolledWindow").Ignore("PrepareDC");
		Repeats(tweaks);
	}

	// Members that only repeat another, which is the one to use.
	private static void Repeats(Binding.Tweaks tweaks) {
		// Modifiers says which keys are down, in one value. RawControlDown stays, since on a Mac it is
		// the Control key, where Modifiers' Control is Command.
		foreach (string held in (string[])["wxKeyEvent", "wxMouseEvent"]) tweaks.Class(held).Ignore("AltDown", "ControlDown", "MetaDown", "ShiftDown", "CmdDown");
		// A window converts for itself, so the static forms that take the window say it twice.
		foreach (string convert in (string[])["FromDIP", "ToDIP", "FromPhys", "ToPhys"]) tweaks.Class("wxWindow").IgnoreOverload(convert, "w", "const wxWindow *");
		// A setter a class repeats from its base, where it is a property.
		tweaks.Class("wxDialog").Ignore("SetIcon");
		tweaks.Class("wxStaticText").Ignore("SetLabel");
		tweaks.Class("wxTopLevelWindow").Ignore("SetMinSize", "SetMaxSize");
		tweaks.Class("wxInfoBar").Ignore("SetFont");
		tweaks.Class("wxListCtrl").Ignore("SetBackgroundColour");
		// A color given as three bytes, or by name, is a Color.
		tweaks.Class("wxBrush").IgnoreOverload("SetColour", "red", "unsigned char");
		tweaks.Class("wxPen").IgnoreOverload("SetColour", "red", "unsigned char");
		tweaks.Class("wxColourPickerCtrl").IgnoreOverload("SetColour", "colname", "const wxString &");
		// SelectionStart and SelectionLength, or the styled text control's SelectionEnd, give the
		// selection without out parameters. The hand-written properties call the import.
		foreach (string entry in (string[])["wxTextCtrl", "wxComboBox", "wxSearchCtrl", "wxStyledTextCtrl"]) tweaks.Class(entry).InternalOverload("GetSelection", "from", "long *");
	}
}
