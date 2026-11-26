namespace WxKit.Generator.Tweaks;

// The events windows send, and the keys and modifiers they name. A control's own events are with
// the control.
internal static class Events {
	public static void Apply(Binding.Tweaks tweaks) {
		tweaks.Class("wxEvent", e => e.NoConstructors().Rename("GetSkipped", "IsSkipped"));
		// An event does not delete the item data it is given, so only the item's data comes out.
		tweaks.Class("wxCommandEvent").Ignore("SetClientObject");
		// Veto is written by hand, since it refuses when the close cannot be vetoed.
		tweaks.Class("wxCloseEvent", e => e.Ignore("Veto").Rename("GetVeto", "IsVetoed").Property("IsLoggingOff", "GetLoggingOff", "SetLoggingOff"));
		// Booleans are named as IsChecked is, and whether a handler set a state as IsCheckedSet is,
		// rather than as SetChecked, which reads as a command.
		tweaks.Class("wxActivateEvent").Property("IsActive", "GetActive");
		// An update event's Checked keeps its name, since IsChecked is its base's, which says
		// whether the control that sent a command is checked.
		tweaks.Class("wxUpdateUIEvent", e => e
			.Property("IsEnabled", "GetEnabled")
			.Property("IsShown", "GetShown")
			.Property("IsCheckedSet", "GetSetChecked")
			.Property("IsEnabledSet", "GetSetEnabled")
			.Property("IsShownSet", "GetSetShown")
			.Property("IsTextSet", "GetSetText"));
		tweaks.Class("wxFont").Property("IsUnderlined", "GetUnderlined", "SetUnderlined");
		tweaks.Class("wxHyperlinkCtrl").Property("IsVisited", "GetVisited", "SetVisited");
		tweaks.Class("wxPrinter").Property("IsAborted", "GetAbort");
		// The keys a key event names, which a letter's or digit's code is the character of, as 'A' is.
		tweaks.Enum("wxKeyCode", "KeyCode");
		// Enums an event class nests, whose names say nothing outside it.
		tweaks.Enum("wxActivateEvent::Reason", "ActivationReason");
		tweaks.Enum("wxHelpEvent::Origin", "HelpOrigin");
		// Which way, and how, the focus moves, as Tab and Shift+Tab move it.
		tweaks.Enum("wxNavigationKeyEvent::wxNavigationKeyEventFlags", "NavigationFlags", isFlags: true, memberPrefix: "Is");
		// Whether the focus moves forward, a bool, which a Direction property would read as the enum.
		tweaks.Class("wxNavigationKeyEvent").Property("IsForward", "GetDirection", "SetDirection");
		tweaks.Enum("wxKeyModifier", "KeyModifiers", isFlags: true);
		tweaks.Class("wxKeyEvent", e => e.ParameterEnum("GetKeyCode", "return", "wxKeyCode").ParameterEnum("GetModifiers", "return", "wxKeyModifier"));
		// Button(button) asks whether the event came from that button. GetButton is the Button
		// property, and IsButton is taken too.
		tweaks.Class("wxMouseEvent", e => e.ParameterEnum("GetModifiers", "return", "wxKeyModifier").ParameterEnum("GetButton", "return", "wxMouseButton").Rename("Button", "IsFromButton"));
		// Numbers wxWidgets documents as one of an enum's values are that enum.
		foreach (string scroll in (string[])["wxScrollEvent", "wxScrollWinEvent"]) tweaks.Class(scroll, e => e.ParameterEnum("GetOrientation", "return", "Orientation").ParameterEnum("SetOrientation", "orient", "Orientation"));
		tweaks.Class("wxListEvent", e => e.ParameterEnum("GetKeyCode", "return", "wxKeyCode").ParameterEnum("SetKeyCode", "code", "wxKeyCode"));
		tweaks.Class("wxTreeEvent", e => e.ParameterEnum("GetKeyCode", "return", "wxKeyCode"));
		tweaks.Class("wxWindow", window => {
			foreach (string member in (string[])["CanScroll", "HasScrollbar", "IsScrollbarAlwaysShown"]) window.ParameterEnum(member, "orient", "Orientation");
			foreach (string member in (string[])["GetScrollPos", "GetScrollRange", "GetScrollThumb", "SetScrollPos", "SetScrollbar"]) window.ParameterEnum(member, "orientation", "Orientation");
			window.ParameterEnum("RegisterHotKey", "modifiers", "wxKeyModifier");
		});
		foreach (string eventClass in (string[])[
			"wxNotifyEvent", "wxKeyEvent", "wxSizeEvent", "wxMoveEvent", "wxFocusEvent",
			"wxChildFocusEvent", "wxPaintEvent", "wxShowEvent", "wxIconizeEvent", "wxMaximizeEvent",
			"wxActivateEvent", "wxIdleEvent", "wxUpdateUIEvent", "wxContextMenuEvent", "wxHelpEvent",
			"wxSysColourChangedEvent", "wxDPIChangedEvent", "wxInitDialogEvent", "wxMenuEvent",
			"wxFullScreenEvent", "wxNavigationKeyEvent", "wxTimerEvent", "wxScrollWinEvent", "wxEraseEvent",
			"wxSpinEvent", "wxSpinDoubleEvent", "wxScrollEvent", "wxHyperlinkEvent", "wxBookCtrlEvent",
			"wxSplitterEvent", "wxCollapsiblePaneEvent",
		]) {
			tweaks.Class(eventClass);
		}
		// Files dropped on a window that accepts them. Files is written by hand, since GetFiles gives a C array.
		tweaks.Class("wxDropFilesEvent", e => e.Ignore("GetFiles"));
		// Bound by hand-written classes: menu items' clicks and timers' ticks.
		tweaks.EventType("wxEVT_MENU", "wxCommandEvent");
		tweaks.EventType("wxEVT_TIMER", "wxTimerEvent");
		// The headers list the context menu among the command events, though it's sent as a
		// wxContextMenuEvent, which says where the menu goes, as when the keyboard opens it.
		tweaks.EventType("wxEVT_CONTEXT_MENU", "wxContextMenuEvent");
		// A window asks for its cursor with a wxSetCursorEvent, whose handler gives one.
		tweaks.Class("wxSetCursorEvent");
		tweaks.EventType("wxEVT_SET_CURSOR", "wxSetCursorEvent");
		// The headers give the color dialog's event the color picker's class, which it is not.
		tweaks.EventType("wxEVT_COLOUR_CHANGED", "wxColourDialogEvent");
	}
}
