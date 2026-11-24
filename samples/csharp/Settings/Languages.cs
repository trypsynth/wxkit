using WxKit;

namespace Settings;

// The languages the sample is translated into, from its catalogs in po, which its build compiles
// and embeds, with wxWidgets' own for the same languages.
internal static class Languages {
	// The name of the sample's catalogs, its assembly's name, as in po/de.po's compiled Settings.mo.
	private const string Domain = "Settings";

	// The language the sample's text is in, such as de, or en for its original English.
	public static string Current => Translations.Current?.GetBestTranslation(Domain) ?? "en";

	// Whether that language is written right to left, as Hebrew and Arabic are, so that windows lay
	// out from the right. A window takes the direction before its contents are made.
	public static bool IsRightToLeft => LanguageInfo.Find(Current)?.LayoutDirection == LayoutDirection.RightToLeft;

	// Translates the sample, and wxWidgets' own text, such as stock button labels, into a language,
	// or into the user's preferred one that there is a catalog for when it is empty. Windows made
	// before keep the text they were made with.
	public static void Use(string language) => Translations.Use(Domain, language.Length == 0 ? null : language);

	// The languages there are catalogs for, and English, each named in itself, as a language picker
	// names them, so that a user who cannot read the current one finds their own.
	public static List<(string Code, string Name)> Available() {
		IEnumerable<string> codes = Translations.Current?.GetAvailableTranslations(Domain) ?? [];
		return [.. codes.Prepend("en").Distinct().Select(static code => (code, LanguageInfo.Find(code)?.NativeName ?? code)).OrderBy(static language => language.Item2, StringComparer.CurrentCultureIgnoreCase)];
	}
}
