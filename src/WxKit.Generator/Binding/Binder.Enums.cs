using System.Globalization;
using System.Text.RegularExpressions;
using WxKit.Generator.Doxygen;

namespace WxKit.Generator.Binding;

// A class's styles and wxWidgets' enums, as C# enums.
internal sealed partial class Binder {
	private BoundStyles? BindStyles(BoundClass bound) {
		if (bound.Source.Styles.Count == 0 && bound.Tweak.AddedStyles.Count == 0) return null;
		string? prefix = bound.Tweak.StylePrefix ?? Naming.SharedStylePrefix(bound.Source.Styles.Select(static style => style.Name), api.Constants.ContainsKey);
		List<BoundStyle> members = [];
		foreach (DoxygenStyle style in bound.Source.Styles.Where(style => !bound.Tweak.IgnoredStyles.Contains(style.Name))) {
			// Extra styles, such as wxDIALOG_EX_CONTEXTHELP, are set with SetExtraStyle rather than
			// passed with the others, though some classes list them in the same table.
			if (style.Name.Contains("_EX_", StringComparison.Ordinal)) {
				_skipped.Add($"{bound.CppName} style {style.Name}: extra styles are not bound yet");
				continue;
			}
			if (_constants.Value(style.Name) is not long value) {
				_skipped.Add($"{bound.CppName} style {style.Name}: its value is unknown");
				continue;
			}
			// A style that is 0 is the absence of the others, which None says. Under a name of its
			// own, HasFlag would find it in every value.
			if (value == 0) {
				_skipped.Add($"{bound.CppName} style {style.Name}: it is 0, which None is");
				continue;
			}
			string name = Naming.Style(style.Name, prefix, bound.CppName);
			if (members.Any(existing => existing.Name == name)) continue;
			members.Add(new BoundStyle(name, style.Name, value, style.Description));
		}
		foreach ((string constant, string name, string description) in bound.Tweak.AddedStyles) {
			long value = _constants.Value(constant) ?? _constants.Evaluate(constant) ?? throw new InvalidOperationException($"A tweak gives {bound.CppName} the style {constant}, whose value is unknown.");
			members.RemoveAll(existing => existing.Name == name);
			members.Add(new BoundStyle(name, constant, value, description));
		}
		// A window's own enum has the styles every window takes too, such as the borders, so a
		// borderless list is ListBoxStyle.BorderNone rather than a cast. One whose number the class
		// uses for a style of its own is left out, as is one the class already lists.
		if (bound.CppName != "wxWindow" && bound.Handle == HandleKind.Window && !bound.Tweak.KeepsStylesApart && _classes.GetValueOrDefault("wxWindow")?.Styles is BoundStyles shared) {
			foreach (BoundStyle style in shared.Members.Where(static style => style.Value != 0)) {
				if (!members.Any(existing => existing.Name == style.Name || existing.Value == style.Value)) members.Add(style);
			}
		}
		// Every style enum has None, even when a style such as wxTE_LEFT is also 0, so a default of 0 reads as None.
		if (!members.Any(static member => member.Name == "None")) members.Insert(0, new BoundStyle("None", "0", 0, "No styles."));
		return new BoundStyles(bound.Name + "Style", members);
	}

	// The C# enum for a C++ one, made the first time a bound member uses it. Its members drop the
	// prefix they all share, so wxSHOW_EFFECT_ROLL_TO_LEFT becomes ShowEffect.RollToLeft.
	private BoundEnum? EnumFor(string cppName) {
		if (cppName.Length == 0) return null;
		if (_enums.TryGetValue(cppName, out BoundEnum? cached)) return cached;
		_enums[cppName] = null;
		if (!api.Enums.TryGetValue(cppName, out DoxygenEnum? source) || source.Values.Count == 0) return null;
		EnumTweak? tweak = tweaks.Enums.GetValueOrDefault(cppName);
		string managed = tweak?.Name ?? Naming.Type(cppName.Contains("::", StringComparison.Ordinal) ? cppName[(cppName.LastIndexOf(':') + 1)..] : cppName);
		string[] excluded = [.. tweak?.Exclude ?? [], .. tweaks.ExcludedEnumMembers.GetValueOrDefault(cppName) ?? []];
		List<BoundStyle> members = EnumMembers(cppName, source, tweak?.MemberPrefix, excluded);
		if (tweaks.AddedEnumMembers.TryGetValue(cppName, out List<(string Name, long Value, string Description, string Wraps)>? added)) members.AddRange(added.Select(static member => new BoundStyle(member.Name, member.Wraps, member.Value, member.Description)));
		if (tweak?.IsFlags == true && members.All(static member => member.Value != 0)) members.Insert(0, new BoundStyle("None", "0", 0, "None of the flags."));
		BoundEnum bound = new(managed, cppName, members) { IsFlags = tweak?.IsFlags ?? false, Guard = tweak?.Guard };
		_enums[cppName] = bound;
		return bound;
	}

	private List<BoundStyle> EnumMembers(string owner, DoxygenEnum source, string? memberPrefix, string[]? exclude = null, bool sentinels = false) {
		int shared = SharedWords(source);
		List<BoundStyle> members = [];
		foreach (DoxygenEnumValue value in source.Values) {
			// A tweak leaves out a sentinel, or an alias, such as wxALIGN_MASK.
			if (exclude?.Contains(value.Name) == true) continue;
			// A bound of the enum's values, or a value for none of them, is for wxWidgets' own checks.
			// A class of constants keeps its bounds, such as the styled text control's last style.
			if (!sentinels && IsSentinel(value.Name)) continue;
			// A value that is unknown, or depends on the platform, such as wxCURSOR_ARROWWAIT, is left out.
			if (_constants.Value(value.Name) is not long number) {
				_skipped.Add($"enum {owner}: the value of {value.Name} is unknown or depends on the platform");
				continue;
			}
			string name = tweaks.EnumMemberNames.TryGetValue(value.Name, out string? given) || StyledTextWords.Commands.TryGetValue(value.Name, out given) ? given : memberPrefix is not null && value.Name.StartsWith(memberPrefix, StringComparison.Ordinal) ? Naming.Words(value.Name[memberPrefix.Length..]) : MemberName(value.Name, shared);
			if (char.IsDigit(name[0])) name = "Value" + name;
			// A value of an enum class, or of an enum in a class, is named with its scope, as in
			// wxSysMetric::Other, which says what it wraps, and is what the shim checks its value as.
			string constant = source.Scope is string scope ? $"{scope}::{value.Name}" : value.Name;
			if (members.All(existing => existing.Name != name)) members.Add(new BoundStyle(name, constant, number, value.Brief));
		}
		return members;
	}

	private static bool IsSentinel(string constant) => constant.EndsWith("_MAX", StringComparison.OrdinalIgnoreCase) || constant.EndsWith("_INVALID", StringComparison.Ordinal) || constant.EndsWith("_FIRST", StringComparison.Ordinal) || constant.EndsWith("_LAST", StringComparison.Ordinal) || constant is "wxBORDER_MASK" or "wxFONTENCODING_MACMAX" or "wxBITMAP_TYPE_TIF" or "wxBITMAP_TYPE_TIF_RESOURCE";

	// Constants wxWidgets defines one by one, such as wxACC_STATE_SYSTEM_FOCUSED, as one enum in
	// the order of their values. Flags get None, for none of them.
	// Constants a class of constants leaves out, since an enum has them.
	private readonly HashSet<string> _groupedConstants = new(StringComparer.Ordinal);

	private void BindConstantEnum(ConstantEnumTweak group) {
		List<string> names = group.Constants is IReadOnlyList<string> named
			? [.. named.Select(name => api.Constants.ContainsKey(name) ? name : throw new InvalidOperationException($"A tweak binds the constant {name}, which the interface headers do not have."))]
			: [.. api.Constants.Keys.Where(name => name.StartsWith(group.Prefix, StringComparison.Ordinal) && !group.ExcludePrefixes.Any(excluded => name.StartsWith(excluded, StringComparison.Ordinal))).Order(StringComparer.Ordinal)];
		// A group taken from a class of constants, as the styled text control's are, has only those
		// the real headers define and don't mark deprecated, as the class does, which then leaves them out.
		if (tweaks.PrefixConstantClasses.Keys.Any(prefix => group.Prefix.StartsWith(prefix, StringComparison.Ordinal) || names.Any(name => name.StartsWith(prefix, StringComparison.Ordinal)))) {
			names.RemoveAll(name => !api.HeaderDefines.TryGetValue(name, out string? defined) || defined.Contains("DEPRECATED", StringComparison.Ordinal));
			_groupedConstants.UnionWith(names);
		}
		if (names.Count == 0) throw new InvalidOperationException($"A tweak binds the constants named {group.Prefix}*, which the interface headers do not have.");
		DoxygenEnum source = new($"{group.Prefix}*", [.. names.Select(static name => new DoxygenEnumValue(name, string.Empty))]);
		List<BoundStyle> members = [.. EnumMembers(source.Name, source, memberPrefix: null).OrderBy(static member => member.Value)];
		if (group.IsFlags && members.All(static member => member.Value != 0)) members.Insert(0, new BoundStyle("None", "0", 0, "None of the flags."));
		_enums[group.Name] = new BoundEnum(group.Name, source.Name, members) { IsFlags = group.IsFlags, Guard = group.Guard };
	}

	// How many leading words all of an enum's members share, such as SHOW_EFFECT in wxSHOW_EFFECT_NONE.
	private static int SharedWords(DoxygenEnum source) {
		List<string[]> words = [.. source.Values.Select(static value => Words(value.Name))];
		int shared = 0;
		while (words.All(found => found.Length > shared + 1 && found[shared] == words[0][shared])) shared++;
		return shared;
	}

	private static string MemberName(string constant, int shared) => Naming.Words(string.Join('_', Words(constant)[shared..]));

	private static string[] Words(string constant) => (constant.StartsWith("wx", StringComparison.Ordinal) ? constant[2..] : constant).Split('_');
}
