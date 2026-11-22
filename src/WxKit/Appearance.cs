using WxKit.Interop;

namespace WxKit;

/// <summary>Light or dark, for <see cref="Application.SetAppearance"/>.</summary>
public enum Appearance {
	/// <summary>The system's, light or dark as the user set it.</summary>
	System,
	/// <summary>Light, whatever the system's is.</summary>
	Light,
	/// <summary>Dark, whatever the system's is.</summary>
	Dark,
}

public unsafe partial class Application {
	/// <summary>
	/// Shows the application light or dark, or as the system is, which affects the windows open
	/// already and those made after. macOS and Linux follow the system by default. Windows does
	/// not, since its dark mode for applications is undocumented and has limits, so an application
	/// that wants it calls this with <see cref="Appearance.System"/> or <see cref="Appearance.Dark"/>.
	/// Returns false when the system can't change it, such as when GTK_THEME is set on Linux.
	/// Call it on the GUI thread.
	/// </summary>
	public static bool SetAppearance(Appearance appearance) {
		if (!Enum.IsDefined(appearance)) throw new ArgumentOutOfRangeException(nameof(appearance));
		VerifyAccess();
		return WxKitNative.AppSetAppearance((int)appearance) == 1;
	}
}

/// <summary>
/// The system's appearance, light or dark, as it was when it was asked for. The user can change
/// it while the application runs.
/// </summary>
/// <param name="Name">The system's name for its theme, such as Aqua on macOS, which may be empty.</param>
/// <param name="IsDark">Whether the system is dark.</param>
/// <param name="IsUsingDarkBackground">Whether windows have dark backgrounds, which a high contrast theme can have while the system isn't dark.</param>
/// <param name="AreAppsDark">Whether the user asked applications to be dark, which on Windows is a setting of its own.</param>
public sealed record SystemAppearance(string Name, bool IsDark, bool IsUsingDarkBackground, bool AreAppsDark);

public static unsafe partial class SystemSettings {
	/// <summary>The system's appearance, light or dark.</summary>
	public static SystemAppearance Appearance {
		get {
			Application.VerifyAccess();
			int flags = WxKitNative.SystemAppearanceGetFlags();
			return new SystemAppearance(NativeString.Take(WxKitNative.SystemAppearanceGetName()), (flags & 1) != 0, (flags & 2) != 0, (flags & 4) != 0);
		}
	}
}
