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
			.Open("public static unsafe partial class WxKitNative");
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
}
