#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

// Picks wxWidgets' own catalog, wxstd.mo, for each language an application has, to embed as
// locale/<language>/wxstd.mo, which EmbeddedTranslationsLoader finds. A language uses wxWidgets'
// catalog of the same name, or else of its base language, as de_AT uses de, or else of the first of
// that language's regions, as ko uses ko_KR, since wxWidgets names some catalogs by region only.
public sealed class WxKitStdCatalogs : Task {
	[Required]
	public string Directory { get; set; } = string.Empty;

	public ITaskItem[] Languages { get; set; } = Array.Empty<ITaskItem>();

	public bool All { get; set; }

	[Output]
	public ITaskItem[] Catalogs { get; set; } = Array.Empty<ITaskItem>();

	public override bool Execute() {
		List<string> available = System.IO.Directory.Exists(Directory) ? System.IO.Directory.GetDirectories(Directory).Where(folder => File.Exists(Path.Combine(folder, "wxstd.mo"))).Select(folder => Path.GetFileName(folder)).OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList() : new List<string>();
		List<string> wanted = All ? available : Languages.Select(language => language.ItemSpec.Replace('-', '_')).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		if (wanted.Count == 0) return true;
		if (available.Count == 0) {
			Log.LogWarning($"wxWidgets' own catalogs are not in {Directory}, so its text, such as stock button labels, stays in English. A native build from the WxKit repository has them when GNU gettext's msgfmt is installed.");
			return true;
		}
		List<ITaskItem> catalogs = new List<ITaskItem>();
		foreach (string language in wanted) {
			string? match = Match(language, available);
			if (match is null) {
				Log.LogMessage(MessageImportance.Low, $"wxWidgets has no catalog for {language}.");
				continue;
			}
			TaskItem catalog = new TaskItem(Path.GetFullPath(Path.Combine(Directory, match, "wxstd.mo")));
			catalog.SetMetadata("LogicalName", $"locale/{language}/wxstd.mo");
			catalog.SetMetadata("WithCulture", "false");
			catalogs.Add(catalog);
		}
		Catalogs = catalogs.ToArray();
		return true;
	}

	internal static string? Match(string language, List<string> available) {
		string? exact = available.FirstOrDefault(name => string.Equals(name, language, StringComparison.OrdinalIgnoreCase));
		if (exact is not null) return exact;
		string baseLanguage = language.Split('_', '@')[0];
		return available.FirstOrDefault(name => string.Equals(name, baseLanguage, StringComparison.OrdinalIgnoreCase)) ?? available.FirstOrDefault(name => name.StartsWith(baseLanguage + "_", StringComparison.OrdinalIgnoreCase));
	}
}
