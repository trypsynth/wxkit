using WxKit.Interop;

namespace WxKit;

/// <summary>
/// What an About box shows about the application. Wraps wxAboutDialogInfo. Anything left null or
/// empty is left out.
/// </summary>
public sealed class AboutDialogInfo {
	/// <summary>The application's name, or null for the name wxWidgets gives it, which is the executable's.</summary>
	public string? Name { get; set; }

	/// <summary>The version, such as 1.2, shown under the name.</summary>
	public string? Version { get; set; }

	/// <summary>A longer form of the version, such as "Version 1.2.3 (build 456)", where the platform shows one.</summary>
	public string? LongVersion { get; set; }

	/// <summary>A sentence or two about what the application does.</summary>
	public string? Description { get; set; }

	/// <summary>The copyright line, such as "(c) 2026 Example".</summary>
	public string? Copyright { get; set; }

	/// <summary>The license's text. On Windows and macOS, a license makes wxWidgets show its own About box, rather than the platform's.</summary>
	public string? License { get; set; }

	/// <summary>The application's web site.</summary>
	public string? WebSite { get; set; }

	/// <summary>The text of the link to the web site, or null for its address.</summary>
	public string? WebSiteDescription { get; set; }

	/// <summary>The icon, or null for the application's own.</summary>
	public Icon? Icon { get; set; }

	public IList<string> Developers { get; } = [];

	public IList<string> DocumentationWriters { get; } = [];

	public IList<string> Artists { get; } = [];

	public IList<string> Translators { get; } = [];
}

/// <summary>Shows an application's About box. Wraps wxAboutBox.</summary>
public static unsafe class AboutBox {
	/// <summary>
	/// Shows the platform's About box with what <paramref name="info"/> gives, or wxWidgets's own
	/// when the platform's cannot show all of it. On Windows, it is modal, and returns once it is
	/// closed. On Linux and macOS, it returns at once.
	/// </summary>
	public static void Show(AboutDialogInfo info, Window? parent = null) {
		ArgumentNullException.ThrowIfNull(info);
		Application.VerifyAccess();
		using NativeStringArray developers = new(info.Developers);
		using NativeStringArray docWriters = new(info.DocumentationWriters);
		using NativeStringArray artists = new(info.Artists);
		using NativeStringArray translators = new(info.Translators);
		WxkAboutInfo about = new() {
			Name = NativeString.Create(info.Name),
			Version = NativeString.Create(info.Version),
			LongVersion = NativeString.Create(info.LongVersion),
			Description = NativeString.Create(info.Description),
			Copyright = NativeString.Create(info.Copyright),
			License = NativeString.Create(info.License),
			WebSite = NativeString.Create(info.WebSite),
			WebSiteDescription = NativeString.Create(info.WebSiteDescription),
			Developers = info.Developers.Count > 0 ? developers.Handle : null,
			DocWriters = info.DocumentationWriters.Count > 0 ? docWriters.Handle : null,
			Artists = info.Artists.Count > 0 ? artists.Handle : null,
			Translators = info.Translators.Count > 0 ? translators.Handle : null,
			Icon = info.Icon is null ? null : info.Icon.Native,
		};
		WxKitNative.AboutBoxShow(&about, Window.NativeOf(parent));
		GC.KeepAlive(info.Icon);
	}
}
