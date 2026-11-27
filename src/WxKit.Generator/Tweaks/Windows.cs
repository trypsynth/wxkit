namespace WxKit.Generator.Tweaks;

// Windows, frames, panels, and menus.
internal static class Windows {
	public static void Apply(Binding.Tweaks tweaks) {
		// Written by hand, because they track ownership or need the shim's own helpers.
		tweaks.Class("wxEvtHandler").Handwritten();
		tweaks.Class("wxMenu").Handwritten().Wrappable().Internal("wxMenu", "Append", "AppendSeparator", "AppendSubMenu", "GetParent", "IsAttached", "GetMenuItemCount", "FindItemByPosition", "FindItem");
		tweaks.Class("wxMenuItem").Handwritten().Internal("GetId", "Enable", "IsEnabled", "Check", "IsChecked", "GetItemLabel", "SetItemLabel", "GetHelp", "SetHelp", "IsSeparator", "GetSubMenu", "GetMenu");
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
			// wxGTK has no system-wide hot keys. Their event, whose id is the one RegisterHotKey was
			// given, is documented only as an event type.
			.Guard("wxUSE_HOTKEY", "RegisterHotKey", "UnregisterHotKey")
			.AddEvent("EVT_HOTKEY", "wxEVT_HOTKEY", "wxKeyEvent", "HotKeyPressed", "Process a wxEVT_HOTKEY event (a hot key RegisterHotKey registered has been pressed, anywhere on the system). The event's Id is the hot key's id. See wxKeyEvent.")
			// Windows sends a top-level window this, which passes it on to its children, as it does
			// wxEVT_SYS_COLOUR_CHANGED. It is documented only on its event class.
			.AddEvent("EVT_SYS_METRIC_CHANGED", "wxEVT_SYS_METRIC_CHANGED", "wxSysMetricChangedEvent", "SystemMetricChanged", "Process a wxEVT_SYS_METRIC_CHANGED event (a system metric, which SystemSettings.GetMetric() gives, has changed). Only Windows sends it, when the cursor size changes. See wxSysMetricChangedEvent.")
			// Abbreviations .NET spells out, as its naming guidelines ask.
			.Rename("GetScrollPos", "GetScrollPosition")
			.Rename("SetScrollPos", "SetScrollPosition")
			.Property("UsesBackgroundColor", "UseBgCol"));
		tweaks.GuardEventType("wxUSE_HOTKEY", "wxEVT_HOTKEY");
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
			.AddEvent("EVT_CLOSE", "wxEVT_CLOSE_WINDOW", "wxCloseEvent", "Closing")
			// The system tells top-level windows when it suspends and resumes, when a display changes,
			// and, on Windows, as the user resizes one. wxWidgets documents these events on their event
			// classes, or only as event types. It never sends a suspend that can be vetoed or canceled,
			// wxEVT_POWER_SUSPENDING and wxEVT_POWER_SUSPEND_CANCEL, on Windows since Vista, or anywhere else.
			.AddEvent("EVT_POWER_SUSPENDED", "wxEVT_POWER_SUSPENDED", "wxPowerEvent", "PowerSuspended", "System is about to suspend: normally the application should quickly (i.e. without user intervention) close all the open files and network connections here, possibly remembering them to reopen them later when the system is resumed. Linux and macOS only send it while PowerResource.Acquire(PowerResourceKind.System, reason, PowerBlockKind.Delay) holds the system. See wxPowerEvent.")
			.AddEvent("EVT_POWER_RESUME", "wxEVT_POWER_RESUME", "wxPowerEvent", "PowerResumed", "System resumed from suspend: normally the application should restore the state in which it had been before the suspension. Linux and macOS only send it while PowerResource.Acquire(PowerResourceKind.System, reason, PowerBlockKind.Delay) holds the system. See wxPowerEvent.")
			.AddEvent("EVT_DISPLAY_CHANGED", "wxEVT_DISPLAY_CHANGED", "wxDisplayChangedEvent", "DisplayChanged", "Process a wxEVT_DISPLAY_CHANGED event (the display resolution has changed). Only Windows sends it. See wxDisplayChangedEvent.")
			.AddEvent("EVT_SIZING", "wxEVT_SIZING", "wxSizeEvent", "SizeChanging", "Process a wxEVT_SIZING event (the user is resizing the window). The event's Rect is the window's new bounds, in screen coordinates, and a handler can set it to other bounds. Only Windows sends it. See wxSizeEvent."));
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
