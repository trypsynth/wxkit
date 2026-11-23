using WxKit.Generator.Doxygen;

namespace WxKit.Generator.Binding;

// What the generator decided to bind, and how, which the emitters turn into code.

internal sealed record BindingResult(
	IReadOnlyList<BoundClass> Classes,
	IReadOnlyList<BoundEventType> EventTypes,
	IReadOnlyList<BoundEnum> Enums,
	IReadOnlyList<string> Skipped) {
	public IReadOnlyDictionary<string, string> StringConstants { get; init; } = new Dictionary<string, string>();
}

/// <summary>A C++ enum the managed API shows as a C# one, such as wxShowEffect as ShowEffect.</summary>
internal sealed record BoundEnum(string Name, string CppName, IReadOnlyList<BoundStyle> Members) {
	/// <summary>Whether its members are flags to combine, which gives it [Flags].</summary>
	public bool IsFlags { get; init; }

	/// <summary>The preprocessor condition under which wxWidgets declares it, which its checks in the shim are under.</summary>
	public string? Guard { get; init; }

	/// <summary>Whether it is a static class of int constants, such as StandardId, rather than a C# enum.</summary>
	public bool IsConstantClass { get; init; }
}

/// <summary>How objects of a class cross the boundary: the handle type and what it points at.</summary>
/// <param name="CType">The shim's handle type, such as wxk_Window.</param>
/// <param name="CppBase">The wxWidgets class the handle points at, such as wxWindow.</param>
/// <param name="InteropType">The handle's C# struct, such as WxkWindow.</param>
/// <param name="Accessor">The managed member that gives a wrapper's handle.</param>
/// <param name="Kind">The kind's name, which keeps kinds with the same handle, such as values and owned objects, apart.</param>
internal sealed record HandleKind(string CType, string CppBase, string InteropType, string Accessor, string Kind) {
	public static HandleKind Window { get; } = new("wxk_Window", "wxWindow", "WxkWindow", "Native", "Window");

	public static HandleKind Sizer { get; } = new("wxk_Sizer", "wxSizer", "WxkSizer", "Native", "Sizer");

	public static HandleKind Menu { get; } = new("wxk_Menu", "wxMenu", "WxkMenu", "Native", "Menu");

	public static HandleKind Event { get; } = new("wxk_Event", "wxEvent", "WxkEvent", "Handle", "Event");

	public static HandleKind EvtHandler { get; } = new("wxk_EvtHandler", "wxEvtHandler", "WxkEvtHandler", "NativeHandler", "EvtHandler");

	/// <summary>A value object, such as a font or a bitmap, which the managed side owns a copy of.</summary>
	public static HandleKind Value { get; } = new("wxk_Object", "wxObject", "WxkObject", "Native", "Value");

	/// <summary>A class with only static members, such as wxSystemSettings, which becomes a static class.</summary>
	public static HandleKind Static { get; } = new(string.Empty, string.Empty, string.Empty, string.Empty, "Static");

	/// <summary>An object the managed side creates and deletes, such as a drawing context.</summary>
	public static HandleKind Owned { get; } = new("wxk_Object", "wxObject", "WxkObject", "Native", "Owned");

	/// <summary>
	/// A data view's column, which its control owns and deletes. The control keeps the one managed
	/// object of each column, which is written by hand in part, and marks it destroyed with the column.
	/// </summary>
	public static HandleKind Column { get; } = new("wxk_DataViewColumn", "wxDataViewColumn", "WxkDataViewColumn", "Native", "Column");

	/// <summary>A toolbar's tool, which the toolbar owns, deletes, and keeps the managed object of, as a data view does its columns.</summary>
	public static HandleKind Tool { get; } = new("wxk_ToolBarTool", "wxToolBarToolBase", "WxkToolBarTool", "Native", "Tool");

	/// <summary>
	/// A menu's item, which the menu owns. The hand-written Menu calls the members that give one back,
	/// such as Append, for its id, so only internal members take or return one.
	/// </summary>
	public static HandleKind MenuItem { get; } = new("wxk_MenuItem", "wxMenuItem", "WxkMenuItem", "Native", "MenuItem");

	/// <summary>Whether the object belongs to a control, which gives back its one managed object, written by hand in part.</summary>
	public bool IsKeptByOwner => this == Column || this == Tool;

	/// <summary>Whether the managed side owns the object, and deletes it when it is disposed.</summary>
	public bool IsOwned => this == Value || this == Owned;

	/// <summary>
	/// A class that isn't a wxObject, such as wxDisplay, as a handle of its own type, wxk_Display,
	/// which the shim deletes with wxk_Display_delete.
	/// </summary>
	public static HandleKind Plain(string cppName) => new($"wxk_{Naming.Type(cppName)}", cppName, $"Wxk{Naming.Type(cppName)}", "Handle", "Plain");

	public bool IsPlain => Kind == "Plain";

	public bool IsEvtHandler => this == Window || this == Menu || this == EvtHandler;
}

internal sealed class BoundClass(DoxygenClass source, ClassTweak tweak, HandleKind handle) {
	public DoxygenClass Source { get; } = source;

	public ClassTweak Tweak { get; } = tweak;

	public HandleKind Handle { get; } = handle;

	public string CppName => Source.Name;

	public string Name => Tweak.ManagedName ?? Naming.Type(Source.Name);

	/// <summary>The nearest ancestor WxKit also binds.</summary>
	public BoundClass? Base { get; set; }

	/// <summary>Whether the class derives from wxTopLevelWindow, whose parent can be null.</summary>
	public bool IsTopLevel { get; set; }

	public List<BoundFunction> Constructors { get; } = [];

	public List<BoundMethod> Methods { get; } = [];

	public List<BoundProperty> Properties { get; } = [];

	/// <summary>Functions only hand-written code calls, which the managed class leaves out.</summary>
	public List<BoundFunction> InternalFunctions { get; } = [];

	public List<BoundEvent> Events { get; } = [];

	public BoundStyles? Styles { get; set; }

	/// <summary>The virtual members C# can override, each bound as a call to wxWidgets's own implementation.</summary>
	public List<BoundFunction> Directors { get; } = [];

	/// <summary>The overridable members of the class and its bases, which its director overrides.</summary>
	public IEnumerable<BoundFunction> AllDirectors => (Base?.AllDirectors ?? []).Concat(Directors);

	/// <summary>Whether WxKit creates a director, a subclass whose virtual members call into .NET, instead of the class itself.</summary>
	public bool HasDirector => (Constructors.Count > 0 || InternalFunctions.Any(static function => function.IsConstructor)) && AllDirectors.Any();

	public IEnumerable<BoundFunction> Functions => Constructors.Concat(Directors.Where(static director => director.HasBaseExport)).Concat(InternalFunctions).Concat(Methods.Select(static method => method.Function)).Concat(Properties.SelectMany(static property => property.Setter is null ? [property.Getter] : new[] { property.Getter, property.Setter }));
}

internal sealed record BoundParameter(string CppName, string Name, Conversion Conversion, bool Nullable, string? Default);

internal sealed class BoundFunction(BoundClass owner, DoxygenMember source, Conversion returns, IReadOnlyList<BoundParameter> parameters, bool isConstructor) {
	public BoundClass Owner { get; } = owner;

	public DoxygenMember Source { get; } = source;

	public Conversion Returns { get; } = returns;

	/// <summary>In the order the C++ function takes them.</summary>
	public IReadOnlyList<BoundParameter> Parameters { get; } = parameters;

	/// <summary>In the order the managed API takes them, which can differ, such as an id moved after the label.</summary>
	public IReadOnlyList<BoundParameter> ManagedParameters { get; set; } = parameters;

	public bool IsConstructor { get; } = isConstructor;

	/// <summary>
	/// The locals the shim gives the C++ call for what it fills, which the function returns, such as
	/// the list a <see cref="FilledListConversion"/> returns, each with where it goes among the call's arguments.
	/// </summary>
	public IReadOnlyList<(int Index, string Argument)> FilledArguments { get; init; } = [];

	public bool IsStatic => Source.IsStatic && !IsConstructor;

	/// <summary>Told apart from overloads with the same name, such as "SetSize_Int4".</summary>
	public string ExportName { get; set; } = isConstructor ? "new" : source.Name;

	public string ShimName => $"wxk_{Naming.Type(Owner.CppName)}_{ExportName}";

	public string InteropName => Naming.Type(Owner.CppName) + char.ToUpperInvariant(ExportName[0]) + ExportName[1..].Replace("_", string.Empty);

	public IReadOnlyList<string> OnlyFor => Source.OnlyFor;

	/// <summary>Whether this calls wxWidgets's own implementation of an overridable member, which a managed override's default calls.</summary>
	public bool IsDirectorBase { get; set; }

	/// <summary>
	/// Whether a director has an export of the class's own member, for the managed default. A pure
	/// virtual member has none, nor does one the director calls around .NET's, nor one whose callback
	/// is hand-written.
	/// </summary>
	public bool HasBaseExport { get; set; } = true;

	/// <summary>
	/// Whether the function creates a director, which takes the userdata it gives each callback
	/// and, when it is deleted, the shim's onFree.
	/// </summary>
	public bool TakesUserdata => IsConstructor && Owner.HasDirector;
}

internal sealed record BoundMethod(string Name, BoundFunction Function) {
	/// <summary>Whether it hides a static member of a base with the same signature, which needs the new modifier.</summary>
	public bool Hides { get; init; }
}

internal sealed record BoundProperty(string Name, BoundFunction Getter, BoundFunction? Setter) {
	public bool Hides { get; init; }
}

internal sealed record BoundEvent(string Name, BoundEventType Type, string Description);

/// <param name="Header">The header that declares the constant, such as wx/textctrl.h.</param>
internal sealed record BoundEventType(string Constant, string Name, BoundClass EventClass, string? Header);

internal sealed record BoundStyles(string Name, IReadOnlyList<BoundStyle> Members);

internal sealed record BoundStyle(string Name, string Constant, long Value, string Description);
