namespace WxKit.Generator.Tweaks;

// The controls most applications use, pickers, validators, toolbars, and dates.
internal static class Controls {
	public static void Apply(Binding.Tweaks tweaks) {
		tweaks.Class("wxButton", button => button.Event("EVT_BUTTON", "Click"));
		tweaks.Class("wxStaticText");
		// Spell checking needs gspell on Linux, which WxKit leaves out for now, so there it does nothing.
		tweaks.Class("wxTextCtrl", text => text.Event("EVT_TEXT", "TextChanged").Guard("wxUSE_SPELLCHECK", "EnableProofCheck"));
		foreach (string control in (string[])[
			"wxControl", "wxAnyButton", "wxControlWithItems", "wxCheckBox", "wxRadioButton", "wxRadioBox",
			"wxChoice", "wxComboBox", "wxListBox", "wxCheckListBox", "wxSpinCtrl", "wxSpinCtrlDouble",
			"wxSpinButton", "wxSlider", "wxGauge", "wxStaticBox", "wxStaticLine", "wxToggleButton",
			"wxHyperlinkCtrl", "wxScrollBar", "wxCollapsiblePane", "wxSearchCtrl", "wxStatusBar",
			"wxBookCtrlBase", "wxNotebook", "wxSplitterWindow",
			"wxStaticBitmap", "wxBitmapButton", "wxBitmapToggleButton", "wxCommandLinkButton",
			"wxListbook", "wxChoicebook", "wxTreebook", "wxToolbook", "wxSimplebook",
			"wxInfoBar", "wxActivityIndicator", "wxBitmapComboBox", "wxRearrangeList", "wxRearrangeCtrl",
			"wxBannerWindow", "wxMiniFrame", "wxRearrangeDialog",
		]) {
			tweaks.Class(control);
		}
		// An item's data is an object, which the control deletes with the item. Detaching it would
		// leave no one to free it.
		foreach (string container in (string[])["wxControlWithItems", "wxChoice", "wxComboBox", "wxListBox"]) tweaks.Class(container).Ignore("DetachClientObject");
		// Null removes the search control's menu. Its documentation lists only its search events, but
		// every port sends a text event as it's typed in, as a text control does, which filtering as the
		// user types needs.
		tweaks.Class("wxSearchCtrl", search => search
			.NullableParameter("SetMenu", "menu")
			.AddEvent("EVT_TEXT", "wxEVT_TEXT", "wxCommandEvent", "TextChanged"));
		// A check box, a radio button, and a toggle button say whether they are checked, as WPF's do.
		tweaks.Class("wxCheckBox").Ignore("IsChecked").Property("IsChecked", "GetValue", "SetValue");
		tweaks.Class("wxRadioButton").Property("IsChecked", "GetValue", "SetValue");
		tweaks.Class("wxToggleButton").Property("IsChecked", "GetValue", "SetValue");
		// IsEmpty is ambiguous on purpose, between the list and the text. IsListEmpty and IsTextEmpty say which.
		tweaks.Class("wxComboBox").Event("EVT_TEXT", "TextChanged").Ignore("IsEmpty");
		tweaks.Class("wxSlider").Rename("GetSelStart", "SelectionStart").Rename("GetSelEnd", "SelectionEnd").Rename("ClearSel", "ClearSelection");
		tweaks.Class("wxSplitterWindow").ParameterEnum("SetSplitMode", "mode", "SplitMode");
		tweaks.ConstantEnumOf("StatusBarFieldStyle", isFlags: false, "wxSB_NORMAL", "wxSB_FLAT", "wxSB_RAISED", "wxSB_SUNKEN");
		// A getter that can fail returns whether it could, and its result through out, as .NET's Try
		// methods do, and is named as they are.
		tweaks.Class("wxStatusBar", bar => bar
			.ParameterName("SetStatusWidths", "widths_field", "widths")
			.ParameterEnum("GetStatusStyle", "return", "StatusBarFieldStyle")
			.Rename("GetFieldRect", "TryGetFieldRect"));
		// Pickers: a button, and optionally a text box, that choose a color, a file, a folder, or a
		// font. Events are only received, so the events' values are only read.
		// CreateBase is for a picker's own Create.
		tweaks.Class("wxPickerBase", picker => picker.Ignore("CreateBase"));
		tweaks.Class("wxColourPickerCtrl", picker => picker
			.StylesPrefixed("CLRP")
			.Event("EVT_COLOURPICKER_CHANGED", "ColorChanged")
			.Event("EVT_COLOURPICKER_CURRENT_CHANGED", "CurrentColorChanged")
			.Event("EVT_COLOURPICKER_DIALOG_CANCELLED", "DialogCanceled"));
		tweaks.Class("wxFilePickerCtrl", picker => picker
			.Event("EVT_FILEPICKER_CHANGED", "PathChanged")
			.ParameterName("SetInitialDirectory", "dir", "directory"));
		tweaks.Class("wxDirPickerCtrl", picker => picker
			.StylesPrefixed("DIRP")
			.Event("EVT_DIRPICKER_CHANGED", "PathChanged")
			.ParameterName("SetInitialDirectory", "dir", "directory"));
		tweaks.Class("wxFontPickerCtrl", picker => picker
			.StylesPrefixed("FNTP")
			.Event("EVT_FONTPICKER_CHANGED", "FontChanged"));
		tweaks.Class("wxColourPickerEvent", e => e.Ignore("SetColour"));
		tweaks.Class("wxFileDirPickerEvent", e => e.Ignore("SetPath"));
		tweaks.Class("wxFontPickerEvent", e => e.Ignore("SetFont"));
		// A bitmap button raises its base's Click. CreateCloseButton is the second step of a two-step creation.
		tweaks.Class("wxBitmapButton", button => button.Event("EVT_BUTTON", "Click").Ignore("CreateCloseButton"));
		tweaks.Class("wxStaticBitmap", bitmap => bitmap.ParameterName("wxStaticBitmap", "label", "bitmap"));
		tweaks.Class("wxTreebook", book => book.ParameterName("AddSubPage", "bSelect", "select").ParameterName("InsertSubPage", "bSelect", "select"));
		tweaks.Class("wxInfoBar", bar => bar
			.ParameterName("AddButton", "btnid", "buttonId")
			.ParameterName("HasButtonId", "btnid", "buttonId")
			.ParameterName("RemoveButton", "btnid", "buttonId")
			.ParameterName("ShowMessage", "msg", "message")
			.ParameterName("ShowMessage", "flags", "icon")
			.ParameterEnum("ShowMessage", "flags", "MessageIcon"));
		// wxOSX has no static box with a window as its label.
		tweaks.Class("wxStaticBox").GuardOverload("defined(wxHAS_WINDOW_LABEL_IN_STATIC_BOX)", "wxStaticBox", "label", "wxWindow *");
		// Validators, which WxKit creates and windows copy. Custom ones are hand-written, in CustomValidator.
		tweaks.Class("wxValidator", validator => validator.Owned().NoConstructors().Ignore("SetWindow", "Clone"));
		tweaks.Class("wxTextValidator", validator => validator
			.Owned()
			.Ignore("Clone", "OnChar")
			.ParameterEnum("wxTextValidator", "style", "wxTextValidatorStyle")
			.ParameterEnum("SetStyle", "style", "wxTextValidatorStyle")
			.ParameterEnum("GetStyle", "return", "wxTextValidatorStyle"));
		// Toolbars and their tools. A tool is only made by adding it, so the members that make or remove
		// a tool nobody owns are left out. Deleting tools is written by hand, since the toolbar marks
		// their objects destroyed. A tool's setters only change what the tool records, not what the
		// toolbar shows, so its state is written by hand, through the toolbar.
		// EVT_MENU is EVT_TOOL, which ToolClicked is, and a Menu event would hide the Menu class.
		tweaks.Class("wxToolBar", toolBar => toolBar
			.IgnoreEvent("EVT_MENU")
			// The toolbar calls these itself when a tool is clicked or the mouse reaches one.
			.Ignore("OnLeftClick", "OnMouseEnter")
			.Internal("DeleteTool", "DeleteToolByPos", "ClearTools")
			.Ignore("CreateTool", "CreateSeparator", "RemoveTool", "GetToolClientData", "SetToolClientData")
			.Rename("GetToolByPos", "GetToolByPosition")
			.Rename("DeleteToolByPos", "DeleteToolByPosition")
			.Rename("GetToolPos", "GetToolPosition"));
		tweaks.Class("wxToolBarToolBase", tool => tool
			.Named("ToolBarTool")
			.NoConstructors()
			.Only("GetId", "GetControl", "GetKind", "GetLabel", "GetNormalBitmap", "GetDisabledBitmap", "IsButton", "IsControl", "IsSeparator", "IsStretchableSpace", "CanBeToggled"));
		// Dates and times, which are DateTimes in local time. Events are only received, so their
		// dates are only read.
		tweaks.Class("wxDatePickerCtrl", picker => picker
			.ParameterName("wxDatePickerCtrl", "dt", "date")
			.ParameterName("SetRange", "dt1", "earliest")
			.ParameterName("SetRange", "dt2", "latest"));
		// The time is the Value property.
		tweaks.Class("wxTimePickerCtrl", picker => picker
			.ParameterName("wxTimePickerCtrl", "dt", "time")
			.ParameterName("GetTime", "min", "minute")
			.ParameterName("GetTime", "sec", "second")
			.ParameterName("SetTime", "min", "minute")
			.ParameterName("SetTime", "sec", "second")
			.Ignore("GetTime"));
		// The headers list the double click as EVT_CALENDAR, which sends wxEVT_CALENDAR_DOUBLECLICKED.
		tweaks.Class("wxCalendarCtrl", calendar => calendar
			.Property("HeaderForegroundColor", "GetHeaderColourFg")
			.Property("HeaderBackgroundColor", "GetHeaderColourBg")
			.Property("HighlightForegroundColor", "GetHighlightColourFg")
			.Property("HighlightBackgroundColor", "GetHighlightColourBg")
			.Property("HolidayForegroundColor", "GetHolidayColourFg")
			.Property("HolidayBackgroundColor", "GetHolidayColourBg")
			.ParameterName("SetDateRange", "lowerdate", "earliest")
			.ParameterName("SetDateRange", "upperdate", "latest")
			.Event("EVT_CALENDAR_SEL_CHANGED", "SelectionChanged")
			.Event("EVT_CALENDAR_PAGE_CHANGED", "PageChanged")
			.Event("EVT_CALENDAR_WEEK_CLICKED", "WeekClicked")
			.Event("EVT_CALENDAR_WEEKDAY_CLICKED", "WeekDayClicked")
			.AddEvent("EVT_CALENDAR", "wxEVT_CALENDAR_DOUBLECLICKED", "wxCalendarEvent", "DateActivated")
			.Rename("ResetAttr", "ResetAttributes"));
		tweaks.Class("wxDateEvent", e => e.Ignore("SetDate"));
		tweaks.Class("wxCalendarEvent", e => e.Ignore("SetWeekDay"));
	}
}
