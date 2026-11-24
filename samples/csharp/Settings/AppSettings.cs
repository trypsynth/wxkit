using System.Globalization;
using WxKit;
using static WxKit.Gettext;

namespace Settings;

// What the sample remembers, as lines of key=value in a file of its own.
internal sealed class AppSettings {
	// The themes' names, in the current language, which the file stores the position of.
	public static string[] Themes => [_("Match the system"), _("Light"), _("Dark")];

	// The events' names, in the current language, which the file stores the positions of.
	public static string[] Events => [
		_("A file is saved"), _("A file is changed elsewhere"), _("An update is ready"), _("A backup finishes"),
		_("A backup fails"), _("A download finishes"), _("Someone shares a file"), _("A comment is added"),
		_("A reminder is due"), _("The disk is nearly full"), _("The battery is low"), _("A sync conflict is found"),
	];

	public static string DefaultPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WxKit Settings sample", "settings.txt");

	public string Name { get; set; } = Environment.UserName;

	// An index into Themes.
	public int Theme { get; set; }

	public Color Accent { get; set; } = new(0, 120, 215);

	// The font's description, as Font.NativeFontInfoDescription gives it, or empty for the default font.
	public string Font { get; set; } = string.Empty;

	public int RecentFiles { get; set; } = 10;

	public string DocumentsFolder { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

	public bool StartWithSystem { get; set; }

	// The language, such as de, or empty for the user's preferred language that there is a catalog for.
	public string Language { get; set; } = string.Empty;

	// The indexes in Events of the events to be notified of.
	public HashSet<int> Notifications { get; } = [0, 2, 4];

	// Reads the settings, or gives the defaults when there are none yet or the file cannot be read.
	public static AppSettings Load(string path) {
		AppSettings settings = new();
		string[] lines;
		try {
			lines = File.ReadAllLines(path);
		} catch (Exception error) when (error is IOException or UnauthorizedAccessException) {
			return settings;
		}
		Dictionary<string, string> values = new(StringComparer.Ordinal);
		foreach (string line in lines) {
			int equals = line.IndexOf('=');
			if (equals > 0) values[line[..equals]] = line[(equals + 1)..];
		}
		if (values.TryGetValue("name", out string? name)) settings.Name = name;
		if (values.TryGetValue("theme", out string? theme) && int.TryParse(theme, CultureInfo.InvariantCulture, out int themeIndex) && themeIndex >= 0 && themeIndex < Themes.Length) settings.Theme = themeIndex;
		if (values.TryGetValue("accent", out string? accent) && accent.Length == 7 && int.TryParse(accent.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int rgb)) settings.Accent = new Color((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
		if (values.TryGetValue("font", out string? font)) settings.Font = font;
		if (values.TryGetValue("recentFiles", out string? recent) && int.TryParse(recent, CultureInfo.InvariantCulture, out int count)) settings.RecentFiles = count;
		if (values.TryGetValue("documents", out string? documents)) settings.DocumentsFolder = documents;
		if (values.TryGetValue("startWithSystem", out string? start)) settings.StartWithSystem = start == "true";
		if (values.TryGetValue("language", out string? language)) settings.Language = language;
		if (values.TryGetValue("notifications", out string? notifications)) {
			settings.Notifications.Clear();
			foreach (string item in notifications.Split('|', StringSplitOptions.RemoveEmptyEntries)) {
				if (int.TryParse(item, CultureInfo.InvariantCulture, out int index) && index >= 0 && index < Events.Length) settings.Notifications.Add(index);
			}
		}
		return settings;
	}

	public void Save(string path) {
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllLines(path, [
			$"name={Name}",
			$"theme={Theme.ToString(CultureInfo.InvariantCulture)}",
			$"accent=#{Accent.R:X2}{Accent.G:X2}{Accent.B:X2}",
			$"font={Font}",
			$"recentFiles={RecentFiles.ToString(CultureInfo.InvariantCulture)}",
			$"documents={DocumentsFolder}",
			$"startWithSystem={(StartWithSystem ? "true" : "false")}",
			$"language={Language}",
			$"notifications={string.Join('|', Notifications.Order().Select(static index => index.ToString(CultureInfo.InvariantCulture)))}",
		]);
	}
}
