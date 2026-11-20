namespace WxKit.Build.Tests;

public class ExtractorTests {
	[Fact]
	public void FindsEachShortcutInCSharp() {
		StringTemplate template = Extract("""
			var a = _("Open");
			var b = _n("{0} file", "{0} files", count, count);
			var c = _p("state", "Open");
			var d = _pn("menu", "{0} item", "{0} items", count, count);
			var e = _("Hello, {0}", name);
			""");
		Assert.Collection(template.Strings,
			found => Assert.Equal((null, "Open", null, false), Describe(found)),
			found => Assert.Equal((null, "{0} file", "{0} files", true), Describe(found)),
			found => Assert.Equal(("state", "Open", null, false), Describe(found)),
			found => Assert.Equal(("menu", "{0} item", "{0} items", true), Describe(found)),
			found => Assert.Equal((null, "Hello, {0}", null, true), Describe(found)));
	}

	[Fact]
	public void SkipsCommentsCharsAndDirectivesInCSharp() {
		StringTemplate template = Extract("""
			#region Don't read "this" as a string
			// _("not in a line comment")
			/* _("not in a block comment") */
			char quote = '"';
			char apostrophe = '\'';
			var a = _("after them");
			#endregion
			""");
		Assert.Equal(["after them"], template.Strings.Select(found => found.Id));
	}

	[Fact]
	public void ReadsEveryKindOfCSharpString() {
		StringTemplate template = Extract("""""
			_("tab\there, line\nbreak, \u00e9, \x41, \"quoted\"");
			_(@"C:\folder, ""quoted""");
			_(""""raw "with" quotes"""");
			_("""
				Indented
				  more
				""");
			_("joined " + "together");
			""""");
		Assert.Equal(["tab\there, line\nbreak, \u00e9, A, \"quoted\"", "C:\\folder, \"quoted\"", "raw \"with\" quotes", "Indented\n  more", "joined together"], template.Strings.Select(found => found.Id));
	}

	[Fact]
	public void ReadsPastInterpolatedStrings() {
		List<ExtractionProblem> problems = Extract("""""
			var a = $"{(ready ? "}" : "{")} and {count:N0}" + _("after one");
			var b = $$"""{{name}} {literal} {{{nested}}}""" + _("after a raw one");
			var c = $@"{path}\file ""quoted""" + _("after a verbatim one");
			var d = _($"Hello, {name}");
			""""", out StringTemplate template);
		Assert.Equal(["after one", "after a raw one", "after a verbatim one"], template.Strings.Select(found => found.Id));
		ExtractionProblem problem = Assert.Single(problems);
		Assert.Equal(("WXKIT2002", 4), (problem.Code, problem.Line));
	}

	[Fact]
	public void WarnsAboutStringsItCannotRead() {
		List<ExtractionProblem> problems = Extract("""
			_(label);
			_n("{0} file", plural, count);
			_("bytes"u8);
			""", out StringTemplate template);
		Assert.Empty(template.Strings);
		Assert.Equal([("WXKIT2001", 1), ("WXKIT2001", 2), ("WXKIT2001", 3)], problems.Select(problem => (problem.Code, problem.Line)));
	}

	[Fact]
	public void TellsCallsFromDiscardsAndOtherNames() {
		StringTemplate template = Extract("""
			_ = Load();
			handler = (_, e) => Run();
			Gettext._("qualified");
			WxKit.Gettext._n("{0} way", "{0} ways", 2);
			other._("someone else's");
			Translations.Translate("looked up");
			Translations.TranslatePluralInContext("context", "one", "many", n);
			Translate("too general to be ours");
			""");
		Assert.Equal(["qualified", "{0} way", "looked up", "one"], template.Strings.Select(found => found.Id));
	}

	[Fact]
	public void TakesCommentsForTranslators() {
		StringTemplate template = Extract("""
			// TRANSLATORS: A status, not a button.
			var a = _("Ready");
			// TRANSLATORS: Not for this one,
			Run();
			var b = _("Go");
			/* A note before the tag.
			 * TRANSLATORS: The first line,
			 * and the second. */
			var c = _("Two lines");
			var d = /* TRANSLATORS: On the same line. */ _("Inline");
			""");
		Assert.Equal([["TRANSLATORS: A status, not a button."], [], ["TRANSLATORS: The first line,\nand the second."], ["TRANSLATORS: On the same line."]], template.Strings.Select(found => found.Comments));
	}

	[Fact]
	public void MergesAStringFoundTwice() {
		StringTemplate template = new();
		StringExtractor.Extract("_(\"Open\");\n_(\"Save\");", SourceLanguage.CSharp, "Main.cs", template);
		List<ExtractionProblem> problems = StringExtractor.Extract("\n\n_(\"Open\");\n_n(\"Save\", \"Saves\", n);", SourceLanguage.CSharp, "Other.cs", template);
		Assert.Equal(["Main.cs:1", "Other.cs:3"], template.Strings[0].References);
		Assert.Equal("WXKIT2003", Assert.Single(problems).Code);
	}

	[Fact]
	public void ReadsVisualBasic() {
		StringTemplate template = Extract("""""
			' _("not in a comment")
			REM _("nor in a REM")
			Dim quote = """"c
			Dim a = _n("{0} ""file""", "{0} files", count, count)
			Dim b = gettext._P("state", "Open")
			Dim c = TRANSLATIONS.TRANSLATE("any " & "case")
			Dim d = $"{name} and {{braces}}" & _p("after", "interpolation")
			Dim e = Foo(x, _
				y)
			""""", SourceLanguage.VisualBasic);
		Assert.Equal(["{0} \"file\"", "Open", "any case", "interpolation"], template.Strings.Select(found => found.Id));
	}

	[Fact]
	public void ReadsFSharp() {
		StringTemplate template = Extract("""""
			// _n("not in a comment", "", 1)
			(* _n("nor in (* a nested *) comment", "", 1) *)
			let product = List.reduce (*) [1; 2]
			let first (items: 'T list) = items.Head
			let x' = 'a'
			let y = '\n'
			let a = _p("context", "text")
			let b = _n("""triple "quoted" """, "plural", n)
			let c = _p(@"verbatim ""quoted""", "text")
			let d = _p("spans \
			            lines", "text")
			let e = $"{x} {{braces}}" + _p("after", "interpolation")
			""""", SourceLanguage.FSharp);
		Assert.Equal(["text", "triple \"quoted\" ", "text", "text", "interpolation"], template.Strings.Select(found => found.Id));
		Assert.Equal(["context", null, "verbatim \"quoted\"", "spans lines", "after"], template.Strings.Select(found => found.Context));
	}

	[Fact]
	public void WritesATemplate() {
		StringTemplate template = new();
		StringExtractor.Extract("""
			// TRANSLATORS: Shown at start-up.
			_("Hello, {0}", name);
			_p("menu", "Line one\nline two");
			_n("{0} \"file\"", "{0} files", n);
			""", SourceLanguage.CSharp, "Program.cs", template);
		string text = template.Write("MyApp");
		Assert.Contains("\"Project-Id-Version: MyApp\\n\"\n", text);
		Assert.Contains("\"Content-Type: text/plain; charset=UTF-8\\n\"\n", text);
		Assert.Contains("\n#. TRANSLATORS: Shown at start-up.\n#: Program.cs:2\n#, csharp-format\nmsgid \"Hello, {0}\"\nmsgstr \"\"\n", text);
		Assert.Contains("\n#: Program.cs:3\nmsgctxt \"menu\"\nmsgid \"\"\n\"Line one\\n\"\n\"line two\"\nmsgstr \"\"\n", text);
		Assert.Contains("\n#: Program.cs:4\nmsgid \"{0} \\\"file\\\"\"\nmsgid_plural \"{0} files\"\nmsgstr[0] \"\"\nmsgstr[1] \"\"\n", text);
		Assert.DoesNotContain("Creation-Date", text);
	}

	private static StringTemplate Extract(string source, SourceLanguage language = SourceLanguage.CSharp) {
		List<ExtractionProblem> problems = Extract(source, out StringTemplate template, language);
		Assert.Empty(problems);
		return template;
	}

	private static List<ExtractionProblem> Extract(string source, out StringTemplate template, SourceLanguage language = SourceLanguage.CSharp) {
		template = new StringTemplate();
		return StringExtractor.Extract(source, language, "Test.cs", template);
	}

	private static (string? Context, string Id, string? Plural, bool IsFormat) Describe(ExtractedString found) => (found.Context, found.Id, found.Plural, found.IsFormat);
}
