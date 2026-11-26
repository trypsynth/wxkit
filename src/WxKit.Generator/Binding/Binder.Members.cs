using System.Globalization;
using System.Text.RegularExpressions;
using WxKit.Generator.Doxygen;

namespace WxKit.Generator.Binding;

// The members of a class: which are bound, how overloads are named and merged, and which pairs
// form a property.
internal sealed partial class Binder {
	private void BindMembers(BoundClass bound) {
		ClassTweak tweak = bound.Tweak;
		IEnumerable<DoxygenMember> members = MembersOf(bound.Source, tweak.InheritsUnboundBases);
		foreach (string source in tweak.Borrowed.Keys) {
			if (!api.Classes.TryGetValue(source, out DoxygenClass? lender)) throw new InvalidOperationException($"A tweak gives {bound.CppName} the members of {source}, which the interface headers do not have.");
			members = members.Concat(lender.Members.Where(member => member.Name != source && member.Name != "~" + source && !bound.Source.Members.Any(own => own.Name == member.Name)).Select(member => member with { Mixin = source }));
		}
		List<DoxygenMember> candidates = [.. members.Where(static member => member.IsPublic && !member.Name.StartsWith('~') && !member.Name.StartsWith("operator", StringComparison.Ordinal))];
		// An overload that takes x and y, or a width and a height, beside one that takes a point, a
		// size, or a rectangle, only repeats it. A constructor keeps both, as .NET's Bitmap does.
		candidates.RemoveAll(member => member.Name != bound.CppName && candidates.Any(other => other.Name == member.Name && other.IsStatic == member.IsStatic && other.Parameters.Count < member.Parameters.Count && RepeatsWithCoordinates(member, other)));
		HashSet<string> known = [.. candidates.Select(static member => member.Name)];
		foreach (string kept in tweak.KeptAlive) {
			if (!candidates.Any(member => member.Name == bound.CppName && member.Parameters.Any(parameter => parameter.Name == kept))) throw new InvalidOperationException($"A tweak keeps {bound.CppName}'s constructor parameter {kept} alive, which no constructor has.");
		}
		foreach (string named in (tweak.OnlyMembers ?? []).Concat(tweak.IgnoredMembers).Concat(tweak.InternalMembers).Concat(tweak.MemberGuards.Keys).Concat(tweak.OverloadGuards.Select(static guard => guard.Member)).Concat(tweak.IgnoredOverloads.Select(static ignored => ignored.Member)).Concat(tweak.NullableParameters.Select(static nullable => nullable.Member)).Concat(tweak.NonNullReturns).Concat(tweak.Renames.Keys.Where(name => !tweak.PropertyPairs.ContainsKey(name))).Concat(tweak.PropertyPairs.Values.SelectMany(static pair => pair.Setter is null ? [pair.Getter] : new[] { pair.Getter, pair.Setter }))) {
			if (!known.Contains(named)) throw new InvalidOperationException($"A tweak names {bound.CppName}::{named}, which the interface headers do not have.");
		}
		string shortName = bound.CppName;
		List<BoundFunction> functions = [];
		foreach (DoxygenMember member in candidates) {
			bool isConstructor = member.Name == shortName;
			if (tweak.IgnoredMembers.Contains(member.Name) || tweak.IgnoredOverloads.Any(ignored => ignored.Matches(member))) continue;
			bool isInternal = tweak.IsInternal(member, shortName);
			// An application receives events and doesn't make them, so what fills one in is left out,
			// but for what a handler answers with, such as the cursor a wxSetCursorEvent asks for.
			if (bound.CppName.EndsWith("Event", StringComparison.Ordinal) && !member.IsStatic && !isInternal && member.Name.StartsWith("Set", StringComparison.Ordinal) && !tweak.HandlerSetters.Contains(member.Name)) {
				_skipped.Add($"{bound.CppName}::{member.Name}: it fills in an event, which an application only receives");
				continue;
			}
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
		bound.InternalFunctions.AddRange(functions.Where(function => tweak.IsInternal(function.Source, shortName)));
		functions.RemoveAll(bound.InternalFunctions.Contains);
		// Two C++ constructors can have the same managed signature, as an int and a long do, and C#
		// allows only one.
		List<BoundFunction> constructors = [.. functions.Where(static function => function.IsConstructor).DistinctBy(static function => string.Join(",", function.ManagedParameters.Select(static parameter => parameter.Conversion.ManagedType(parameter.Nullable))))];
		// A constructor whose parameters start another's, which has only defaults after them, is left
		// out, as wxBannerWindow(parent, dir) is beside the one that adds the id, position, size, and
		// style, since a call such as new BannerWindow(parent) could mean either.
		constructors.RemoveAll(shorter => constructors.Any(longer => !ReferenceEquals(longer, shorter) && Extends(longer, shorter)));
		// With a list of items that can be left out, so is the constructor that has no list, whose
		// parameters are among the other's. What it defaults, the other does too.
		if (bound.Tweak.HasOptionalItems) {
			foreach (BoundFunction shorter in constructors.ToArray()) {
				if (constructors.FirstOrDefault(longer => !ReferenceEquals(longer, shorter) && Includes(longer, shorter)) is not BoundFunction longer) continue;
				longer.ManagedParameters = [.. longer.ManagedParameters.Select(parameter => parameter.Default is null && shorter.ManagedParameters.FirstOrDefault(other => other.Name == parameter.Name)?.Default is string value ? parameter with { Default = value } : parameter)];
				constructors.Remove(shorter);
			}
		}
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
			bool CanCallBack(Conversion conversion) => conversion is BoolConversion or NumberConversion or EnumConversion or ItemIdConversion || (bound.Tweak.IsDirectorInternal && conversion is OutConversion { CanCallBack: true } or CallbackOutConversion or CallbackLentConversion or GeometryConversion or ItemIdConversion or CountConversion);
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
		bound.Properties.RemoveAll(property => !property.Getter.IsStatic && !bound.Tweak.HidingProperties.Contains(property.Name) && inherited.Contains(property.Name) || !seen.Add(property.Name));
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
			if (owner.Tweak.DiscardedReturns.Contains(member.Name)) mapped = VoidConversion.Instance;
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
			bool nullable = (parameter.Name == "parent" && isConstructor && owner.IsTopLevel) || owner.Tweak.NullableParameters.Contains((member.Name, parameter.Name));
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
			// An item control can be made empty and filled later, so its list of items can be left out.
			if (isConstructor && owner.Tweak.HasOptionalItems && managedDefault is null && conversion is StringArrayConversion) {
				managedDefault = "null";
				nullable = true;
			}
			// wxWidgets abbreviates both a column and a color as col, and only a number is a column.
			string managedName = owner.Tweak.ParameterNames.GetValueOrDefault((member.Name, parameter.Name)) ?? (parameter.Name == "col" && parameter.Type.Contains("wxColour", StringComparison.Ordinal) ? "color" : Naming.Parameter(parameter.Name));
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
		// A list of items that can be left out stays ahead of the rest that can, as it is what a
		// call most often gives.
		static bool IsItems(BoundParameter parameter) => parameter is { Conversion: StringArrayConversion, Default: "null" };
		return [.. ordered.Where(static parameter => parameter.Default is null), .. ordered.Where(IsItems), .. ordered.Where(static parameter => parameter.Default is not null && !IsItems(parameter))];
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

	// The last four are a mask, a bound without the underscore, and second spellings of Tiff.
	// Whether a member takes as numbers what another overload takes as a point, a size, or a
	// rectangle: x and y for a point, a width and a height for a size, and all four for a rectangle
	// or for a point and a size.
	private static bool RepeatsWithCoordinates(DoxygenMember member, DoxygenMember other) {
		bool point = HasPair(member, "x", "y") || HasPair(member, "x1", "y1");
		bool size = HasPair(member, "width", "height") || HasPair(member, "w", "h");
		if (!point && !size) return false;
		HashSet<string> types = [.. other.Parameters.Select(static parameter => CppType.Parse(parameter.Type).Name)];
		if (point && size) return types.Contains("wxRect") || (types.Contains("wxPoint") && types.Contains("wxSize"));
		return types.Contains(point ? "wxPoint" : "wxSize");
	}

	private static bool HasPair(DoxygenMember member, string first, string second) {
		for (int index = 0; index + 1 < member.Parameters.Count; index++) {
			if (member.Parameters[index].Name != first || member.Parameters[index + 1].Name != second) continue;
			if (CppType.Parse(member.Parameters[index].Type).Name is "int" or "wxCoord" && CppType.Parse(member.Parameters[index + 1].Type).Name is "int" or "wxCoord") return true;
		}
		return false;
	}

	// Whether every parameter of one constructor is among another's, in order, and the rest of the
	// other's can be left out.
	private static bool Includes(BoundFunction longer, BoundFunction shorter) {
		IReadOnlyList<BoundParameter> all = longer.ManagedParameters;
		if (all.Count <= shorter.ManagedParameters.Count) return false;
		int next = 0;
		foreach (BoundParameter parameter in shorter.ManagedParameters) {
			while (next < all.Count && (all[next].Name != parameter.Name || all[next].Conversion.ManagedType(all[next].Nullable) != parameter.Conversion.ManagedType(parameter.Nullable))) {
				if (all[next].Default is null) return false;
				next++;
			}
			if (next == all.Count) return false;
			next++;
		}
		return all.Skip(next).All(static parameter => parameter.Default is not null);
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

	[GeneratedRegex(@"^wx(HORIZONTAL|VERTICAL|BOTH)$")]
	private static partial Regex OrientationPattern();

	[GeneratedRegex(@"^wx\w+$")]
	private static partial Regex IdentifierPattern();

	[GeneratedRegex(@"^\*wx[A-Z]+$")]
	private static partial Regex ColorConstantPattern();
}
