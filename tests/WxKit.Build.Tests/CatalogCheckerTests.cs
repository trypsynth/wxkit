namespace WxKit.Build.Tests;

public class CatalogCheckerTests {
	[Fact]
	public void ReadsEntries() {
		List<PoEntry> entries = PoCatalog.Read("""
			msgid ""
			msgstr "Language: de\n"

			msgctxt "state"
			msgid "Open"
			msgstr "Geöffnet"

			#, fuzzy, csharp-format
			msgid ""
			"Two "
			"lines\n"
			msgid_plural "Plural"
			msgstr[0] "Zwei \"Zeilen\""
			msgstr[1] ""
			"Mehr\t\101"

			#~ msgid "Gone"
			#~ msgstr "Weg"
			""");
		Assert.Equal(4, entries.Count);
		Assert.Equal(("state", "Open", "Geöffnet", 6), (entries[1].Context, entries[1].Id, entries[1].Translations[0].Text, entries[1].Translations[0].Line));
		Assert.Equal(("Two lines\n", "Plural", true), (entries[2].Id, entries[2].Plural, entries[2].IsFuzzy));
		Assert.Equal(["Zwei \"Zeilen\"", "Mehr\tA"], entries[2].Translations.Select(translation => translation.Text));
		Assert.False(entries[1].IsFuzzy);
		Assert.True(entries[3].IsObsolete);
	}

	[Theory]
	[InlineData("&Open", 1)]
	[InlineData("Save &As", 1)]
	[InlineData("Fish && chips", 0)]
	[InlineData("Black & white", 0)]
	[InlineData("&Fish && &chips", 2)]
	[InlineData("Ends with &", 0)]
	public void CountsAccelerators(string text, int count) => Assert.Equal(count, CatalogChecker.Accelerators(text));

	[Fact]
	public void FindsLostAndAddedAcceleratorsAndShortcuts() {
		List<CatalogProblem> problems = CatalogChecker.Check(PoCatalog.Read("""
			msgid "&Open"
			msgstr "Öffnen"

			msgid "Save"
			msgstr "&Speichern"

			msgid "&Quit"
			msgstr "&Be&enden"

			msgid "&New\tCtrl+N"
			msgstr "&Neu"

			msgid "Close"
			msgstr "Schließen\tStrg+W"

			msgid "&Fine\tCtrl+F"
			msgstr "&Gut\tStrg+F"

			msgid "Fish && chips"
			msgstr "Fisch && Pommes"
			"""));
		Assert.Equal([("WXKIT1001", 2), ("WXKIT1002", 5), ("WXKIT1003", 8), ("WXKIT1004", 11), ("WXKIT1005", 14)], problems.Select(problem => (problem.Code, problem.Line)));
	}

	[Fact]
	public void ChecksPluralFormsAgainstTheirOriginals() {
		List<CatalogProblem> problems = CatalogChecker.Check(PoCatalog.Read("""
			msgid "&One file"
			msgid_plural "&Files"
			msgstr[0] "&Eine Datei"
			msgstr[1] "Dateien"
			"""));
		Assert.Equal([("WXKIT1001", 4)], problems.Select(problem => (problem.Code, problem.Line)));
	}

	[Fact]
	public void SkipsFuzzyAndUntranslatedEntries() {
		List<CatalogProblem> problems = CatalogChecker.Check(PoCatalog.Read("""
			#, fuzzy
			msgid "&Open"
			msgstr "Öffnen"

			msgid "&Save"
			msgstr ""
			"""));
		Assert.Empty(problems);
	}

	[Theory]
	[InlineData("de", "de")]
	[InlineData("DE", "de")]
	[InlineData("de_AT", "de")]
	[InlineData("pt_BR", "pt_BR")]
	[InlineData("pt_PT", "pt")]
	[InlineData("ko", "ko_KR")]
	[InlineData("ca_ES@valencia", "ca")]
	[InlineData("xx", null)]
	public void MatchesLanguagesToWxWidgetsCatalogs(string language, string? catalog) {
		List<string> available = ["ca", "ca@valencia", "de", "ko_KR", "pt", "pt_BR"];
		Assert.Equal(catalog, WxKitStdCatalogs.Match(language, available));
	}
}
