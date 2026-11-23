namespace WxKit.Generator.Tweaks;

// The desktop: notifications, taskbar icons, displays, input, timers, and the free functions the
// hand-written Desktop, Keyboard, Mouse, and BusyCursor call.
internal static class Desktop {
	public static void Apply(Binding.Tweaks tweaks) {
		// Notifications, which the application frees with Dispose, as it does timers. Only wxMSW has toasts.
		// The icons a notification or an info bar shows, which are wxWidgets's message icons, without
		// the synonyms and the masks.
		tweaks.ConstantEnum("MessageIcon", "wxICON_", excludePrefixes: ["wxICON_EXCLAMATION", "wxICON_HAND", "wxICON_STOP", "wxICON_ASTERISK", "wxICON_MASK", "wxICON_AUTH_NEEDED", "wxICON_DEFAULT", "wxICON_SCREEN_"]);
		tweaks.Class("wxNotificationMessage", notification => notification
			.Guard("defined(__WXMSW__)", "MSWUseToasts")
			.ParameterEnum("wxNotificationMessage", "flags", "MessageIcon")
			.ParameterEnum("SetFlags", "flags", "MessageIcon")
			.ParameterName("wxNotificationMessage", "flags", "icon")
			.ParameterName("AddAction", "actionid", "actionId")
			.Event("EVT_NOTIFICATION_MESSAGE_CLICK", "Clicked")
			.Event("EVT_NOTIFICATION_MESSAGE_DISMISSED", "Dismissed")
			.Event("EVT_NOTIFICATION_MESSAGE_ACTION", "ActionSelected"));
		// Taskbar icons, which are made and freed by hand, since the shim's own class shows the menu
		// .NET gives it, and Destroy must only be called once, which Dispose sees to.
		tweaks.Enum("wxTaskBarIconType", "TaskBarIconType");
		tweaks.Class("wxTaskBarIcon", icon => icon
			.NoConstructors()
			.Ignore("Destroy")
			.Event("EVT_TASKBAR_MOVE", "MouseMoved")
			.Event("EVT_TASKBAR_LEFT_DOWN", "LeftDown")
			.Event("EVT_TASKBAR_LEFT_UP", "LeftUp")
			.Event("EVT_TASKBAR_RIGHT_DOWN", "RightDown")
			.Event("EVT_TASKBAR_RIGHT_UP", "RightUp")
			.Event("EVT_TASKBAR_LEFT_DCLICK", "LeftDoubleClick")
			.Event("EVT_TASKBAR_RIGHT_DCLICK", "RightDoubleClick")
			.Event("EVT_TASKBAR_CLICK", "Click"));
		tweaks.Class("wxTaskBarIconEvent");
		// Free functions that the hand-written Desktop, Keyboard, Mouse, and BusyCursor call.
		tweaks.Functions("Desktop", "wx/utils.h", "wxBell", "wxLaunchDefaultBrowser", "wxLaunchDefaultApplication", "wxGetKeyState", "wxGetMousePosition");
		tweaks.Functions("BusyCursor", "wx/utils.h", "wxBeginBusyCursor", "wxEndBusyCursor", "wxIsBusy");
		// Classes that aren't wxObjects, which the hand-written Display, UIActionSimulator,
		// SingleInstanceChecker, and WindowDisabler call.
		tweaks.Class("wxDisplay", display => display.Plain().Only("wxDisplay", "GetCount", "GetFromPoint", "GetFromWindow", "GetGeometry", "GetClientArea", "GetName", "IsPrimary", "GetScaleFactor"));
		tweaks.Class("wxUIActionSimulator", simulator => simulator.Plain().Only("wxUIActionSimulator", "MouseMove", "MouseDown", "MouseUp", "MouseClick", "MouseDblClick", "KeyDown", "KeyUp", "Char", "Text", "Select"));
		tweaks.Class("wxSingleInstanceChecker", checker => checker.Plain().Only("wxSingleInstanceChecker", "Create", "IsAnotherRunning"));
		tweaks.Class("wxWindowDisabler").Plain();
		// The hand-written Timer, which makes a timer its own owner, so it sends its events to itself.
		tweaks.Class("wxTimer", timer => timer.InteropOnly().Only("wxTimer", "SetOwner", "Start", "StartOnce", "Stop", "IsRunning"));
	}
}
