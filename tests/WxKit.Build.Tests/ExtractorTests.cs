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

	// A hole is code, so a call in it takes a literal like any other, as a file dialog's filter does.
	[Fact]
	public void FindsCallsInTheHolesOfInterpolatedStrings() {
		List<ExtractionProblem> problems = Extract("""""
			var filter = $"{_("Text files")} (*.txt)|*.txt";
			var raw = $$"""{{_p("menu", "Open")}} {literal}""";
			var verbatim = $@"{_("In a verbatim one")}\{name}";
			var count = $"{_n("{0} file", "{0} files", n, n)}, {(ready ? _("ready") : _("waiting"))}";
			var nested = $"{$"{_("Nested")}"}";
			var outer = _($"Hello, {_("World")}");
			var formatted = $"{when:d'} {global::System.Environment.NewLine}{_("After a format")}";
			""""", out StringTemplate template);
		Assert.Equal(["Text files", "Open", "In a verbatim one", "{0} file", "ready", "waiting", "Nested", "World", "After a format"], template.Strings.Select(found => found.Id));
		Assert.Equal("menu", template.Strings[1].Context);
		Assert.Equal("{0} files", template.Strings[3].Plural);
		ExtractionProblem problem = Assert.Single(problems);
		Assert.Equal(("WXKIT2002", 6), (problem.Code, problem.Line));
	}

	[Fact]
	public void FindsCallsInTheHolesOfInterpolatedStringsInFSharp() {
		StringTemplate template = Extract(""""
			let filter = $"{tr "Text files"} (*.txt)|*.txt"
			let triple = $"""{trp "menu" "Open"} "quoted" """
			let upcast = $"{value :> obj |> fun _ -> tr "After an upcast"}"
			let downcast = $"{value :?> string |> fun _ -> tr "After a downcast"}"
			"""", SourceLanguage.FSharp);
		Assert.Equal(["Text files", "Open", "After an upcast", "After a downcast"], template.Strings.Select(found => found.Id));
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

	// A string passed first to String.Format is formatted, so msgfmt checks each translation's
	// placeholders, as it does for a C# call that takes the arguments itself.
	[Fact]
	public void MarksAStringFormatted() {
		StringTemplate fsharp = Extract("""
			let a = String.Format(tr "{0} files", count)
			let b = String.Format(Translation.tr "{0} folders", count)
			let c = tr "Plain"
			""", SourceLanguage.FSharp);
		Assert.Equal([true, true, false], fsharp.Strings.Select(found => found.IsFormat));
		StringTemplate csharp = Extract("""
			string a = string.Format(Translations.Translate("{0} files"), count);
			string b = Format(Translations.Translate("Not String's"));
			string c = Translations.Translate("Plain");
			""");
		Assert.Equal([true, true, false], csharp.Strings.Select(found => found.IsFormat));
	}

	// WxKit.FSharp's functions take their strings one after another, or one in parentheses. A
	// declaration of the same name, a member of something else, and the function passed as a
	// value are not calls.
	[Fact]
	public void ReadsWxKitFSharpsFunctions() {
		List<ExtractionProblem> problems = Extract("""
			let a = tr "Open"
			let b = trn "{0} file" "{0} files" count
			let c = trp "verb" "Open"
			let d = trpn "menu" "{0} item" "{0} items" count
			let e = String.Format(tr "Hello, {0}!", name)
			let f = Translation.tr "qualified"
			let g = tr ("in parentheses")
			let h = tr "one" + "not part of it"
			let tr (text: string) = text
			let i = other.tr "a member"
			let j = List.map tr labels
			let k = tr label
			let l = tr $"Hello {name}"
			""", out StringTemplate template, SourceLanguage.FSharp);
		Assert.Collection(template.Strings,
			found => Assert.Equal((null, "Open", null, false), Describe(found)),
			found => Assert.Equal((null, "{0} file", "{0} files", false), Describe(found)),
			found => Assert.Equal(("verb", "Open", null, false), Describe(found)),
			found => Assert.Equal(("menu", "{0} item", "{0} items", false), Describe(found)),
			found => Assert.Equal((null, "Hello, {0}!", null, true), Describe(found)),
			found => Assert.Equal((null, "qualified", null, false), Describe(found)),
			found => Assert.Equal((null, "in parentheses", null, false), Describe(found)),
			found => Assert.Equal((null, "one", null, false), Describe(found)));
		Assert.Equal(["WXKIT2001", "WXKIT2002"], problems.Select(problem => problem.Code));
		Assert.Equal([12, 13], problems.Select(problem => problem.Line));
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

	// An application's own shortcuts, which pass their arguments on to Gettext's, are declarations,
	// not calls, so neither they nor the calls in them are warned about. A call elsewhere still is.
	[Theory]
	[InlineData(SourceLanguage.CSharp, """
		public static string _(string text) => Gettext._(text);
		public static string _(string text, params object?[] args) => Gettext._(text, args);
		public static IEnumerable<string> _n(string text, string plural, long n) {
			return [Gettext._n(text, plural, n)];
		}
		string name = _(label);
		string title = _("Title");
		""")]
	[InlineData(SourceLanguage.FSharp, """
		let _p (context: string) (text: string) = Gettext._p(context, text)
		let inline _n (text: string) (plural: string) (n: int64) =
		    Gettext._n(text, plural, n)
		let name = Translations.Translate(label)
		let title = Translations.Translate("Title")
		""")]
	public void LeavesOutAnApplicationsOwnShortcuts(SourceLanguage language, string source) {
		List<ExtractionProblem> problems = Extract(source, out StringTemplate template, language);
		Assert.Equal(["Title"], template.Strings.Select(found => found.Id));
		ExtractionProblem problem = Assert.Single(problems);
		Assert.Equal("WXKIT2001", problem.Code);
		Assert.Equal(source.Split('\n').Length - 1, problem.Line);
	}

	// A verbatim string that starts with a doubled quote is not a raw string, which would run to the
	// end of the file.
	[Fact]
	public void ReadsAVerbatimStringThatStartsWithAQuote() {
		StringTemplate csharp = Extract(""""
			var a = _(@"""Hi"" he said");
			var b = _("second");
			"""");
		Assert.Equal(["\"Hi\" he said", "second"], csharp.Strings.Select(found => found.Id));
		StringTemplate fsharp = Extract(""""
			let a = tr @"""Hi"" he said"
			let b = tr "second"
			"""", SourceLanguage.FSharp);
		Assert.Equal(["\"Hi\" he said", "second"], fsharp.Strings.Select(found => found.Id));
	}

	[Fact]
	public void ReadsFSharpsEscapes() {
		StringTemplate template = Extract("""
			let a = tr "\065BC"
			let b = tr "\x41BD"
			let c = tr "\120"
			(* the '"' char *)
			let d = tr "after a comment"
			""", SourceLanguage.FSharp);
		Assert.Equal(["ABC", "ABD", "x", "after a comment"], template.Strings.Select(found => found.Id));
	}

	// A string or comment that never ends takes the rest of the file with it, which is said once,
	// where it starts.
	[Theory]
	[InlineData(SourceLanguage.CSharp, "var a = _(\"first\");\nvar b = $\"{t\nvar c = _(\"lost\");\n", 2)]
	[InlineData(SourceLanguage.CSharp, "var a = _(\"first\");\n/* never closed\nvar c = _(\"lost\");\n", 2)]
	[InlineData(SourceLanguage.FSharp, "let a = tr \"first\"\nlet b = \"\"\"never closed\nlet c = tr \"lost\"\n", 2)]
	public void WarnsAboutWhatNeverEnds(SourceLanguage language, string source, int line) {
		List<ExtractionProblem> problems = Extract(source, out StringTemplate template, language);
		Assert.Equal(["first"], template.Strings.Select(found => found.Id));
		ExtractionProblem problem = Assert.Single(problems);
		Assert.Equal(("WXKIT2004", line), (problem.Code, problem.Line));
	}

	[Fact]
	public void LeavesOutAnEmptyString() {
		List<ExtractionProblem> problems = Extract("""
			var a = _("");
			var b = _("kept");
			""", out StringTemplate template);
		Assert.Equal(["kept"], template.Strings.Select(found => found.Id));
		Assert.Equal(("WXKIT2005", 1), (Assert.Single(problems).Code, problems[0].Line));
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
