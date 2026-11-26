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
		tweaks.Class("wxTreeCtrl").Ignore("SetWindowStyle");
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
