namespace WxKit.Generator.Tweaks;

// Accessibility, for the hand-written Accessible, which describes a custom control to screen
// readers through Microsoft Active Accessibility. Only wxMSW declares these, under
// wxUSE_ACCESSIBILITY.
internal static class Accessibility {
	public static void Apply(Binding.Tweaks tweaks) {
		const string accessibility = "wxUSE_ACCESSIBILITY";
		tweaks.Enum("wxAccRole", "AccessibleRole", memberPrefix: "wxROLE_SYSTEM_", guard: accessibility);
		tweaks.Enum("wxAccObject", "AccessibleObject", guard: accessibility);
		tweaks.Enum("wxNavDir", "NavigationDirection", guard: accessibility);
		tweaks.Enum("wxAccSelectionFlags", "AccessibleSelection", isFlags: true, guard: accessibility);
		tweaks.ConstantEnum("AccessibleStates", "wxACC_STATE_SYSTEM_", isFlags: true, guard: accessibility);
		tweaks.ConstantEnum("AccessibleEvent", "wxACC_EVENT_", guard: accessibility);
		// The hand-written Accessible makes a director, whose callbacks it gives, which answer what a
		// screen reader asks, or leave wxWidgets to say they aren't implemented.
		tweaks.Class("wxAccessible", accessible => accessible
			.Handwritten()
			.Plain()
			.GuardClass(accessibility)
			.Internal("wxAccessible", "NotifyEvent")
			.InternalDirector("GetName", "GetDescription", "GetHelpText", "GetKeyboardShortcut", "GetDefaultAction", "GetValue", "GetRole", "GetState", "GetChildCount", "GetLocation", "GetChild", "GetFocus", "HitTest", "Navigate", "DoDefaultAction", "Select"));
	}
}
