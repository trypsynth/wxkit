using System.Text.RegularExpressions;

namespace WxKit.Generator.Doxygen;

// A few constants are only in wxWidgets's real headers, not its interface headers, though the
// interface headers use them: wxFONTFAMILY_DEFAULT is wxDEFAULT, for example, which only the
// deprecated enum in wx/defs.h defines. This reads that enum from the same pinned wxWidgets.
internal static partial class HeaderConstants {
	public static Dictionary<string, string> ReadDeprecatedGuiConstants(string defsHeader) {
		Dictionary<string, string> constants = new(StringComparer.Ordinal);
		Match block = EnumPattern().Match(File.ReadAllText(defsHeader));
		if (!block.Success) throw new InvalidOperationException($"{defsHeader} no longer has the wxDeprecatedGUIConstants enum.");
		string body = CommentPattern().Replace(block.Groups[1].Value, " ");
		string previous = "-1";
		foreach (string entry in body.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)) {
			string[] parts = entry.Split('=', 2, StringSplitOptions.TrimEntries);
			// A value without an initializer follows the one before, as in C.
			constants[parts[0]] = parts.Length == 2 ? parts[1] : $"({previous}) + 1";
			previous = parts[0];
		}
		return constants;
	}

	// The interface headers sometimes list an enum's values without their initializers, as for
	// wxTextValidatorStyle's flags, which would then count up from zero. This reads the values of
	// the enums in the real headers, following C for those without an initializer.
	//
	// The real headers also show which constants depend on the platform or the build, which a C#
	// constant cannot: an enum value inside an #if, or one that follows such a value without an
	// initializer, as wxCURSOR_ARROWWAIT follows wxGTK's wxCURSOR_DEFAULT, and a #define written
	// more than one way, as wxTR_DEFAULT_STYLE is for each platform.
	// The headers of each directory, but not its subdirectories, which hold the ports' own headers,
	// whose values would differ.
	public static HeaderValues ReadValues(params string[] includeDirectories) {
		Dictionary<string, string> values = new(StringComparer.Ordinal);
		HashSet<string> conditional = new(StringComparer.Ordinal);
		Dictionary<string, string> defines = new(StringComparer.Ordinal);
		foreach (string header in includeDirectories.SelectMany(static directory => Directory.EnumerateFiles(directory, "*.h"))) {
			string text = ContinuationPattern().Replace(CommentPattern().Replace(File.ReadAllText(header), " "), " ");
			foreach (Match define in DefinePattern().Matches(text)) {
				string name = define.Groups[1].Value;
				string value = WhitespacePattern().Replace(define.Groups[2].Value, " ").Trim();
				if (defines.TryGetValue(name, out string? existing) && existing != value) conditional.Add(name);
				defines[name] = value;
			}
			// Each directive becomes an entry of its own, so the enum's entries can be read in order with them.
			string marked = DirectivePattern().Replace(text, static directive => $",#{directive.Groups[1].Value},");
			foreach (Match block in AnyEnumPattern().Matches(marked)) {
				string previous = "-1";
				int depth = 0;
				bool shifted = false;
				foreach (string entry in block.Groups[1].Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)) {
					if (entry.StartsWith('#')) {
						if (entry.StartsWith("#if", StringComparison.Ordinal)) depth++;
						if (entry == "#endif") depth--;
						continue;
					}
					string[] parts = entry.Split('=', 2, StringSplitOptions.TrimEntries);
					if (!IdentifierPattern().IsMatch(parts[0])) break;
					string value = parts.Length == 2 ? WhitespacePattern().Replace(parts[1], " ") : $"({previous}) + 1";
					previous = parts[0];
					if (depth > 0 || (shifted && parts.Length == 1)) conditional.Add(parts[0]);
					// A value after an #if follows it only until one gives its own value, as
					// wxID_EDIT = 5030 does, after which the count no longer depends on the platform.
					shifted = depth > 0 || (shifted && parts.Length == 1);
					if (values.TryGetValue(parts[0], out string? existing) && existing != value) conditional.Add(parts[0]);
					values[parts[0]] = value;
				}
			}
		}
		foreach (string name in conditional) values.Remove(name);
		foreach (string name in conditional) defines.Remove(name);
		return new HeaderValues(values, conditional) { Defines = defines };
	}

	// The art provider's ids are macros that make strings, which the interface headers only declare
	// as variables: wxART_MAKE_ART_ID(X) is "X", and wxART_MAKE_CLIENT_ID(X) is "X_C".
	public static Dictionary<string, string> ReadArtIds(string artProviderHeader) {
		Dictionary<string, string> ids = new(StringComparer.Ordinal);
		foreach (Match match in ArtIdPattern().Matches(File.ReadAllText(artProviderHeader))) {
			ids[match.Groups[1].Value] = match.Groups[3].Value + (match.Groups[2].Value == "CLIENT" ? "_C" : string.Empty);
		}
		if (ids.Count == 0) throw new InvalidOperationException($"{artProviderHeader} no longer defines its ids the way the generator reads them.");
		return ids;
	}

	[GeneratedRegex(@"^#define\s+(wxART_\w+)\s+wxART_MAKE_(ART|CLIENT)_ID\((\w+)\)", RegexOptions.Multiline)]
	private static partial Regex ArtIdPattern();

	[GeneratedRegex(@"enum wxDeprecatedGUIConstants\s*\{(.*?)\};", RegexOptions.Singleline)]
	private static partial Regex EnumPattern();

	[GeneratedRegex(@"/\*.*?\*/|//[^\n]*", RegexOptions.Singleline)]
	private static partial Regex CommentPattern();

	[GeneratedRegex(@"\benum(?:\s+class)?(?:\s+\w+)?(?:\s*:\s*\w+)?\s*\{([^{}]*)\}", RegexOptions.Singleline)]
	private static partial Regex AnyEnumPattern();

	[GeneratedRegex(@"^[ \t]*#[ \t]*(\w+).*$", RegexOptions.Multiline)]
	private static partial Regex DirectivePattern();

	[GeneratedRegex(@"^[ \t]*#[ \t]*define[ \t]+(wx\w+)\b(?!\()[ \t]+(.+?)[ \t]*\r?$", RegexOptions.Multiline)]
	private static partial Regex DefinePattern();

	// A backslash at the end of a line continues it, in a macro or, as for wxEVT_CATEGORY_ALL, an initializer.
	[GeneratedRegex(@"\\[ \t]*\r?\n")]
	private static partial Regex ContinuationPattern();

	// wxWidgets's constants, such as wxID_OK, the key codes, such as WXK_LEFT, and the few that mix
	// cases, such as wxLayout_RightToLeft.
	[GeneratedRegex(@"^(?:wx|WX)\w+$")]
	private static partial Regex IdentifierPattern();

	[GeneratedRegex(@"\s+")]
	private static partial Regex WhitespacePattern();
}

/// <param name="Values">The enum values the real headers give, by name.</param>
/// <param name="Conditional">The constants whose value depends on the platform or the build.</param>
internal sealed record HeaderValues(IReadOnlyDictionary<string, string> Values, IReadOnlySet<string> Conditional) {
	/// <summary>The real headers' #defines, by name, other than those written more than one way.</summary>
	public IReadOnlyDictionary<string, string> Defines { get; init; } = new Dictionary<string, string>();
}
