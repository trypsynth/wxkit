#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

// Extracts the strings an application passes to Gettext's shortcuts and Translations' lookups into
// a gettext template, <domain>.pot, which translators' catalogs are made and updated from. It reads
// C#, Visual Basic, and F# with a lexer for each, so that strings in comments, char literals, and
// verbatim, raw, and interpolated strings are told apart from the ones a call passes, and it takes
// only string literals, and literals joined with + (or & in Visual Basic), since it reads the source
// and does not run it. A comment just above a call that starts with TRANSLATORS: goes to translators
// with the string, and a call that formats the string with arguments marks it csharp-format, so
// that msgfmt checks each translation formats as the original does.
public sealed class WxKitExtractStrings : Task {
	public ITaskItem[] Sources { get; set; } = Array.Empty<ITaskItem>();

	[Required]
	public string ProjectDirectory { get; set; } = string.Empty;

	[Required]
	public string Output { get; set; } = string.Empty;

	[Required]
	public string Domain { get; set; } = string.Empty;

	public override bool Execute() {
		StringTemplate template = new StringTemplate();
		string projectDirectory = Path.GetFullPath(ProjectDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
		IEnumerable<string> paths = Sources.Select(source => source.GetMetadata("FullPath")).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(path => path, StringComparer.Ordinal);
		foreach (string path in paths) {
			SourceLanguage? language = StringExtractor.LanguageOf(path);
			if (language is null || !File.Exists(path)) continue;
			string reference = path.StartsWith(projectDirectory, StringComparison.OrdinalIgnoreCase) ? path.Substring(projectDirectory.Length) : path;
			foreach (ExtractionProblem problem in StringExtractor.Extract(File.ReadAllText(path), language.Value, reference.Replace('\\', '/'), template)) {
				Log.LogWarning(null, problem.Code, null, path, problem.Line, problem.Column, 0, 0, problem.Message);
			}
		}
		string text = template.Write(Domain);
		string? directory = Path.GetDirectoryName(Path.GetFullPath(Output));
		if (directory is not null) System.IO.Directory.CreateDirectory(directory);
		// The template has no creation date, so it only changes when the strings do.
		if (!File.Exists(Output) || File.ReadAllText(Output) != text) File.WriteAllText(Output, text, new UTF8Encoding(false));
		Log.LogMessage(MessageImportance.High, $"Extracted {template.Count} strings to {Output}.");
		return !Log.HasLoggedErrors;
	}
}

public enum SourceLanguage {
	CSharp,
	VisualBasic,
	FSharp,
}

public sealed class ExtractionProblem {
	public ExtractionProblem(int line, int column, string code, string message) {
		Line = line;
		Column = column;
		Code = code;
		Message = message;
	}

	public int Line { get; }

	public int Column { get; }

	public string Code { get; }

	public string Message { get; }

	public override string ToString() => $"{Line},{Column}: {Code}: {Message}";
}

public sealed class ExtractedString {
	public ExtractedString(string? context, string id, string? plural) {
		Context = context;
		Id = id;
		Plural = plural;
	}

	public string? Context { get; }

	public string Id { get; }

	public string? Plural { get; }

	public List<string> References { get; } = new List<string>();

	public List<string> Comments { get; } = new List<string>();

	public bool IsFormat { get; set; }
}

// The strings found so far, each once, in the order they were first found.
public sealed class StringTemplate {
	private readonly Dictionary<string, ExtractedString> _byKey = new Dictionary<string, ExtractedString>(StringComparer.Ordinal);
	private readonly List<ExtractedString> _strings = new List<ExtractedString>();

	public int Count => _strings.Count;

	public IReadOnlyList<ExtractedString> Strings => _strings;

	// Adds a string, and returns why it conflicts with the same string found before, if it does.
	public string? Add(string? context, string id, string? plural, string reference, string? comment, bool isFormat) {
		string key = (context ?? "\u0004") + "\u0004" + id;
		if (!_byKey.TryGetValue(key, out ExtractedString? found)) {
			found = new ExtractedString(context, id, plural);
			_byKey.Add(key, found);
			_strings.Add(found);
		}
		if (!found.References.Contains(reference)) found.References.Add(reference);
		if (comment is not null && !found.Comments.Contains(comment)) found.Comments.Add(comment);
		found.IsFormat |= isFormat;
		if (found.Plural != plural) return found.Plural is null ? "is used without a plural form elsewhere" : plural is null ? $"is used with the plural form \"{found.Plural}\" elsewhere" : $"is used with the plural form \"{found.Plural}\" elsewhere, which this one's \"{plural}\" is not";
		return null;
	}

	public string Write(string domain) {
		StringBuilder text = new StringBuilder();
		text.Append("# The strings of ").Append(domain).Append(" to translate, which its build extracts from its source. Each\n");
		text.Append("# language's catalog, such as de.po, is made from this template with msginit and updated from it\n");
		text.Append("# with msgmerge.\n");
		text.Append("msgid \"\"\nmsgstr \"\"\n");
		text.Append("\"Project-Id-Version: ").Append(Escape(domain)).Append("\\n\"\n");
		text.Append("\"Language: \\n\"\n");
		text.Append("\"MIME-Version: 1.0\\n\"\n");
		text.Append("\"Content-Type: text/plain; charset=UTF-8\\n\"\n");
		text.Append("\"Content-Transfer-Encoding: 8bit\\n\"\n");
		text.Append("\"Plural-Forms: nplurals=INTEGER; plural=EXPRESSION;\\n\"\n");
		foreach (ExtractedString found in _strings) {
			text.Append('\n');
			foreach (string comment in found.Comments) {
				foreach (string line in comment.Split('\n')) text.Append("#. ").Append(line).Append('\n');
			}
			foreach (string reference in found.References) text.Append("#: ").Append(reference).Append('\n');
			if (found.IsFormat) text.Append("#, csharp-format\n");
			if (found.Context is not null) AppendString(text, "msgctxt", found.Context);
			AppendString(text, "msgid", found.Id);
			if (found.Plural is null) {
				text.Append("msgstr \"\"\n");
			} else {
				AppendString(text, "msgid_plural", found.Plural);
				text.Append("msgstr[0] \"\"\nmsgstr[1] \"\"\n");
			}
		}
		return text.ToString();
	}

	// A string with line breaks is written a line at a time, as gettext's tools write it.
	private static void AppendString(StringBuilder text, string keyword, string value) {
		text.Append(keyword).Append(' ');
		int newline = value.IndexOf('\n');
		if (newline < 0 || newline == value.Length - 1) {
			text.Append('"').Append(Escape(value)).Append("\"\n");
			return;
		}
		text.Append("\"\"\n");
		int start = 0;
		while (start < value.Length) {
			int end = value.IndexOf('\n', start);
			end = end < 0 ? value.Length : end + 1;
			text.Append('"').Append(Escape(value.Substring(start, end - start))).Append("\"\n");
			start = end;
		}
	}

	private static string Escape(string value) {
		StringBuilder text = new StringBuilder(value.Length);
		foreach (char character in value) {
			switch (character) {
				case '\\': text.Append("\\\\"); break;
				case '"': text.Append("\\\""); break;
				case '\n': text.Append("\\n"); break;
				case '\r': text.Append("\\r"); break;
				case '\t': text.Append("\\t"); break;
				case '\a': text.Append("\\a"); break;
				case '\b': text.Append("\\b"); break;
				case '\f': text.Append("\\f"); break;
				case '\v': text.Append("\\v"); break;
				default:
					if (character < ' ') text.Append('\\').Append(Convert.ToString((int)character, 8).PadLeft(3, '0'));
					else text.Append(character);
					break;
			}
		}
		return text.ToString();
	}
}

public static class StringExtractor {
	// Each function, how many string literals it takes first, and how many arguments it takes before
	// any to format with. The context comes first, as in _p(context, text).
	private sealed class Keyword {
		public Keyword(int literals, int arguments, bool hasContext, bool hasPlural) {
			Literals = literals;
			Arguments = arguments;
			HasContext = hasContext;
			HasPlural = hasPlural;
		}

		public int Literals { get; }

		public int Arguments { get; }

		public bool HasContext { get; }

		public bool HasPlural { get; }
	}

	private static readonly Dictionary<string, Keyword> Shortcuts = new Dictionary<string, Keyword>(StringComparer.Ordinal) {
		["_"] = new Keyword(1, 1, false, false),
		["_n"] = new Keyword(2, 3, false, true),
		["_p"] = new Keyword(2, 2, true, false),
		["_pn"] = new Keyword(3, 4, true, true),
	};

	private static readonly Dictionary<string, Keyword> Lookups = new Dictionary<string, Keyword>(StringComparer.Ordinal) {
		["Translate"] = new Keyword(1, 1, false, false),
		["TranslatePlural"] = new Keyword(2, 3, false, true),
		["TranslateInContext"] = new Keyword(2, 2, true, false),
		["TranslatePluralInContext"] = new Keyword(3, 4, true, true),
	};

	public static SourceLanguage? LanguageOf(string path) {
		switch (Path.GetExtension(path).ToLowerInvariant()) {
			case ".cs": case ".csx": return SourceLanguage.CSharp;
			case ".vb": return SourceLanguage.VisualBasic;
			case ".fs": case ".fsx": case ".fsi": return SourceLanguage.FSharp;
			default: return null;
		}
	}

	public static List<ExtractionProblem> Extract(string source, SourceLanguage language, string reference, StringTemplate template) {
		List<ExtractionProblem> problems = new List<ExtractionProblem>();
		Lexer lexer = new Lexer(source, language);
		List<Token> tokens = lexer.Tokens();
		problems.AddRange(lexer.Problems);
		for (int index = 0; index < tokens.Count; index++) {
			Token token = tokens[index];
			if (token.Kind != TokenKind.Identifier) continue;
			// An application's own shortcut passes its arguments on, so neither it nor its body has strings.
			if (IsDeclaration(tokens, index, language)) {
				index = BodyEnd(tokens, index, language) - 1;
				continue;
			}
			if (!IsCall(tokens, index, language, out Keyword? keyword)) continue;
			List<List<Token>>? arguments = Arguments(tokens, index + 1, language);
			if (arguments is null || arguments.Count < keyword!.Arguments) continue;
			string?[] literals = new string?[keyword.Literals];
			bool usable = true;
			for (int position = 0; position < keyword.Literals; position++) {
				literals[position] = Literal(arguments[position], language, out string? why);
				if (literals[position] is null) {
					Token at = arguments[position].Count > 0 ? arguments[position][0] : token;
					problems.Add(new ExtractionProblem(at.Line, at.Column, why == "interpolated" ? "WXKIT2002" : "WXKIT2001", why == "interpolated" ? $"{token.Text}'s string is interpolated, so it cannot be translated. Use a placeholder such as {{0}}, and pass the value as an argument to format with." : $"{token.Text}'s argument is not a string literal, so it cannot be extracted for translators."));
					usable = false;
					break;
				}
			}
			if (!usable) continue;
			int next = 0;
			string? context = keyword.HasContext ? literals[next++] : null;
			string id = literals[next++]!;
			string? plural = keyword.HasPlural ? literals[next] : null;
			string? conflict = template.Add(context, id, plural, $"{reference}:{token.Line}", lexer.CommentBefore(token), arguments.Count > keyword.Arguments);
			if (conflict is not null) problems.Add(new ExtractionProblem(token.Line, token.Column, "WXKIT2003", $"\"{id}\" {conflict}, and a string has one set of forms in a catalog."));
		}
		return problems;
	}

	// A shortcut, bare or as Gettext's, or a lookup as Translations', followed by its arguments in
	// parentheses. Visual Basic's _ alone continues a line, and its names are not case sensitive.
	private static bool IsCall(List<Token> tokens, int index, SourceLanguage language, out Keyword? keyword) {
		StringComparison comparison = language == SourceLanguage.VisualBasic ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
		string name = tokens[index].Text;
		keyword = null;
		if (index + 1 >= tokens.Count || !tokens[index + 1].Is('(')) return false;
		bool qualified = index >= 1 && tokens[index - 1].Is('.');
		string? qualifier = qualified && index >= 2 && tokens[index - 2].Kind == TokenKind.Identifier ? tokens[index - 2].Text : null;
		foreach (KeyValuePair<string, Keyword> shortcut in Shortcuts) {
			if (!string.Equals(shortcut.Key, name, comparison) || (language == SourceLanguage.VisualBasic && shortcut.Key == "_")) continue;
			if (qualified && !string.Equals(qualifier, "Gettext", comparison)) return false;
			keyword = shortcut.Value;
			return true;
		}
		foreach (KeyValuePair<string, Keyword> lookup in Lookups) {
			if (!string.Equals(lookup.Key, name, comparison) || !string.Equals(qualifier, "Translations", comparison)) continue;
			keyword = lookup.Value;
			return true;
		}
		return false;
	}

	// The words that come before a call, which a declaration's return type does not look like.
	private static readonly HashSet<string> CSharpCallKeywords = new HashSet<string>(StringComparer.Ordinal) {
		"return", "await", "throw", "yield", "else", "case", "in", "is", "as", "when", "and", "or", "not", "new", "out", "ref", "default", "select", "where", "orderby", "group", "by",
	};

	private static readonly HashSet<string> FSharpDeclarationKeywords = new HashSet<string>(StringComparer.Ordinal) {
		"let", "rec", "inline", "member", "static", "private", "internal", "public", "override", "default", "and",
	};

	// A method named as a shortcut or a lookup, which an application declares to wrap Gettext's
	// or Translations', rather than a call of one: after a return type in C#, Function or Sub in
	// Visual Basic, and let or member in F#.
	private static bool IsDeclaration(List<Token> tokens, int index, SourceLanguage language) {
		StringComparison comparison = language == SourceLanguage.VisualBasic ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
		string name = tokens[index].Text;
		bool named = Shortcuts.Keys.Concat(Lookups.Keys).Any(key => string.Equals(key, name, comparison) && !(language == SourceLanguage.VisualBasic && key == "_"));
		if (!named || index == 0 || index + 1 >= tokens.Count || !tokens[index + 1].Is('(')) return false;
		Token before = tokens[index - 1];
		switch (language) {
			case SourceLanguage.VisualBasic:
				return string.Equals(before.Text, "Function", comparison) || string.Equals(before.Text, "Sub", comparison);
			case SourceLanguage.FSharp:
				return before.Kind == TokenKind.Identifier && FSharpDeclarationKeywords.Contains(before.Text);
			default:
				// A generic return type ends in >, which => also does.
				if (before.Is('>')) return !(index >= 2 && tokens[index - 2].Is('='));
				return before.Kind == TokenKind.Identifier && !CSharpCallKeywords.Contains(before.Text);
		}
	}

	// Where the body of the declaration at index ends: its block or expression body's ; in C#, its
	// End Function or End Sub in Visual Basic, and the next line indented no deeper in F#.
	private static int BodyEnd(List<Token> tokens, int index, SourceLanguage language) {
		switch (language) {
			case SourceLanguage.VisualBasic:
				for (int at = index + 1; at + 1 < tokens.Count; at++) {
					if (string.Equals(tokens[at].Text, "End", StringComparison.OrdinalIgnoreCase) && (string.Equals(tokens[at + 1].Text, "Function", StringComparison.OrdinalIgnoreCase) || string.Equals(tokens[at + 1].Text, "Sub", StringComparison.OrdinalIgnoreCase))) return at + 2;
				}
				return tokens.Count;
			case SourceLanguage.FSharp:
				int line = tokens[index].Line;
				int start = index;
				while (start > 0 && tokens[start - 1].Line == line) start--;
				for (int at = index + 1; at < tokens.Count; at++) {
					if (tokens[at].Line > line && tokens[at].Column <= tokens[start].Column) return at;
				}
				return tokens.Count;
			default:
				int depth = 0;
				for (int at = index + 1; at < tokens.Count; at++) {
					Token token = tokens[at];
					if (token.Is('(') || token.Is('[')) depth++;
					if (token.Is(')') || token.Is(']')) depth--;
					if (depth == 0 && token.Is(';')) return at + 1;
					if (depth == 0 && token.Is('{')) return BlockEnd(tokens, at);
				}
				return tokens.Count;
		}
	}

	// Just past the } that closes the { at open.
	private static int BlockEnd(List<Token> tokens, int open) {
		int braces = 0;
		for (int at = open; at < tokens.Count; at++) {
			if (tokens[at].Is('{')) braces++;
			if (tokens[at].Is('}') && --braces == 0) return at + 1;
		}
		return tokens.Count;
	}

	// The arguments between the parentheses at open, split at the commas outside any brackets.
	private static List<List<Token>>? Arguments(List<Token> tokens, int open, SourceLanguage language) {
		List<List<Token>> arguments = new List<List<Token>>();
		List<Token> current = new List<Token>();
		int depth = 0;
		for (int index = open + 1; index < tokens.Count; index++) {
			Token token = tokens[index];
			if (token.Is('(') || token.Is('[') || token.Is('{')) depth++;
			if (token.Is(')') || token.Is(']') || token.Is('}')) {
				if (depth == 0) {
					if (current.Count > 0 || arguments.Count > 0) arguments.Add(current);
					return arguments;
				}
				depth--;
			}
			if (depth == 0 && token.Is(',')) {
				arguments.Add(current);
				current = new List<Token>();
				continue;
			}
			current.Add(token);
		}
		return null;
	}

	// A string literal, or literals joined with + (or & in Visual Basic), or null.
	private static string? Literal(List<Token> argument, SourceLanguage language, out string? why) {
		why = null;
		if (argument.Count == 0 || argument.Count % 2 == 0) return null;
		StringBuilder text = new StringBuilder();
		for (int index = 0; index < argument.Count; index++) {
			Token token = argument[index];
			if (index % 2 == 1) {
				if (!(token.Is('+') || (language == SourceLanguage.VisualBasic && token.Is('&')))) return null;
				continue;
			}
			if (token.Kind == TokenKind.Interpolated) {
				why = "interpolated";
				return null;
			}
			if (token.Kind != TokenKind.String) return null;
			text.Append(token.Text);
		}
		return text.ToString();
	}
}

public enum TokenKind {
	Identifier,
	String,
	Interpolated,
	Other,
	Punctuation,
}

public sealed class Token {
	public Token(TokenKind kind, string text, int line, int column) {
		Kind = kind;
		Text = text;
		Line = line;
		Column = column;
	}

	public TokenKind Kind { get; }

	public string Text { get; }

	public int Line { get; }

	public int Column { get; }

	public bool Is(char punctuation) => Kind == TokenKind.Punctuation && Text.Length == 1 && Text[0] == punctuation;

	public override string ToString() => $"{Kind} {Text}";
}

public sealed class Lexer {
	private readonly string _source;
	private readonly SourceLanguage _language;
	private readonly List<(int Start, int End, int Column, string Text)> _comments = new List<(int, int, int, string)>();
	private int _index;
	private int _line = 1;
	private int _lineStart;

	public Lexer(string source, SourceLanguage language) {
		_source = source;
		_language = language;
	}

	public List<ExtractionProblem> Problems { get; } = new List<ExtractionProblem>();

	private char Current => _index < _source.Length ? _source[_index] : '\0';

	private int Column => _index - _lineStart + 1;

	public List<Token> Tokens() {
		List<Token> tokens = new List<Token>();
		bool lineStart = true;
		while (_index < _source.Length) {
			char character = Current;
			if (character == '\n') {
				Advance();
				lineStart = true;
				continue;
			}
			if (char.IsWhiteSpace(character)) {
				Advance();
				continue;
			}
			// A preprocessor directive, such as #region, runs to the end of its line, whatever is in it.
			if (lineStart && character == '#') {
				SkipLine();
				continue;
			}
			lineStart = false;
			Token? token = Next();
			if (token is not null) tokens.Add(token);
		}
		return tokens;
	}

	// The comment just above a token, or before it on its line, when it is for translators, and the
	// comments just above that one, from where TRANSLATORS: starts.
	public string? CommentBefore(Token token) {
		int index = _comments.FindLastIndex(comment => comment.End == token.Line - 1 || (comment.End == token.Line && comment.Column < token.Column));
		if (index < 0) return null;
		int first = index;
		while (first > 0 && _comments[first - 1].End == _comments[first].Start - 1) first--;
		List<string> lines = new List<string>();
		for (int position = first; position <= index; position++) lines.AddRange(_comments[position].Text.Split('\n').Select(line => line.Trim()));
		int tag = lines.FindIndex(line => line.StartsWith("TRANSLATORS:", StringComparison.Ordinal));
		if (tag < 0) return null;
		return string.Join("\n", lines.Skip(tag).Where(line => line.Length > 0));
	}

	private Token? Next() {
		int line = _line;
		int column = Column;
		char character = Current;
		switch (_language) {
			case SourceLanguage.CSharp:
				if (character == '/' && Peek(1) == '/') return LineComment(2);
				if (character == '/' && Peek(1) == '*') return BlockComment("/*", "*/", false);
				if (character == '\'') return CSharpChar(line, column);
				if (character == '"' || ((character == '@' || character == '$') && IsCSharpStringPrefix())) return CSharpString(line, column);
				if (character == '@' && IsIdentifierStart(Peek(1))) {
					Advance();
					return Identifier(line, column);
				}
				break;
			case SourceLanguage.VisualBasic:
				if (character == '\'' || character == '\u2018' || character == '\u2019') return LineComment(1);
				if (character == '"' || character == '\u201C' || character == '\u201D') return BasicString(line, column, false);
				if (character == '$' && IsQuote(Peek(1))) {
					Advance();
					return BasicString(line, column, true);
				}
				if (IsIdentifierStart(character)) {
					Token identifier = Identifier(line, column);
					if (string.Equals(identifier.Text, "REM", StringComparison.OrdinalIgnoreCase)) return LineComment(0);
					return identifier;
				}
				break;
			case SourceLanguage.FSharp:
				if (character == '/' && Peek(1) == '/') return LineComment(2);
				if (character == '(' && Peek(1) == '*' && Peek(2) != ')') return BlockComment("(*", "*)", true);
				if (character == '\'') return FSharpChar(line, column);
				if (character == '"' || ((character == '@' || character == '$') && IsFSharpStringPrefix())) return FSharpString(line, column);
				if (character == '`' && Peek(1) == '`') {
					int end = _source.IndexOf("``", _index + 2, StringComparison.Ordinal);
					string name = end < 0 ? string.Empty : _source.Substring(_index + 2, end - _index - 2);
					AdvanceTo(end < 0 ? _source.Length : end + 2);
					return new Token(TokenKind.Identifier, name, line, column);
				}
				break;
		}
		if (IsIdentifierStart(character)) return Identifier(line, column);
		if (char.IsDigit(character)) {
			while (_index < _source.Length && (char.IsLetterOrDigit(Current) || Current == '_' || (Current == '.' && char.IsDigit(Peek(1))))) Advance();
			return new Token(TokenKind.Other, string.Empty, line, column);
		}
		Advance();
		return new Token(TokenKind.Punctuation, character.ToString(), line, column);
	}

	private Token Identifier(int line, int column) {
		int start = _index;
		while (_index < _source.Length && (char.IsLetterOrDigit(Current) || Current == '_' || (_language == SourceLanguage.FSharp && Current == '\''))) Advance();
		return new Token(TokenKind.Identifier, _source.Substring(start, _index - start), line, column);
	}

	private static bool IsIdentifierStart(char character) => char.IsLetter(character) || character == '_';

	private static bool IsQuote(char character) => character == '"' || character == '\u201C' || character == '\u201D';

	private Token? LineComment(int skip) {
		int start = _line;
		int column = Column;
		AdvanceTo(_index + skip);
		int end = _source.IndexOf('\n', _index);
		if (end < 0) end = _source.Length;
		string text = _source.Substring(_index, end - _index).TrimEnd('\r');
		// A Visual Basic or C# XML documentation comment's extra marker is not part of the text.
		if (text.StartsWith("/", StringComparison.Ordinal) || (_language == SourceLanguage.VisualBasic && text.StartsWith("''", StringComparison.Ordinal))) text = text.TrimStart('/', '\'');
		_comments.Add((start, start, column, text));
		AdvanceTo(end);
		return null;
	}

	private Token? BlockComment(string open, string close, bool nests) {
		int start = _line;
		int column = Column;
		AdvanceTo(_index + open.Length);
		int depth = 1;
		StringBuilder text = new StringBuilder();
		while (_index < _source.Length) {
			if (string.CompareOrdinal(_source, _index, close, 0, close.Length) == 0) {
				AdvanceTo(_index + close.Length);
				if (--depth == 0) break;
				text.Append(close);
				continue;
			}
			if (nests && string.CompareOrdinal(_source, _index, open, 0, open.Length) == 0) {
				depth++;
				text.Append(open);
				AdvanceTo(_index + open.Length);
				continue;
			}
			// F# strings in a comment are read as strings, so a *) in one does not end it.
			if (nests && Current == '"') {
				int stringStart = _index;
				FSharpString(_line, Column);
				text.Append(_source, stringStart, _index - stringStart);
				continue;
			}
			if (Current != '\r') text.Append(Current);
			Advance();
		}
		string comment = text.ToString();
		// The asterisks that start the lines of a block comment are not part of the text.
		comment = string.Join("\n", comment.Split('\n').Select(line => line.TrimStart().StartsWith("*", StringComparison.Ordinal) ? line.TrimStart().Substring(1) : line));
		_comments.Add((start, _line, column, comment));
		return null;
	}

	private void SkipLine() {
		int end = _source.IndexOf('\n', _index);
		AdvanceTo(end < 0 ? _source.Length : end);
	}

	// C#: 'a', '\n', '\u0041', and '\''.
	private Token CSharpChar(int line, int column) {
		Advance();
		while (_index < _source.Length && Current != '\'' && Current != '\n') {
			if (Current == '\\') Advance();
			Advance();
		}
		if (Current == '\'') Advance();
		return new Token(TokenKind.Other, string.Empty, line, column);
	}

	private bool IsCSharpStringPrefix() {
		int index = _index;
		while (index < _source.Length && (_source[index] == '$' || _source[index] == '@')) index++;
		return index < _source.Length && _source[index] == '"' && index > _index;
	}

	private Token CSharpString(int line, int column) {
		int dollars = 0;
		bool verbatim = false;
		while (Current == '$' || Current == '@') {
			if (Current == '$') dollars++;
			else verbatim = true;
			Advance();
		}
		int quotes = 0;
		while (Peek(quotes) == '"') quotes++;
		string? text;
		if (quotes >= 3) {
			text = RawString(quotes, dollars);
		} else if (verbatim) {
			text = VerbatimString(dollars > 0 ? 1 : 0);
		} else {
			text = EscapedString(dollars > 0 ? 1 : 0, false);
		}
		// A UTF-8 literal, such as "text"u8, is bytes, not a string.
		if ((Current == 'u' || Current == 'U') && Peek(1) == '8') {
			AdvanceTo(_index + 2);
			return new Token(TokenKind.Other, string.Empty, line, column);
		}
		return dollars > 0 ? new Token(TokenKind.Interpolated, string.Empty, line, column) : new Token(TokenKind.String, text ?? string.Empty, line, column);
	}

	// A string with backslash escapes, as C# and F# have, which holes interrupt when it is interpolated.
	// An F# one can span lines.
	private string EscapedString(int holeBraces, bool fsharp) {
		Advance();
		StringBuilder text = new StringBuilder();
		while (_index < _source.Length) {
			char character = Current;
			if (character == '"') {
				Advance();
				break;
			}
			if (character == '\n' && !fsharp) break;
			if (holeBraces > 0 && character == '{') {
				if (Peek(1) == '{') {
					AdvanceTo(_index + 2);
					text.Append('{');
					continue;
				}
				SkipHole(holeBraces);
				continue;
			}
			if (holeBraces > 0 && character == '}' && Peek(1) == '}') {
				AdvanceTo(_index + 2);
				text.Append('}');
				continue;
			}
			if (character == '\\') {
				text.Append(Escape(fsharp));
				continue;
			}
			if (character != '\r') text.Append(character);
			Advance();
		}
		return text.ToString();
	}

	private string Escape(bool fsharp) {
		Advance();
		char character = Current;
		Advance();
		switch (character) {
			case 'n': return "\n";
			case 't': return "\t";
			case 'r': return "\r";
			case '0': return "\0";
			case 'a': return "\a";
			case 'b': return "\b";
			case 'f': return "\f";
			case 'v': return "\v";
			case 'e': return "\u001B";
			case 'x': return CodePoint(Hex(1, 4));
			case 'u': return CodePoint(Hex(4, 4));
			case 'U': return CodePoint(Hex(8, 8));
			case '\n':
				// An F# string continues on the next line after a backslash, without its indentation.
				if (fsharp) {
					while (Current == ' ' || Current == '\t') Advance();
					return string.Empty;
				}
				return "\\\n";
			case '\r':
				if (fsharp && Current == '\n') {
					Advance();
					while (Current == ' ' || Current == '\t') Advance();
					return string.Empty;
				}
				return "\\";
			default:
				// F#'s trigraphs, such as \065, are decimal.
				if (fsharp && char.IsDigit(character) && char.IsDigit(Current) && char.IsDigit(Peek(1))) {
					int value = (character - '0') * 100 + (Current - '0') * 10 + (Peek(1) - '0');
					AdvanceTo(_index + 2);
					return ((char)value).ToString();
				}
				return character.ToString();
		}
	}

	// A lone surrogate, such as half of "\uD83D\uDE00", is a char of its own.
	private static string CodePoint(int value) => value >= 0xD800 && value <= 0xDFFF ? ((char)value).ToString() : char.ConvertFromUtf32(value);

	private int Hex(int least, int most) {
		int value = 0;
		int digits = 0;
		while (digits < most && Uri.IsHexDigit(Current)) {
			value = value * 16 + Convert.ToInt32(Current.ToString(), 16);
			Advance();
			digits++;
		}
		return digits >= least && value <= 0x10FFFF ? value : 0xFFFD;
	}

	private string VerbatimString(int holeBraces) {
		Advance();
		StringBuilder text = new StringBuilder();
		while (_index < _source.Length) {
			char character = Current;
			if (character == '"') {
				if (Peek(1) == '"') {
					AdvanceTo(_index + 2);
					text.Append('"');
					continue;
				}
				Advance();
				break;
			}
			if (holeBraces > 0 && character == '{') {
				if (Peek(1) == '{') {
					AdvanceTo(_index + 2);
					text.Append('{');
					continue;
				}
				SkipHole(holeBraces);
				continue;
			}
			if (holeBraces > 0 && character == '}' && Peek(1) == '}') {
				AdvanceTo(_index + 2);
				text.Append('}');
				continue;
			}
			if (character != '\r') text.Append(character);
			Advance();
		}
		return text.ToString();
	}

	// C#'s raw strings, which end at as many quotes as they start with. One that starts at the end of
	// its line takes the lines up to the closing quotes, less the closing line's indentation. In an
	// interpolated one, a hole opens with as many braces as there are dollar signs.
	private string RawString(int quotes, int dollars) {
		AdvanceTo(_index + quotes);
		int lineEnd = _source.IndexOf('\n', _index);
		bool multiline = lineEnd >= 0 && _source.Substring(_index, lineEnd - _index).Trim().Length == 0;
		StringBuilder text = new StringBuilder();
		if (multiline) AdvanceTo(lineEnd + 1);
		while (_index < _source.Length) {
			if (Current == '"' && Count('"') >= quotes) {
				AdvanceTo(_index + Count('"'));
				break;
			}
			if (dollars > 0 && Current == '{' && Count('{') >= dollars) {
				int braces = Count('{');
				text.Append('{', braces - dollars);
				AdvanceTo(_index + braces - dollars);
				SkipHole(dollars);
				continue;
			}
			if (Current != '\r') text.Append(Current);
			Advance();
		}
		if (!multiline) return text.ToString();
		string content = text.ToString();
		int lastNewline = content.LastIndexOf('\n');
		string indentation = lastNewline < 0 ? content : content.Substring(lastNewline + 1);
		content = lastNewline < 0 ? string.Empty : content.Substring(0, lastNewline);
		return string.Join("\n", content.Split('\n').Select(line => line.StartsWith(indentation, StringComparison.Ordinal) ? line.Substring(indentation.Length) : line.TrimStart()));
	}

	private int Count(char character) {
		int count = 0;
		while (Peek(count) == character) count++;
		return count;
	}

	// An interpolated string's hole, from its opening braces to its closing ones, read as code, so
	// that a string or brace in it does not end the string.
	private void SkipHole(int braces) {
		AdvanceTo(_index + braces);
		int depth = 0;
		while (_index < _source.Length) {
			char character = Current;
			if (character == '}' && depth == 0) {
				AdvanceTo(Math.Min(_source.Length, _index + braces));
				return;
			}
			if (char.IsWhiteSpace(character)) {
				Advance();
				continue;
			}
			Token? token = Next();
			if (token is null) continue;
			if (token.Is('{') || token.Is('(') || token.Is('[')) depth++;
			if (token.Is(')') || token.Is(']')) depth--;
			if (token.Is('}')) depth--;
		}
	}

	// Visual Basic: "" is a quote, "a"c is a char, and an interpolated string's holes are in braces.
	private Token BasicString(int line, int column, bool interpolated) {
		Advance();
		StringBuilder text = new StringBuilder();
		while (_index < _source.Length) {
			char character = Current;
			if (IsQuote(character)) {
				if (IsQuote(Peek(1))) {
					AdvanceTo(_index + 2);
					text.Append('"');
					continue;
				}
				Advance();
				break;
			}
			if (character == '\n') break;
			if (interpolated && character == '{') {
				if (Peek(1) == '{') {
					AdvanceTo(_index + 2);
					text.Append('{');
					continue;
				}
				SkipHole(1);
				continue;
			}
			if (interpolated && character == '}' && Peek(1) == '}') {
				AdvanceTo(_index + 2);
				text.Append('}');
				continue;
			}
			if (character != '\r') text.Append(character);
			Advance();
		}
		if ((Current == 'c' || Current == 'C') && !char.IsLetterOrDigit(Peek(1))) {
			Advance();
			return new Token(TokenKind.Other, string.Empty, line, column);
		}
		return interpolated ? new Token(TokenKind.Interpolated, string.Empty, line, column) : new Token(TokenKind.String, text.ToString(), line, column);
	}

	// F#: 'a' and '\n' are chars, but 'T is a type variable and x' an identifier, so a quote only
	// starts a char when a closing quote follows the one character or escape.
	private Token FSharpChar(int line, int column) {
		int length = Peek(1) == '\\' ? (Peek(2) == 'u' ? 6 : Peek(2) == 'U' ? 10 : char.IsDigit(Peek(2)) ? 4 : 2) : 1;
		if (Peek(1 + length) == '\'' && Peek(1) != '\n') {
			AdvanceTo(_index + length + 2);
			return new Token(TokenKind.Other, string.Empty, line, column);
		}
		Advance();
		return new Token(TokenKind.Punctuation, "'", line, column);
	}

	private bool IsFSharpStringPrefix() {
		int index = _index;
		while (index < _source.Length && (_source[index] == '$' || _source[index] == '@')) index++;
		return index < _source.Length && _source[index] == '"' && index > _index;
	}

	private Token FSharpString(int line, int column) {
		int dollars = 0;
		bool verbatim = false;
		while (Current == '$' || Current == '@') {
			if (Current == '$') dollars++;
			else verbatim = true;
			Advance();
		}
		string text;
		if (Peek(0) == '"' && Peek(1) == '"' && Peek(2) == '"') {
			text = TripleQuoted(dollars);
		} else if (verbatim) {
			text = VerbatimString(dollars > 0 ? 1 : 0);
		} else {
			text = EscapedString(dollars > 0 ? 1 : 0, true);
		}
		// A byte string, such as "text"B, is bytes, not a string.
		if (Current == 'B') {
			Advance();
			return new Token(TokenKind.Other, string.Empty, line, column);
		}
		return dollars > 0 ? new Token(TokenKind.Interpolated, string.Empty, line, column) : new Token(TokenKind.String, text, line, column);
	}

	// F#'s triple-quoted strings have no escapes, and end at the first three quotes. In an
	// interpolated one, a hole opens with as many braces as there are dollar signs.
	private string TripleQuoted(int dollars) {
		AdvanceTo(_index + 3);
		StringBuilder text = new StringBuilder();
		while (_index < _source.Length) {
			if (Current == '"' && Peek(1) == '"' && Peek(2) == '"') {
				AdvanceTo(_index + 3);
				break;
			}
			if (dollars > 0 && Current == '{' && Count('{') >= dollars) {
				int braces = Count('{');
				text.Append('{', braces - dollars);
				AdvanceTo(_index + braces - dollars);
				SkipHole(dollars);
				continue;
			}
			if (Current != '\r') text.Append(Current);
			Advance();
		}
		return text.ToString();
	}

	private char Peek(int offset) => _index + offset < _source.Length ? _source[_index + offset] : '\0';

	private void Advance() {
		if (_index >= _source.Length) return;
		if (_source[_index] == '\n') {
			_line++;
			_lineStart = _index + 1;
		}
		_index++;
	}

	private void AdvanceTo(int index) {
		while (_index < index && _index < _source.Length) Advance();
	}
}
