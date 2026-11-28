using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace WxKit.Generator.Doxygen;

// Reads Doxygen's XML of the wxWidgets interface headers. The style and event tables are not
// Doxygen constructs: wxWidgets writes them with aliases that come out as a heading followed by a
// list, so they are read from each class's description in document order.
internal static partial class DoxygenReader {
	private const string StylesHeading = "Styles";
	private const string EventsHeadingPrefix = "Events ";
	private const string Etcetera = "etc.";

	// Abbreviations a sentence goes on after.
	private static readonly string[] Abbreviations = ["e.g.", "i.e."];

	// What Doxygen nests in a paragraph that isn't one of its sentences, such as a remark or a list.
	private static readonly HashSet<string> BlockElements = new(StringComparer.Ordinal) { "simplesect", "parameterlist", "itemizedlist", "orderedlist", "programlisting", "table", "xrefsect", "heading" };

	public static DoxygenApi Read(string directory) {
		XElement index = XDocument.Load(Path.Combine(directory, "index.xml")).Root!;
		Dictionary<string, DoxygenClass> classes = new(StringComparer.Ordinal);
		Dictionary<string, string> constants = new(StringComparer.Ordinal);
		Dictionary<string, string> eventTypes = new(StringComparer.Ordinal);
		Dictionary<string, DoxygenEnum> enums = new(StringComparer.Ordinal);
		Dictionary<string, List<DoxygenMember>> functions = new(StringComparer.Ordinal);
		HashSet<string> functionIds = new(StringComparer.Ordinal);
		foreach (XElement compound in index.Elements("compound")) {
			string kind = (string)compound.Attribute("kind")!;
			if (kind is not ("class" or "struct" or "file" or "group")) continue;
			XElement definition = XDocument.Load(Path.Combine(directory, (string)compound.Attribute("refid")! + ".xml")).Root!.Element("compounddef")!;
			// Event types are variables: the common ones in a group of their own, and a control's in
			// its own header, such as wxEVT_TEXT in textctrl.h. Each is kept with the header that
			// declares it, which the shim has to include to use it.
			foreach (XElement variable in definition.Descendants("memberdef").Where(static member => (string?)member.Attribute("kind") == "variable")) {
				string name = (string)variable.Element("name")!;
				string? file = (string?)variable.Element("location")?.Attribute("file");
				if (name.StartsWith("wxEVT_", StringComparison.Ordinal) && file is not null && file.StartsWith("interface/", StringComparison.Ordinal)) eventTypes.TryAdd(name, file["interface/".Length..]);
			}
			// Functions outside any class, such as wxBell, defined in a group of related functions, such
			// as the dialog functions, or in their header's own file. A file refers to those its groups define.
			if (kind is "group" or "file") {
				foreach (XElement function in definition.Elements("sectiondef").Where(static section => (string?)section.Attribute("kind") == "func").Elements("memberdef")) {
					if (!functionIds.Add((string)function.Attribute("id")!)) continue;
					DoxygenMember member = ReadMember(function);
					if (!functions.TryGetValue(member.Name, out List<DoxygenMember>? overloads)) functions[member.Name] = overloads = [];
					overloads.Add(member);
				}
			}
			if (kind == "group") continue;
			ReadConstants(definition, constants, enums, kind == "file" ? null : (string)definition.Element("compoundname")!);
			if (kind == "file") continue;
			DoxygenClass read = ReadClass(definition);
			// A class documented twice, which wxWidgets does for a few platform-specific ones, keeps its first entry.
			classes.TryAdd(read.Name, read);
		}
		return new DoxygenApi(classes, constants, eventTypes, enums) {
			Functions = functions.ToDictionary(static pair => pair.Key, static pair => (IReadOnlyList<DoxygenMember>)pair.Value, StringComparer.Ordinal),
		};
	}

	internal static DoxygenClass ReadClass(XElement definition) {
		string name = (string)definition.Element("compoundname")!;
		List<string> bases = [.. definition.Elements("basecompoundref").Select(static element => Normalize(element.Value))];
		List<DoxygenMember> members = [];
		foreach (XElement section in definition.Elements("sectiondef")) {
			foreach (XElement member in section.Elements("memberdef")) {
				if ((string?)member.Attribute("kind") == "function") members.Add(ReadMember(member));
			}
		}
		(List<DoxygenStyle> styles, List<DoxygenEvent> events) = ReadTables(definition.Element("detaileddescription"));
		string? header = (string?)definition.Element("includes");
		return new DoxygenClass(name, bases, header, Brief(definition), members, styles, events);
	}

	private static DoxygenMember ReadMember(XElement member) {
		List<DoxygenParameter> parameters = [];
		foreach (XElement parameter in member.Elements("param")) {
			// Doxygen gives an array parameter's brackets apart from its type, as in wxMenu* menus[]. They
			// stay part of the type, which no conversion accepts, so the parameter is not bound as a pointer.
			string type = Text(parameter.Element("type")) + (parameter.Element("array") is XElement array ? " " + array.Value : string.Empty);
			// A parameter list of just "void" has no parameters.
			if (type == "void" && parameter.Element("declname") is null) continue;
			XElement? defaultValue = parameter.Element("defval");
			parameters.Add(new DoxygenParameter(type, (string?)parameter.Element("declname") ?? $"arg{parameters.Count}", defaultValue is null ? null : Text(defaultValue)));
		}
		XElement? details = member.Element("detaileddescription");
		List<string> onlyFor = details is null ? [] : [.. details.Descendants("onlyfor").SelectMany(static element => element.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))];
		bool deprecated = details is not null && details.Descendants("xrefsect").Any(static element => ((string?)element.Attribute("id") ?? string.Empty).StartsWith("deprecated", StringComparison.Ordinal));
		return new DoxygenMember(
			(string)member.Element("name")!,
			Text(member.Element("type")),
			parameters,
			(string?)member.Attribute("const") == "yes",
			(string?)member.Attribute("static") == "yes",
			(string?)member.Attribute("virt") is "virtual" or "pure-virtual",
			(string?)member.Attribute("prot") == "public",
			deprecated,
			onlyFor,
			Brief(member)) {
			IsPureVirtual = (string?)member.Attribute("virt") == "pure-virtual",
		};
	}

	private static (List<DoxygenStyle> Styles, List<DoxygenEvent> Events) ReadTables(XElement? description) {
		List<DoxygenStyle> styles = [];
		List<DoxygenEvent> events = [];
		if (description is null) return (styles, events);
		string section = string.Empty;
		string? eventClass = null;
		bool inList = false;
		foreach (XElement element in description.Descendants()) {
			switch (element.Name.LocalName) {
				case "heading":
					section = Normalize(element.Value);
					eventClass = null;
					inList = false;
					break;
				// The sentence before an event list names the class its handlers receive, when they
				// all receive the same one. An entry can name its own, which the entry itself reads.
				case "ref" when !inList && section.StartsWith(EventsHeadingPrefix, StringComparison.Ordinal) && eventClass is null && EventClassPattern().IsMatch(element.Value):
					eventClass = element.Value;
					break;
				case "itemizedlist":
					inList = true;
					break;
				case "listitem" when section == StylesHeading:
					if (ReadEntry(element) is (string name, string text) && StyleNamePattern().IsMatch(name)) styles.Add(new DoxygenStyle(name, text));
					break;
				case "listitem" when section.StartsWith(EventsHeadingPrefix, StringComparison.Ordinal):
					if (ReadEntry(element) is (string macro, string about)) {
						// The event type the macro is named after, rather than another the entry mentions, as
						// EVT_TREE_ITEM_MENU's suggests wxEVT_CONTEXT_MENU before giving its own.
						string named = "wx" + macro.Split('(')[0].Trim();
						List<string> eventTypes = [.. element.Descendants("computeroutput").Select(static code => Normalize(code.Value)).Where(static code => code.StartsWith("wxEVT_", StringComparison.Ordinal))];
						string? eventType = eventTypes.Contains(named) ? named : eventTypes.FirstOrDefault();
						string? ownClass = element.Descendants("ref").Select(static reference => reference.Value).FirstOrDefault(static text => EventClassPattern().IsMatch(text));
						events.Add(new DoxygenEvent(macro.Split('(')[0].Trim(), eventType, ownClass ?? eventClass, about));
					}
					break;
			}
		}
		return (styles, events);
	}

	// A table entry reads "NAME: what it does", or "EVT_NAME(id, func): what it does", and can end
	// with the version of wxWidgets that added it, which isn't what it does.
	private static (string Name, string Text)? ReadEntry(XElement item) {
		string text = Normalize(string.Concat(item.DescendantNodes().OfType<XText>().Where(static node => !node.Ancestors("simplesect").Any(static section => (string?)section.Attribute("kind") == "since")).Select(static node => node.Value)));
		int colon = text.IndexOf("):", StringComparison.Ordinal) is int call and >= 0 ? call + 1 : text.IndexOf(':');
		if (colon <= 0) return null;
		return (text[..colon].Trim(), text[(colon + 1)..].Trim());
	}

	internal static void ReadConstants(XElement definition, Dictionary<string, string> constants, Dictionary<string, DoxygenEnum> enums, string? owner) {
		foreach (XElement member in definition.Descendants("memberdef")) {
			string? kind = (string?)member.Attribute("kind");
			if (kind == "define" && member.Element("initializer") is XElement initializer) {
				constants.TryAdd((string)member.Element("name")!, Normalize(initializer.Value));
			} else if (kind == "enum") {
				string enumName = (string)member.Element("name")!;
				// An anonymous enum only holds constants, which the loop below still reads.
				if (enumName.Length > 0 && !enumName.StartsWith('@')) {
					string qualified = owner is null ? enumName : $"{owner}::{enumName}";
					DoxygenEnum read = new(enumName, [.. member.Elements("enumvalue").Select(static value => new DoxygenEnumValue((string)value.Element("name")!, Brief(value)))]) {
						Scope = (string?)member.Attribute("strong") == "yes" ? qualified : owner,
					};
					enums.TryAdd(enumName, read);
					if (owner is not null) enums.TryAdd(qualified, read);
				}
				// Values without an initializer follow the one before, as in C.
				string previous = "-1";
				foreach (XElement value in member.Elements("enumvalue")) {
					string name = (string)value.Element("name")!;
					string given = Normalize((string?)value.Element("initializer") ?? string.Empty).TrimStart('=').Trim();
					constants.TryAdd(name, given.Length > 0 ? given : $"({previous}) + 1");
					previous = name;
				}
			}
		}
	}

	// wxWidgets' Doxyfile takes a comment's first sentence as its brief, and Doxygen ends the sentence
	// at the first full stop before a space, even one of "e.g." or "i.e.", as in "Reparents the
	// window, i.e. the window will be removed from its current parent window (e.g.", and starts the
	// detailed description with the rest of it. The brief takes the rest back, up to where the
	// sentence ends.
	internal static string Brief(XElement element) {
		string brief = Text(element.Element("briefdescription"));
		if (!IsCutShort(brief, out bool always) || element.Element("detaileddescription")?.Element("para") is not XElement next) return brief;
		string rest = Normalize(string.Concat(next.DescendantNodes().OfType<XText>().Where(static node => !node.Ancestors().Any(static ancestor => BlockElements.Contains(ancestor.Name.LocalName))).Select(static node => node.Value)));
		if (rest.Length == 0 || (!always && !char.IsLower(rest[0]))) return brief;
		return $"{brief} {FirstSentence(rest)}";
	}

	// "etc." can end a sentence too, so a brief ending with it is only cut short when what follows
	// it starts in lowercase.
	private static bool IsCutShort(string brief, out bool always) {
		always = Abbreviations.Any(abbreviation => EndsWithWord(brief, abbreviation));
		return always || EndsWithWord(brief, Etcetera);
	}

	private static bool EndsWithWord(string text, string word) => text.EndsWith(word, StringComparison.Ordinal) && (text.Length == word.Length || !char.IsLetter(text[^(word.Length + 1)]));

	// The text up to the first full stop that ends a sentence, or all of it when none does.
	internal static string FirstSentence(string text) {
		for (int index = text.IndexOf('.', StringComparison.Ordinal); index >= 0; index = text.IndexOf('.', index + 1)) {
			if (index + 1 < text.Length && !char.IsWhiteSpace(text[index + 1])) continue;
			string sentence = text[..(index + 1)];
			if (Abbreviations.Any(abbreviation => EndsWithWord(sentence, abbreviation))) continue;
			string after = text[(index + 1)..].TrimStart();
			if (EndsWithWord(sentence, Etcetera) && after.Length > 0 && !char.IsUpper(after[0])) continue;
			return sentence;
		}
		return text;
	}

	internal static string Text(XElement? element) => element is null ? string.Empty : Normalize(string.Concat(element.DescendantNodes().OfType<XText>().Select(static node => node.Value)));

	private static string Normalize(string text) => WhitespacePattern().Replace(text, " ").Trim();

	[GeneratedRegex(@"\s+")]
	private static partial Regex WhitespacePattern();

	[GeneratedRegex(@"^wx\w*Event$")]
	private static partial Regex EventClassPattern();

	[GeneratedRegex(@"^wx[A-Z0-9_]+$")]
	private static partial Regex StyleNamePattern();
}
