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
		// A paint event lends the DC to paint on, which it makes and disposes itself.
		tweaks.Class("wxPaintDC").Internal("wxPaintDC");
		// A frame attaches the menu bar it is given.
		tweaks.Class("wxMenuBar").Ignore("Attach", "Detach");
		tweaks.Class("wxEvent").Ignore("GetEventUserData", "GetEventCategory", "IsCommandEvent");
		tweaks.Class("wxDialog").Ignore(
			"CanDoLayoutAdaptation", "DoLayoutAdaptation", "EnableLayoutAdaptation", "IsLayoutAdaptationEnabled", "GetLayoutAdaptationDone", "SetLayoutAdaptationDone",
			"GetLayoutAdaptationLevel", "SetLayoutAdaptationLevel", "GetLayoutAdaptationMode", "SetLayoutAdaptationMode");
		// What the styled text control's drop target calls.
		tweaks.Class("wxStyledTextCtrl").Ignore("DoDragEnter", "DoDragOver", "DoDragLeave", "DoDropText");
		// Scintilla's names for what every text control has under another: Value, LastPosition, and
		// InsertionPoint, which wxWidgets implements with these.
		tweaks.Class("wxStyledTextCtrl").Ignore("GetText", "SetText", "GetTextLength", "GetCurrentPos", "SetCurrentPos");
		tweaks.Class("wxScrolledWindow").Ignore("PrepareDC");
		Repeats(tweaks);
		Forms(tweaks);
		Properties(tweaks);
	}

	// Members that say better what they are under another name or as a property.
	private static void Forms(Binding.Tweaks tweaks) {
		// A font is changed by making a copy that differs, each named for what the copy has. The
		// members that changed the font itself, and returned it, read the same and are left out.
		tweaks.Class("wxFont").Ignore("MakeBold", "MakeItalic", "MakeLarger", "MakeSmaller", "MakeStrikethrough", "MakeUnderlined", "Scale")
			.Rename("Bold", "WithBold").Rename("Italic", "WithItalic").Rename("Underlined", "WithUnderline").Rename("Strikethrough", "WithStrikethrough")
			.Rename("Larger", "WithLargerSize").Rename("Smaller", "WithSmallerSize").Rename("Scaled", "WithScaledSize");
		// A grid sizer's constructor is written by hand, to take the columns and a gap. wxWidgets'
		// four take a first number that is the rows or the columns by how many follow.
		foreach (string grid in (string[])["wxGridSizer", "wxFlexGridSizer"]) tweaks.Class(grid).IgnoreOverload(grid, "vgap", "int").InternalOverload(grid, "gap", "const wxSize &");
		// A window's id is an int everywhere else.
		tweaks.Class("wxWindow").IntParameter("FindWindow", "id").IntParameter("FindWindowById", "id");
		// An image's Rescale and Resize change it, where Scale and Resized return a copy.
		tweaks.Class("wxImage").DiscardReturn("Rescale", "Resize");
		// A position's column and line, named for what they are and in the order given.
		foreach (string text in (string[])["wxTextCtrl", "wxStyledTextCtrl"]) {
			tweaks.Class(text).Rename("PositionToXY", "TryGetColumnAndLine").ParameterName("PositionToXY", "x", "column").ParameterName("PositionToXY", "y", "line")
				.Rename("XYToPosition", "PositionFromColumnAndLine").ParameterName("XYToPosition", "x", "column").ParameterName("XYToPosition", "y", "line");
		}
		// What kind of mouse event one is, and which buttons are held, are properties. IsLeftDown
		// says the event is the left button going down, and IsLeftButtonPressed that it is held.
		tweaks.Class("wxMouseEvent", e => {
			foreach (string kind in (string[])["Dragging", "Entering", "Leaving", "Moving", "Magnify"]) e.Property("Is" + kind, kind);
			foreach (string button in (string[])["Left", "Middle", "Right", "Aux1", "Aux2"]) {
				e.Property($"Is{button}Down", button + "Down").Property($"Is{button}Up", button + "Up").Property($"Is{button}DoubleClick", button + "DClick").Property($"Is{button}ButtonPressed", button + "IsDown");
			}
		});
	}

	// A getter with a setter under another name is one property.
	private static void Properties(Binding.Tweaks tweaks) {
		// A window's styles are its Style property, of its class's own style enum, which the
		// generator writes for each class that has one. wxWidgets has them as a long, with a getter
		// under two names, a setter under two, and HasFlag and ToggleWindowStyle that take a number.
		tweaks.Class("wxWindow").Internal("GetWindowStyleFlag", "SetWindowStyleFlag").Ignore("GetWindowStyle", "SetWindowStyle", "HasFlag", "ToggleWindowStyle");
		tweaks.Class("wxListCtrl").Ignore("SetWindowStyleFlag");
		// A message dialog's buttons and icon aren't its window's styles, and MessageDialogStyle gives them.
		tweaks.Class("wxMessageDialog").StylesKeptApart();
		// The text entry dialog's documentation has its styles in prose. It keeps them apart from
		// its window's, and passes those that aren't its own to its text control.
		tweaks.Class("wxTextEntryDialog").StylesKeptApart()
			.Style("wxOK", "Ok", "Show an OK button.")
			.Style("wxCANCEL", "Cancel", "Show a Cancel button.")
			.Style("wxCENTRE", "Center", "Center the dialog on its parent, or on the screen without one.")
			.Style("wxTE_MULTILINE", "MultiLine", "The text can have more than one line.")
			.Style("wxTE_PASSWORD", "Password", "The text is shown as password characters.")
			.Style("wxTextEntryDialogStyle", "Default", "OK and Cancel buttons, and centered, which is what the dialog has when no styles are given.");
		// The choice dialogs take a dialog's styles with their own, and their default has both.
		foreach (string dialog in (string[])["wxSingleChoiceDialog", "wxMultiChoiceDialog"]) {
			tweaks.Class(dialog)
				.Style("wxCAPTION", "Caption", "Puts a caption on the dialog.")
				.Style("wxSYSTEM_MENU", "SystemMenu", "Shows a system menu.")
				.Style("wxCLOSE_BOX", "CloseBox", "Shows a close box.")
				.Style("wxRESIZE_BORDER", "ResizeBorder", "The dialog can be resized.")
				.Style("wxCHOICEDLG_STYLE", "Default", "A resizable dialog with a caption, a system menu, a close box, and OK and Cancel buttons, centered, which is what the dialog has when no styles are given.");
		}
		tweaks.Class("wxTreeCtrl").Ignore("SetWindowStyle");
		// The number an application keeps with a list's item is as wide to set as to read.
		tweaks.Class("wxListItem").LongParameter("SetData", "data").IgnoreOverload("SetData", "data", "void *").Property("Data", "GetData", "SetData");
		tweaks.Class("wxListCtrl").LongParameter("SetItemData", "data");
		// A window's event handler is wxWidgets' own chain, which PushEventHandler was for, and the
		// property hid System.EventHandler in every class derived from a window.
		tweaks.Class("wxWindow").Ignore("GetEventHandler", "SetEventHandler");
		// Style.HasFlag says it.
		tweaks.Class("wxTextValidator").Ignore("HasFlag");
		tweaks.Enum("wxTextValidatorStyle", "TextValidatorStyle", isFlags: true);
		// Names that are C# keywords, which a call that names them would have to escape.
		tweaks.Class("wxFont").ParameterName("SetSymbolicSizeRelativeTo", "base", "baseSize");
		tweaks.Class("wxInfoBar").ParameterName("ShowCheckBox", "checked", "isChecked");
		// What only wxWidgets calls, or a class overrides in C++.
		tweaks.Class("wxWindow").Ignore("SetFocusFromKbd", "GetMinSizeFromKnownDirection", "AdjustForLayoutDirection", "UseBackgroundColour", "UseForegroundColour").IgnoreOverload("GetBorder", "flags", "long");
		tweaks.Class("wxTopLevelWindow").Ignore("ShouldPreventAppExit", "GetTmpDefaultItem", "SetTmpDefaultItem");
		// Dispose frees an image, and nothing gives the name a handler is removed by.
		tweaks.Class("wxImage").Ignore("Destroy", "RemoveHandler");
		tweaks.Class("wxBitmap").Ignore("RemoveHandler");
		// Members that say again what another says: a toggle button's IsChecked, an event's
		// Position, a window's VirtualSize, the count of Files, Insert, Selection, a window's
		// Enable, and StringSelection.
		tweaks.Class("wxBitmapToggleButton").Ignore("GetValue", "SetValue");
		foreach (string positioned in (string[])["wxMouseEvent", "wxKeyEvent", "wxSetCursorEvent"]) tweaks.Class(positioned).Ignore("GetX", "GetY");
		tweaks.Class("wxScrolledWindow").Ignore("GetVirtualSize");
		tweaks.Class("wxDropFilesEvent").Ignore("GetNumberOfFiles");
		tweaks.Class("wxListBox").Ignore("InsertItems");
		tweaks.Class("wxRadioBox").Ignore("Select");
		tweaks.Class("wxStaticBox").Ignore("Enable");
		tweaks.Class("wxStyledTextCtrl").Ignore("GetSelectedText");
		// Second spellings, masks, and bounds among an enum's members.
		tweaks.ExcludeEnumMembers("wxSystemColour", "wxSYS_COLOUR_BTNHILIGHT", "wxSYS_COLOUR_3DHILIGHT");
		tweaks.ExcludeEnumMembers("wxKeyModifier", "wxMOD_CMD", "wxMOD_WIN", "wxMOD_ALL");
		tweaks.ExcludeEnumMembers("wxBrushStyle", "wxBRUSHSTYLE_FIRST_HATCH", "wxBRUSHSTYLE_LAST_HATCH");
		tweaks.ExcludeEnumMembers("wxPenStyle", "wxPENSTYLE_FIRST_HATCH", "wxPENSTYLE_LAST_HATCH");
		tweaks.ExcludeEnumMembers("wxFontEncoding", "wxFONTENCODING_MACMIN", "wxFONTENCODING_MACMAX");
		tweaks.Class("wxWindow").IgnoreStyle("wxBORDER_DOUBLE");
		tweaks.Class("wxMessageDialog").IgnoreStyle("wxICON_HAND", "wxICON_EXCLAMATION");
		// Booleans a method gave, which are properties named as IsChecked is.
		tweaks.Class("wxCheckBox").Property("IsThreeState", "Is3State").Property("IsThirdStateAllowedForUser", "Is3rdStateAllowedForUser");
		tweaks.Class("wxListCtrl").Property("IsInReportView", "InReportView");
		tweaks.Class("wxIdleEvent").Property("IsMoreRequested", "MoreRequested");
		tweaks.Class("wxWindow").Property("InheritsBackgroundColor", "InheritsBackgroundColour").Property("InheritsForegroundColor", "InheritsForegroundColour").Property("IsThemeEnabled", "GetThemeEnabled", "SetThemeEnabled");
		tweaks.Class("wxCollapsiblePaneEvent").Property("IsCollapsed", "GetCollapsed");
		tweaks.Class("wxButton").Property("IsAuthenticationNeeded", "GetAuthNeeded", "SetAuthNeeded");
		tweaks.Class("wxFont").Property("IsStrikethrough", "GetStrikethrough", "SetStrikethrough");
		// A getter whose setter has another name.
		tweaks.Class("wxTopLevelWindow").Property("OsxIsModified", "OSXIsModified", "OSXSetModified");
		tweaks.Class("wxDataViewCtrl").Property("IsMultiColumnSortAllowed", "IsMultiColumnSortAllowed", "AllowMultiColumnSort");
		tweaks.Class("wxDataViewColumn").Property("IsSortOrderAscending", "IsSortOrderAscending", "SetSortOrder");
		tweaks.Class("wxBitmap").Property("HasAlpha", "HasAlpha", "UseAlpha");
		tweaks.Class("wxValidator").Property("IsSilent", "IsSilent", "SuppressBellOnError");
		tweaks.Class("wxRichMessageDialog").Property("DetailedText", "GetDetailedText", "ShowDetailedText");
		tweaks.Class("wxMenuBar").Property("MacCommonMenuBar", "MacGetCommonMenuBar", "MacSetCommonMenuBar").Property("OsxAppleMenu", "OSXGetAppleMenu");
		tweaks.Class("wxUpdateUIEvent").Property("IsThreeState", "Is3State").Property("IsChecked", "GetChecked", "Check").Property("IsEnabled", "GetEnabled", "Enable").Property("IsShown", "GetShown", "Show")
			// Whether the item is to be checked, which a command event's IsChecked, whether it was, says nothing of here.
			.HidesInherited("IsChecked");
		// What a DC drew within is one rectangle, in Points.cs.
		tweaks.Class("wxDC").Internal("MinX", "MaxX", "MinY", "MaxY");
		// Numbers that are an enum, flags, or a time.
		tweaks.Class("wxFlexGridSizer").ParameterEnum("GetFlexibleDirection", "return", "Orientation").ParameterEnum("SetFlexibleDirection", "direction", "Orientation");
		tweaks.Class("wxWindow").ParameterEnum("GetScrollbarSize", "orient", "Orientation").ParameterEnum("EnableTouchEvents", "eventsMask", "TouchEvents");
		tweaks.ConstantEnum("TouchEvents", "wxTOUCH_", isFlags: true);
		tweaks.Class("wxInfoBar").ParameterMilliseconds("GetEffectDuration", "return").ParameterMilliseconds("SetEffectDuration", "duration");
		tweaks.Class("wxTreeListCtrl").ParameterEnum("AppendColumn", "flags", "wxDataViewColumnFlags").ParameterName("AppendColumn", "align", "alignment");
		tweaks.Class("wxRichMessageDialog").ParameterEnum("GetFooterIcon", "return", "MessageIcon").ParameterEnum("SetFooterIcon", "icon", "MessageIcon");
		// A web view's count of matches is an int, as other counts are.
		tweaks.Class("wxWebView").IntIndexes();
		// A message dialog's buttons and icon are its Style, which hides the window's, as each
		// class's does, though it is kept apart.
		foreach (string dialog in (string[])["wxMessageDialog", "wxRichMessageDialog"]) tweaks.Class(dialog).Property("Style", "GetMessageDialogStyle").HidesInherited("Style");
		// Where a splitter's sash was dragged to is a Point, and a grid bag's gaps are a Size, as a grid's are, in Points.cs.
		tweaks.Class("wxSplitterEvent").Internal("GetX", "GetY");
		tweaks.Class("wxGridBagSizer").InternalOverload("wxGridBagSizer", "vgap", "int");
		// Enum members that repeated their enum's name, and enums named flags that aren't.
		tweaks.NameEnumMember("wxWRAPSIZER_DEFAULT_FLAGS", "Default");
		tweaks.NameEnumMember("wxPRINTER_NO_ERROR", "None");
		tweaks.NameEnumMember("wxSYS_OEM_FIXED_FONT", "OemFixed");
		tweaks.NameEnumMember("wxSYS_ANSI_FIXED_FONT", "AnsiFixed");
		tweaks.NameEnumMember("wxSYS_ANSI_VAR_FONT", "AnsiVariable");
		tweaks.NameEnumMember("wxSYS_SYSTEM_FONT", "System");
		tweaks.NameEnumMember("wxSYS_DEVICE_DEFAULT_FONT", "DeviceDefault");
		tweaks.NameEnumMember("wxSYS_DEFAULT_GUI_FONT", "DefaultGui");
		tweaks.NameEnumMember("wxSYS_SYSTEM_FIXED_FONT", "SystemFixed");
		tweaks.Enum("wxWebViewNavigationActionFlags", "WebViewNavigationAction");
		tweaks.Enum("wxWebViewReloadFlags", "WebViewReload");
		// One name for one thing: the text selected, where something happened, a spin control's
		// value, a list item's index, a file's name, a count, an item of a radio box, and what
		// removes or selects a tree's item.
		foreach (string text in (string[])["wxTextCtrl", "wxSearchCtrl", "wxStyledTextCtrl"]) tweaks.Class(text).Property("SelectedText", "GetStringSelection");
		tweaks.Class("wxListEvent").Property("Position", "GetPoint");
		tweaks.Class("wxTreeEvent").Property("Position", "GetPoint");
		tweaks.Class("wxSpinEvent").Property("Value", "GetPosition");
		tweaks.Class("wxListItem").Property("Index", "GetId", "SetId").Property("Alignment", "GetAlign", "SetAlign");
		tweaks.Class("wxFileCtrlEvent").Property("FileName", "GetFile").Property("FileNames", "GetFiles");
		tweaks.Class("wxStatusBar").Property("FieldCount", "GetFieldsCount", "SetFieldsCount");
		tweaks.Class("wxToolBar").Property("ToolCount", "GetToolsCount");
		tweaks.Class("wxTextCtrl").Property("LineCount", "GetNumberOfLines");
		tweaks.Class("wxStyledTextCtrl").Property("LineCount", "GetNumberOfLines");
		tweaks.Class("wxRadioBox").Rename("Enable", "EnableItem").Rename("Show", "ShowItem");
		tweaks.Class("wxTreeCtrl").Rename("Delete", "DeleteItem");
		tweaks.Class("wxTreeListCtrl").Rename("Select", "SelectItem").Rename("Unselect", "UnselectItem");
		tweaks.Class("wxMouseEvent").Rename("ButtonDClick", "ButtonDoubleClick");
		// A status bar's field has one name, and a file's name is fileName.
		foreach (string member in (string[])["AddFieldControl", "GetFieldRect", "GetStatusText", "SetStatusText"]) tweaks.Class("wxStatusBar").ParameterName(member, "i", "field");
		foreach (string member in (string[])["GetStatusStyle", "GetStatusWidth"]) tweaks.Class("wxStatusBar").ParameterName(member, "n", "field");
		tweaks.Class("wxBitmap").ParameterName("SaveFile", "name", "fileName");
		tweaks.Class("wxImage").ParameterName("SaveFile", "name", "fileName");
		tweaks.Class("wxFont").ParameterName("Scaled", "x", "factor");
		tweaks.Class("wxWindow").ParameterName("FromDIP", "d", "value").ParameterName("ToDIP", "d", "value").ParameterName("FromPhys", "d", "value").ParameterName("ToPhys", "d", "value");
		// A bitmap combo box has one constructor, as a combo box does, and a number dialog's numbers are ints.
		tweaks.Class("wxBitmapComboBox").OptionalItems();
		tweaks.Class("wxNumberEntryDialog").IntIndexes();
		tweaks.Class("wxCollapsiblePane").Property("IsCollapsed", "IsCollapsed", "Collapse");
		tweaks.Class("wxListCtrl").Property("HasCheckBoxes", "HasCheckBoxes", "EnableCheckBoxes");
		tweaks.Class("wxSearchCtrl").Property("IsSearchButtonVisible", "IsSearchButtonVisible", "ShowSearchButton").Property("IsCancelButtonVisible", "IsCancelButtonVisible", "ShowCancelButton");
		// Scintilla puts Get and Set after a setting's subject, as in AutoCompGetAutoHide, where the
		// generator looks for them first.
		foreach (string setting in (string[])["AutoHide", "CancelAtStart", "ChooseSingle", "DropRestOfWord", "IgnoreCase", "MaxHeight", "MaxWidth", "Multi", "Order", "Separator", "TypeSeparator"]) {
			tweaks.Class("wxStyledTextCtrl").Property($"AutoComp{setting}", $"AutoCompGet{setting}", $"AutoCompSet{setting}");
		}
		tweaks.Class("wxStyledTextCtrl")
			.Property("AutoCompCaseInsensitiveBehavior", "AutoCompGetCaseInsensitiveBehaviour", "AutoCompSetCaseInsensitiveBehaviour")
			.Property("AutoCompCurrent", "AutoCompGetCurrent")
			.Property("AutoCompCurrentText", "AutoCompGetCurrentText")
			.Property("CallTipPosAtStart", "CallTipPosAtStart", "CallTipSetPosAtStart")
			.Property("IsAutoCompActive", "AutoCompActive")
			.Property("IsCallTipActive", "CallTipActive")
			.Property("IsSelectionRectangle", "SelectionIsRectangle");
		// These are written by hand, in PairedProperties.cs, since a setter takes two values, or
		// returns one, or there are two to choose between.
		tweaks.Class("wxSplitterWindow").Internal("GetSashPosition", "SetSashPosition");
		tweaks.Class("wxBookCtrlBase").Internal("GetSelection", "SetSelection", "ChangeSelection");
		tweaks.Class("wxNotebook").Ignore("GetSelection", "SetSelection", "ChangeSelection");
		tweaks.Class("wxTreebook").Ignore("GetSelection");
		tweaks.Class("wxImage").Internal("GetMaskRed", "GetMaskGreen", "GetMaskBlue", "SetMaskColour", "HasMask", "SetMask");
		tweaks.Class("wxWindow").Internal("GetToolTipText", "UnsetToolTip");
		// An image's members that take a color as three bytes, or a pixel as x and y, are written
		// by hand, in ImageColors.cs, to take a Color and a Point.
		tweaks.Class("wxImage").Internal("GetRed", "GetGreen", "GetBlue", "GetAlpha", "SetAlpha", "SetRGB", "IsTransparent", "Replace", "ConvertToMono", "SetMaskFromImage", "Resize", "Size")
			.InternalOverload("ConvertAlphaToMask", "mr", "unsigned char");
		// A constructor makes a bitmap or an image of a size, so the Create that makes an empty one
		// over is left out. Bitmap.FromDipSize and FromLogicalSize, written by hand, are the two
		// that have no constructor.
		tweaks.Class("wxBitmap").Ignore("Create", "CreateScaled").Internal("CreateWithDIPSize", "CreateWithLogicalSize");
		tweaks.Class("wxImage").Ignore("Create");
		// GetItem and GetColumn, written by hand, return the item they fill.
		tweaks.Class("wxListCtrl").Internal("GetItem", "GetColumn");
		// ShowWindowModalAsync, written by hand, gives what the dialog returned as a task's result.
		tweaks.Class("wxDialog").Internal("ShowWindowModal");
		// The origins are written by hand, since the setters take x and y.
		tweaks.Class("wxDC").Internal("GetDeviceOrigin", "SetDeviceOrigin", "GetLogicalOrigin", "SetLogicalOrigin");
		tweaks.Class("wxMemoryDC").Property("SelectedBitmap", "GetSelectedBitmap", "SelectObject");
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
		// A place or a size given as two numbers, which another overload or a property takes as one value.
		tweaks.Class("wxAnyButton").IgnoreOverload("SetBitmapMargins", "x", "wxCoord");
		tweaks.Class("wxToolBar").IgnoreOverload("SetMargins", "x", "int");
		tweaks.Class("wxControl").IgnoreOverload("GetSizeFromTextSize", "xlen", "int");
		tweaks.Class("wxDC")
			.IgnoreOverload("DeviceToLogicalRel", "x", "int")
			.IgnoreOverload("LogicalToDeviceRel", "x", "int")
			.IgnoreOverload("DrawArc", "xStart", "wxCoord")
			// The rest have no such overload, so each is written by hand, in Points.cs, to take Points.
			.Internal("Blit", "StretchBlit", "CalcBoundingBox", "DrawSpline");
		tweaks.Class("wxImage").Internal("Paste");
		// An empty bitmap or image was only good for LoadFile, and one made from a file that could
		// not be read was an object that was not usable. FromFile and FromStream give null instead.
		// SaveFile is written by hand, so that a format without a handler leaves the file alone.
		tweaks.Class("wxBitmap").InternalEmptyConstructor().InternalOverload("wxBitmap", "name", "const wxString &").Internal("LoadFile", "SaveFile");
		tweaks.Class("wxImage").InternalEmptyConstructor().InternalOverload("wxImage", "name", "const wxString &").Internal("LoadFile", "SaveFile");
		tweaks.Class("wxIcon").InternalEmptyConstructor().InternalOverload("wxIcon", "name", "const wxString &").Internal("LoadFile", "CopyFromBitmap");
		// A cursor from a bitmap takes its hot spot as a Point, so the overload with two numbers goes too.
		tweaks.Class("wxCursor").InternalEmptyConstructor().InternalOverload("wxCursor", "hotSpotX", "int").InternalOverload("wxCursor", "name", "const wxString &");
		tweaks.Class("wxPrintout").Internal("SetLogicalOrigin", "OffsetLogicalOrigin");
		tweaks.Class("wxStyledTextCtrl").Internal("CharPositionFromPoint", "CharPositionFromPointClose", "PositionFromPointClose");
		tweaks.Class("wxToolBar").Internal("FindToolForPosition");
		tweaks.Class("wxWindow").Internal("WarpPointer");
		// A color given as three bytes, or by name, is a Color.
		tweaks.Class("wxBrush").IgnoreOverload("SetColour", "red", "unsigned char");
		tweaks.Class("wxPen").IgnoreOverload("SetColour", "red", "unsigned char");
		tweaks.Class("wxColourPickerCtrl").IgnoreOverload("SetColour", "colname", "const wxString &");
		// SelectionStart and SelectionLength, or the styled text control's SelectionEnd, give the
		// selection without out parameters. The hand-written properties call the import.
		foreach (string entry in (string[])["wxTextCtrl", "wxComboBox", "wxSearchCtrl", "wxStyledTextCtrl"]) tweaks.Class(entry).InternalOverload("GetSelection", "from", "long *");
		// Scintilla's names for what the styled text control has as a text control too, under the
		// names TextCtrl has. Its Clear deletes the selection, where a text control's clears it all,
		// which ClearAll does here.
		tweaks.Class("wxStyledTextCtrl").Ignore("GetModify", "GetLine", "LineLength", "GetLineCount", "GetTextRange", "GetReadOnly", "SetReadOnly").Rename("Clear", "DeleteSelection");
	}
}
