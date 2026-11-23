using System.Globalization;
using System.Text.RegularExpressions;
using WxKit.Generator.Doxygen;

namespace WxKit.Generator.Binding;

// Decides how each tweaked class is bound: its handle, its base, which members it gets and how
// their types and defaults convert, which getters and setters form properties, and its events and
// styles. Anything it cannot bind goes in the skipped list with the reason, so gaps stay visible.
internal sealed partial class Binder(DoxygenApi api, Tweaks tweaks) {
	private const string EventObjectClass = "wxEvtHandler";

	// Known string constants used as defaults, and their values.
	private static readonly Dictionary<string, string> StringDefaults = new(StringComparer.Ordinal) {
		["wxEmptyString"] = string.Empty,
		["wxString()"] = string.Empty,
		["wxMessageBoxCaptionStr"] = "Message",
	};

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
			List<string> names = [.. api.Constants.Keys.Where(name => name.StartsWith(prefix, StringComparison.Ordinal) && api.HeaderDefines.TryGetValue(name, out string? defined) && !defined.Contains("DEPRECATED", StringComparison.Ordinal)).Order(StringComparer.Ordinal)];
			if (names.Count == 0) throw new InvalidOperationException($"A tweak binds the constants named {prefix}*, which the headers do not define.");
			DoxygenEnum source = new($"{prefix}*", [.. names.Select(static name => new DoxygenEnumValue(name, string.Empty))]);
			_enums[$"{prefix}* constants"] = new BoundEnum(managedName, source.Name, EnumMembers(source.Name, source, memberPrefix: null)) { IsConstantClass = true };
		}
		foreach ((string cppName, ConstantClassTweak constants) in tweaks.ConstantClasses) {
			if (!api.Enums.TryGetValue(cppName, out DoxygenEnum? source)) throw new InvalidOperationException($"A tweak binds the enum {cppName} as constants, which the interface headers do not have.");
			DoxygenEnum kept = source with { Values = [.. source.Values.Where(value => !constants.Exclude.Contains(value.Name)).DistinctBy(static value => value.Name)] };
			_enums[$"{cppName} constants"] = new BoundEnum(constants.Name, cppName, EnumMembers(cppName, kept, memberPrefix: null)) { IsConstantClass = true };
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

	private void BindMembers(BoundClass bound) {
		ClassTweak tweak = bound.Tweak;
		IEnumerable<DoxygenMember> members = MembersOf(bound.Source, tweak.InheritsUnboundBases);
		foreach (string source in tweak.Borrowed.Keys) {
			if (!api.Classes.TryGetValue(source, out DoxygenClass? lender)) throw new InvalidOperationException($"A tweak gives {bound.CppName} the members of {source}, which the interface headers do not have.");
			members = members.Concat(lender.Members.Where(member => member.Name != source && member.Name != "~" + source && !bound.Source.Members.Any(own => own.Name == member.Name)).Select(member => member with { Mixin = source }));
		}
		List<DoxygenMember> candidates = [.. members.Where(static member => member.IsPublic && !member.Name.StartsWith('~') && !member.Name.StartsWith("operator", StringComparison.Ordinal))];
		HashSet<string> known = [.. candidates.Select(static member => member.Name)];
		foreach (string kept in tweak.KeptAlive) {
			if (!candidates.Any(member => member.Name == bound.CppName && member.Parameters.Any(parameter => parameter.Name == kept))) throw new InvalidOperationException($"A tweak keeps {bound.CppName}'s constructor parameter {kept} alive, which no constructor has.");
		}
		foreach (string named in (tweak.OnlyMembers ?? []).Concat(tweak.IgnoredMembers).Concat(tweak.InternalMembers).Concat(tweak.MemberGuards.Keys).Concat(tweak.OverloadGuards.Select(static guard => guard.Member)).Concat(tweak.Renames.Keys.Where(name => !tweak.PropertyPairs.ContainsKey(name))).Concat(tweak.PropertyPairs.Values.SelectMany(static pair => pair.Setter is null ? [pair.Getter] : new[] { pair.Getter, pair.Setter }))) {
			if (!known.Contains(named)) throw new InvalidOperationException($"A tweak names {bound.CppName}::{named}, which the interface headers do not have.");
		}
		string shortName = bound.CppName;
		List<BoundFunction> functions = [];
		foreach (DoxygenMember member in candidates) {
			bool isConstructor = member.Name == shortName;
			if (tweak.IgnoredMembers.Contains(member.Name)) continue;
			bool isInternal = tweak.InternalMembers.Contains(member.Name);
			if (tweak.IsHandwritten && !isInternal) continue;
			if (isConstructor) {
				// A class with pure virtual members, such as wxBookCtrlBase, cannot be created.
				if (!tweak.HasConstructors || bound.Handle == HandleKind.Event || bound.Source.Members.Any(own => own.IsPureVirtual && !tweak.DirectorMembers.Contains(own.Name))) continue;
				// Two-step creation is not exposed, so a window's default constructor is not bound.
				if (member.Parameters.Count == 0 && bound.Handle == HandleKind.Window) continue;
				if (member.Parameters.Count == 1 && CppType.Parse(member.Parameters[0].Type).Name == shortName) continue;
			} else if (tweak.OnlyMembers is not null && !tweak.OnlyMembers.Contains(member.Name) && !isInternal) {
				continue;
			} else if (member.Name == "Create" && bound.Handle == HandleKind.Window) {
				// Two-step creation is not exposed, so the Create that completes it is not either.
				continue;
			}
			if (member.IsDeprecated) {
				Skip(bound, member, "it is deprecated");
				continue;
			}
			if (bound.Handle == HandleKind.Static && (!member.IsStatic || isConstructor)) continue;
			if (BindFunction(bound, member, isConstructor) is BoundFunction function) functions.Add(function);
		}
		// A mixin can declare a member the class declares too, such as SetValue in both wxComboBox
		// and wxTextEntry. The class's own, which comes first, is kept.
		functions = [.. functions.DistinctBy(static function => $"{function.Source.Name}({string.Join(",", function.Source.Parameters.Select(static parameter => parameter.Type))})")];
		functions = [.. functions.Where(function => !HasStructOverload(function, functions))];
		NameOverloads(functions);
		// A free function's export drops the wx its name begins with, as wxk_Desktop_Bell does.
		if (tweak.IsGlobal) {
			foreach (BoundFunction function in functions.Where(static function => function.ExportName.StartsWith("wx", StringComparison.Ordinal))) function.ExportName = function.ExportName[2..];
		}
		bound.InternalFunctions.AddRange(functions.Where(function => tweak.InternalMembers.Contains(function.Source.Name)));
		functions.RemoveAll(bound.InternalFunctions.Contains);
		// Two C++ constructors can have the same managed signature, as an int and a long do, and C#
		// allows only one.
		List<BoundFunction> constructors = [.. functions.Where(static function => function.IsConstructor).DistinctBy(static function => string.Join(",", function.ManagedParameters.Select(static parameter => parameter.Conversion.ManagedType(parameter.Nullable))))];
		// A constructor whose parameters start another's, which has only defaults after them, is left
		// out, as wxBannerWindow(parent, dir) is beside the one that adds the id, position, size, and
		// style, since a call such as new BannerWindow(parent) could mean either.
		constructors.RemoveAll(shorter => constructors.Any(longer => !ReferenceEquals(longer, shorter) && Extends(longer, shorter)));
		bound.Constructors.AddRange(constructors);
		PairProperties(bound, [.. functions.Where(static function => !function.IsConstructor)]);
		RemoveRedeclarations(bound);
		BindDirectors(bound);
	}

	// The virtual members a tweak lets C# override, such as a virtual list's OnGetItemText, which
	// are often protected. Each is bound as a call to wxWidgets's own implementation, which the
	// managed override's default calls. Their parameters only take numbers, enums, and booleans for
	// now, which a callback can pass on to that implementation as they are.
	private void BindDirectors(BoundClass bound) {
		foreach (string named in bound.Tweak.DirectorsAfterBase.Concat(bound.Tweak.DirectorsBeforeBase)) {
			if (!bound.Tweak.DirectorMembers.Contains(named)) throw new InvalidOperationException($"A tweak calls .NET around {bound.CppName}::{named}, which isn't one of its directors.");
		}
		foreach (string name in bound.Tweak.DirectorMembers) {
			DoxygenMember member = MembersOf(bound.Source, bound.Tweak.InheritsUnboundBases).FirstOrDefault(member => member.Name == name && member.IsVirtual && !member.IsStatic) ?? throw new InvalidOperationException($"A tweak lets C# override {bound.CppName}::{name}, which is not one of its virtual members.");
			BoundFunction? function = BindFunction(bound, member, isConstructor: false);
			// A hand-written callback can also write through a number's pointer, as GetPageInfo's are.
			bool CanCallBack(Conversion conversion) => conversion is BoolConversion or NumberConversion or EnumConversion || (bound.Tweak.IsDirectorInternal && conversion is OutConversion { CanCallBack: true } or CallbackOutConversion or CallbackLentConversion or GeometryConversion or ItemIdConversion or CountConversion);
			if (function is null || function.Parameters.Count != member.Parameters.Count || function.Returns.CallbackCType is null || !function.Parameters.All(parameter => CanCallBack(parameter.Conversion))) {
				string problem = function is null ? "a type the generator can't bind" : function.Returns.CallbackCType is null ? $"its return, a {member.Type}" : string.Join(", ", function.Parameters.Where(parameter => !CanCallBack(parameter.Conversion)).Select(static parameter => $"{parameter.CppName}, as {parameter.Conversion.GetType().Name}").DefaultIfEmpty("a parameter it leaves out"));
				throw new InvalidOperationException($"C# cannot override {bound.CppName}::{name} yet: a director cannot convert {problem}.");
			}
			bool afterBase = bound.Tweak.DirectorsAfterBase.Contains(name);
			bool beforeBase = bound.Tweak.DirectorsBeforeBase.Contains(name);
			if (afterBase && function.Returns is not (VoidConversion or BoolConversion)) throw new InvalidOperationException($"{bound.CppName}::{name} can only call .NET after its own member when it returns nothing or a bool.");
			if (beforeBase && function.Returns is not VoidConversion) throw new InvalidOperationException($"{bound.CppName}::{name} can only call .NET before its own member when it returns nothing.");
			// Hand-written callbacks give their own defaults, so need no export of the class's member.
			function.HasBaseExport = !member.IsPureVirtual && !afterBase && !beforeBase && !bound.Tweak.IsDirectorInternal;
			function.ExportName = "base_" + name;
			function.ManagedParameters = function.Parameters;
			function.IsDirectorBase = true;
			bound.Directors.Add(function);
		}
	}

	// The interface headers repeat a virtual function in each class that overrides it, such as
	// Show in wxTopLevelWindow. Binding it again would only hide the base's, which already reaches
	// the override, so a member that a base binds with the same signature is left out. So is a
	// second member with the same managed signature, such as CenterOnParent beside CentreOnParent.
	// A static member is not inherited the same way: the derived one does something else, as each
	// class's GetClassDefaultAttributes does, so it stays and hides the base's.
	private static void RemoveRedeclarations(BoundClass bound) {
		HashSet<string> inherited = [];
		for (BoundClass? ancestor = bound.Base; ancestor is not null; ancestor = ancestor.Base) {
			inherited.UnionWith(ancestor.Properties.Select(static property => property.Name));
			inherited.UnionWith(ancestor.Methods.Select(Signature));
		}
		HashSet<string> seen = [];
		bound.Properties.RemoveAll(property => !property.Getter.IsStatic && inherited.Contains(property.Name) || !seen.Add(property.Name));
		bound.Methods.RemoveAll(method => !method.Function.IsStatic && inherited.Contains(Signature(method)) || !seen.Add(Signature(method)));
		for (int index = 0; index < bound.Properties.Count; index++) {
			if (inherited.Contains(bound.Properties[index].Name)) bound.Properties[index] = bound.Properties[index] with { Hides = true };
		}
		for (int index = 0; index < bound.Methods.Count; index++) {
			if (inherited.Contains(Signature(bound.Methods[index]))) bound.Methods[index] = bound.Methods[index] with { Hides = true };
		}
		// C# cannot have a property and a method with the same name, and which to rename is a choice.
		foreach (BoundMethod method in bound.Methods) {
			if (bound.Properties.Any(property => property.Name == method.Name)) throw new InvalidOperationException($"{bound.CppName}::{method.Function.Source.Name} and a property are both called {method.Name}. Rename one with a tweak.");
		}
	}

	private static string Signature(BoundMethod method) => $"{method.Name}({string.Join(",", method.Function.ManagedParameters.Select(static parameter => parameter.Conversion.Suffix))})";

	private BoundFunction? BindFunction(BoundClass owner, DoxygenMember member, bool isConstructor) {
		Conversion returns = VoidConversion.Instance;
		if (!isConstructor) {
			Conversion? mapped = Map(CppType.Parse(member.Type), owner, member.Name, null, isReturn: true);
			if (mapped is null && owner.Tweak.DiscardsUnboundReturns && CppType.Parse(member.Type).Pointers == 1) mapped = VoidConversion.Instance;
			if (mapped is null) {
				Skip(owner, member, $"WxKit cannot convert its return type, {member.Type}");
				return null;
			}
			returns = mapped;
		}
		List<BoundParameter> parameters = [];
		foreach (DoxygenParameter parameter in member.Parameters) {
			// A window's name, which few applications set, is left to its default, so every
			// constructor ends the same way.
			if (isConstructor && owner.Handle == HandleKind.Window && parameter.Name == "name" && parameter.Default is not null) break;
			Conversion? conversion;
			// Ints after their count, as in SetStatusWidths(int n, const int widths[]), take the count with them.
			// One with a default, as SetFieldsCount's has, is left to it, and the count stays a parameter.
			if (parameter.Default is null && parameter.Type.Replace(" ", string.Empty, StringComparison.Ordinal) is "constint*" or "constint[]" && parameters.Count > 0 && parameters[^1].CppName is "n" or "number" or "count" && parameters[^1].Conversion == NumberConversion.Int) {
				parameters.RemoveAt(parameters.Count - 1);
				conversion = CountedIntsConversion.Instance;
			} else {
				conversion = Map(CppType.Parse(parameter.Type), owner, member.Name, parameter.Name, isReturn: false);
			}
			// A number that defaults to wxBOTH, wxHORIZONTAL, or wxVERTICAL is an orientation.
			if (conversion == NumberConversion.Int && parameter.Default is string orientation && OrientationPattern().IsMatch(orientation)) conversion = new EnumConversion("Orientation", NumberConversion.Int);
			bool nullable = parameter.Name == "parent" && isConstructor && owner.IsTopLevel;
			string? managedDefault = null;
			bool resolved = true;
			if (conversion is not null && parameter.Default is string cppDefault) {
				resolved = TryDefault(cppDefault, conversion, out managedDefault, out bool defaultIsNull);
				nullable |= defaultIsNull;
				// A default that depends on the platform, such as wxTR_DEFAULT_STYLE, is null, which the shim turns into it.
				if (!resolved && conversion is EnumConversion platformEnum && api.PlatformConstants.Contains(cppDefault)) {
					conversion = new PlatformDefaultConversion(platformEnum, cppDefault);
					managedDefault = "null";
					nullable = true;
					resolved = true;
				}
				// A string that defaults to one of wxWidgets's string constants, such as a file
				// dialog's wxFileSelectorPromptStr, is null too, which the shim turns into the constant.
				if (!resolved && conversion is StringConversion && IdentifierPattern().IsMatch(cppDefault)) {
					conversion = new StringDefaultConversion(cppDefault);
					managedDefault = "null";
					nullable = true;
					resolved = true;
				}
				// A color that defaults to one of wxWidgets's colors, such as a color picker's *wxBLACK,
				// is null too, which the shim turns into that color.
				if (!resolved && conversion is ColorConversion && ColorConstantPattern().IsMatch(cppDefault)) {
					conversion = new ColorDefaultConversion(cppDefault);
					managedDefault = "null";
					nullable = true;
					resolved = true;
				}
			}
			// A trailing parameter with a default that WxKit cannot convert is left out, and the
			// rest after it, so the C++ default applies. Anything else makes the function unbindable.
			if (conversion is null || !resolved) {
				if (parameter.Default is not null) {
					// An array cut off this way takes its count with it, as in wxChoice(..., int n, const wxString choices[]).
					if (parameter.Type.EndsWith(']') && parameters.Count > 0 && parameters[^1].CppName is "n" or "count") parameters.RemoveAt(parameters.Count - 1);
					break;
				}
				Skip(owner, member, conversion is null ? $"WxKit cannot convert {parameter.Name}, a {parameter.Type}" : $"WxKit cannot write the default of {parameter.Name}, {parameter.Default}");
				return null;
			}
			// A constructor's position or size can always be left to wxWidgets: the overloads that
			// require one, such as the one of wxChoice that takes a list, mean the same by the default.
			if (isConstructor && owner.Handle == HandleKind.Window && managedDefault is null && conversion is GeometryConversion) {
				managedDefault = "null";
				nullable = true;
			}
			string managedName = owner.Tweak.ParameterNames.GetValueOrDefault((member.Name, parameter.Name)) ?? Naming.Parameter(parameter.Name);
			parameters.Add(new BoundParameter(parameter.Name, managedName, conversion, nullable, managedDefault));
		}
		// A function that fills one list, as GetSelections(wxArrayInt&) and GetPaths(wxArrayString&)
		// do, returns the list, as .NET does, and a getter of it becomes a property. What it returned
		// besides, such as the list's length or whether it could, is left out.
		List<(int Index, string Argument)> filled = [];
		List<int> lists = [.. parameters.Select(static (parameter, index) => (parameter, index)).Where(static pair => pair.parameter.Conversion is StringArrayOutConversion or NumberListOutConversion && pair.parameter.Default is null).Select(static pair => pair.index)];
		if (!isConstructor && lists is [int position] && returns is VoidConversion or BoolConversion or NumberConversion or CountConversion) {
			returns = parameters[position].Conversion is NumberListOutConversion numbers ? FilledListConversion.Of(numbers.Kind) : FilledListConversion.Strings;
			parameters.RemoveAt(position);
			filled.Add((position, FilledListConversion.Argument));
		}
		// A function that only fills a width and a height, as wxPrintout::GetPageSizeMM(int* w, int* h)
		// does, returns them as a Size, and a getter of it becomes a property.
		// Only a pair named as a size is one: x and y are a position, which another overload gives
		// as a Point already.
		if (!isConstructor && returns is VoidConversion && parameters.Count == 2 && parameters.All(static parameter => parameter.Conversion == OutConversion.Int) && (parameters[0].CppName, parameters[1].CppName) is ("w", "h") or ("width", "height") or ("xUnit", "yUnit")) {
			bool byReference = member.Parameters[0].Type.Contains('&', StringComparison.Ordinal);
			returns = FilledSizeConversion.Instance;
			filled.Add((0, byReference ? "first" : "&first"));
			filled.Add((1, byReference ? "second" : "&second"));
			parameters.Clear();
		}
		BoundFunction function = new(owner, member, returns, parameters, isConstructor) { FilledArguments = filled };
		function.ManagedParameters = ManagedOrder(parameters, CreatesWindow(member, isConstructor));
		return function;
	}

	// A window's own id is rarely needed, since WxKit binds events to objects rather than ids, so
	// where a window is made, an id without a default gets wxID_ANY and moves after the label or
	// title, before the position. Elsewhere an id names what the call is about, such as the menu
	// item MenuBar.Check checks, so it keeps its place and stays required.
	// A constructor, or a function that makes a window and returns it, such as CreateStatusBar or
	// wxBitmapButton::NewCloseButton.
	private static bool CreatesWindow(DoxygenMember member, bool isConstructor) => isConstructor || member.Name.StartsWith("Create", StringComparison.Ordinal) || member.Name.StartsWith("New", StringComparison.Ordinal);

	private static List<BoundParameter> ManagedOrder(List<BoundParameter> parameters, bool createsWindow) {
		List<BoundParameter> ordered = [.. parameters];
		int id = createsWindow ? ordered.FindIndex(static parameter => parameter.CppName is "id" or "winid" && parameter.Conversion == NumberConversion.Int) : -1;
		if (id >= 0) {
			BoundParameter moved = ordered[id] with { Default = "StandardId.Any" };
			ordered.RemoveAt(id);
			int position = ordered.FindIndex(id, static parameter => parameter.CppName == "pos");
			ordered.Insert(position >= 0 ? position : ordered.Count, moved);
		}
		// C# needs every parameter after an optional one to be optional too, so the required ones
		// move first, keeping their order, as wxChoice's list moves ahead of its id and position.
		return [.. ordered.Where(static parameter => parameter.Default is null), .. ordered.Where(static parameter => parameter.Default is not null)];
	}

	// What a hand-written callback gives back through a pointer or a reference: text, a long, an
	// enum, a rectangle, or an object's handle, such as an accessible object's name, state, role,
	// location, and child.
	private Conversion? CallbackOutFor(CppType type, BoundClass owner) {
		if (type is { Name: "wxVariant", IsReference: true, Pointers: 0 }) return new CallbackLentConversion("wxk_Variant*", "WxkVariant*", type.IsConst ? "wxk::Wrap(const_cast<wxVariant*>(&{0}))" : "wxk::Wrap(&{0})");
		if (type is { Name: "wxDataViewItemArray", IsReference: true, IsConst: false, Pointers: 0 }) return new CallbackLentConversion("wxk_DataViewItems*", "WxkDataViewItems*", "reinterpret_cast<wxk_DataViewItems*>(&{0})");
		if (type.IsConst) return null;
		if (type.IsReference && type.Pointers == 0) return type.Name == "wxRect" ? new CallbackOutConversion("wxRect", "wxk_Rect", "Rect", byReference: true) : null;
		if (type.IsReference) return null;
		if (type.Pointers == 2) return _classes.TryGetValue(type.Name, out BoundClass? target) && target.Handle != HandleKind.Static ? new CallbackOutConversion(type.Name + "*", target.Handle.CType + "*", target.Handle.InteropType + "*", byReference: false) : null;
		if (type.Pointers != 1) return null;
		if (type.Name == "wxString") return new CallbackOutConversion("wxString", "wxk_String*", "WxkString*", byReference: false);
		if (type.Name == "long") return new CallbackOutConversion("long", "int64_t", "long", byReference: false);
		string nested = Ancestors(owner.CppName).Select(ancestor => $"{ancestor}::{type.Name}").FirstOrDefault(api.Enums.ContainsKey) ?? type.Name;
		return api.Enums.ContainsKey(nested) ? new CallbackOutConversion(nested, "int32_t", "int", byReference: false) : null;
	}

	private Conversion? Map(CppType type, BoundClass owner, string member, string? parameter, bool isReturn) {
		bool isHandwrittenDirector = owner.Tweak.IsDirectorInternal && owner.Tweak.DirectorMembers.Contains(member);
		if (!isReturn && isHandwrittenDirector && CallbackOutFor(type, owner) is Conversion given) return given;
		if (!isReturn && _classes.TryGetValue(type.Name, out BoundClass? owned) && owned.Handle == HandleKind.Owned && (type.IsReference || type.Pointers == 1)) return new OwnedConversion(owned, type.IsReference);
		// UTF-8 text, as wxUIActionSimulator::Text takes it.
		if (!isReturn && type is { Name: "char", IsConst: true, Pointers: 1, IsReference: false }) return Utf8StringConversion.Instance;
		// A value object passed by a reference that can change, such as the bitmap a wxMemoryDC
		// draws into, is the managed object's own copy, so the change shows there.
		if (!isReturn && type.IsReference && !type.IsConst && _classes.TryGetValue(type.Name, out BoundClass? changed) && changed.Handle == HandleKind.Value) return new ValueConversion(changed, mutable: true);
		// A tweak names an enum by its managed name, or by its C++ one, which is then generated like
		// any other. It names a return value as the parameter "return".
		if (owner.Tweak.ParameterEnums.TryGetValue((member, isReturn ? "return" : parameter ?? string.Empty), out string? enumName)) {
			string managed = enumName.StartsWith("wx", StringComparison.Ordinal) ? EnumFor(enumName)?.Name ?? throw new InvalidOperationException($"A tweak gives {owner.CppName}::{member} the enum {enumName}, which the headers do not have.") : enumName;
			return new EnumConversion(managed, type.Name == "long" ? NumberConversion.Long : NumberConversion.Int);
		}
		// A class without a style table of its own, such as wxListView, takes its nearest ancestor's.
		BoundStyles? styles = null;
		for (BoundClass? current = owner; current is not null && styles is null; current = current.Base) styles = current.Styles;
		if (parameter == "style" && type.Name == "long" && type.Pointers == 0 && styles is not null && member == owner.CppName) return new EnumConversion(styles.Name, NumberConversion.Long);
		// A getter of the style, such as GetMessageDialogStyle, returns the class's style enum.
		if (isReturn && type.Name == "long" && type.Pointers == 0 && member.EndsWith("Style", StringComparison.Ordinal) && owner.Styles is BoundStyles ownStyles) return new EnumConversion(ownStyles.Name, NumberConversion.Long);
		if (type.IsVoid) return isReturn ? VoidConversion.Instance : null;
		// Numbers a function writes through a pointer become out parameters.
		if (!isReturn && type.Pointers == 1 && !type.IsConst && !type.IsReference) {
			switch (type.Name) {
				case "int" or "wxCoord":
					return OutConversion.Int;
				case "long":
					return OutConversion.Long;
				case "double":
					return OutConversion.Double;
			}
		}
		// A function that changes its object and returns it, as wxFont::MakeBold does.
		if (isReturn && type.IsReference && !type.IsConst && type.Pointers == 0 && type.Name == owner.CppName && owner.Handle.IsOwned) return new SelfConversion(owner);
		if (isReturn && type.Name is "wxWindowList" && type.Pointers == 0) return WindowListConversion.Instance;
		if (type.Pointers == 1 && !type.IsReference) {
			if (type.Name == "wxObject" && isReturn) return AnyObjectConversion.Instance;
			// Data a control keeps with an item, which it deletes with the item.
			if (type.Name == "wxClientData") return ItemDataConversion.Client;
			if (type.Name == "wxTreeItemData") return ItemDataConversion.TreeItem;
			// Events are only received, and a static class has no objects to pass.
			if (!_classes.TryGetValue(type.Name, out BoundClass? target) || target.Handle == HandleKind.Event || target.Handle == HandleKind.Static) return null;
			// Only event handlers are tracked, so only they can be wrapped when wxWidgets hands one back.
			// A tool cannot go in, since the only tools are a toolbar's own, which it has already.
			if (!isReturn && target.Handle == HandleKind.Tool) return null;
			// Hand-written code takes any object's handle from an internal member, such as a printout's
			// DC or a menu's item, and knows what it may do with it.
			if (owner.Tweak.InternalMembers.Contains(member)) return new ObjectConversion(target);
			// A menu item's handle, and a class's that isn't a wxObject, are only for hand-written code.
			if (target.Handle == HandleKind.MenuItem || target.Handle.IsPlain) return null;
			return isReturn && !target.Handle.IsEvtHandler && !target.Handle.IsKeptByOwner ? null : new ObjectConversion(target);
		}
		// A rectangle or strings a call fills by reference are out parameters. Only a getter's
		// rectangle is, since wxWindow::IsExposed takes one it reads by a reference that could change.
		if (!isReturn && type.Pointers == 0 && type.IsReference && !type.IsConst) {
			if (type.Name == "wxRect" && member.StartsWith("Get", StringComparison.Ordinal)) return RectOutConversion.Instance;
			if (type.Name == "wxArrayString") return StringArrayOutConversion.Instance;
		}
		// A list of ints or item ids, such as a list box's selections, comes back as an array, goes in
		// as any sequence, and is an out array when the call fills it.
		if (type.Pointers == 0 && NumberListKind.For(type.Name) is NumberListKind list) {
			if (isReturn) return new NumberListConversion(list);
			if (type.IsReference && !type.IsConst) return new NumberListOutConversion(list);
			return list.ToCpp is null ? null : new NumberListConversion(list);
		}
		// A reference that comes back, even one that could change, such as a color dialog's data,
		// is copied, as a value or a const reference is.
		if (!type.IsValueOrConstReference && !(isReturn && type.IsReference && type.Pointers == 0)) return null;
		if (_classes.TryGetValue(type.Name, out BoundClass? value) && value.Handle == HandleKind.Value) return new ValueConversion(value);
		// Bundles of bitmaps or icons, which wxWidgets uses for every resolution, convert from one.
		if (!isReturn && type.Name == "wxBitmapBundle" && _classes.TryGetValue("wxBitmap", out BoundClass? bitmap)) return new ValueConversion(bitmap, "wxBitmapBundle");
		if (!isReturn && type.Name == "wxIconBundle" && _classes.TryGetValue("wxIcon", out BoundClass? icon)) return new ValueConversion(icon, "wxIconBundle");
		if (HandwrittenEnums.TryGetValue(type.Name, out string? handwritten)) return new EnumConversion(handwritten, NumberConversion.Int, type.Name);
		if (!isReturn && type.Name.Replace(" ", string.Empty, StringComparison.Ordinal) == "wxVector<wxVariant>") return VariantListConversion.Instance;
		if (!isReturn && type.Name.Replace(" ", string.Empty, StringComparison.Ordinal) == "wxVector<wxBitmapBundle>" && _classes.ContainsKey("wxBitmap")) return BitmapListConversion.Instance;
		// An enum a class nests, which its own members name without the class, as wxStaticBitmap's
		// SetScaleMode takes ScaleMode, is named with it, which the shim's cast needs.
		string nestedName = type.Name.Contains("::", StringComparison.Ordinal) ? type.Name : Ancestors(owner.CppName).Select(ancestor => $"{ancestor}::{type.Name}").FirstOrDefault(api.Enums.ContainsKey) ?? type.Name;
		// A hand-written callback's enum crosses as its number, such as wxAccStatus, unless a tweak binds
		// it, so the callback adds no enum to the managed API, nor one the shim checks where it isn't.
		if (isHandwrittenDirector && type.Pointers == 0 && !type.IsReference && api.Enums.ContainsKey(nestedName) && !tweaks.Enums.ContainsKey(nestedName)) return new EnumConversion("int", NumberConversion.Int, nestedName);
		if (EnumFor(nestedName) is BoundEnum bound) return new EnumConversion(bound.Name, NumberConversion.Int, nestedName);
		return type.Name switch {
			"bool" => BoolConversion.Instance,
			"int" or "wxWindowID" or "wxCoord" => NumberConversion.Int,
			"long" or "wxTextPos" => NumberConversion.Long,
			"double" => NumberConversion.Double,
			"float" => NumberConversion.Float,
			"unsigned char" or "wxByte" => NumberConversion.Byte,
			// A wxMessageDialog button label converts from a string, which is how WxKit gives it.
			// wxArtID and wxArtClient are typedefs of wxString.
			"wxString" or "ButtonLabel" or "wxArtID" or "wxArtClient" => StringConversion.Instance,
			// An unsigned long is 32 bits on Windows and 64 elsewhere, so it crosses as a size_t.
			"size_t" or "unsigned long" => CountConversion.Size,
			"unsigned" or "unsigned int" or "wxUint32" => CountConversion.Unsigned,
			"wxRect" => RectConversion.Instance,
			"wxTreeItemId" => ItemIdConversion.TreeItem,
			"wxDataViewItem" => ItemIdConversion.DataViewItem,
			"wxTreeListItem" => ItemIdConversion.TreeListItem,
			"wxVariant" => VariantConversion.Instance,
			"wxDateTime" => DateTimeConversion.Instance,
			"wxArrayString" => StringArrayConversion.Instance,
			"wxColour" => ColorConversion.Instance,
			"wxPoint" => GeometryConversion.Point,
			"wxSize" => GeometryConversion.Size,
			"wxGBPosition" => GeometryConversion.GridPosition,
			"wxGBSpan" => GeometryConversion.GridSpan,
			_ => null,
		};
	}

	private bool TryDefault(string cppDefault, Conversion conversion, out string? managed, out bool isNull) {
		managed = null;
		isNull = false;
		string value = cppDefault.Trim();
		if (conversion is GeometryConversion geometry && geometry.IsDefault(value) || value is "nullptr" or "NULL" or "0" && conversion.AcceptsNull || conversion is ColorConversion && value == "wxNullColour" || conversion is DateTimeConversion && value is "wxDefaultDateTime" or "wxInvalidDateTime" || conversion is ValueConversion && (value.StartsWith("wxNull", StringComparison.Ordinal) || value.EndsWith("()", StringComparison.Ordinal))) {
			managed = "null";
			isNull = true;
			return true;
		}
		switch (conversion) {
			case BoolConversion when value is "true" or "false":
				managed = value;
				return true;
			case StringConversion when StringDefaults.TryGetValue(value, out string? text):
				managed = $"\"{text}\"";
				return true;
			case StringConversion when StringLiteralPattern().IsMatch(value):
				managed = value;
				return true;
			// An art id is written as the ArtId or ArtClient constant WxKit generates for it.
			case StringConversion when api.StringConstants.TryGetValue(value, out string? constant):
				managed = $"{(constant.EndsWith("_C", StringComparison.Ordinal) ? "ArtClient" : "ArtId")}.{Naming.Words(value["wxART_".Length..])}";
				return true;
			case NumberConversion number when number == NumberConversion.Int && value == "wxID_ANY":
				managed = "StandardId.Any";
				return true;
			case NumberConversion number when number == NumberConversion.Double || number == NumberConversion.Float:
				if (!double.TryParse(value.TrimEnd('f'), NumberStyles.Float, CultureInfo.InvariantCulture, out double floating)) return false;
				managed = number == NumberConversion.Float ? $"{floating.ToString(CultureInfo.InvariantCulture)}f" : floating.ToString(CultureInfo.InvariantCulture);
				return true;
			case NumberConversion or CountConversion:
				if (_constants.Evaluate(value) is not long integer) return false;
				managed = integer.ToString(CultureInfo.InvariantCulture);
				return true;
			case EnumConversion enumeration:
				managed = EnumDefault(value, enumeration);
				return managed is not null;
			default:
				return false;
		}
	}

	// A default made of constants the enum has, such as wxOK | wxCENTRE, is written with its
	// members, so it reads as MessageDialogStyle.Ok | MessageDialogStyle.Center.
	private string? EnumDefault(string value, EnumConversion enumeration) {
		BoundStyles? styles = _classes.Values.Select(static bound => bound.Styles).FirstOrDefault(found => found?.Name == enumeration.EnumName)
			?? _enums.Values.OfType<BoundEnum>().Where(found => found.Name == enumeration.EnumName).Select(static found => new BoundStyles(found.Name, found.Members)).FirstOrDefault();
		string[] parts = [.. value.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)];
		List<string> members = [];
		int shared = enumeration.CppType is string cppEnum && api.Enums.TryGetValue(cppEnum, out DoxygenEnum? source) ? SharedWords(source) : 0;
		foreach (string part in parts) {
			string? member = styles is not null
				? styles.Members.FirstOrDefault(style => style.Constant == part || part.EndsWith("::" + style.Constant, StringComparison.Ordinal))?.Name
				: ConstantPattern().IsMatch(part) ? MemberName(part, shared) : null;
			if (member is null) {
				members.Clear();
				break;
			}
			members.Add($"{enumeration.EnumName}.{member}");
		}
		if (members.Count > 0) return string.Join(" | ", members);
		if (_constants.Evaluate(value) is not long number) return null;
		if (styles?.Members.FirstOrDefault(style => style.Value == number) is BoundStyle exact) return $"{enumeration.EnumName}.{exact.Name}";
		return number == 0 ? "0" : $"({enumeration.EnumName})0x{number:X}";
	}

	// An overload that gives back two or more numbers through out parameters, such as
	// GetSize(out int width, out int height), when another of the same name gives them as one
	// value, such as Size GetSize(), or takes them as one, as ClientToScreen(Point) does, which is
	// how .NET has them.
	private static bool HasStructOverload(BoundFunction function, List<BoundFunction> functions) {
		List<BoundParameter> outs = [.. function.Parameters.Where(static parameter => parameter.Conversion is OutConversion)];
		if (outs.Count < 2 || function.IsConstructor) return false;
		List<string> rest = [.. function.Parameters.Where(static parameter => parameter.Conversion is not OutConversion).Select(static parameter => parameter.Conversion.ManagedType(parameter.Nullable))];
		return functions.Any(other => other != function && other.Source.Name == function.Source.Name && other.IsStatic == function.IsStatic && !other.Parameters.Any(static parameter => parameter.Conversion is OutConversion) && GivesTheSameAsOne(rest, other));
	}

	// The other returns the numbers as one value, or takes them as one, rather than meaning
	// something else, as ComboBox's GetSelection() gives the item, not the text selected.
	private static bool GivesTheSameAsOne(List<string> rest, BoundFunction other) {
		List<string> parameters = [.. other.Parameters.Select(static parameter => parameter.Conversion.ManagedType(parameter.Nullable))];
		if (rest.SequenceEqual(parameters)) return IsGeometry(other.Returns.ManagedType(false));
		// Two ints in for two out, as CalcScrolledPosition(x, y, out xx, out yy), beside a point in
		// for a point out.
		if (rest is ["int", "int"] && parameters is ["Point"] && other.Returns.ManagedType(false) == "Point") return true;
		// Four ints out beside a Rect out, as GetClippingBox's are.
		if (parameters.Count == rest.Count + 1 && parameters.Take(rest.Count).SequenceEqual(rest) && other.Parameters.Any(static parameter => parameter.Conversion is RectOutConversion)) return true;
		return parameters.Count == rest.Count + 1 && parameters.Take(rest.Count).SequenceEqual(rest) && IsGeometry(parameters[^1]);
	}

	private static bool IsGeometry(string type) => type.TrimEnd('?') is "Point" or "Size" or "Rect";

	private static void NameOverloads(List<BoundFunction> functions) {
		foreach (IGrouping<string, BoundFunction> group in functions.GroupBy(static function => function.ExportName).Where(static group => group.Count() > 1)) {
			foreach (BoundFunction function in group) {
				string suffix = function.Parameters.Count == 0 ? "Default" : string.Concat(function.Parameters.Select(static parameter => parameter.Conversion.Suffix));
				function.ExportName = $"{function.ExportName}_{suffix}";
			}
		}
	}

	// A getter with no parameters and a setter with one, of the same type, form a property:
	// GetLabel and SetLabel become Label, and IsShown becomes IsShown. A tweak can pair others,
	// such as IsEnabled and Enable.
	private static void PairProperties(BoundClass bound, List<BoundFunction> functions) {
		ClassTweak tweak = bound.Tweak;
		HashSet<BoundFunction> used = [];
		BoundFunction? Find(string name) => functions.FirstOrDefault(function => function.Source.Name == name && !used.Contains(function));
		foreach ((string propertyName, (string getterName, string? setterName)) in tweak.PropertyPairs) {
			BoundFunction getter = Find(getterName) ?? throw new InvalidOperationException($"The getter {bound.CppName}::{getterName} of property {propertyName} could not be bound.");
			BoundFunction? setter = setterName is null ? null : Find(setterName) ?? throw new InvalidOperationException($"The setter {bound.CppName}::{setterName} of property {propertyName} could not be bound.");
			used.Add(getter);
			if (setter is not null) used.Add(setter);
			bound.Properties.Add(new BoundProperty(propertyName, getter, setter));
		}
		foreach (BoundFunction getter in functions.Where(static function => function.Parameters.Count == 0 && function.Returns is not VoidConversion)) {
			if (used.Contains(getter)) continue;
			string name = getter.Source.Name;
			string? propertyName = name.StartsWith("Get", StringComparison.Ordinal) && name.Length > 3 ? name[3..]
				: getter.Returns is BoolConversion && IsPredicate(name) ? name
				: null;
			if (propertyName is null) continue;
			BoundFunction? setter = functions.FirstOrDefault(function => !used.Contains(function) && function.IsStatic == getter.IsStatic && function.Source.Name == "Set" + (name.StartsWith("Get", StringComparison.Ordinal) ? propertyName : name[2..]) && function.Parameters.Count == 1 && function.Parameters[0].Conversion.Suffix == getter.Returns.Suffix);
			used.Add(getter);
			if (setter is not null) used.Add(setter);
			bound.Properties.Add(new BoundProperty(tweak.Renames.GetValueOrDefault(name) ?? Naming.Member(propertyName), getter, setter));
		}
		foreach (BoundFunction function in functions.Where(function => !used.Contains(function))) {
			bound.Methods.Add(new BoundMethod(tweak.Renames.GetValueOrDefault(function.Source.Name) ?? Naming.Member(function.Source.Name), function));
		}
	}

	private static bool IsPredicate(string name) => name.Length > 2 && (name.StartsWith("Is", StringComparison.Ordinal) || name.StartsWith("Has", StringComparison.Ordinal) || name.StartsWith("Can", StringComparison.Ordinal)) && char.IsUpper(name[name.StartsWith("Is", StringComparison.Ordinal) ? 2 : 3]);

	private void BindEvents(BoundClass bound) {
		ClassTweak tweak = bound.Tweak;
		List<DoxygenEvent> events = [.. bound.Source.Events, .. tweak.AddedEvents.Select(static added => new DoxygenEvent(added.Macro, added.Constant, added.EventClass, string.Empty))];
		HashSet<string> macros = [.. events.Select(static found => found.Macro)];
		foreach (string named in tweak.EventNames.Keys) {
			if (!macros.Contains(named)) throw new InvalidOperationException($"A tweak names the event {named} of {bound.CppName}, which its event table does not list.");
		}
		foreach (DoxygenEvent found in events) {
			// With a list of members to bind, events are bound only when a tweak names them too.
			if (!tweak.EventNames.TryGetValue(found.Macro, out string? name)) {
				if (tweak.OnlyMembers is not null) continue;
				name = Naming.Event(found.Macro);
			}
			// An entry that leaves out its event type follows the convention that EVT_X sends wxEVT_X.
			// Entries for groups of events, such as EVT_MOUSE_EVENTS or EVT_JOY_*, send no one type, so
			// only an event type the headers declare is bound.
			string constant = found.EventType ?? "wx" + found.Macro;
			if (!MacroPattern().IsMatch(found.Macro) || !api.EventTypes.ContainsKey(constant)) {
				_skipped.Add($"{bound.CppName} event {found.Macro}: it is not one event type the headers declare");
				continue;
			}
			BoundEventType type = EventTypeFor(constant, found.EventClass ?? "wxEvent");
			// An event class's own table only says which event types it carries, and a class
			// re-lists the events it inherits, so neither gets a .NET event.
			if (!bound.Handle.IsEvtHandler || HasEvent(bound.Base, name)) continue;
			// C# allows no member named after its class, and none named after another member.
			if (HasMember(bound, name) || name == bound.Name) {
				_skipped.Add($"{bound.CppName} event {found.Macro}: named {name}Event, since {name} is taken");
				name += "Event";
			}
			if (bound.Events.Any(existing => existing.Name == name)) continue;
			bound.Events.Add(new BoundEvent(name, type, found.Description));
		}
	}

	private static bool HasEvent(BoundClass? bound, string name) {
		for (BoundClass? current = bound; current is not null; current = current.Base) {
			if (current.Events.Any(boundEvent => boundEvent.Name == name)) return true;
		}
		return false;
	}

	private static bool HasMember(BoundClass bound, string name) {
		for (BoundClass? current = bound; current is not null; current = current.Base) {
			if (current.Properties.Any(property => property.Name == name) || current.Methods.Any(method => method.Name == name)) return true;
		}
		return false;
	}

	private BoundEventType EventTypeFor(string constant, string eventClass) {
		if (_eventTypes.TryGetValue(constant, out BoundEventType? existing)) return existing;
		if (tweaks.ExtraEventTypes.TryGetValue(constant, out string? given)) eventClass = given;
		// An event class WxKit does not bind is delivered as its nearest bound ancestor.
		BoundClass wrapper = Ancestors(eventClass).Select(name => _classes.GetValueOrDefault(name)).FirstOrDefault(static candidate => candidate?.Handle == HandleKind.Event)
			?? throw new InvalidOperationException($"No bound event class can carry {constant}, a {eventClass}.");
		BoundEventType type = new(constant, Naming.EventType(constant), wrapper, api.EventTypes.GetValueOrDefault(constant));
		_eventTypes[constant] = type;
		return type;
	}

	private BoundStyles? BindStyles(BoundClass bound) {
		if (bound.Source.Styles.Count == 0) return null;
		string? prefix = bound.Tweak.StylePrefix ?? Naming.SharedStylePrefix(bound.Source.Styles.Select(static style => style.Name), api.Constants.ContainsKey);
		List<BoundStyle> members = [];
		foreach (DoxygenStyle style in bound.Source.Styles) {
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
			string name = Naming.Style(style.Name, prefix, bound.CppName);
			if (members.Any(existing => existing.Name == name)) continue;
			members.Add(new BoundStyle(name, style.Name, value, style.Description));
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
		BoundEnum bound = new(managed, cppName, EnumMembers(cppName, source, tweak?.MemberPrefix)) { IsFlags = tweak?.IsFlags ?? false, Guard = tweak?.Guard };
		_enums[cppName] = bound;
		return bound;
	}

	private List<BoundStyle> EnumMembers(string owner, DoxygenEnum source, string? memberPrefix) {
		int shared = SharedWords(source);
		List<BoundStyle> members = [];
		foreach (DoxygenEnumValue value in source.Values) {
			// A value that is unknown, or depends on the platform, such as wxCURSOR_ARROWWAIT, is left out.
			if (_constants.Value(value.Name) is not long number) {
				_skipped.Add($"enum {owner}: the value of {value.Name} is unknown or depends on the platform");
				continue;
			}
			string name = memberPrefix is not null && value.Name.StartsWith(memberPrefix, StringComparison.Ordinal) ? Naming.Words(value.Name[memberPrefix.Length..]) : MemberName(value.Name, shared);
			if (char.IsDigit(name[0])) name = "Value" + name;
			if (members.All(existing => existing.Name != name)) members.Add(new BoundStyle(name, value.Name, number, value.Brief));
		}
		return members;
	}

	// Constants wxWidgets defines one by one, such as wxACC_STATE_SYSTEM_FOCUSED, as one enum in
	// the order of their values. Flags get None, for none of them.
	private void BindConstantEnum(ConstantEnumTweak group) {
		List<string> names = group.Constants is IReadOnlyList<string> named
			? [.. named.Select(name => api.Constants.ContainsKey(name) ? name : throw new InvalidOperationException($"A tweak binds the constant {name}, which the interface headers do not have."))]
			: [.. api.Constants.Keys.Where(name => name.StartsWith(group.Prefix, StringComparison.Ordinal) && !group.ExcludePrefixes.Any(excluded => name.StartsWith(excluded, StringComparison.Ordinal))).Order(StringComparer.Ordinal)];
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

	private static bool Extends(BoundFunction longer, BoundFunction shorter) {
		IReadOnlyList<BoundParameter> start = shorter.ManagedParameters;
		IReadOnlyList<BoundParameter> all = longer.ManagedParameters;
		if (all.Count <= start.Count) return false;
		for (int index = 0; index < start.Count; index++) {
			if (all[index].Conversion.ManagedType(all[index].Nullable) != start[index].Conversion.ManagedType(start[index].Nullable)) return false;
		}
		return all.Skip(start.Count).All(static parameter => parameter.Default is not null);
	}

	private static string MemberName(string constant, int shared) => Naming.Words(string.Join('_', Words(constant)[shared..]));

	private static string[] Words(string constant) => (constant.StartsWith("wx", StringComparison.Ordinal) ? constant[2..] : constant).Split('_');

	private void Skip(BoundClass bound, DoxygenMember member, string reason) => _skipped.Add($"{bound.CppName}::{member.Name}({string.Join(", ", member.Parameters.Select(static parameter => parameter.Type))}): {reason}");

	[GeneratedRegex(@"^wx(HORIZONTAL|VERTICAL|BOTH)$")]
	private static partial Regex OrientationPattern();

	[GeneratedRegex(@"^wx\w+$")]
	private static partial Regex IdentifierPattern();

	[GeneratedRegex(@"^EVT_[A-Z0-9_]+$")]
	private static partial Regex MacroPattern();

	[GeneratedRegex("^\".*\"$")]
	private static partial Regex StringLiteralPattern();

	[GeneratedRegex(@"^wx[A-Z0-9_]+$")]
	private static partial Regex ConstantPattern();

	[GeneratedRegex(@"^\*wx[A-Z]+$")]
	private static partial Regex ColorConstantPattern();
}
