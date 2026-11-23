using WxKit.Generator.Binding;
using WxKit.Generator.Doxygen;

namespace WxKit.Generator.Tests;

public class BinderTests {
	private static DoxygenMember Function(string name, string type = "void", params DoxygenParameter[] parameters) => new(name, type, parameters, IsConst: false, IsStatic: false, IsVirtual: false, IsPublic: true, IsDeprecated: false, OnlyFor: [], Brief: string.Empty);

	// A protected virtual member, as a virtual list's OnGetItemText is.
	private static DoxygenMember Overridable(string name, string type, params DoxygenParameter[] parameters) => Function(name, type, parameters) with { IsVirtual = true, IsPublic = false, IsConst = true };

	private static DoxygenParameter Parameter(string type, string name, string? defaultValue = null) => new(type, name, defaultValue);

	private static DoxygenClass Class(string name, string? baseName, DoxygenMember[]? members = null, DoxygenStyle[]? styles = null, DoxygenEvent[]? events = null) =>
		new(name, baseName is null ? [] : [baseName], $"wx/{name}.h", string.Empty, members ?? [], styles ?? [], events ?? []);

	// A small hierarchy shaped like wxWidgets's: a window, a button, and the events they send.
	private static DoxygenApi Api() {
		DoxygenClass[] classes = [
			Class("wxObject", null),
			Class("wxEvtHandler", "wxObject"),
			Class("wxWindow", "wxEvtHandler", [
				Function("GetLabel", "wxString"),
				Function("SetLabel", "void", Parameter("const wxString &", "label")),
				Function("IsEnabled", "bool"),
				Function("Enable", "bool", Parameter("bool", "enable", "true")),
				Function("Centre", "void", Parameter("int", "direction", "wxBOTH")),
				Function("GetValidator", "wxValidator *"),
			]),
			Class("wxButton", "wxWindow",
				[
					Function("wxButton"),
					Function("wxButton", "", Parameter("wxWindow *", "parent"), Parameter("wxWindowID", "id"), Parameter("const wxString &", "label", "wxEmptyString"), Parameter("const wxPoint &", "pos", "wxDefaultPosition"), Parameter("long", "style", "0"), Parameter("const wxValidator &", "validator", "wxDefaultValidator")),
					Overridable("OnGetLabel", "wxString", Parameter("long", "index")),
					Overridable("OnGetNamed", "bool", Parameter("const wxString &", "name")),
					Overridable("OnGetText", "bool", Parameter("long", "index"), Parameter("wxString *", "text"), Parameter("long *", "flags")),
					Function("Press"),
				],
				[new DoxygenStyle("wxBU_LEFT", "Left-justifies the label."), new DoxygenStyle("wxBU_TOP", "Aligns the label to the top.")],
				[new DoxygenEvent("EVT_BUTTON", "wxEVT_BUTTON", "wxCommandEvent", "Clicked.")]),
			Class("wxEvent", "wxObject"),
			Class("wxCommandEvent", "wxEvent", [Function("GetString", "wxString")]),
		];
		Dictionary<string, string> constants = new() { ["wxBU_LEFT"] = "0x0040", ["wxBU_TOP"] = "0x0080", ["wxBOTH"] = "0x000C", ["wxACC_STATE_SYSTEM_FOCUSED"] = "0x1000", ["wxACC_STATE_SYSTEM_BUSY"] = "0x0010" };
		return new DoxygenApi(classes.ToDictionary(static found => found.Name), constants, new Dictionary<string, string> { ["wxEVT_BUTTON"] = "wx/button.h" }, new Dictionary<string, DoxygenEnum>()) {
			Functions = new Dictionary<string, IReadOnlyList<DoxygenMember>> {
				["wxBell"] = [Function("wxBell")],
				["wxLaunchDefaultBrowser"] = [Function("wxLaunchDefaultBrowser", "bool", Parameter("const wxString &", "url"), Parameter("int", "flags", "0"))],
			},
		};
	}

	private static BindingResult Bind(Action<Binding.Tweaks>? configure = null) {
		Binding.Tweaks tweaks = new();
		tweaks.Class("wxEvtHandler").Handwritten();
		tweaks.Class("wxWindow", window => window.NoConstructors().Property("IsEnabled", "IsEnabled", "Enable").ParameterEnum("Centre", "direction", "Orientation"));
		tweaks.Class("wxButton", button => button.Event("EVT_BUTTON", "Click"));
		tweaks.Class("wxEvent");
		tweaks.Class("wxCommandEvent");
		configure?.Invoke(tweaks);
		return new Binder(Api(), tweaks).Bind();
	}

	private static BoundClass Bound(BindingResult result, string name) => result.Classes.Single(bound => bound.CppName == name);

	// Free functions bound as a group, which the shim calls without a class, and whose exports drop
	// their wx, for hand-written code to call, since the group has no managed class.
	[Fact]
	public void BindsFreeFunctionsAsAGroup() {
		BindingResult result = Bind(static tweaks => tweaks.Functions("Desktop", "wx/utils.h", "wxBell", "wxLaunchDefaultBrowser"));
		BoundClass desktop = Bound(result, "Desktop");
		Assert.Equal(["wxk_Desktop_Bell", "wxk_Desktop_LaunchDefaultBrowser"], desktop.Functions.Select(static function => function.ShimName).Order(StringComparer.Ordinal));
		Assert.All(desktop.Functions, static function => Assert.True(function.IsStatic));
		string shim = Emitting.ShimEmitter.Files(result).Single(static file => file.FileName == "Desktop.cpp").Text;
		Assert.Contains("::wxBell();", shim, StringComparison.Ordinal);
		Assert.Contains("::wxLaunchDefaultBrowser(wxk::FromUtf16(url, urlLength), static_cast<int>(flags))", shim, StringComparison.Ordinal);
		Assert.DoesNotContain(Emitting.ManagedEmitter.Files(result), static file => file.FileName == "Desktop.g.cs");
		Assert.Throws<InvalidOperationException>(() => Bind(static tweaks => tweaks.Functions("Nothing", "wx/utils.h", "wxNoSuchFunction")));
	}

	// An internal member has a shim function and an import for hand-written code to call, and no
	// managed member. A hand-written class binds only its internal members.
	[Fact]
	public void BindsInternalMembersForHandwrittenCode() {
		BindingResult result = Bind(static tweaks => tweaks.Class("wxButton").Internal("Press"));
		BoundClass button = Bound(result, "wxButton");
		Assert.Equal("wxk_Button_Press", Assert.Single(button.InternalFunctions).ShimName);
		Assert.DoesNotContain(button.Methods, static method => method.Function.Source.Name == "Press");
		Assert.Contains("ButtonPress(", Emitting.InteropEmitter.Source(result), StringComparison.Ordinal);
		Assert.DoesNotContain("Press", Emitting.ManagedEmitter.Files(result).Single(static file => file.FileName == "Button.g.cs").Text, StringComparison.Ordinal);
		BoundClass handwritten = Bound(Bind(static tweaks => tweaks.Class("wxButton").Handwritten().Internal("Press")), "wxButton");
		Assert.Equal(["wxk_Button_Press"], handwritten.Functions.Select(static function => function.ShimName));
		Assert.Throws<InvalidOperationException>(() => Bind(static tweaks => tweaks.Class("wxButton").Internal("NoSuchMember")));
	}

	[Fact]
	public void FindsEachClassesHandleAndNearestBoundBase() {
		BindingResult result = Bind();
		Assert.Same(HandleKind.Window, Bound(result, "wxButton").Handle);
		Assert.Same(Bound(result, "wxWindow"), Bound(result, "wxButton").Base);
		Assert.Same(HandleKind.Event, Bound(result, "wxCommandEvent").Handle);
	}

	[Fact]
	public void PairsGettersAndSettersIntoProperties() {
		BoundClass window = Bound(Bind(), "wxWindow");
		BoundProperty label = window.Properties.Single(static property => property.Name == "Label");
		Assert.Equal("SetLabel", label.Setter?.Source.Name);
		BoundProperty enabled = window.Properties.Single(static property => property.Name == "IsEnabled");
		Assert.Equal("Enable", enabled.Setter?.Source.Name);
	}

	[Fact]
	public void RespellsMethodsAndGivesNumbersTheirEnum() {
		BoundMethod center = Bound(Bind(), "wxWindow").Methods.Single(static method => method.Name == "Center");
		BoundParameter direction = Assert.Single(center.Function.Parameters);
		Assert.Equal("Orientation", Assert.IsType<EnumConversion>(direction.Conversion).EnumName);
		Assert.Equal("Orientation.Both", direction.Default);
	}

	[Fact]
	public void SkipsWhatItCannotConvert() {
		BindingResult result = Bind();
		Assert.DoesNotContain(Bound(result, "wxWindow").Methods, static method => method.Name == "GetValidator");
		Assert.Contains(result.Skipped, static reason => reason.StartsWith("wxWindow::GetValidator", StringComparison.Ordinal));
	}

	[Fact]
	public void BindsOneStepConstructorsWithIdsMovedAfterTheLabel() {
		BoundFunction constructor = Assert.Single(Bound(Bind(), "wxButton").Constructors);
		// The validator, which WxKit cannot convert yet, is left to its C++ default.
		Assert.Equal(["parent", "id", "label", "pos", "style"], constructor.Parameters.Select(static parameter => parameter.CppName));
		Assert.Equal(["parent", "label", "id", "position", "style"], constructor.ManagedParameters.Select(static parameter => parameter.Name));
		// The id has a default now, so it is optional like the parameters around it.
		Assert.Equal([null, "\"\"", "StandardId.Any", "null", "ButtonStyle.None"], constructor.ManagedParameters.Select(static parameter => parameter.Default));
	}

	[Fact]
	public void MakesAStyleEnumFromTheStyleTable() {
		BoundStyles styles = Bound(Bind(), "wxButton").Styles!;
		Assert.Equal("ButtonStyle", styles.Name);
		Assert.Equal([("None", 0L), ("Left", 0x40L), ("Top", 0x80L)], styles.Members.Select(static style => (style.Name, style.Value)));
	}

	[Fact]
	public void BindsEventsWithTheirEventClass() {
		BindingResult result = Bind();
		BoundEvent click = Assert.Single(Bound(result, "wxButton").Events);
		Assert.Equal("Click", click.Name);
		Assert.Equal("Button", click.Type.Name);
		Assert.Same(Bound(result, "wxCommandEvent"), click.Type.EventClass);
	}

	[Fact]
	public void RefusesTweaksThatNameNothing() {
		Assert.Throws<InvalidOperationException>(() => Bind(static tweaks => tweaks.Class("wxNoSuchClass")));
		Assert.Throws<InvalidOperationException>(() => Bind(static tweaks => tweaks.Class("wxButton").Only("NoSuchMember")));
		Assert.Throws<InvalidOperationException>(() => Bind(static tweaks => tweaks.Class("wxButton").Event("EVT_NO_SUCH", "Nothing")));
	}

	[Fact]
	public void BindsOverridableMembersAsCallsToTheOriginal() {
		BoundClass button = Bound(Bind(tweaks => tweaks.Class("wxButton").Director("OnGetLabel")), "wxButton");
		BoundFunction label = Assert.Single(button.Directors);
		Assert.True(label.IsDirectorBase);
		Assert.Equal("wxk_Button_base_OnGetLabel", label.ShimName);
		Assert.IsType<StringConversion>(label.Returns);
		Assert.True(button.HasDirector);
		Assert.DoesNotContain(button.Methods, static method => method.Name == "OnGetLabel");
	}

	// A director made with userdata, whose callbacks hand-written code gives, has no managed
	// override and no export of the class's own member, and its constructor takes the userdata.
	[Fact]
	public void BindsDirectorsForHandwrittenCallbacks() {
		BindingResult result = Bind(static tweaks => tweaks.Class("wxButton").InternalDirector("OnGetLabel"));
		BoundClass button = Bound(result, "wxButton");
		Assert.False(Assert.Single(button.Directors).HasBaseExport);
		Assert.DoesNotContain(button.Functions, static function => function.ShimName == "wxk_Button_base_OnGetLabel");
		Assert.All(button.Constructors, static constructor => Assert.True(constructor.TakesUserdata));
		string shim = Emitting.ShimEmitter.Files(result).Single(static file => file.FileName == "Button.cpp").Text;
		Assert.Contains("new ButtonDirector(userdata, ", shim, StringComparison.Ordinal);
		Assert.Contains("wxk::directors::Button_OnGetLabel(wxk::Wrap(static_cast<wxWindow*>(const_cast<ButtonDirector*>(this))), _userdata, ", shim, StringComparison.Ordinal);
		Assert.Contains(Emitting.ShimEmitter.Files(result), static file => file.FileName == "Directors.cpp");
		Assert.DoesNotContain("OnGetLabel", Emitting.ManagedEmitter.Files(result).Single(static file => file.FileName == "Button.g.cs").Text, StringComparison.Ordinal);
		Assert.Throws<InvalidOperationException>(() => Bind(static tweaks => tweaks.Class("wxButton").InternalDirector("OnGetLabel").DirectorBeforeBase("OnGetLabel")));
	}

	// A hand-written callback gives text and numbers back through pointers, which the director
	// passes as temporaries that write them through wxWidgets' own once the callback returns. A
	// class under a condition has only its callbacks outside it.
	[Fact]
	public void PassesHandwrittenCallbacksWhatTheyGiveBack() {
		BindingResult result = Bind(static tweaks => tweaks.Class("wxButton").InternalDirector("OnGetText").GuardClass("wxUSE_BUTTON"));
		string shim = Emitting.ShimEmitter.Files(result).Single(static file => file.FileName == "Button.cpp").Text;
		Assert.Contains("wxk::CallbackOut<wxString, wxk_String*>{text}, wxk::CallbackOut<long, int64_t>{flags}", shim, StringComparison.Ordinal);
		Assert.Contains("int64_t index, wxk_String** text, int64_t* flags", shim, StringComparison.Ordinal);
		Assert.True(shim.IndexOf("Button_OnGetText)", StringComparison.Ordinal) < shim.IndexOf("#if wxUSE_BUTTON", StringComparison.Ordinal));
		Assert.Throws<InvalidOperationException>(() => Bind(static tweaks => tweaks.Class("wxButton").Director("OnGetText")));
	}

	[Fact]
	public void RefusesToOverrideWhatIsNotVirtual() => Assert.Throws<InvalidOperationException>(() => Bind(tweaks => tweaks.Class("wxButton").Director("Press")));

	[Fact]
	public void RefusesToOverrideWhatADirectorCannotConvert() => Assert.Throws<InvalidOperationException>(() => Bind(tweaks => tweaks.Class("wxButton").Director("OnGetNamed")));

	[Fact]
	public void BindsDefinedConstantsAsOneEnum() {
		BindingResult result = Bind(static tweaks => tweaks.ConstantEnum("AccessibleStates", "wxACC_STATE_SYSTEM_", isFlags: true));
		BoundEnum states = result.Enums.Single(static bound => bound.Name == "AccessibleStates");
		Assert.True(states.IsFlags);
		Assert.Equal(["None", "Busy", "Focused"], states.Members.Select(static member => member.Name));
		Assert.Equal(0x1000, states.Members[^1].Value);
	}
}
