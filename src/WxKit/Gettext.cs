using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace WxKit;

/// <summary>
/// Shortcuts with gettext's usual names for <see cref="Translations"/>' lookups, for
/// <c>using static WxKit.Gettext;</c>: <see cref="_(string)"/> for a string, <see cref="_n(string, string, long)"/>
/// for a count, <see cref="_p(string, string)"/> for a string in a context, and
/// <see cref="_pn(string, string, string, long)"/> for both.
/// </summary>
/// <remarks>
/// Pass string literals, so that the strings can be extracted for translators. The overloads that
/// take arguments format the translation with them, in the current culture, as string.Format
/// does. The ones that do not leave braces as they are.
/// </remarks>
[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "gettext's names, which extractors know.")]
public static class Gettext {
	/// <summary>The translation of text.</summary>
	public static string _(string text) => Translations.Translate(text);

	/// <summary>The translation of text, formatted with arguments.</summary>
	public static string _(string text, params object?[] args) => Format(Translations.Translate(text), args);

	/// <summary>The form of a string for a count.</summary>
	public static string _n(string singular, string plural, long n) => Translations.TranslatePlural(singular, plural, n);

	/// <summary>The form of a string for a count, formatted with arguments, which usually include the count.</summary>
	public static string _n(string singular, string plural, long n, params object?[] args) => Format(Translations.TranslatePlural(singular, plural, n), args);

	/// <summary>The translation of text in a context.</summary>
	public static string _p(string context, string text) => Translations.TranslateInContext(context, text);

	/// <summary>The translation of text in a context, formatted with arguments.</summary>
	public static string _p(string context, string text, params object?[] args) => Format(Translations.TranslateInContext(context, text), args);

	/// <summary>The form of a string in a context for a count.</summary>
	public static string _pn(string context, string singular, string plural, long n) => Translations.TranslatePluralInContext(context, singular, plural, n);

	/// <summary>The form of a string in a context for a count, formatted with arguments.</summary>
	public static string _pn(string context, string singular, string plural, long n, params object?[] args) => Format(Translations.TranslatePluralInContext(context, singular, plural, n), args);

	private static string Format(string format, object?[] args) {
		ArgumentNullException.ThrowIfNull(args);
		return string.Format(CultureInfo.CurrentCulture, format, args);
	}
}
