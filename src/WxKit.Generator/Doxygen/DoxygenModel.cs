namespace WxKit.Generator.Doxygen;

// What Doxygen says about the wxWidgets interface headers, before any decision about binding it.

/// <param name="EventTypes">Every wxEVT_ constant the headers declare, and the header that declares it, such as wx/textctrl.h.</param>
/// <param name="Enums">Every enum, by its name and, for one inside a class, also by Class::Name.</param>
internal sealed record DoxygenApi(IReadOnlyDictionary<string, DoxygenClass> Classes, IReadOnlyDictionary<string, string> Constants, IReadOnlyDictionary<string, string> EventTypes, IReadOnlyDictionary<string, DoxygenEnum> Enums) {
	/// <summary>Functions outside any class, such as wxBell, by name, each with its overloads.</summary>
	public IReadOnlyDictionary<string, IReadOnlyList<DoxygenMember>> Functions { get; init; } = new Dictionary<string, IReadOnlyList<DoxygenMember>>();

	/// <summary>String constants, such as wxART_OTHER, which is "wxART_OTHER_C", by name.</summary>
	public IReadOnlyDictionary<string, string> StringConstants { get; init; } = new Dictionary<string, string>();

	/// <summary>
	/// The real headers' #defines, which the shim compiles against, so their values win over the
	/// interface headers' where they can be worked out: wxVSCROLL is ((int)0x80000000) there, which
	/// is negative, but 0x80000000 in the interface headers.
	/// </summary>
	public IReadOnlyDictionary<string, string> HeaderDefines { get; init; } = new Dictionary<string, string>();

	/// <summary>The constants whose value depends on the platform, such as wxTR_DEFAULT_STYLE, which the real headers show.</summary>
	public IReadOnlySet<string> PlatformConstants { get; init; } = new HashSet<string>();
}

internal sealed record DoxygenEnum(string Name, IReadOnlyList<DoxygenEnumValue> Values);

internal sealed record DoxygenEnumValue(string Name, string Brief);

internal sealed record DoxygenClass(
	string Name,
	IReadOnlyList<string> Bases,
	string? Header,
	string Brief,
	IReadOnlyList<DoxygenMember> Members,
	IReadOnlyList<DoxygenStyle> Styles,
	IReadOnlyList<DoxygenEvent> Events);

internal sealed record DoxygenMember(
	string Name,
	string Type,
	IReadOnlyList<DoxygenParameter> Parameters,
	bool IsConst,
	bool IsStatic,
	bool IsVirtual,
	bool IsPublic,
	bool IsDeprecated,
	IReadOnlyList<string> OnlyFor,
	string Brief) {
	public bool IsPureVirtual { get; init; }

	/// <summary>The mixin a member came from, such as wxTextEntry for wxTextCtrl's GetValue, or null for the class's own.</summary>
	public string? Mixin { get; init; }
}

internal sealed record DoxygenParameter(string Type, string Name, string? Default);

internal sealed record DoxygenStyle(string Name, string Description);

// An entry in a class's event table, such as EVT_BUTTON, which sends wxEVT_BUTTON as a wxCommandEvent.
internal sealed record DoxygenEvent(string Macro, string? EventType, string? EventClass, string Description);
