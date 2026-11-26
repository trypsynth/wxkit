namespace WxKit.Generator.Tweaks;

// The styled text control and the web view, each in a wxWidgets library of its own, linked only by
// an application that uses it.
internal static class Libraries {
	public static void Apply(Binding.Tweaks tweaks) {
		// The styled text control, Scintilla, which is wxWidgets's stc library. The interface headers
		// give it wxTextEntry's members, which it has through wxTextEntryBase, and document its events
		// on wxStyledTextEvent. Its constants are enums, in StyledText.cs, but for each lexer's
		// styles and a few numbers, which are StyledText's ints, as Scintilla takes them.
		StyledText.Apply(tweaks);
		tweaks.ConstantClassOf("StyledText", "wxSTC_");
		tweaks.Class("wxStyledTextCtrl", stc => stc
			.MixinBase("wxTextEntry", "wxTextEntryBase")
			.AddEvent("EVT_STC_AUTOCOMP_CANCELLED", "wxEVT_STC_AUTOCOMP_CANCELLED", "wxStyledTextEvent", "AutoCompleteCanceled")
			.AddEvent("EVT_STC_AUTOCOMP_CHAR_DELETED", "wxEVT_STC_AUTOCOMP_CHAR_DELETED", "wxStyledTextEvent", "AutoCompleteCharDeleted")
			.AddEvent("EVT_STC_AUTOCOMP_COMPLETED", "wxEVT_STC_AUTOCOMP_COMPLETED", "wxStyledTextEvent", "AutoCompleteCompleted")
			.AddEvent("EVT_STC_AUTOCOMP_SELECTION", "wxEVT_STC_AUTOCOMP_SELECTION", "wxStyledTextEvent", "AutoCompleteSelected")
			.AddEvent("EVT_STC_AUTOCOMP_SELECTION_CHANGE", "wxEVT_STC_AUTOCOMP_SELECTION_CHANGE", "wxStyledTextEvent", "AutoCompleteSelectionChanged")
			.AddEvent("EVT_STC_CALLTIP_CLICK", "wxEVT_STC_CALLTIP_CLICK", "wxStyledTextEvent", "CallTipClick")
			.AddEvent("EVT_STC_CHANGE", "wxEVT_STC_CHANGE", "wxStyledTextEvent", "Changed")
			.AddEvent("EVT_STC_CHARADDED", "wxEVT_STC_CHARADDED", "wxStyledTextEvent", "CharAdded")
			.AddEvent("EVT_STC_CLIPBOARD_COPY", "wxEVT_STC_CLIPBOARD_COPY", "wxStyledTextEvent", "ClipboardCopy")
			.AddEvent("EVT_STC_CLIPBOARD_PASTE", "wxEVT_STC_CLIPBOARD_PASTE", "wxStyledTextEvent", "ClipboardPaste")
			.AddEvent("EVT_STC_DOUBLECLICK", "wxEVT_STC_DOUBLECLICK", "wxStyledTextEvent", "DoubleClick")
			.AddEvent("EVT_STC_DO_DROP", "wxEVT_STC_DO_DROP", "wxStyledTextEvent", "TextDropping")
			.AddEvent("EVT_STC_DRAG_OVER", "wxEVT_STC_DRAG_OVER", "wxStyledTextEvent", "DragOver")
			.AddEvent("EVT_STC_DWELLEND", "wxEVT_STC_DWELLEND", "wxStyledTextEvent", "DwellEnd")
			.AddEvent("EVT_STC_DWELLSTART", "wxEVT_STC_DWELLSTART", "wxStyledTextEvent", "DwellStart")
			.AddEvent("EVT_STC_HOTSPOT_CLICK", "wxEVT_STC_HOTSPOT_CLICK", "wxStyledTextEvent", "HotspotClick")
			.AddEvent("EVT_STC_HOTSPOT_DCLICK", "wxEVT_STC_HOTSPOT_DCLICK", "wxStyledTextEvent", "HotspotDoubleClick")
			.AddEvent("EVT_STC_HOTSPOT_RELEASE_CLICK", "wxEVT_STC_HOTSPOT_RELEASE_CLICK", "wxStyledTextEvent", "HotspotReleaseClick")
			.AddEvent("EVT_STC_INDICATOR_CLICK", "wxEVT_STC_INDICATOR_CLICK", "wxStyledTextEvent", "IndicatorClick")
			.AddEvent("EVT_STC_INDICATOR_RELEASE", "wxEVT_STC_INDICATOR_RELEASE", "wxStyledTextEvent", "IndicatorRelease")
			.AddEvent("EVT_STC_MACRORECORD", "wxEVT_STC_MACRORECORD", "wxStyledTextEvent", "MacroRecord")
			.AddEvent("EVT_STC_MARGINCLICK", "wxEVT_STC_MARGINCLICK", "wxStyledTextEvent", "MarginClick")
			.AddEvent("EVT_STC_MARGIN_RIGHT_CLICK", "wxEVT_STC_MARGIN_RIGHT_CLICK", "wxStyledTextEvent", "MarginRightClick")
			.AddEvent("EVT_STC_MODIFIED", "wxEVT_STC_MODIFIED", "wxStyledTextEvent", "Modified")
			.AddEvent("EVT_STC_NEEDSHOWN", "wxEVT_STC_NEEDSHOWN", "wxStyledTextEvent", "NeedShown")
			.AddEvent("EVT_STC_PAINTED", "wxEVT_STC_PAINTED", "wxStyledTextEvent", "Painted")
			.AddEvent("EVT_STC_ROMODIFYATTEMPT", "wxEVT_STC_ROMODIFYATTEMPT", "wxStyledTextEvent", "ReadOnlyModifyAttempt")
			.AddEvent("EVT_STC_SAVEPOINTLEFT", "wxEVT_STC_SAVEPOINTLEFT", "wxStyledTextEvent", "SavePointLeft")
			.AddEvent("EVT_STC_SAVEPOINTREACHED", "wxEVT_STC_SAVEPOINTREACHED", "wxStyledTextEvent", "SavePointReached")
			.AddEvent("EVT_STC_START_DRAG", "wxEVT_STC_START_DRAG", "wxStyledTextEvent", "StartDrag")
			.AddEvent("EVT_STC_STYLENEEDED", "wxEVT_STC_STYLENEEDED", "wxStyledTextEvent", "StyleNeeded")
			.AddEvent("EVT_STC_UPDATEUI", "wxEVT_STC_UPDATEUI", "wxStyledTextEvent", "UIUpdated")
			.AddEvent("EVT_STC_USERLISTSELECTION", "wxEVT_STC_USERLISTSELECTION", "wxStyledTextEvent", "UserListSelected")
			.AddEvent("EVT_STC_ZOOM", "wxEVT_STC_ZOOM", "wxStyledTextEvent", "Zoomed")
			// wxTextEntry's members it has without their implementation, which do nothing, or with a
			// hint, stop its value from being set. Its own SetMargins, which takes a left and a right, stays.
			.Ignore("SetHint", "GetHint", "SetMaxLength", "AutoComplete", "AutoCompleteFileNames", "AutoCompleteDirectories", "ForceUpper", "GetMargins")
			.IgnoreOverload("SetMargins", "pt", "const wxPoint &")
			.IgnoreOverload("SetMargins", "top", "wxCoord")
			// UTF-8 versions of the string members, which .NET has no use for.
			.Ignore("AddTextRaw", "AppendTextRaw", "InsertTextRaw", "ReplaceSelectionRaw", "ReplaceTargetRaw", "ReplaceTargetRERaw", "SetTextRaw"));
		// The modifier keys are booleans named as booleans, Control would hide the Control class, and
		// the drag flags are DragFlags.
		tweaks.Class("wxStyledTextEvent", e => e
			.Rename("GetAlt", "IsAltDown")
			.Rename("GetControl", "IsControlDown")
			.Rename("GetShift", "IsShiftDown")
			// Scintilla numbers the modifier keys its own way, so Modifiers is written by hand, to convert them.
			.Internal("GetModifiers")
			.ParameterEnum("GetDragFlags", "return", "DragFlags")
			.ParameterEnum("SetDragFlags", "flags", "DragFlags"));
		// The web view, the system's browser engine. It is created in two steps, with a configuration
		// that says where its data goes, and pages and scripts are given to it through members of
		// WebView's own, which WebView.cs has.
		tweaks.Enum("wxWebViewBrowsingDataTypes", "WebViewBrowsingDataTypes", isFlags: true);
		tweaks.Enum("wxWebViewPrintFlags", "WebViewPrintFlags", isFlags: true);
		tweaks.Enum("wxWebViewFindFlags", "WebViewFindFlags", isFlags: true);
		tweaks.Class("wxWebView", web => web
			.NoConstructors()
			.Ignore("New", "Create", "NewConfiguration", "RegisterFactory", "GetBackendVersionInfo", "IsBackendAvailable", "LoadURL", "GetCurrentURL", "SetPage", "RunScript", "RunScriptAsync", "GetNativeBackend", "RegisterHandler")
			.Property("Title", "GetCurrentTitle")
			.Property("IsContextMenuEnabled", "IsContextMenuEnabled", "EnableContextMenu")
			.Property("IsAccessToDevToolsEnabled", "IsAccessToDevToolsEnabled", "EnableAccessToDevTools")
			.Property("AreBrowserAcceleratorKeysEnabled", "AreBrowserAcceleratorKeysEnabled", "EnableBrowserAcceleratorKeys")
			.Rename("PrintToPDF", "PrintToPdf")
			.ParameterEnum("ClearBrowsingData", "types", "WebViewBrowsingDataTypes")
			.ParameterEnum("Print", "flags", "WebViewPrintFlags")
			.Event("EVT_WEBVIEW_CREATED", "Created")
			.Event("EVT_WEBVIEW_NAVIGATING", "Navigating")
			.Event("EVT_WEBVIEW_NAVIGATED", "Navigated")
			.Event("EVT_WEBVIEW_LOADED", "Loaded")
			.Event("EVT_WEBVIEW_ERROR", "NavigationFailed")
			.Event("EVT_WEBVIEW_NEWWINDOW", "NewWindowRequested")
			.Event("EVT_WEBVIEW_NEWWINDOW_FEATURES", "NewWindowFeatures")
			.Event("EVT_WEBVIEW_WINDOW_CLOSE_REQUESTED", "WindowCloseRequested")
			.Event("EVT_WEBVIEW_TITLE_CHANGED", "TitleChanged")
			.Event("EVT_WEBVIEW_FULLSCREEN_CHANGED", "FullScreenChanged")
			.Event("EVT_WEBVIEW_SCRIPT_MESSAGE_RECEIVED", "ScriptMessageReceived")
			.Event("EVT_WEBVIEW_SCRIPT_RESULT", "ScriptResult")
			.AddEvent("EVT_WEBVIEW_BROWSING_DATA_CLEARED", "wxEVT_WEBVIEW_BROWSING_DATA_CLEARED", "wxWebViewEvent", "BrowsingDataCleared")
			.Event("EVT_WEBVIEW_PDF_SAVED", "PdfSaved"));
		tweaks.Class("wxWebViewEvent", e => e.Ignore("GetTargetWindowFeatures").Rename("GetURL", "Url"));
	}
}
