using WxKit.Generator.Binding;

namespace WxKit.Generator.Tests;

public class NamingTests {
	[Theory]
	[InlineData("wxButton", "Button")]
	[InlineData("wxColourDialog", "ColorDialog")]
	[InlineData("wxCentreDialog", "CenterDialog")]
	public void DropsThePrefixAndRespellsTypes(string cpp, string managed) => Assert.Equal(managed, Naming.Type(cpp));

	[Theory]
	[InlineData("Centre", "Center")]
	[InlineData("SetBackgroundColour", "SetBackgroundColor")]
	[InlineData("GetLabel", "GetLabel")]
	public void RespellsMembersByWholeWord(string cpp, string managed) => Assert.Equal(managed, Naming.Member(cpp));

	// .NET writes an acronym of three letters or more in Pascal case, and keeps two in upper case.
	[Theory]
	[InlineData("GetURL", "GetUrl")]
	[InlineData("FromDIP", "FromDip")]
	[InlineData("CreateWithDIPSize", "CreateWithDipSize")]
	[InlineData("RGBAImageSetWidth", "RgbaImageSetWidth")]
	[InlineData("OSXSetModified", "OsxSetModified")]
	[InlineData("PrintToPDF", "PrintToPdf")]
	[InlineData("VCHome", "VCHome")]
	[InlineData("XYToPosition", "XYToPosition")]
	[InlineData("GetSizeMM", "GetSizeMM")]
	[InlineData("GetUIState", "GetUIState")]
	[InlineData("GetDC", "GetDC")]
	[InlineData("IsOk", "IsOk")]
	public void WritesAcronymsAsDotNetDoes(string cpp, string managed) => Assert.Equal(managed, Naming.Member(cpp));

	[Theory]
	[InlineData("wxDPIChangedEvent", "DpiChangedEvent")]
	[InlineData("wxHTMLDataObject", "HtmlDataObject")]
	public void WritesAcronymsInTypesAsDotNetDoes(string cpp, string managed) => Assert.Equal(managed, Naming.Type(cpp));

	[Theory]
	[InlineData("URL", "url")]
	[InlineData("baseURL", "baseUrl")]
	public void WritesAcronymsInParametersAsDotNetDoes(string cpp, string managed) => Assert.Equal(managed, Naming.Parameter(cpp));

	[Theory]
	[InlineData("pos", "position")]
	[InlineData("orient", "orientation")]
	[InlineData("label", "label")]
	[InlineData("event", "@event")]
	public void RenamesParameters(string cpp, string managed) => Assert.Equal(managed, Naming.Parameter(cpp));

	[Theory]
	[InlineData("wxEVT_BUTTON", "Button")]
	[InlineData("wxEVT_CLOSE_WINDOW", "CloseWindow")]
	[InlineData("wxEVT_LEFT_DCLICK", "LeftDoubleClick")]
	[InlineData("wxEVT_STC_SAVEPOINTREACHED", "StyledTextSavePointReached")]
	[InlineData("wxEVT_WEBVIEW_NEWWINDOW", "WebViewNewWindow")]
	[InlineData("wxEVT_KILL_FOCUS", "LostFocus")]
	public void NamesEventTypes(string constant, string name) => Assert.Equal(name, Naming.EventType(constant));

	[Theory]
	[InlineData("CLOSE_WINDOW", "CloseWindow")]
	[InlineData("HRULES", "HorizontalRules")]
	[InlineData("MACCENTRALEUR", "MacCentralEuropean")]
	[InlineData("10X11", "Standard10x11")]
	[InlineData("Layout_RightToLeft", "LayoutRightToLeft")]
	[InlineData("SHOW_EFFECT_NONE", "ShowEffectNone")]
	public void JoinsWordsInPascalCase(string upper, string pascal) => Assert.Equal(pascal, Naming.Words(upper));

	[Fact]
	public void FindsThePrefixMostStylesShare() {
		Assert.Equal("TE", Naming.SharedStylePrefix(["wxTE_MULTILINE", "wxTE_READONLY", "wxHSCROLL"]));
		Assert.Null(Naming.SharedStylePrefix(["wxOK", "wxCANCEL", "wxICON_ERROR", "wxICON_WARNING", "wxYES_NO", "wxCENTRE"]));
		Assert.Equal("SP", Naming.SharedStylePrefix(["wxSP_ARROW_KEYS", "wxSP_WRAP", "wxTE_PROCESS_ENTER", "wxALIGN_LEFT"]));
		Assert.Null(Naming.SharedStylePrefix(["wxYES_NO", "wxYES_DEFAULT", "wxOK"], static name => name == "wxYES"));
	}

	[Theory]
	[InlineData("wxTE_MULTILINE", "TE", "wxTextCtrl", "MultiLine")]
	[InlineData("wxDEFAULT_FRAME_STYLE", null, "wxFrame", "Default")]
	[InlineData("wxTR_DEFAULT_STYLE", "TR", "wxTreeCtrl", "Default")]
	[InlineData("wxFRAME_TOOL_WINDOW", null, "wxFrame", "ToolWindow")]
	[InlineData("wxICON_ERROR", null, "wxMessageDialog", "IconError")]
	[InlineData("wxCENTRE", null, "wxMessageDialog", "Center")]
	public void NamesStyles(string constant, string? prefix, string className, string name) => Assert.Equal(name, Naming.Style(constant, prefix, className));

	// A leading digit is spelled out, since an identifier cannot start with one, but not one later,
	// and a number after a number keeps the underscore between them.
	[Theory]
	[InlineData("3STATE", "ThreeState")]
	[InlineData("ZOOM_100", "Zoom100")]
	[InlineData("ISO8859_10", "Iso8859_10")]
	[InlineData("SAVEAS", "SaveAs")]
	public void SpellsWordsAndNumbers(string upper, string managed) => Assert.Equal(managed, Naming.Words(upper));
}
