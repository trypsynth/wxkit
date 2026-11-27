using System.Xml.Linq;
using WxKit.Generator.Binding;
using WxKit.Generator.Doxygen;

namespace WxKit.Generator.Tests;

public class ReaderTests {
	// Trimmed from Doxygen's output for wxButton, with the shapes the reader has to handle: a type
	// with a reference in it, defaults, a port-only member, a deprecated one, and the style and
	// event tables as headed lists, with the version an entry was added in.
	private const string ButtonXml = """
		<compounddef kind="class">
			<compoundname>wxButton</compoundname>
			<basecompoundref prot="public">wxAnyButton</basecompoundref>
			<includes local="no">wx/button.h</includes>
			<briefdescription><para>A button is a control.</para></briefdescription>
			<sectiondef kind="public-func">
				<memberdef kind="function" prot="public" static="no" const="no" virt="non-virtual">
					<type /><name>wxButton</name>
					<param><type><ref>wxWindow</ref> *</type><declname>parent</declname></param>
					<param><type>const <ref>wxString</ref> &amp;</type><declname>label</declname><defval><ref>wxEmptyString</ref></defval></param>
					<briefdescription><para>Constructor.</para></briefdescription>
				</memberdef>
				<memberdef kind="function" prot="public" static="no" const="yes" virt="virtual">
					<type><ref>wxString</ref></type><name>GetLabel</name>
					<briefdescription><para>Returns the label.</para></briefdescription>
				</memberdef>
				<memberdef kind="function" prot="public" static="no" const="no" virt="non-virtual">
					<type>void</type><name>SetElevationRequired</name>
					<detaileddescription><para><onlyfor>wxmsw</onlyfor></para></detaileddescription>
				</memberdef>
				<memberdef kind="function" prot="public" static="no" const="no" virt="non-virtual">
					<type>void</type><name>OldThing</name>
					<detaileddescription><para><xrefsect id="deprecated_1_a"><xreftitle>Deprecated</xreftitle></xrefsect></para></detaileddescription>
				</memberdef>
			</sectiondef>
			<detaileddescription>
				<para><heading level="2">Styles</heading></para>
				<para><itemizedlist>
					<listitem><para>wxBU_LEFT: Left-justifies the label.</para></listitem>
					<listitem><para>wxBU_EXACTFIT: Makes the button as small as it can be.</para></listitem>
				</itemizedlist></para>
				<para><heading level="2">Events emitted by this class</heading></para>
				<para>Handlers take a <ref>wxCommandEvent</ref>.</para>
				<para><itemizedlist>
					<listitem><para>EVT_BUTTON(id, func): Process a <computeroutput>wxEVT_BUTTON</computeroutput> event, when the button is clicked. <simplesect kind="since"><para>3.1.1</para></simplesect></para></listitem>
					<listitem><para>EVT_OTHER(id, func): Process a <ref>wxOtherEvent</ref>.</para></listitem>
				</itemizedlist><heading level="2" /></para>
			</detaileddescription>
		</compounddef>
		""";

	private static DoxygenClass Button => DoxygenReader.ReadClass(XElement.Parse(ButtonXml));

	[Fact]
	public void ReadsTheClass() {
		DoxygenClass button = Button;
		Assert.Equal("wxButton", button.Name);
		Assert.Equal(["wxAnyButton"], button.Bases);
		Assert.Equal("wx/button.h", button.Header);
		Assert.Equal("A button is a control.", button.Brief);
	}

	[Fact]
	public void ReadsTypesAndDefaults() {
		DoxygenMember constructor = Button.Members.Single(static member => member.Name == "wxButton");
		Assert.Equal("wxWindow *", constructor.Parameters[0].Type);
		Assert.Null(constructor.Parameters[0].Default);
		Assert.Equal("const wxString &", constructor.Parameters[1].Type);
		Assert.Equal("wxEmptyString", constructor.Parameters[1].Default);
		DoxygenMember getter = Button.Members.Single(static member => member.Name == "GetLabel");
		Assert.True(getter.IsConst);
		Assert.True(getter.IsVirtual);
	}

	[Fact]
	public void ReadsPortsAndDeprecation() {
		Assert.Equal(["wxmsw"], Button.Members.Single(static member => member.Name == "SetElevationRequired").OnlyFor);
		Assert.True(Button.Members.Single(static member => member.Name == "OldThing").IsDeprecated);
	}

	[Fact]
	public void ReadsTheStyleTable() => Assert.Equal(["wxBU_LEFT", "wxBU_EXACTFIT"], Button.Styles.Select(static style => style.Name));

	[Fact]
	public void ReadsTheEventTableWithEachEntrysClass() {
		Assert.Collection(Button.Events,
			static first => {
				Assert.Equal("EVT_BUTTON", first.Macro);
				Assert.Equal("wxEVT_BUTTON", first.EventType);
				Assert.Equal("wxCommandEvent", first.EventClass);
				Assert.Equal("Process a wxEVT_BUTTON event, when the button is clicked.", first.Description);
			},
			static second => {
				Assert.Equal("EVT_OTHER", second.Macro);
				Assert.Null(second.EventType);
				Assert.Equal("wxOtherEvent", second.EventClass);
			});
	}

	[Theory]
	[InlineData("const wxString &", "wxString", true, 0, true)]
	[InlineData("wxWindow *", "wxWindow", false, 1, false)]
	[InlineData("unsigned int", "unsigned int", false, 0, false)]
	public void ParsesCppTypes(string text, string name, bool isConst, int pointers, bool isReference) => Assert.Equal(new CppType(name, isConst, pointers, isReference), CppType.Parse(text));
}
