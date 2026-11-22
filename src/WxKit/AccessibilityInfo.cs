using WxKit.Interop;

namespace WxKit;

/// <summary>
/// What screen readers learn of a window, read from the platform's accessibility API as a screen
/// reader reads it: MSAA on Windows, which NVDA and JAWS use for standard controls,
/// NSAccessibility on macOS, which VoiceOver uses, and ATK on Linux, which Orca uses. An
/// application's own tests can check with it that each control has the role and the name a user
/// needs, such as a text box named by the label before it.
/// </summary>
/// <param name="Role">Its role, from the platform's, or <see cref="AccessibleRole.None"/> for one WxKit does not map, which <paramref name="PlatformRole"/> still names.</param>
/// <param name="Name">The name a screen reader announces it by, which may be empty.</param>
/// <param name="PlatformRole">The platform's own name for its role, such as push button on Windows and Linux, and AXButton on macOS.</param>
/// <param name="IsEnabled">Whether it can be used.</param>
/// <param name="IsFocused">Whether it has the keyboard focus.</param>
/// <param name="IsChecked">Whether it is checked, for a check box or a radio button.</param>
public sealed unsafe record AccessibilityInfo(AccessibleRole Role, string Name, string PlatformRole, bool IsEnabled, bool IsFocused, bool IsChecked) {
	/// <summary>What screen readers learn of <paramref name="window"/>.</summary>
	/// <exception cref="InvalidOperationException">The platform's accessibility API had nothing for it.</exception>
	public static AccessibilityInfo Of(Window window) {
		Application.VerifyAccess();
		ArgumentNullException.ThrowIfNull(window);
		int role;
		uint states;
		WxkString* name;
		WxkString* platformRole;
		if (WxKitNative.AccessibilityInfoGet((WxkWindow*)window.Native, &role, &states, &name, &platformRole) == 0) throw new InvalidOperationException("The platform's accessibility API had nothing for the window.");
		return new AccessibilityInfo((AccessibleRole)role, NativeString.Take(name), NativeString.Take(platformRole), (states & 1) != 0, (states & 2) != 0, (states & 4) != 0);
	}
}
