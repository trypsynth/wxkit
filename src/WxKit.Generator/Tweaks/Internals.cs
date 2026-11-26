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
	}
}
