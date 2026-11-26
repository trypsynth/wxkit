namespace WxKit.Generator.Tweaks;

// Windows, frames, panels, and menus.
internal static class Windows {
	public static void Apply(Binding.Tweaks tweaks) {
		// Written by hand, because they track ownership or need the shim's own helpers.
		tweaks.Class("wxEvtHandler").Handwritten();
		tweaks.Class("wxMenu").Handwritten().Wrappable().Internal("wxMenu", "Append", "AppendSeparator", "AppendSubMenu", "GetParent", "IsAttached");
		tweaks.Class("wxMenuItem").Handwritten().Internal("GetId", "Enable", "IsEnabled", "Check", "IsChecked", "GetItemLabel", "SetItemLabel", "GetHelp", "SetHelp", "IsSeparator", "GetSubMenu");
		tweaks.Class("wxWindow", window => window
			.NoConstructors()
			// wxWidgets calls these itself as it makes a window, and a class overrides the first.
			.Ignore("ShouldInheritColours", "InheritAttributes")
			// WxKit tracks the sizer a window owns, gives the native handle as NativeHandle, keeps
			// the validator it was given, since the window holds a copy, and gives a window its
			// accessible object through the hand-written Accessible. A name for screen readers, and a
			// tooltip, which names a button that shows only an icon, are set by hand on Linux and macOS.
			.Internal("SetSizer")
			.Ignore("SetSizerAndFit", "GetSizer", "GetHandle", "SetValidator", "GetValidator", "GetAccessible", "SetAccessible", "CreateAccessible", "GetOrCreateAccessible", "SetAccessibleName", "SetToolTip")
			.Property("IsEnabled", "IsEnabled", "Enable")
			.ParameterEnum("Centre", "direction", "Orientation")
			.ParameterEnum("Move", "flags", "SizeFlags")
			.ParameterEnum("SetSize", "sizeFlags", "SizeFlags")
			.ParameterEnum("SendSizeEvent", "flags", "SendSizeEventFlags")
			.ParameterEnum("SendSizeEventToParent", "flags", "SendSizeEventFlags")
			.ParameterEnum("UpdateWindowUI", "flags", "wxUpdateUI")
			// wxWidgets documents the mouse events on wxMouseEvent, not on the windows that receive
			// them. They do not travel to the parent, so each window has its own.
			.AddEvent("EVT_LEFT_DOWN", "wxEVT_LEFT_DOWN", "wxMouseEvent", "LeftDown")
			.AddEvent("EVT_LEFT_UP", "wxEVT_LEFT_UP", "wxMouseEvent", "LeftUp")
			.AddEvent("EVT_LEFT_DCLICK", "wxEVT_LEFT_DCLICK", "wxMouseEvent", "LeftDoubleClick")
			.AddEvent("EVT_RIGHT_DOWN", "wxEVT_RIGHT_DOWN", "wxMouseEvent", "RightDown")
			.AddEvent("EVT_RIGHT_UP", "wxEVT_RIGHT_UP", "wxMouseEvent", "RightUp")
			.AddEvent("EVT_RIGHT_DCLICK", "wxEVT_RIGHT_DCLICK", "wxMouseEvent", "RightDoubleClick")
			.AddEvent("EVT_MIDDLE_DOWN", "wxEVT_MIDDLE_DOWN", "wxMouseEvent", "MiddleDown")
			.AddEvent("EVT_MIDDLE_UP", "wxEVT_MIDDLE_UP", "wxMouseEvent", "MiddleUp")
			.AddEvent("EVT_MOTION", "wxEVT_MOTION", "wxMouseEvent", "MouseMoved")
			.AddEvent("EVT_MOUSEWHEEL", "wxEVT_MOUSEWHEEL", "wxMouseEvent", "MouseWheel")
			.AddEvent("EVT_ENTER_WINDOW", "wxEVT_ENTER_WINDOW", "wxMouseEvent", "MouseEntered")
			.AddEvent("EVT_LEAVE_WINDOW", "wxEVT_LEAVE_WINDOW", "wxMouseEvent", "MouseLeft")
			.ParameterEnum("Navigate", "flags", "NavigationFlags")
			.ParameterEnum("NavigateIn", "flags", "NavigationFlags")
			.ParameterEnum("GetExtraStyle", "return", "WindowExtraStyle")
			.ParameterEnum("SetExtraStyle", "exStyle", "WindowExtraStyle")
			.ParameterEnum("HasExtraStyle", "exFlag", "WindowExtraStyle")
			// Char would hide System.Char inside a control.
			.Event("EVT_CHAR", "CharTyped")
			// wxGTK has no system-wide hot keys.
			.Guard("wxUSE_HOTKEY", "RegisterHotKey", "UnregisterHotKey")
			// Abbreviations .NET spells out, as its naming guidelines ask.
			.Rename("GetScrollPos", "GetScrollPosition")
			.Rename("SetScrollPos", "SetScrollPosition")
			.Property("UsesBackgroundColor", "UseBgCol"));
		// Null clears the default item.
		tweaks.Class("wxTopLevelWindow", window => window
			.NoConstructors()
			.NullableParameter("SetDefaultItem", "win")
			.NullableParameter("SetTmpDefaultItem", "win")
			// Only wxUniversal has these, though the documentation does not mark them for one port.
			.Ignore("UseNativeDecorations", "UseNativeDecorationsByDefault", "IsUsingNativeDecorations")
			.Guard("defined(__WXMSW__)", "MSWGetSystemMenu")
			.ParameterEnum("ShowFullScreen", "style", "FullScreenStyle")
			.ParameterEnum("EnableFullScreenView", "style", "FullScreenStyle")
			.ParameterEnum("RequestUserAttention", "flags", "UserAttention")
			.AddEvent("EVT_CLOSE", "wxEVT_CLOSE_WINDOW", "wxCloseEvent", "Closing"));
		// What's at a point in a window, named so the enum isn't hidden by Window.HitTest inside a control.
		tweaks.Enum("wxHitTest", "HitTestResult");
		// The flags these take, which wxWidgets declares as constants rather than a named enum.
		// An alignment is one way across and one way down, together. Not is Left and Top, which are
		// both no flag, and Mask and Invalid are for wxWidgets's own use.
		tweaks.Enum("wxAlignment", "Alignment", isFlags: true, exclude: ["wxALIGN_NOT", "wxALIGN_MASK", "wxALIGN_INVALID"]);
		tweaks.Class("wxDC", dc => dc.ParameterEnum("DrawLabel", "alignment", "wxAlignment"));
		tweaks.ConstantEnum("WindowExtraStyle", "wxWS_EX_", isFlags: true);
		tweaks.ConstantEnum("SizeFlags", "wxSIZE_", isFlags: true, excludePrefixes: ["wxSIZE_CONV_", "wxSIZE_SET_"]);
		tweaks.ConstantEnum("SendSizeEventFlags", "wxSEND_EVENT_", isFlags: true);
		tweaks.ConstantEnum("FullScreenStyle", "wxFULLSCREEN_", isFlags: true);
		tweaks.ConstantEnum("UserAttention", "wxUSER_ATTENTION_");
		tweaks.Enum("wxUpdateUI", "UpdateUIFlags", isFlags: true);
		// WxKit keeps the menu bar a frame owns. OnCreateStatusBar and OnCreateToolBar are what
		// CreateStatusBar and CreateToolBar call, for a derived frame to override, not to call.
		// Null removes the status bar or the toolbar.
		tweaks.Class("wxFrame", frame => frame
			// The frame calls it itself to show a menu item's help in the status bar.
			.Ignore("DoGiveHelp")
			.NullableParameter("SetStatusBar", "statusBar")
			.NullableParameter("SetToolBar", "toolBar")
			.ParameterName("SetStatusWidths", "widths_field", "widths")
			.Internal("SetMenuBar")
			.Ignore("GetMenuBar", "OnCreateStatusBar", "OnCreateToolBar")
			.ParameterEnum("CreateToolBar", "style", "ToolBarStyle")
			.ParameterEnum("CreateStatusBar", "style", "StatusBarStyle"));
		tweaks.Class("wxPanel");
		// wxMenuBar's Enable and IsEnabled take an item's id, so they would hide Window.IsEnabled.
		tweaks.Class("wxMenuBar", menuBar => menuBar.Rename("IsEnabled", "IsItemEnabled").Rename("Enable", "EnableItem"));
		// A panel that scrolls its contents, such as a long page of settings.
		tweaks.TemplateInstance("wxScrolled", "wxScrolledWindow", "wxPanel");
		// OnDraw is for a derived class to override, and DoPrepareReadOnlyDC takes the wxReadOnlyDC
		// wxWidgets 3.3 added, which is not bound.
		tweaks.Class("wxScrolledWindow", scrolled => scrolled
			.Ignore("OnDraw", "DoPrepareReadOnlyDC")
			.ParameterEnum("GetScrollLines", "orient", "Orientation")
			.ParameterEnum("GetScrollPageSize", "orient", "Orientation")
			.ParameterEnum("SetScrollPageSize", "orient", "Orientation")
			.ParameterName("SetScrollRate", "xstep", "xStep")
			.ParameterName("SetScrollRate", "ystep", "yStep"));
		// The standard ids, which give a button or menu item the platform's label and shortcut. The
		// bounds of the ids wxWidgets assigns itself, and those of its own file controls, are its own.
		tweaks.ConstantClass("wxStandardID", "StandardId", "wxID_AUTO_LOWEST", "wxID_AUTO_HIGHEST", "wxID_LOWEST", "wxID_HIGHEST", "wxID_FILEDLGG", "wxID_FILECTRL");
	}
}
