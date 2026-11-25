using System.Text.RegularExpressions;

namespace WxKit.Generator.Emitting;

// Writes the LibraryImport declarations for the shim's hand-written functions from wxkit.h, which
// declares each of them, so the C# side can't disagree with the C side. It also checks that every
// function the shim's sources define is declared there, and every one declared is defined: the
// C++ compiler checks that a definition matches its declaration, since the sources include it.
internal static partial class HeaderInteropEmitter {
	private static readonly Dictionary<string, string> Numbers = new(StringComparer.Ordinal) {
		{ "void", "void" },
		{ "int8_t", "sbyte" },
		{ "uint8_t", "byte" },
		{ "int16_t", "short" },
		{ "uint16_t", "ushort" },
		{ "int32_t", "int" },
		{ "uint32_t", "uint" },
		{ "int64_t", "long" },
		{ "uint64_t", "ulong" },
		{ "size_t", "nuint" },
		{ "intptr_t", "nint" },
		{ "uintptr_t", "nuint" },
		{ "wxk_ItemId", "nint" },
		{ "double", "double" },
		{ "float", "float" },
		{ "char16_t", "char" },
	};

	// Structs the shim passes by value, which have a C# type of the same layout in WxKit itself.
	private static readonly Dictionary<string, string> Values = new(StringComparer.Ordinal) {
		{ "wxk_Point", "Point" },
		{ "wxk_Size", "Size" },
		{ "wxk_Rect", "Rect" },
		{ "wxk_GridPosition", "GridPosition" },
		{ "wxk_GridSpan", "GridSpan" },
	};

	private static readonly HashSet<string> Keywords = ["base", "checked", "class", "default", "event", "fixed", "lock", "object", "operator", "out", "params", "ref", "string", "this"];

	public static string Source(string header, IEnumerable<string> sources) {
		string text = File.ReadAllText(header).Replace("\r\n", "\n", StringComparison.Ordinal);
		Dictionary<string, string> callbacks = [];
		foreach (Match typedef in CallbackTypedef().Matches(text)) callbacks[typedef.Groups["name"].Value] = Delegate(typedef.Groups["returns"].Value, typedef.Groups["parameters"].Value, callbacks);
		CodeWriter writer = new();
		writer.Line(ManagedEmitter.Banner)
			.Line("using System.Runtime.CompilerServices;")
			.Line("using System.Runtime.InteropServices;")
			.Line()
			.Line("namespace WxKit.Interop;")
			.Line()
			.Open("internal static unsafe partial class WxKitNative");
		HashSet<string> declared = new(StringComparer.Ordinal);
		bool first = true;
		string? section = null;
		string[] lines = text.Split('\n');
		for (int index = 0; index < lines.Length; index++) {
			// A section's heading is a comment on a line of its own, with a blank line after it.
			if (lines[index].StartsWith("// ", StringComparison.Ordinal) && index + 1 < lines.Length && lines[index + 1].Length == 0 && (index == 0 || lines[index - 1].Length == 0)) {
				section = lines[index];
				continue;
			}
			if (!lines[index].StartsWith("WXK_FUNCTION(", StringComparison.Ordinal)) continue;
			string declaration = lines[index];
			while (!declaration.EndsWith(';')) declaration += " " + lines[++index].Trim();
			Match function = Function().Match(declaration);
			if (!function.Success) throw new InvalidOperationException($"wxkit.h declares a function WxKit cannot read: {declaration}");
			string name = function.Groups["name"].Value;
			if (!declared.Add(name)) throw new InvalidOperationException($"wxkit.h declares {name} twice.");
			if (!first) writer.Line();
			first = false;
			if (section is not null) {
				writer.Line(section).Line();
				section = null;
			}
			string parameters = string.Join(", ", Parameters(function.Groups["parameters"].Value).Select(parameter => Parameter(parameter, callbacks)));
			writer.Line($"[LibraryImport(Library, EntryPoint = \"{name}\")]");
			writer.Line("[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]");
			writer.Line($"public static partial {Type(function.Groups["returns"].Value, callbacks)} {ManagedName(name)}({parameters});");
		}
		writer.Close();
		Check(declared, sources);
		return writer.ToString();
	}

	// wxk_Menu_new is MenuNew: each part after wxk_ begins with a capital.
	private static string ManagedName(string name) => string.Concat(name["wxk_".Length..].Split('_').Select(static part => char.ToUpperInvariant(part[0]) + part[1..]));

	private static void Check(HashSet<string> declared, IEnumerable<string> sources) {
		HashSet<string> defined = new(StringComparer.Ordinal);
		foreach (string source in sources) {
			foreach (Match definition in Definition().Matches(File.ReadAllText(source))) defined.Add(definition.Groups["name"].Value);
		}
		List<string> undeclared = [.. defined.Except(declared).Order(StringComparer.Ordinal)];
		List<string> undefined = [.. declared.Except(defined).Order(StringComparer.Ordinal)];
		if (undeclared.Count > 0) throw new InvalidOperationException($"The shim defines functions wxkit.h does not declare: {string.Join(", ", undeclared)}.");
		if (undefined.Count > 0) throw new InvalidOperationException($"wxkit.h declares functions the shim does not define: {string.Join(", ", undefined)}.");
		List<string> unguarded = [.. sources.SelectMany(static source => Unguarded(File.ReadAllText(source).Replace("\r\n", "\n", StringComparison.Ordinal))).Distinct().Order(StringComparer.Ordinal)];
		if (unguarded.Count > 0) throw new InvalidOperationException($"The shim's functions call into C++ outside wxk::Guard, so an exception would end the process rather than reach .NET: {string.Join(", ", unguarded)}. Run the call in Guard, or put a line before the function that begins \"// Can't throw:\" and says why.");
	}

	// A function whose body calls something, other than through Guard, unless the line before it says
	// why it can't throw. A body that only stores, returns, or deletes a struct of WxKit's own is fine.
	private static IEnumerable<string> Unguarded(string source) {
		foreach (Match definition in Body().Matches(source)) {
			string body = BodyFrom(source, definition.Index + definition.Length);
			if (body.Contains("Guard", StringComparison.Ordinal) || !Call().IsMatch(body)) continue;
			int line = source.LastIndexOf('\n', definition.Index);
			int previous = line <= 0 ? -1 : source.LastIndexOf('\n', line - 1);
			if (line > 0 && source[(previous + 1)..line].StartsWith("// Can't throw:", StringComparison.Ordinal)) continue;
			yield return definition.Groups["name"].Value;
		}
	}

	// The text between a function's braces, from just after the opening one, without its comments and
	// literals, which could hold braces of their own.
	private static string BodyFrom(string source, int start) {
		System.Text.StringBuilder body = new();
		int depth = 1;
		for (int index = start; index < source.Length; index++) {
			char character = source[index];
			if (character is '"' or '\'') {
				char quote = character;
				for (index++; index < source.Length && source[index] != quote; index++) {
					if (source[index] == '\\') index++;
				}
				body.Append(quote).Append(quote);
				continue;
			}
			if (character == '/' && index + 1 < source.Length && source[index + 1] == '/') {
				index = source.IndexOf('\n', index) is int end and >= 0 ? end : source.Length;
				body.Append('\n');
				continue;
			}
			if (character == '{') depth++;
			if (character == '}' && --depth == 0) break;
			body.Append(character);
		}
		return body.ToString();
	}

	private static IEnumerable<string> Parameters(string list) {
		if (list.Trim() is "" or "void") yield break;
		int depth = 0;
		int start = 0;
		for (int index = 0; index < list.Length; index++) {
			switch (list[index]) {
				case '(':
					depth++;
					break;
				case ')':
					depth--;
					break;
				case ',' when depth == 0:
					yield return list[start..index].Trim();
					start = index + 1;
					break;
				default:
					break;
			}
		}
		yield return list[start..].Trim();
	}

	private static string Parameter(string parameter, Dictionary<string, string> callbacks) {
		Match pointer = FunctionPointer().Match(parameter);
		if (pointer.Success) return $"{Delegate(pointer.Groups["returns"].Value, pointer.Groups["parameters"].Value, callbacks)} {Escape(pointer.Groups["name"].Value)}";
		Match named = NamedParameter().Match(parameter);
		if (!named.Success) throw new InvalidOperationException($"wxkit.h has a parameter WxKit cannot read: {parameter}");
		return $"{Type(named.Groups["type"].Value, callbacks)} {Escape(named.Groups["name"].Value)}";
	}

	private static string Delegate(string returns, string parameters, Dictionary<string, string> callbacks) {
		IEnumerable<string> types = Parameters(parameters).Select(parameter => {
			Match named = NamedParameter().Match(parameter);
			return Type(named.Success ? named.Groups["type"].Value : parameter, callbacks);
		});
		return $"delegate* unmanaged[Cdecl]<{string.Join(", ", types.Append(Type(returns, callbacks)))}>";
	}

	private static string Type(string type, Dictionary<string, string> callbacks) {
		string bare = type.Replace("const ", string.Empty, StringComparison.Ordinal).Trim();
		int pointers = bare.Count(static character => character == '*');
		string name = bare.TrimEnd('*', ' ');
		string mapped = Numbers.GetValueOrDefault(name)
			?? Values.GetValueOrDefault(name)
			?? callbacks.GetValueOrDefault(name)
			?? (name.StartsWith("wxk_", StringComparison.Ordinal) ? "Wxk" + name["wxk_".Length..] : throw new InvalidOperationException($"wxkit.h uses the type {type}, which WxKit has no C# type for."));
		return mapped + new string('*', pointers);
	}

	private static string Escape(string name) => Keywords.Contains(name) ? "@" + name : name;

	[GeneratedRegex(@"^typedef (?<returns>[\w ]+\**)\s*\(\*(?<name>wxk_\w+)\)\((?<parameters>[^;]*)\);", RegexOptions.Multiline)]
	private static partial Regex CallbackTypedef();

	[GeneratedRegex(@"^WXK_FUNCTION\((?<returns>[^)]*)\) (?<name>wxk_\w+)\((?<parameters>.*)\) WXK_NOEXCEPT;$")]
	private static partial Regex Function();

	[GeneratedRegex(@"^(?<returns>[\w ]+\**)\s*\(\*(?<name>\w+)\)\((?<parameters>.*)\)$")]
	private static partial Regex FunctionPointer();

	[GeneratedRegex(@"^(?<type>.+?[\s*])(?<name>\w+)$")]
	private static partial Regex NamedParameter();

	[GeneratedRegex(@"^WXK_FUNCTION\([^)]*\)\s+(?<name>wxk_\w+)\(", RegexOptions.Multiline)]
	private static partial Regex Definition();

	[GeneratedRegex(@"^WXK_FUNCTION\([^)]*\)\s+(?<name>wxk_\w+)\([^{;]*\)\s*noexcept\s*\{", RegexOptions.Multiline)]
	private static partial Regex Body();

	// A call, which is a name and an opening parenthesis, other than the keywords that look like one.
	[GeneratedRegex(@"\b(?!if\b|while\b|for\b|return\b|sizeof\b|switch\b|defined\b)[A-Za-z_]\w*\s*\(")]
	private static partial Regex Call();
}
