using System.Globalization;
using System.Text.RegularExpressions;
using WxKit.Generator.Doxygen;

namespace WxKit.Generator.Binding;

// Decides how each tweaked class is bound: its handle, its base, which members it gets and how
// their types and defaults convert, which getters and setters form properties, and its events and
// styles. Anything it cannot bind goes in the skipped list with the reason, so gaps stay visible.
internal sealed partial class Binder(DoxygenApi api, Tweaks tweaks) {
	private readonly Constants _constants = new(api.Constants, api.HeaderDefines);
	private readonly Dictionary<string, BoundClass> _classes = new(StringComparer.Ordinal);
	private readonly Dictionary<string, BoundEventType> _eventTypes = new(StringComparer.Ordinal);
	private readonly Dictionary<string, BoundEnum?> _enums = new(StringComparer.Ordinal);

	// C++ enums WxKit writes by hand, because their wxWidgets form is not the one .NET code wants.
	private static readonly Dictionary<string, string> HandwrittenEnums = new(StringComparer.Ordinal) {
		["wxOrientation"] = "Orientation",
		["wxDirection"] = "Direction",
		["wxItemKind"] = "MenuItemKind",
	};
	private readonly List<string> _skipped = [];

	public BindingResult Bind() {
		// A class made from a template is the template's members over the template's argument.
		if (tweaks.TemplateInstances.Count > 0) {
			Dictionary<string, DoxygenClass> classes = new(api.Classes, StringComparer.Ordinal);
			foreach (TemplateInstanceTweak instance in tweaks.TemplateInstances) {
				if (!api.Classes.TryGetValue(instance.Template, out DoxygenClass? template)) throw new InvalidOperationException($"A tweak makes {instance.Name} from {instance.Template}, which the interface headers do not have.");
				classes[instance.Name] = template with {
					Name = instance.Name,
					Bases = [instance.BaseClass],
					Members = [.. template.Members.Select(member => member.Name == instance.Template ? member with { Name = instance.Name } : member.Name == "~" + instance.Template ? member with { Name = "~" + instance.Name } : member)],
				};
			}
			api = api with { Classes = classes };
		}
		if (tweaks.Classes.Values.Any(static tweak => tweak.BaseOverride is not null)) {
			Dictionary<string, DoxygenClass> classes = new(api.Classes, StringComparer.Ordinal);
			foreach (ClassTweak tweak in tweaks.Classes.Values.Where(static tweak => tweak.BaseOverride is not null)) {
				if (classes.TryGetValue(tweak.CppName, out DoxygenClass? overridden)) classes[tweak.CppName] = overridden with { Bases = tweak.BaseOverride! };
			}
			api = api with { Classes = classes };
		}
		if (tweaks.FunctionGroups.Count > 0) {
			Dictionary<string, DoxygenClass> classes = new(api.Classes, StringComparer.Ordinal);
			foreach (FunctionGroupTweak group in tweaks.FunctionGroups) {
				List<DoxygenMember> members = [];
				foreach (string name in group.Functions) {
					if (!api.Functions.TryGetValue(name, out IReadOnlyList<DoxygenMember>? overloads)) throw new InvalidOperationException($"A tweak binds the function {name}, which the interface headers do not have.");
					members.AddRange(overloads.Select(static member => member with { IsStatic = true }));
				}
				classes[group.Name] = new DoxygenClass(group.Name, [], group.Header, string.Empty, members, [], []);
			}
			api = api with { Classes = classes };
		}
		foreach (ClassTweak tweak in tweaks.Classes.Values) {
			if (!api.Classes.TryGetValue(tweak.CppName, out DoxygenClass? source)) throw new InvalidOperationException($"A tweak names {tweak.CppName}, which the interface headers do not have.");
			HandleKind handle = tweak.IsStaticOnly ? HandleKind.Static : tweak.IsPlain ? HandleKind.Plain(tweak.PlainHandle ?? tweak.CppName) : tweak.IsValueObject ? HandleKind.Value : tweak.IsOwnedObject ? HandleKind.Owned : HandleFor(source.Name);
			_classes[tweak.CppName] = new BoundClass(source, tweak, handle);
		}
		foreach (BoundClass bound in _classes.Values) {
			List<string> ancestors = Ancestors(bound.CppName);
			// A base must share the class's lifetime, so a validator, which WxKit owns, does not
			// derive from EvtHandler, which wxWidgets owns.
			bound.Base = bound.Tweak.HasNoBase ? null : ancestors.Skip(1).Select(name => _classes.GetValueOrDefault(name)).FirstOrDefault(candidate => candidate is not null && candidate.Handle.IsOwned == bound.Handle.IsOwned);
			bound.IsTopLevel = ancestors.Contains("wxTopLevelWindow");
		}
		// Enums are bound first, so a default can name their members.
		foreach (string cppName in tweaks.Enums.Keys) {
			if (EnumFor(cppName) is null) throw new InvalidOperationException($"A tweak binds the enum {cppName}, which the interface headers do not have.");
		}
		foreach (ConstantEnumTweak group in tweaks.ConstantEnums) BindConstantEnum(group);
		foreach ((string prefix, string managedName) in tweaks.PrefixConstantClasses) {
			// Only the ones the real headers define, which leaves out those they keep for compatibility,
			// such as wxSTC_INDIC0_MASK, and those they mark deprecated.
			List<string> names = [.. api.Constants.Keys.Where(name => name.StartsWith(prefix, StringComparison.Ordinal) && !_groupedConstants.Contains(name) && api.HeaderDefines.TryGetValue(name, out string? defined) && !defined.Contains("DEPRECATED", StringComparison.Ordinal)).Order(StringComparer.Ordinal)];
			if (names.Count == 0) throw new InvalidOperationException($"A tweak binds the constants named {prefix}*, which the headers do not define.");
			DoxygenEnum source = new($"{prefix}*", [.. names.Select(static name => new DoxygenEnumValue(name, string.Empty))]);
			_enums[$"{prefix}* constants"] = new BoundEnum(managedName, source.Name, EnumMembers(source.Name, source, memberPrefix: null, sentinels: true)) { IsConstantClass = true };
		}
		foreach ((string cppName, ConstantClassTweak constants) in tweaks.ConstantClasses) {
			if (!api.Enums.TryGetValue(cppName, out DoxygenEnum? source)) throw new InvalidOperationException($"A tweak binds the enum {cppName} as constants, which the interface headers do not have.");
			DoxygenEnum kept = source with { Values = [.. source.Values.Where(value => !constants.Exclude.Contains(value.Name)).DistinctBy(static value => value.Name)] };
			_enums[$"{cppName} constants"] = new BoundEnum(constants.Name, cppName, EnumMembers(cppName, kept, memberPrefix: null, sentinels: true)) { IsConstantClass = true };
		}
		// Bases are bound before the classes derived from them, so a class can leave out the
		// members its bases already bind.
		foreach (BoundClass bound in _classes.Values.Where(static bound => bound.Tweak.IsHandwritten && bound.Tweak.InternalMembers.Count > 0)) BindMembers(bound);
		foreach (BoundClass bound in _classes.Values.Where(static bound => !bound.Tweak.IsHandwritten).OrderBy(bound => Ancestors(bound.CppName).Count).ThenBy(static bound => bound.CppName, StringComparer.Ordinal)) {
			bound.Styles = BindStyles(bound);
			BindMembers(bound);
			BindEvents(bound);
		}
		foreach ((string constant, string eventClass) in tweaks.ExtraEventTypes) EventTypeFor(constant, eventClass);
		foreach (string constant in tweaks.EventTypeGuards.Keys) {
			if (!_eventTypes.ContainsKey(constant)) throw new InvalidOperationException($"A tweak guards the event type {constant}, which no bound event sends.");
		}
		return new BindingResult(
			[.. _classes.Values.OrderBy(static bound => bound.CppName, StringComparer.Ordinal)],
			[.. _eventTypes.Values.OrderBy(static type => type.Name, StringComparer.Ordinal)],
			[.. _enums.Values.OfType<BoundEnum>().OrderBy(static bound => bound.Name, StringComparer.Ordinal)],
			_skipped) {
			StringConstants = api.StringConstants,
		};
	}

	// A class's primary bases, itself first. wxWidgets classes inherit from wxObject along their first base.
	private List<string> Ancestors(string name) {
		List<string> chain = [];
		for (string? current = name; current is not null && api.Classes.TryGetValue(current, out DoxygenClass? found); current = found.Bases.FirstOrDefault()) chain.Add(current);
		return chain;
	}

	private HandleKind HandleFor(string name) {
		List<string> ancestors = Ancestors(name);
		if (ancestors.Contains("wxWindow")) return HandleKind.Window;
		if (ancestors.Contains("wxSizer")) return HandleKind.Sizer;
		if (ancestors.Contains("wxMenu")) return HandleKind.Menu;
		if (ancestors.Contains("wxMenuItem")) return HandleKind.MenuItem;
		if (ancestors.Contains("wxEvent")) return HandleKind.Event;
		if (ancestors.Contains("wxDataViewColumn")) return HandleKind.Column;
		if (ancestors.Contains("wxToolBarToolBase")) return HandleKind.Tool;
		// Fonts, pens, brushes, bitmaps, icons, and cursors are wxGDIObjects; images are copied the same way.
		if (ancestors.Contains("wxGDIObject") || ancestors.Contains("wxImage")) return HandleKind.Value;
		// Drawing contexts are made, used, and deleted, as a wxPaintDC is in a paint handler.
		if (ancestors.Contains("wxDC")) return HandleKind.Owned;
		if (ancestors.Contains(EventObjectClass)) return HandleKind.EvtHandler;
		throw new InvalidOperationException($"{name} is not a kind of class WxKit can bind yet.");
	}

	// A class's own members, and those of the secondary bases it mixes in, such as wxTextEntry
	// for wxTextCtrl, which are called through the class itself.
	private IEnumerable<DoxygenMember> MembersOf(DoxygenClass source, bool inheritUnboundBases = false) {
		IEnumerable<DoxygenMember> members = source.Members;
		// Bases above the class that are not bound, up to the first that is, called through the class.
		if (inheritUnboundBases && source.Bases.FirstOrDefault() is string primary) {
			// wxObject's own members, such as GetClassInfo, are wxWidgets's run-time type information.
			foreach (string ancestor in Ancestors(primary).TakeWhile(name => !_classes.ContainsKey(name) && name != "wxObject")) {
				members = members.Concat(MembersOf(api.Classes[ancestor]).Where(member => member.Name != ancestor && member.Name != "~" + ancestor).Select(member => member with { Mixin = member.Mixin ?? ancestor }));
			}
		}
		foreach (string mixin in source.Bases.Skip(1)) {
			// A mixin brings its own bases with it, as wxItemContainer brings wxItemContainerImmutable.
			// Their constructors and destructors are not members of the class they are mixed into.
			foreach (string ancestor in Ancestors(mixin).Where(name => !_classes.ContainsKey(name))) {
				DoxygenClass mixed = api.Classes[ancestor];
				members = members.Concat(MembersOf(mixed).Where(member => member.Name != ancestor && member.Name != "~" + ancestor).Select(member => member with { Mixin = member.Mixin ?? ancestor }));
			}
		}
		return members;
	}

	private const string EventObjectClass = "wxEvtHandler";

	private void Skip(BoundClass bound, DoxygenMember member, string reason) => _skipped.Add($"{bound.CppName}::{member.Name}({string.Join(", ", member.Parameters.Select(static parameter => parameter.Type))}): {reason}");
}
