using WxKit.Generator.Doxygen;

namespace WxKit.Generator.Binding;

// The decisions the XML cannot make on its own: which classes and members to bind, what to call
// events, which getters and setters form a property, and which types a number really has. Every
// name a tweak gives is checked against the XML, so a tweak cannot quietly go stale.
internal sealed class Tweaks {
	private readonly Dictionary<string, ClassTweak> _classes = new(StringComparer.Ordinal);

	public IReadOnlyDictionary<string, ClassTweak> Classes => _classes;

	/// <summary>
	/// Event types to bind even though no bound class lists them, and the event class each sends,
	/// which wins over the one the headers give, for when they give the wrong one.
	/// </summary>
	public Dictionary<string, string> ExtraEventTypes { get; } = new(StringComparer.Ordinal);

	public ClassTweak Class(string name, Action<ClassTweak>? configure = null) {
		if (!_classes.TryGetValue(name, out ClassTweak? tweak)) _classes[name] = tweak = new ClassTweak(name);
		configure?.Invoke(tweak);
		return tweak;
	}

	public void EventType(string constant, string eventClass) => ExtraEventTypes[constant] = eventClass;

	/// <summary>C++ enums to bind under a name of their own, such as wxAccRole as AccessibleRole, even when no bound member uses them.</summary>
	public Dictionary<string, EnumTweak> Enums { get; } = new(StringComparer.Ordinal);

	/// <summary>Constants wxWidgets defines one by one with a shared prefix, bound as one C# enum, such as wxACC_STATE_SYSTEM_ as AccessibleStates.</summary>
	public List<ConstantEnumTweak> ConstantEnums { get; } = [];

	/// <param name="memberPrefix">A prefix most members share that their names leave out, such as wxROLE_SYSTEM_.</param>
	/// <param name="guard">The preprocessor condition under which wxWidgets declares the enum, such as wxUSE_ACCESSIBILITY, which only wxMSW has.</param>
	public void Enum(string cppName, string managedName, bool isFlags = false, string? memberPrefix = null, string? guard = null, string[]? exclude = null) => Enums[cppName] = new EnumTweak(managedName, isFlags, memberPrefix, guard, exclude ?? []);

	/// <summary>Members an enum gains that the headers give no one value, each with the value WxKit gives it, a description, and what it wraps.</summary>
	public Dictionary<string, List<(string Name, long Value, string Description, string Wraps)>> AddedEnumMembers { get; } = new(StringComparer.Ordinal);

	public void EnumMember(string cppName, string name, long value, string description, string wraps) {
		if (!AddedEnumMembers.TryGetValue(cppName, out List<(string Name, long Value, string Description, string Wraps)>? members)) AddedEnumMembers[cppName] = members = [];
		members.Add((name, value, description, wraps));
	}

	/// <summary>Members an enum leaves out, such as a second spelling or a bound, beside those its own tweak excludes.</summary>
	public Dictionary<string, HashSet<string>> ExcludedEnumMembers { get; } = new(StringComparer.Ordinal);

	public void ExcludeEnumMembers(string cppName, params string[] members) {
		if (!ExcludedEnumMembers.TryGetValue(cppName, out HashSet<string>? excluded)) ExcludedEnumMembers[cppName] = excluded = new(StringComparer.Ordinal);
		excluded.UnionWith(members);
	}

	/// <param name="excludePrefixes">Prefixes of constants that share the prefix but belong to something else, such as wxSIZE_CONV_ under wxSIZE_.</param>
	public void ConstantEnum(string managedName, string prefix, bool isFlags = false, string? guard = null, string[]? excludePrefixes = null) => ConstantEnums.Add(new ConstantEnumTweak(managedName, prefix, isFlags, guard, excludePrefixes ?? []));

	/// <summary>Constants that share no prefix, such as a wrap sizer's flags, as an enum.</summary>
	public void ConstantEnumOf(string managedName, bool isFlags, params string[] constants) => ConstantEnums.Add(new ConstantEnumTweak(managedName, Prefix: string.Empty, isFlags, Guard: null, ExcludePrefixes: [], constants));

	/// <summary>
	/// C++ enums bound as a static class of int constants rather than a C# enum, such as wxStandardID
	/// as StandardId, for values that go where any int does, such as a window's id.
	/// </summary>
	public Dictionary<string, ConstantClassTweak> ConstantClasses { get; } = new(StringComparer.Ordinal);

	/// <param name="exclude">Values left out, such as the bounds of the ids wxWidgets assigns itself.</param>
	public void ConstantClass(string cppName, string managedName, params string[] exclude) => ConstantClasses[cppName] = new ConstantClassTweak(managedName, new HashSet<string>(exclude, StringComparer.Ordinal));

	/// <summary>
	/// Constants wxWidgets defines one by one with a shared prefix, bound as a static class of int
	/// constants, as the styled text control's wxSTC_ ones are Stc, since they are too many, in too
	/// many groups, to be enums.
	/// </summary>
	public Dictionary<string, string> PrefixConstantClasses { get; } = new(StringComparer.Ordinal);

	public void ConstantClassOf(string managedName, string prefix) => PrefixConstantClasses[prefix] = managedName;

	/// <summary>Classes wxWidgets makes from a template, such as wxScrolledWindow from wxScrolled&lt;wxPanel&gt;, which the interface headers only give as the template.</summary>
	public List<TemplateInstanceTweak> TemplateInstances { get; } = [];

	/// <summary>Groups of free functions, each bound as a static class's members.</summary>
	public List<FunctionGroupTweak> FunctionGroups { get; } = [];

	/// <param name="template">The template, such as wxScrolled, whose base is its parameter.</param>
	/// <param name="name">The class made from it, such as wxScrolledWindow.</param>
	/// <param name="baseClass">The template's argument, which is the class's base, such as wxPanel.</param>
	public void TemplateInstance(string template, string name, string baseClass) => TemplateInstances.Add(new TemplateInstanceTweak(template, name, baseClass));

	/// <summary>
	/// Binds free functions, such as wxBell, as the members of a group the shim names them by, such
	/// as wxk_Desktop_Bell, declared in <paramref name="header"/>. The group has no managed class, and
	/// hand-written code calls its imports.
	/// </summary>
	public ClassTweak Functions(string group, string header, params string[] functions) {
		FunctionGroups.Add(new FunctionGroupTweak(group, header, functions));
		return Class(group, static tweak => tweak.StaticOnly().InteropOnly().Global());
	}
}

internal sealed record OverloadGuard(string Member, string Parameter, string Type, string Condition) {
	public bool Matches(DoxygenMember member) => member.Name == Member && member.Parameters.Any(parameter => parameter.Name == Parameter && parameter.Type.Replace(" ", string.Empty, StringComparison.Ordinal) == Type.Replace(" ", string.Empty, StringComparison.Ordinal));
}

internal sealed record OverloadMatch(string Member, string Parameter, string Type) {
	public bool Matches(DoxygenMember member) => member.Name == Member && member.Parameters.Any(parameter => parameter.Name == Parameter && parameter.Type.Replace(" ", string.Empty, StringComparison.Ordinal) == Type.Replace(" ", string.Empty, StringComparison.Ordinal));
}

internal sealed record ConstantClassTweak(string Name, IReadOnlySet<string> Exclude);

internal sealed record TemplateInstanceTweak(string Template, string Name, string BaseClass);

internal sealed record FunctionGroupTweak(string Name, string Header, IReadOnlyList<string> Functions);

internal sealed record EnumTweak(string Name, bool IsFlags, string? MemberPrefix, string? Guard, string[] Exclude);

internal sealed record ConstantEnumTweak(string Name, string Prefix, bool IsFlags, string? Guard, IReadOnlyList<string> ExcludePrefixes, IReadOnlyList<string>? Constants = null);

internal sealed class ClassTweak(string name) {
	public string CppName { get; } = name;

	/// <summary>Null binds every supported member. Otherwise only these, by their C++ names.</summary>
	public HashSet<string>? OnlyMembers { get; private set; }

	public HashSet<string> IgnoredMembers { get; } = new(StringComparer.Ordinal);

	/// <summary>
	/// Members bound for hand-written code to call, which the shim and its imports have but the
	/// managed class doesn't, such as wxToolBar's DeleteTool, which ToolBar wraps to forget the tool.
	/// A hand-written class binds only these.
	/// </summary>
	public HashSet<string> InternalMembers { get; } = new(StringComparer.Ordinal);

	/// <summary>The class is written by hand. The generator only uses it as a type, and lists it so it can be wrapped.</summary>
	public bool IsHandwritten { get; private set; }

	public bool HasConstructors { get; private set; } = true;

	/// <summary>
	/// Whether the class binds the members of the bases above it that are not bound themselves, as
	/// wxDataViewColumn binds wxHeaderColumn's, rather than leaving them out.
	/// </summary>
	public bool InheritsUnboundBases { get; private set; }

	/// <summary>
	/// The bases to use in place of those the interface headers give, when one they give is not
	/// documented, as wxRichMessageDialog's wxRichMessageDialogBase is not.
	/// </summary>
	public List<string>? BaseOverride { get; private set; }

	/// <summary>Bound classes whose own members this class has too, and the real bases the shim calls them through.</summary>
	public Dictionary<string, string> Borrowed { get; } = new(StringComparer.Ordinal);

	/// <summary>Whether WxKit can wrap a native object of this class it did not create. Generated event handler classes always can.</summary>
	public bool IsWrappable { get; private set; }

	public string? ManagedName { get; private set; }

	public Dictionary<string, string> EventNames { get; } = new(StringComparer.Ordinal);

	public List<(string Macro, string Constant, string EventClass, string Name)> AddedEvents { get; } = [];

	public Dictionary<string, (string Getter, string? Setter)> PropertyPairs { get; } = new(StringComparer.Ordinal);

	public Dictionary<string, string> Renames { get; } = new(StringComparer.Ordinal);

	public Dictionary<(string Member, string Parameter), string> ParameterEnums { get; } = [];

	public HashSet<(string Member, string Parameter)> Milliseconds { get; } = [];

	/// <summary>Managed names of parameters the naming rules get wrong, such as a dir that is a directory, not a direction.</summary>
	public Dictionary<(string Member, string Parameter), string> ParameterNames { get; } = [];

	/// <summary>The prefix the class's styles share, for one the rules do not find, such as FNTP in wxFNTP_USE_TEXTCTRL.</summary>
	public string? StylePrefix { get; private set; }

	public ClassTweak Only(params string[] members) {
		OnlyMembers = new HashSet<string>(members, StringComparer.Ordinal);
		return this;
	}

	/// <summary>An event's setters a handler calls to answer it, which are bound though an event's other setters are not.</summary>
	public HashSet<string> HandlerSetters { get; } = new(StringComparer.Ordinal);

	public ClassTweak HandlerSets(params string[] members) {
		HandlerSetters.UnionWith(members);
		return this;
	}

	public ClassTweak Ignore(params string[] members) {
		IgnoredMembers.UnionWith(members);
		return this;
	}

	public ClassTweak Internal(params string[] members) {
		InternalMembers.UnionWith(members);
		return this;
	}

	public ClassTweak Handwritten() {
		IsHandwritten = true;
		return this;
	}

	/// <summary>The class only has static members, such as wxSystemSettings, and becomes a static class.</summary>
	public bool IsStaticOnly { get; private set; }

	/// <summary>
	/// Members wxWidgets only has under a preprocessor condition the interface headers do not mark,
	/// such as RegisterHotKey under wxUSE_HOTKEY. The shim's function is a stub that does nothing
	/// where the condition is false, as for a member of another port.
	/// </summary>
	public Dictionary<string, string> MemberGuards { get; } = new(StringComparer.Ordinal);

	public ClassTweak Guard(string condition, params string[] members) {
		foreach (string member in members) MemberGuards[member] = condition;
		return this;
	}

	/// <summary>Constructor parameters the native object goes on using, such as a find dialog's data, which the managed object keeps alive.</summary>
	public HashSet<string> KeptAlive { get; } = new(StringComparer.Ordinal);

	public ClassTweak KeepAlive(params string[] parameters) {
		KeptAlive.UnionWith(parameters);
		return this;
	}

	/// <summary>Overloads guarded the same way, told apart by one of their parameters, such as wxStaticBox's that takes a window as its label.</summary>
	public List<OverloadGuard> OverloadGuards { get; } = [];

	public ClassTweak GuardOverload(string condition, string member, string parameter, string type) {
		OverloadGuards.Add(new OverloadGuard(member, parameter, type, condition));
		return this;
	}

	/// <summary>Parameters that can be null though the headers don't say so, as wxFrame::SetStatusBar's, which null removes.</summary>
	public HashSet<(string Member, string Parameter)> NullableParameters { get; } = [];

	public ClassTweak NullableParameter(string member, string parameter) {
		NullableParameters.Add((member, parameter));
		return this;
	}

	/// <summary>Members that return a pointer that's never null, such as wxStaticBoxSizer::GetStaticBox, whose sizer always has its box.</summary>
	public HashSet<string> NonNullReturns { get; } = new(StringComparer.Ordinal);

	public ClassTweak NonNullReturn(params string[] members) {
		NonNullReturns.UnionWith(members);
		return this;
	}

	/// <summary>Overloads left out, told apart by one of their parameters, such as wxImage's SetAlpha that takes an alpha array.</summary>
	public List<OverloadMatch> IgnoredOverloads { get; } = [];

	/// <summary>Overloads whose import is kept for hand-written code, without a managed member, as <see cref="Internal"/> does for every overload.</summary>
	public List<OverloadMatch> InternalOverloads { get; } = [];

	/// <summary>Whether the constructor that takes nothing is internal, for a value whose empty form is of no use to an application.</summary>
	public bool HidesEmptyConstructor { get; private set; }

	public ClassTweak InternalEmptyConstructor() {
		HidesEmptyConstructor = true;
		return this;
	}

	public bool IsInternal(DoxygenMember member, string shortName) => InternalMembers.Contains(member.Name) || InternalOverloads.Any(overload => overload.Matches(member)) || (HidesEmptyConstructor && member.Name == shortName && member.Parameters.Count == 0);

	public ClassTweak InternalOverload(string member, string parameter, string type) {
		InternalOverloads.Add(new OverloadMatch(member, parameter, type));
		return this;
	}

	public ClassTweak IgnoreOverload(string member, string parameter, string type) {
		IgnoredOverloads.Add(new OverloadMatch(member, parameter, type));
		return this;
	}

	/// <summary>The virtual members C# can override, such as a virtual list's OnGetItemText, which a director in the shim calls.</summary>
	public List<string> DirectorMembers { get; } = [];

	public ClassTweak Director(params string[] members) {
		DirectorMembers.AddRange(members);
		return this;
	}

	/// <summary>Whether the director is a class of its own to wxWidgets, which a port may only call an overridable member for, as wxMSW does to sort a tree.</summary>
	public bool HasDirectorClassInfo { get; private set; }

	public ClassTweak DirectorClassInfo() {
		HasDirectorClassInfo = true;
		return this;
	}

	/// <summary>Whether hand-written code gives the directors' callbacks, as Printout does, rather than a generated managed class.</summary>
	public bool IsDirectorInternal { get; private set; }

	/// <summary>Directors whose callbacks hand-written code sets, which can also write through out parameters.</summary>
	public ClassTweak InternalDirector(params string[] members) {
		IsDirectorInternal = true;
		return Director(members);
	}

	/// <summary>Directors that call the class's own member first, and .NET only if it succeeds, as wxPrintout's OnBeginDocument starts the document.</summary>
	public HashSet<string> DirectorsAfterBase { get; } = new(StringComparer.Ordinal);

	/// <summary>Directors that call .NET first, and then the class's own member, as wxPrintout's OnEndDocument ends the document.</summary>
	public HashSet<string> DirectorsBeforeBase { get; } = new(StringComparer.Ordinal);

	/// <summary>A condition the whole class is under, such as wxUSE_ACCESSIBILITY for wxAccessible, without which its functions do nothing.</summary>
	public string? ClassGuard { get; private set; }

	public ClassTweak GuardClass(string condition) {
		ClassGuard = condition;
		return this;
	}

	public ClassTweak DirectorAfterBase(string member) {
		DirectorsAfterBase.Add(member);
		return this;
	}

	public ClassTweak DirectorBeforeBase(string member) {
		DirectorsBeforeBase.Add(member);
		return this;
	}

	/// <summary>The managed side creates and deletes objects of the class, though its ancestry says otherwise, as for wxValidator, an event handler.</summary>
	public bool IsOwnedObject { get; private set; }

	public ClassTweak Owned() {
		IsOwnedObject = true;
		return this;
	}

	/// <summary>The class is copied like a value, though it is not one of the classes that are by their ancestry, such as wxListItem.</summary>
	public bool IsValueObject { get; private set; }

	/// <summary>Whether the class's longs are indexes, positions, and counts, which are ints in the managed API.</summary>
	public bool HasIntIndexes { get; private set; }

	/// <summary>A member's long parameters that are ints in the managed API, such as a window's id.</summary>
	public HashSet<(string Member, string Parameter)> IntParameters { get; } = [];

	public ClassTweak IntParameter(string member, string parameter) {
		IntParameters.Add((member, parameter));
		return this;
	}

	/// <summary>A member's long parameters that stay long in a class whose others are indexes, such as the number an application keeps with a list's item.</summary>
	public HashSet<(string Member, string Parameter)> LongParameters { get; } = [];

	public ClassTweak LongParameter(string member, string parameter) {
		LongParameters.Add((member, parameter));
		return this;
	}

	public ClassTweak IntIndexes() {
		HasIntIndexes = true;
		return this;
	}

	public ClassTweak ValueObject() {
		IsValueObject = true;
		return this;
	}

	/// <summary>Members whose return type cannot be converted are bound without one, as a toolbar's AddTool is without the tool it returns.</summary>
	public bool DiscardsUnboundReturns { get; private set; }

	public ClassTweak DiscardUnboundReturns() {
		DiscardsUnboundReturns = true;
		return this;
	}

	/// <summary>Members that change their object and return it, which are bound without the return, so they aren't taken for ones that return a copy.</summary>
	public HashSet<string> DiscardedReturns { get; } = new(StringComparer.Ordinal);

	public ClassTweak DiscardReturn(params string[] members) {
		DiscardedReturns.UnionWith(members);
		return this;
	}

	/// <summary>Whether only the shim and its imports are written, for hand-written code to call, rather than a managed class too.</summary>
	public bool IsInteropOnly { get; private set; }

	/// <summary>Whether the members are free functions, which the shim calls without a class.</summary>
	public bool IsGlobal { get; private set; }

	/// <summary>Whether the class isn't a wxObject, and crosses as a handle of its own type, as <see cref="HandleKind.Plain"/> describes.</summary>
	public bool IsPlain { get; private set; }

	/// <summary>The class whose handle a plain class crosses as, such as wxDataViewModel for wxDataViewVirtualListModel, or null for its own.</summary>
	public string? PlainHandle { get; private set; }

	/// <summary>Binds a class that isn't a wxObject, such as wxDisplay, for hand-written code to call, as its own handle or a base's.</summary>
	public ClassTweak Plain(string? handleClass = null) {
		IsPlain = true;
		PlainHandle = handleClass;
		return InteropOnly();
	}

	/// <summary>
	/// Whether the class is bound without its nearest bound base, as wxDataViewVirtualListModel is
	/// without wxDataViewModel, whose directors a base between them already overrides.
	/// </summary>
	public bool HasNoBase { get; private set; }

	public ClassTweak NoBase() {
		HasNoBase = true;
		return this;
	}

	public ClassTweak InteropOnly() {
		IsInteropOnly = true;
		return this;
	}

	public ClassTweak Global() {
		IsGlobal = true;
		return this;
	}

	public ClassTweak StaticOnly() {
		IsStaticOnly = true;
		return this;
	}

	public ClassTweak Wrappable() {
		IsWrappable = true;
		return this;
	}

	/// <summary>
	/// Mixins the interface headers name differently from the real ones, and the real base the shim
	/// casts to for their members, as wxStyledTextCtrl's wxTextEntry is wxTextEntryBase there.
	/// </summary>
	public Dictionary<string, string> MixinBases { get; } = new(StringComparer.Ordinal);

	public ClassTweak MixinBase(string mixin, string realBase) {
		MixinBases[mixin] = realBase;
		return this;
	}

	public ClassTweak Bases(params string[] bases) {
		BaseOverride = [.. bases];
		return this;
	}

	/// <summary>
	/// Gives the class <paramref name="source"/>'s own members, called through
	/// <paramref name="through"/>, as wxRichMessageDialog has wxMessageDialog's through
	/// wxMessageDialogBase without being a wxMessageDialog.
	/// </summary>
	public ClassTweak Borrow(string source, string through) {
		Borrowed[source] = through;
		MixinBases[source] = through;
		return this;
	}

	public ClassTweak InheritUnboundBases() {
		InheritsUnboundBases = true;
		return this;
	}

	/// <summary>Whether a constructor's list of items can be left out, as an item control's can, which is made empty and filled later.</summary>
	public bool HasOptionalItems { get; private set; }

	public ClassTweak OptionalItems() {
		HasOptionalItems = true;
		return this;
	}

	public ClassTweak NoConstructors() {
		HasConstructors = false;
		return this;
	}

	public ClassTweak Named(string managedName) {
		ManagedName = managedName;
		return this;
	}

	/// <summary>Events a class's event table lists that aren't bound, such as one another already is under another name.</summary>
	public HashSet<string> IgnoredEvents { get; } = new(StringComparer.Ordinal);

	public ClassTweak IgnoreEvent(params string[] macros) {
		IgnoredEvents.UnionWith(macros);
		return this;
	}

	/// <summary>Names the event a class's event table lists under <paramref name="macro"/>.</summary>
	public ClassTweak Event(string macro, string eventName) {
		EventNames[macro] = eventName;
		return this;
	}

	/// <summary>Adds an event the class's event table leaves out.</summary>
	public ClassTweak AddEvent(string macro, string constant, string eventClass, string eventName) {
		AddedEvents.Add((macro, constant, eventClass, eventName));
		EventNames[macro] = eventName;
		return this;
	}

	/// <summary>Makes a property from a getter and setter whose names do not follow Get and Set.</summary>
	public ClassTweak Property(string propertyName, string getter, string? setter = null) {
		PropertyPairs[propertyName] = (getter, setter);
		return this;
	}

	public ClassTweak Rename(string member, string managedName) {
		Renames[member] = managedName;
		return this;
	}

	/// <summary>Makes a number of milliseconds a TimeSpan. The parameter "return" names the return value.</summary>
	public ClassTweak ParameterMilliseconds(string member, string parameter) {
		Milliseconds.Add((member, parameter));
		return this;
	}

	/// <summary>Gives a number parameter an enum type, such as an orientation. The parameter "return" names the return value.</summary>
	public ClassTweak ParameterEnum(string member, string parameter, string enumName) {
		ParameterEnums[(member, parameter)] = enumName;
		return this;
	}

	public ClassTweak ParameterName(string member, string parameter, string managedName) {
		ParameterNames[(member, parameter)] = managedName;
		return this;
	}

	/// <summary>Whether the class keeps the styles its constructor takes apart from its window's, as a message dialog does its buttons and icon, so they aren't its Style.</summary>
	public bool KeepsStylesApart { get; private set; }

	public ClassTweak StylesKeptApart() {
		KeepsStylesApart = true;
		return this;
	}

	/// <summary>Styles the class takes that its documentation has no table of, each a constant, the member's name, and what it does.</summary>
	public List<(string Constant, string Name, string Description)> AddedStyles { get; } = [];

	public ClassTweak Style(string constant, string name, string description) {
		AddedStyles.Add((constant, name, description));
		return this;
	}

	/// <summary>Styles the class's table lists that its enum leaves out, such as a second name for another.</summary>
	public HashSet<string> IgnoredStyles { get; } = new(StringComparer.Ordinal);

	public ClassTweak IgnoreStyle(params string[] styles) {
		IgnoredStyles.UnionWith(styles);
		return this;
	}

	/// <summary>Properties that hide a base's of the same name, which says something else of this class, rather than being left out as a repeat.</summary>
	public HashSet<string> HidingProperties { get; } = new(StringComparer.Ordinal);

	public ClassTweak HidesInherited(params string[] properties) {
		HidingProperties.UnionWith(properties);
		return this;
	}

	public ClassTweak StylesPrefixed(string prefix) {
		StylePrefix = prefix;
		return this;
	}
}
