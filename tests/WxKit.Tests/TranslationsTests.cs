using System.Text;
using static WxKit.Gettext;

namespace WxKit.Tests;

// Translations from a catalog built here, in Polish, whose three plural forms show that the
// catalog's own rules pick the form.
public class TranslationsTests {
	private const string PolishPlurals = "nplurals=3; plural=(n==1 ? 0 : n%10>=2 && n%10<=4 && (n%100<10 || n%100>=20) ? 1 : 2);";

	[Fact]
	public Task TranslatesFromALoadersCatalog() => WithPolish(() => {
		Assert.Equal("Otwórz", _("Open"));
		Assert.Equal("Otwórz", Translations.Translate("Open"));
		Assert.Equal("Not in the catalog", _("Not in the catalog"));
		Assert.Equal("Witaj, Ala", _("Hello, {0}", "Ala"));
	});

	[Fact]
	public Task TellsContextsApart() => WithPolish(() => {
		Assert.Equal("Otwarty", _p("state", "Open"));
		Assert.Equal("Otwórz", _("Open"));
		Assert.Equal("Open", _p("unknown", "Open"));
	});

	[Fact]
	public Task PicksPluralFormsByTheCatalogsRules() => WithPolish(() => {
		Assert.Equal("1 plik", _n("{0} file", "{0} files", 1, 1));
		Assert.Equal("3 pliki", _n("{0} file", "{0} files", 3, 3));
		Assert.Equal("5 plików", _n("{0} file", "{0} files", 5, 5));
		Assert.Equal("22 pliki", _n("{0} file", "{0} files", 22, 22));
		Assert.Equal("12 plików", _n("{0} file", "{0} files", 12, 12));
		Assert.Equal("{0} plików", _n("{0} file", "{0} files", 5));
	});

	[Fact]
	public Task FallsBackToEnglishWithoutTranslations() => Gui.Run(() => {
		Translations.Current = null;
		Assert.Equal("Open", _("Open"));
		Assert.Equal("1 file", _n("{0} file", "{0} files", 1, 1));
		Assert.Equal("2 files", _n("{0} file", "{0} files", 2, 2));
		Assert.Equal("0 files", _n("{0} file", "{0} files", 0, 0));
		Assert.Equal("{0} file", _n("{0} file", "{0} files", -1));
	});

	[Fact]
	public Task ListsAndPicksLanguages() => WithPolish(translations => {
		Assert.Contains("pl", translations.GetAvailableTranslations("test"));
		Assert.Equal("pl", translations.GetBestTranslation("test"));
		Assert.True(translations.IsLoaded("test"));
		Assert.False(translations.IsLoaded("other"));
		Assert.Equal("pl", translations.GetHeaderValue("Language", "test"));
	});

	// The application's strings are looked up in its own catalog, so one it leaves untranslated
	// doesn't take another catalog's translation, such as wxWidgets', with that catalog's accelerator.
	[Fact]
	public Task LooksUpInTheDomainItIsGiven() => Gui.Run(() => {
		Translations translations = new(new MemoryLoader()) { Language = "pl" };
		try {
			Assert.Null(translations.Domain);
			Assert.True(translations.AddCatalog("other"));
			Assert.True(translations.AddCatalog("test"));
			Translations.Current = translations;
			// Without a domain, every catalog is looked in, as before.
			Assert.Equal("&Zamknij", _("&Close"));
			Assert.Equal("Otwórz", _("Open"));
			translations.Domain = "test";
			Assert.Equal("test", translations.Domain);
			Assert.Equal("&Close", _("&Close"));
			Assert.Equal("&Close", Translations.Translate("&Close"));
			Assert.Equal("Otwórz", _("Open"));
			Assert.Equal("Otwarty", _p("state", "Open"));
			Assert.Equal("3 pliki", _n("{0} file", "{0} files", 3, 3));
			Assert.Equal("{0} closed files", _n("{0} closed file", "{0} closed files", 3));
			translations.Domain = "other";
			Assert.Equal("Open", _("Open"));
			Assert.Equal("3 zamknięte", _n("{0} closed file", "{0} closed files", 3, 3));
			translations.Domain = null;
			Assert.Equal("&Zamknij", _("&Close"));
			Assert.Throws<ArgumentException>(() => translations.Domain = "");
		} finally {
			translations.Dispose();
		}
	});

	[Fact]
	public Task DisposesTheTranslationsItReplaces() => Gui.Run(() => {
		Translations first = new(new MemoryLoader());
		Translations.Current = first;
		Translations second = new(new MemoryLoader());
		Translations.Current = second;
		Assert.Throws<ObjectDisposedException>(() => first.AddCatalog("test"));
		Translations.Current = null;
		Assert.Throws<ObjectDisposedException>(() => second.AddCatalog("test"));
		Assert.Null(Translations.Current);
	});

	[Fact]
	public Task DescribesLanguages() => Gui.Run(() => {
		LanguageInfo? german = LanguageInfo.Find("de");
		Assert.NotNull(german);
		Assert.Equal("German", german.Name);
		Assert.Equal("Deutsch", german.NativeName);
		Assert.Equal(LayoutDirection.LeftToRight, german.LayoutDirection);
		Assert.Equal(LayoutDirection.RightToLeft, LanguageInfo.Find("he")?.LayoutDirection);
		Assert.Equal("pt_BR", LanguageInfo.Find("pt-BR")?.Code);
		Assert.Null(LanguageInfo.Find("not a language"));
	});

	// The test project's build compiles and embeds its po/de.po, and wxWidgets' German catalog with
	// it, as an application's does. A machine without gettext's msgfmt builds without them, and
	// skips this, but CI has it.
	[Fact]
	public Task TranslatesFromTheApplicationsEmbeddedCatalogs() => Gui.WithFrame(frame => {
		EmbeddedTranslationsLoader embedded = new();
		Assert.SkipWhen(!embedded.GetAvailableTranslations("WxKit.Tests").Any() && Environment.GetEnvironmentVariable("CI") is null, "The catalogs are not compiled without GNU gettext's msgfmt.");
		Assert.Contains("de", embedded.GetAvailableTranslations("WxKit.Tests"));
		Assert.Contains("de", embedded.GetAvailableTranslations("wxstd"));
		Translations translations = Translations.Use("WxKit.Tests", "de");
		try {
			Assert.Equal("WxKit.Tests", translations.Domain);
			Assert.Equal("Hallo aus dem Testkatalog", _("Hello from the test catalog"));
			Assert.Equal("Hallo, Welt", _("Hello, {0}", "Welt"));
			Assert.Equal("Geöffnet", _p("state", "Open"));
			Assert.Equal("1 Datei", _n("{0} file", "{0} files", 1, 1));
			Assert.Equal("4 Dateien", _n("{0} file", "{0} files", 4, 4));
			Assert.Equal("Not reviewed yet", _("Not reviewed yet"));
			// The test catalog has no "&Cancel", so it stays in English, though wxWidgets' catalog
			// has it, and a stock button still takes wxWidgets' label.
			Assert.Equal("&Cancel", _("&Cancel"));
			Button cancel = new(frame, id: StandardId.Cancel);
			Assert.Equal("Abbrechen", cancel.LabelText);
			translations.Domain = null;
			Assert.Equal("Ab&brechen", _("&Cancel"));
		} finally {
			translations.Dispose();
		}
	});

	// A command-line mode, or Main, sets translations up before Application.Run, or without it.
	[Fact]
	public Task SetsUpBeforeTheApplicationRuns() => Gui.Run(async () => {
		Assert.SkipWhen(!new EmbeddedTranslationsLoader().GetAvailableTranslations("WxKit.Tests").Any() && Environment.GetEnvironmentVariable("CI") is null, "The catalogs are not compiled without GNU gettext's msgfmt.");
		(int _, string output) = await SingleInstanceTests.RunAnotherProcess("translate", "de", "Hello from the test catalog");
		Assert.Equal("Hallo aus dem Testkatalog", output);
	});

	[Fact]
	public void CountsForPluralRulesKeepTheirLastDigits() {
		Assert.Equal(1u, Translations.PluralCount(-1));
		Assert.Equal(uint.MaxValue, Translations.PluralCount(uint.MaxValue));
		Assert.Equal(1_000_022u, Translations.PluralCount(10_000_000_022));
		Assert.Equal(1_000_000u + (ulong.MaxValue / 2 + 1) % 1_000_000, Translations.PluralCount(long.MinValue));
	}

	private static Task WithPolish(Action body) => WithPolish(_ => body());

	private static Task WithPolish(Action<Translations> body) => Gui.Run(() => {
		Translations translations = new(new MemoryLoader()) { Language = "pl" };
		try {
			Assert.True(translations.AddCatalog("test"));
			Translations.Current = translations;
			body(translations);
		} finally {
			translations.Dispose();
		}
	});

	private sealed class MemoryLoader : TranslationsLoader {
		public override byte[]? LoadCatalog(string domain, string language) => language != "pl" ? null : domain == "test" ? Catalog() : domain == "other" ? OtherCatalog() : null;

		public override IEnumerable<string> GetAvailableTranslations(string domain) => domain is "test" or "other" ? ["pl"] : [];
	}

	private static byte[] Catalog() => Catalog([
		("Open", "Otwórz"),
		("state\u0004Open", "Otwarty"),
		("Hello, {0}", "Witaj, {0}"),
		("{0} file\0{0} files", "{0} plik\0{0} pliki\0{0} plików"),
	]);

	// Another domain's catalog, as wxWidgets' own is beside an application's.
	private static byte[] OtherCatalog() => Catalog([
		("&Close", "&Zamknij"),
		("{0} closed file\0{0} closed files", "{0} zamknięty\0{0} zamknięte\0{0} zamkniętych"),
	]);

	// A .mo file: its header, then a table of where each original is, and one of where each
	// translation is, with a context before \u0004, and plural forms after \0.
	private static byte[] Catalog((string Original, string Translation)[] translated) {
		(string Original, string Translation)[] entries = [
			("", $"Content-Type: text/plain; charset=UTF-8\nLanguage: pl\nPlural-Forms: {PolishPlurals}\n"),
			.. translated,
		];
		Array.Sort(entries, static (left, right) => string.CompareOrdinal(left.Original, right.Original));
		byte[][] originals = [.. entries.Select(static entry => Encoding.UTF8.GetBytes(entry.Original))];
		byte[][] translations = [.. entries.Select(static entry => Encoding.UTF8.GetBytes(entry.Translation))];
		int tables = 28;
		int strings = tables + entries.Length * 16;
		using MemoryStream stream = new();
		using BinaryWriter writer = new(stream);
		foreach (int value in new[] { unchecked((int)0x950412de), 0, entries.Length, tables, tables + entries.Length * 8, 0, strings }) writer.Write(value);
		int offset = strings;
		foreach (byte[] text in originals.Concat(translations)) {
			writer.Write(text.Length);
			writer.Write(offset);
			offset += text.Length + 1;
		}
		foreach (byte[] text in originals.Concat(translations)) {
			writer.Write(text);
			writer.Write((byte)0);
		}
		return stream.ToArray();
	}
}
