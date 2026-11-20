using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WxKit.Interop;

namespace WxKit;

/// <summary>
/// The catalogs an application's text is translated from, and the language they are in. Wraps
/// wxTranslations, which wxWidgets uses for its own text too, such as stock button labels, so both
/// follow one setting.
/// </summary>
/// <remarks>
/// <para>
/// Set translations up in <see cref="Application.Run(Action)"/>'s start-up action, before making any
/// windows, because a window's text is translated when it is made. <see cref="Use"/> does it in one
/// call. To set them up step by step, make a <see cref="Translations"/>, set its
/// <see cref="Language"/>, add its catalogs, and make it <see cref="Current"/>. To change the
/// language later, do the same again, and make the windows again.
/// </para>
/// <para>
/// Catalogs are .mo files, compiled from gettext's .po files. A loader finds them, and by default
/// that is an <see cref="EmbeddedTranslationsLoader"/>, which reads the ones embedded in the
/// application. When it has none, wxWidgets looks for a file, as
/// locale/&lt;language&gt;/&lt;domain&gt;.mo next to the application, among other places.
/// </para>
/// <para>
/// Looking strings up, as <see cref="Translate"/> and <see cref="Gettext"/>'s shortcuts do, works
/// from any thread. Everything else is for the GUI thread.
/// </para>
/// </remarks>
public sealed unsafe class Translations : IDisposable {
	private static readonly Lock Gate = new();
	private static Translations? _current;
	private static bool _callbacksSet;
	private WxkTranslations* _native;
	private string? _language;

	/// <summary>Makes translations whose catalogs come from those embedded in the application, or from files.</summary>
	public Translations() : this(new EmbeddedTranslationsLoader()) { }

	/// <summary>Makes translations whose catalogs come from a loader, or from files when it has none.</summary>
	public Translations(TranslationsLoader loader) {
		ArgumentNullException.ThrowIfNull(loader);
		Application.VerifyAccess();
		SetUp();
		// The native loader holds the managed one strongly, and frees the handle when it is deleted.
		GCHandle handle = GCHandle.Alloc(loader);
		_native = WxKitNative.TranslationsNew((void*)GCHandle.ToIntPtr(handle));
		if (_native is null) {
			handle.Free();
			throw new InvalidOperationException("wxWidgets could not make the translations.");
		}
	}

	/// <summary>
	/// The translations wxWidgets and the application look strings up in, or null for none, which
	/// leaves every string as it is. Setting it makes wxWidgets own the translations, and disposes
	/// the ones before.
	/// </summary>
	public static Translations? Current {
		get {
			lock (Gate) return _current;
		}
		set {
			Application.VerifyAccess();
			if (value is not null) _ = value.Native;
			lock (Gate) {
				if (ReferenceEquals(value, _current)) return;
				WxKitNative.TranslationsSetCurrent(value is null ? null : value._native);
				// wxWidgets deleted the translations before.
				_current?._native = null;
				_current = value;
			}
		}
	}

	/// <summary>
	/// The language to translate into, such as "de" or "pt_BR", or null for the best one the user
	/// prefers that there are catalogs for. Set it before adding catalogs, which are loaded in the
	/// language set when they are added.
	/// </summary>
	public string? Language {
		get => _language;
		set {
			Application.VerifyAccess();
			fixed (char* text = value) WxKitNative.TranslationsSetLanguage(Native, text, value is null ? 0 : NativeString.Length(value));
			_language = value;
		}
	}

	private WxkTranslations* Native => _native is not null ? _native : throw new ObjectDisposedException(nameof(Translations));

	/// <summary>
	/// Sets up translations from the system's language, or another, and makes them current: wxWidgets'
	/// own catalog, and the application's, whose domain is the name of its catalogs.
	/// </summary>
	/// <param name="domain">The name of the application's catalogs, as in myapp.mo.</param>
	/// <param name="language">The language, or null for the best one the user prefers that there are catalogs for.</param>
	/// <returns>The translations, which are now <see cref="Current"/>.</returns>
	public static Translations Use(string domain, string? language = null) {
		ArgumentException.ThrowIfNullOrEmpty(domain);
		Translations translations = new();
		try {
			translations.Language = language;
			translations.AddStdCatalog();
			translations.AddCatalog(domain);
		} catch {
			translations.Dispose();
			throw;
		}
		Current = translations;
		return translations;
	}

	/// <summary>Adds wxWidgets' own catalog, and returns whether there was one for the language.</summary>
	public bool AddStdCatalog() {
		Application.VerifyAccess();
		return WxKitNative.TranslationsAddStdCatalog(Native) != 0;
	}

	/// <summary>
	/// Adds the catalog of a domain, and returns whether there was one for the language, or the
	/// language is English, which the strings are in.
	/// </summary>
	public bool AddCatalog(string domain) {
		ArgumentException.ThrowIfNullOrEmpty(domain);
		Application.VerifyAccess();
		fixed (char* text = domain) return WxKitNative.TranslationsAddCatalog(Native, text, NativeString.Length(domain)) != 0;
	}

	/// <summary>Whether the catalog of a domain was added.</summary>
	public bool IsLoaded(string domain) {
		ArgumentException.ThrowIfNullOrEmpty(domain);
		Application.VerifyAccess();
		fixed (char* text = domain) return WxKitNative.TranslationsIsLoaded(Native, text, NativeString.Length(domain)) != 0;
	}

	/// <summary>The languages there are catalogs of a domain for, such as for a language picker.</summary>
	public IReadOnlyList<string> GetAvailableTranslations(string domain) {
		ArgumentException.ThrowIfNullOrEmpty(domain);
		Application.VerifyAccess();
		fixed (char* text = domain) return NativeStringArray.Take(WxKitNative.TranslationsGetAvailableTranslations(Native, text, NativeString.Length(domain)));
	}

	/// <summary>
	/// The language a domain's catalog is loaded in, or would be: <see cref="Language"/> when it is
	/// set, or else the best one the user prefers that there is a catalog for, or null when there is none.
	/// </summary>
	public string? GetBestTranslation(string domain) {
		ArgumentException.ThrowIfNullOrEmpty(domain);
		Application.VerifyAccess();
		fixed (char* text = domain) {
			WxkString* best = WxKitNative.TranslationsGetBestTranslation(Native, text, NativeString.Length(domain));
			return best is null ? null : NativeString.Take(best);
		}
	}

	/// <summary>A header of a loaded catalog, such as "Last-Translator", or null when it has none.</summary>
	/// <param name="header">The header's name.</param>
	/// <param name="domain">The catalog's domain, or empty for the first catalog that has the header.</param>
	public string? GetHeaderValue(string header, string domain = "") {
		ArgumentException.ThrowIfNullOrEmpty(header);
		ArgumentNullException.ThrowIfNull(domain);
		Application.VerifyAccess();
		fixed (char* headerText = header)
		fixed (char* domainText = domain) {
			WxkString* value = WxKitNative.TranslationsGetHeaderValue(Native, headerText, NativeString.Length(header), domainText, NativeString.Length(domain));
			return value is null ? null : NativeString.Take(value);
		}
	}

	/// <summary>
	/// Adds a folder wxWidgets looks for catalog files in, as &lt;folder&gt;/&lt;language&gt;/&lt;domain&gt;.mo.
	/// The locale folder next to the application is always one.
	/// </summary>
	public static void AddCatalogLookupPathPrefix(string folder) {
		ArgumentException.ThrowIfNullOrEmpty(folder);
		Application.VerifyAccess();
		fixed (char* text = folder) WxKitNative.TranslationsAddCatalogLookupPathPrefix(text, NativeString.Length(folder));
	}

	/// <summary>The translation of text in the current translations, or the text itself when there is none.</summary>
	public static string Translate(string text) => Lookup(text, null);

	/// <summary>
	/// The translation of text in a context, which tells apart two strings that are the same in
	/// English, as msgctxt does, or the text itself when there is none.
	/// </summary>
	public static string TranslateInContext(string context, string text) {
		ArgumentNullException.ThrowIfNull(context);
		return Lookup(text, context);
	}

	/// <summary>
	/// The form of a string for a count, from the current translations, in the plural forms of their
	/// language, or else the singular for 1 and the plural for anything else, as in English.
	/// </summary>
	public static string TranslatePlural(string singular, string plural, long n) => LookupPlural(singular, plural, n, null);

	/// <summary>The form of a string in a context for a count, as <see cref="TranslatePlural"/> and <see cref="TranslateInContext"/> find them.</summary>
	public static string TranslatePluralInContext(string context, string singular, string plural, long n) {
		ArgumentNullException.ThrowIfNull(context);
		return LookupPlural(singular, plural, n, context);
	}

	/// <summary>Deletes the translations, and when they are current, leaves none.</summary>
	public void Dispose() {
		Application.VerifyAccess();
		if (ReferenceEquals(Current, this)) {
			Current = null;
			return;
		}
		if (_native is null) return;
		WxKitNative.TranslationsDelete(_native);
		_native = null;
	}

	private static string Lookup(string text, string? context) {
		ArgumentNullException.ThrowIfNull(text);
		WxkString* translated;
		lock (Gate) {
			fixed (char* textData = text)
			fixed (char* contextData = context) translated = WxKitNative.TranslationsGetString(textData, NativeString.Length(text), contextData, context is null ? 0 : NativeString.Length(context));
		}
		return translated is null ? text : NativeString.Take(translated);
	}

	private static string LookupPlural(string singular, string plural, long n, string? context) {
		ArgumentNullException.ThrowIfNull(singular);
		ArgumentNullException.ThrowIfNull(plural);
		uint count = PluralCount(n);
		WxkString* translated;
		lock (Gate) {
			fixed (char* singularData = singular)
			fixed (char* contextData = context) translated = WxKitNative.TranslationsGetPluralString(singularData, NativeString.Length(singular), count, contextData, context is null ? 0 : NativeString.Length(context));
		}
		if (translated is not null) return NativeString.Take(translated);
		return count == 1 ? singular : plural;
	}

	// gettext's plural rules take an unsigned count, and look at its last digits, so a negative count
	// uses its magnitude, and one past uint.MaxValue keeps its last six digits, and stays large.
	internal static uint PluralCount(long n) {
		ulong magnitude = n < 0 ? (ulong)(-(n + 1)) + 1 : (ulong)n;
		return magnitude <= uint.MaxValue ? (uint)magnitude : (uint)(magnitude % 1_000_000 + 1_000_000);
	}

	private static void SetUp() {
		if (_callbacksSet) return;
		_callbacksSet = true;
		WxkTranslationsLoaderCallbacks callbacks = new() {
			LoadCatalog = &LoadCatalog,
			AvailableTranslations = &AvailableTranslations,
			OnFree = &OnFree,
		};
		WxKitNative.TranslationsLoaderSetCallbacks(&callbacks);
		AddCatalogLookupPathPrefix(Path.Combine(AppContext.BaseDirectory, "locale"));
	}

	private static TranslationsLoader? Of(void* userdata) => (TranslationsLoader?)GCHandle.FromIntPtr((nint)userdata).Target;

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void LoadCatalog(void* userdata, WxkString* domain, WxkString* language, WxkCatalogSink* sink) {
		string domainText = NativeString.Take(domain);
		string languageText = NativeString.Take(language);
		try {
			if (Of(userdata)?.LoadCatalog(domainText, languageText) is not byte[] data || data.Length == 0) return;
			fixed (byte* bytes = data) WxKitNative.CatalogSinkSupply(sink, bytes, (nuint)data.Length);
		} catch (Exception error) {
			Application.ReportException(error);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void AvailableTranslations(void* userdata, WxkString* domain, WxkStringArray* languages) {
		string domainText = NativeString.Take(domain);
		try {
			if (Of(userdata) is not TranslationsLoader loader) return;
			foreach (string language in loader.GetAvailableTranslations(domainText)) {
				fixed (char* text = language) WxKitNative.StringArrayAdd(languages, text, NativeString.Length(language));
			}
		} catch (Exception error) {
			Application.ReportException(error);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void OnFree(void* userdata) => GCHandle.FromIntPtr((nint)userdata).Free();
}

/// <summary>
/// Finds the catalogs a <see cref="Translations"/> loads, before wxWidgets looks for files. Wraps
/// wxTranslationsLoader.
/// </summary>
public abstract class TranslationsLoader {
	/// <summary>
	/// The bytes of the .mo catalog of a domain in a language, or null when this loader has none,
	/// and wxWidgets looks for a file.
	/// </summary>
	/// <param name="domain">The catalog's domain, such as myapp, or wxstd for wxWidgets' own.</param>
	/// <param name="language">The language, such as de or pt_BR.</param>
	public abstract byte[]? LoadCatalog(string domain, string language);

	/// <summary>The languages this loader has catalogs of a domain for.</summary>
	public abstract IEnumerable<string> GetAvailableTranslations(string domain);
}

/// <summary>
/// Loads catalogs embedded in assemblies as resources named locale/&lt;language&gt;/&lt;domain&gt;.mo,
/// which WxKit's build embeds from the application's po folder. A language matches whatever its
/// case, and with either a hyphen or an underscore before its region.
/// </summary>
public sealed class EmbeddedTranslationsLoader : TranslationsLoader {
	private readonly List<(Assembly Assembly, string Resource, string Language, string Domain)> _catalogs = [];

	/// <summary>Loads the catalogs embedded in the application, its entry assembly.</summary>
	public EmbeddedTranslationsLoader() : this(Assembly.GetEntryAssembly() is Assembly entry ? [entry] : []) { }

	/// <summary>Loads the catalogs embedded in the assemblies, in their order.</summary>
	public EmbeddedTranslationsLoader(params IEnumerable<Assembly> assemblies) {
		ArgumentNullException.ThrowIfNull(assemblies);
		foreach (Assembly assembly in assemblies) {
			ArgumentNullException.ThrowIfNull(assembly, nameof(assemblies));
			foreach (string resource in assembly.GetManifestResourceNames()) {
				string[] parts = resource.Split('/');
				if (parts.Length != 3 || parts[0] != "locale" || !parts[2].EndsWith(".mo", StringComparison.Ordinal) || parts[2].Length == 3) continue;
				_catalogs.Add((assembly, resource, Normalize(parts[1]), parts[2][..^3]));
			}
		}
	}

	/// <inheritdoc/>
	public override byte[]? LoadCatalog(string domain, string language) {
		string wanted = Normalize(language);
		foreach ((Assembly assembly, string resource, string catalogLanguage, string catalogDomain) in _catalogs) {
			if (catalogDomain != domain || !string.Equals(catalogLanguage, wanted, StringComparison.OrdinalIgnoreCase)) continue;
			using Stream? stream = assembly.GetManifestResourceStream(resource);
			if (stream is null) continue;
			using MemoryStream bytes = new();
			stream.CopyTo(bytes);
			return bytes.ToArray();
		}
		return null;
	}

	/// <inheritdoc/>
	public override IEnumerable<string> GetAvailableTranslations(string domain) => _catalogs.Where(catalog => catalog.Domain == domain).Select(static catalog => catalog.Language).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

	private static string Normalize(string language) => language.Replace('-', '_');
}

/// <summary>A language, as wxWidgets knows it, such as for a language picker. Wraps wxLanguageInfo.</summary>
/// <param name="Code">Its canonical name, such as de or pt_BR.</param>
/// <param name="Tag">Its BCP 47 tag, such as de or pt-BR.</param>
/// <param name="Name">Its name in English, such as German.</param>
/// <param name="NativeName">Its name in itself, such as Deutsch.</param>
/// <param name="LayoutDirection">The direction it is written in, which windows in it are laid out in.</param>
public sealed record LanguageInfo(string Code, string Tag, string Name, string NativeName, LayoutDirection LayoutDirection) {
	/// <summary>The language a locale such as de, pt_BR, or pt-BR names, or null when wxWidgets does not know it.</summary>
	public static unsafe LanguageInfo? Find(string locale) {
		ArgumentException.ThrowIfNullOrEmpty(locale);
		Application.VerifyAccess();
		WxkLanguageInfo info;
		fixed (char* text = locale) {
			if (WxKitNative.LanguageInfoFind(text, NativeString.Length(locale), &info) == 0) return null;
		}
		return new LanguageInfo(NativeString.Take(info.CanonicalName), NativeString.Take(info.LocaleTag), NativeString.Take(info.Description), NativeString.Take(info.DescriptionNative), (LayoutDirection)info.LayoutDirection);
	}
}

/// <summary>
/// The locale wxWidgets formats numbers and dates in, and the languages the user prefers. Wraps
/// wxUILocale.
/// </summary>
public static unsafe class UILocale {
	/// <summary>The current locale's name, such as en_US, or empty for the C locale, which wxWidgets starts in.</summary>
	public static string CurrentName {
		get {
			Application.VerifyAccess();
			return NativeString.Take(WxKitNative.UILocaleGetCurrentName());
		}
	}

	/// <summary>The languages the user prefers, most preferred first, such as de-DE.</summary>
	public static IReadOnlyList<string> PreferredLanguages {
		get {
			Application.VerifyAccess();
			return NativeStringArray.Take(WxKitNative.UILocaleGetPreferredUILanguages());
		}
	}

	/// <summary>
	/// Uses the user's locale for the numbers and dates wxWidgets shows, and returns whether it could.
	/// Call it at start-up, before making any windows. It does not change the language of text,
	/// which <see cref="Translations"/> sets.
	/// </summary>
	public static bool UseDefault() {
		Application.VerifyAccess();
		return WxKitNative.UILocaleUseDefault() != 0;
	}
}
