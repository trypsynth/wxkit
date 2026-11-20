#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

// Checks an application's catalogs for translations that break what wxWidgets reads out of a
// label: one that loses its original's accelerator, the & before the letter that chooses a menu
// item or control from the keyboard, or adds one, or has two, and one that loses its original's
// shortcut, the text after a tab in a menu item, such as Ctrl+O, or adds one. msgfmt checks the
// placeholders of strings marked csharp-format, which the extractor marks. Entries that are not
// translated yet, and fuzzy ones, which are left out, are not checked.
public sealed class WxKitCheckCatalogs : Task {
	public ITaskItem[] Catalogs { get; set; } = Array.Empty<ITaskItem>();

	public override bool Execute() {
		foreach (ITaskItem catalog in Catalogs) {
			string path = catalog.GetMetadata("FullPath");
			if (!File.Exists(path)) continue;
			foreach (CatalogProblem problem in CatalogChecker.Check(PoCatalog.Read(File.ReadAllText(path)))) {
				Log.LogWarning(null, problem.Code, null, path, problem.Line, 0, 0, 0, problem.Message);
			}
		}
		return !Log.HasLoggedErrors;
	}
}

public sealed class CatalogProblem {
	public CatalogProblem(int line, string code, string message) {
		Line = line;
		Code = code;
		Message = message;
	}

	public int Line { get; }

	public string Code { get; }

	public string Message { get; }

	public override string ToString() => $"{Line}: {Code}: {Message}";
}

public static class CatalogChecker {
	public static List<CatalogProblem> Check(IEnumerable<PoEntry> entries) {
		List<CatalogProblem> problems = new List<CatalogProblem>();
		foreach (PoEntry entry in entries) {
			if (entry.Id.Length == 0 || entry.IsFuzzy || entry.IsObsolete) continue;
			for (int form = 0; form < entry.Translations.Count; form++) {
				string translation = entry.Translations[form].Text;
				if (translation.Length == 0) continue;
				string original = form > 0 && entry.Plural is not null ? entry.Plural : entry.Id;
				int line = entry.Translations[form].Line;
				int originalAccelerators = Accelerators(original);
				int accelerators = Accelerators(translation);
				if (originalAccelerators > 0 && accelerators == 0) problems.Add(new CatalogProblem(line, "WXKIT1001", $"The translation of \"{original}\" loses its accelerator, the & before the letter that chooses it from the keyboard."));
				if (originalAccelerators == 0 && accelerators > 0) problems.Add(new CatalogProblem(line, "WXKIT1002", $"The translation of \"{original}\" adds an accelerator, which its original does not have. Write && for an ampersand."));
				if (originalAccelerators == 1 && accelerators > 1) problems.Add(new CatalogProblem(line, "WXKIT1003", $"The translation of \"{original}\" has more than one accelerator. Write && for an ampersand."));
				bool originalShortcut = original.IndexOf('\t') >= 0;
				bool shortcut = translation.IndexOf('\t') >= 0;
				if (originalShortcut && !shortcut) problems.Add(new CatalogProblem(line, "WXKIT1004", $"The translation of \"{original.Replace("\t", "\\t")}\" loses its shortcut, the text after its tab."));
				if (!originalShortcut && shortcut) problems.Add(new CatalogProblem(line, "WXKIT1005", $"The translation of \"{original}\" adds a shortcut, a tab and the text after it, which its original does not have."));
			}
		}
		return problems;
	}

	// The accelerators in a label: each & before a character other than another &, which is an
	// ampersand, or a space, as in "Black & white", which is not a label's.
	public static int Accelerators(string text) {
		int count = 0;
		for (int index = 0; index < text.Length - 1; index++) {
			if (text[index] != '&') continue;
			char next = text[index + 1];
			if (next == '&') {
				index++;
				continue;
			}
			if (!char.IsWhiteSpace(next)) count++;
		}
		return count;
	}
}

public sealed class PoTranslation {
	public PoTranslation(string text, int line) {
		Text = text;
		Line = line;
	}

	public string Text { get; }

	public int Line { get; }
}

public sealed class PoEntry {
	public string? Context { get; set; }

	public string Id { get; set; } = string.Empty;

	public string? Plural { get; set; }

	public List<PoTranslation> Translations { get; } = new List<PoTranslation>();

	public bool IsFuzzy { get; set; }

	public bool IsObsolete { get; set; }

	public int Line { get; set; }
}

// A .po file's entries: their context, original, plural, and translations, with the lines their
// translations start on, and whether they are fuzzy or obsolete.
public static class PoCatalog {
	public static List<PoEntry> Read(string text) {
		List<PoEntry> entries = new List<PoEntry>();
		PoEntry? entry = null;
		bool fuzzy = false;
		string? lastKeyword = null;
		// Where the strings that continue on the lines below go.
		Action<string>? append = null;
		string[] lines = text.Replace("\r\n", "\n").Split('\n');
		for (int number = 1; number <= lines.Length; number++) {
			string line = lines[number - 1].Trim();
			bool isObsolete = line.StartsWith("#~", StringComparison.Ordinal);
			if (isObsolete) line = line.Substring(2).Trim();
			if (line.Length == 0) {
				append = null;
				continue;
			}
			if (line.StartsWith("#", StringComparison.Ordinal) && !isObsolete) {
				if (line.StartsWith("#,", StringComparison.Ordinal) && line.Substring(2).Split(',').Any(flag => flag.Trim() == "fuzzy")) fuzzy = true;
				append = null;
				continue;
			}
			if (line.StartsWith("\"", StringComparison.Ordinal)) {
				append?.Invoke(Unquote(line));
				continue;
			}
			int space = line.IndexOf(' ');
			if (space < 0) continue;
			string keyword = line.Substring(0, space);
			string value = Unquote(line.Substring(space + 1).Trim());
			// An entry starts at its msgctxt, or at its msgid when it has none.
			if (keyword == "msgctxt" || (keyword == "msgid" && lastKeyword != "msgctxt")) {
				entry = new PoEntry { IsFuzzy = fuzzy, IsObsolete = isObsolete, Line = number };
				entries.Add(entry);
				fuzzy = false;
			}
			lastKeyword = keyword;
			if (entry is null) continue;
			PoEntry current = entry;
			switch (keyword) {
				case "msgctxt":
					current.Context = value;
					append = more => current.Context += more;
					break;
				case "msgid":
					current.Id = value;
					append = more => current.Id += more;
					break;
				case "msgid_plural":
					current.Plural = value;
					append = more => current.Plural += more;
					break;
				default:
					if (keyword != "msgstr" && !keyword.StartsWith("msgstr[", StringComparison.Ordinal)) {
						append = null;
						break;
					}
					StringBuilder translation = new StringBuilder(value);
					int index = current.Translations.Count;
					current.Translations.Add(new PoTranslation(value, number));
					append = more => {
						translation.Append(more);
						current.Translations[index] = new PoTranslation(translation.ToString(), current.Translations[index].Line);
					};
					break;
			}
		}
		return entries;
	}

	private static string Unquote(string quoted) {
		if (quoted.Length < 2 || quoted[0] != '"' || quoted[quoted.Length - 1] != '"') return string.Empty;
		StringBuilder text = new StringBuilder(quoted.Length);
		for (int index = 1; index < quoted.Length - 1; index++) {
			char character = quoted[index];
			if (character != '\\' || index + 1 >= quoted.Length - 1) {
				text.Append(character);
				continue;
			}
			char escaped = quoted[++index];
			switch (escaped) {
				case 'n': text.Append('\n'); break;
				case 't': text.Append('\t'); break;
				case 'r': text.Append('\r'); break;
				case 'a': text.Append('\a'); break;
				case 'b': text.Append('\b'); break;
				case 'f': text.Append('\f'); break;
				case 'v': text.Append('\v'); break;
				default:
					if (escaped >= '0' && escaped <= '7') {
						int value = escaped - '0';
						int digits = 1;
						while (digits < 3 && index + 1 < quoted.Length - 1 && quoted[index + 1] >= '0' && quoted[index + 1] <= '7') {
							value = value * 8 + (quoted[++index] - '0');
							digits++;
						}
						text.Append((char)value);
					} else {
						text.Append(escaped);
					}
					break;
			}
		}
		return text.ToString();
	}
}
