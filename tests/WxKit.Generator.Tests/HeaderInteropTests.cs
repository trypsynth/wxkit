using WxKit.Generator.Emitting;

namespace WxKit.Generator.Tests;

// The imports written from wxkit.h, and the check that the header and the shim's sources declare
// and define the same functions.
public sealed class HeaderInteropTests : IDisposable {
	private readonly string _folder = Directory.CreateTempSubdirectory("wxkit-header-").FullName;

	private const string Header = """
		// Callbacks

		typedef void (*wxk_Callback)(void* userdata);

		// Things

		// Makes a thing.
		WXK_FUNCTION(wxk_Thing*) wxk_Thing_new(const char16_t* name, size_t nameLength) WXK_NOEXCEPT;
		WXK_FUNCTION(uint8_t) wxk_Thing_Watch(wxk_Thing* thing, wxk_Callback onChange, void* userdata) WXK_NOEXCEPT;
		WXK_FUNCTION(wxk_ItemId) wxk_Thing_GetItem(const wxk_Thing* thing, wxk_Point at, int32_t (*pick)(wxk_Event* event, double weight)) WXK_NOEXCEPT;
		""";

	public void Dispose() => Directory.Delete(_folder, recursive: true);

	[Fact]
	public void WritesAnImportForEachDeclaration() {
		string source = HeaderInteropEmitter.Source(Write("wxkit.h", Header), [Write("thing.cpp", Definitions("wxk_Thing_new", "wxk_Thing_Watch", "wxk_Thing_GetItem"))]);
		Assert.Contains("// Things\n\n\t[LibraryImport(Library, EntryPoint = \"wxk_Thing_new\")]", source.Replace("\r\n", "\n", StringComparison.Ordinal), StringComparison.Ordinal);
		Assert.Contains("public static partial WxkThing* ThingNew(char* name, nuint nameLength);", source, StringComparison.Ordinal);
		Assert.Contains("public static partial byte ThingWatch(WxkThing* thing, delegate* unmanaged[Cdecl]<void*, void> onChange, void* userdata);", source, StringComparison.Ordinal);
		Assert.Contains("public static partial nint ThingGetItem(WxkThing* thing, Point at, delegate* unmanaged[Cdecl]<WxkEvent*, double, int> pick);", source, StringComparison.Ordinal);
	}

	[Fact]
	public void RefusesAFunctionTheHeaderDoesNotDeclare() {
		InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => HeaderInteropEmitter.Source(Write("wxkit.h", Header), [Write("thing.cpp", Definitions("wxk_Thing_new", "wxk_Thing_Watch", "wxk_Thing_GetItem", "wxk_Thing_Forgotten"))]));
		Assert.Contains("wxk_Thing_Forgotten", error.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void RefusesADeclarationTheShimDoesNotDefine() {
		InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => HeaderInteropEmitter.Source(Write("wxkit.h", Header), [Write("thing.cpp", Definitions("wxk_Thing_new", "wxk_Thing_Watch"))]));
		Assert.Contains("wxk_Thing_GetItem", error.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void RefusesATypeItCannotMap() {
		string header = "WXK_FUNCTION(bool) wxk_Thing_IsOk(void) WXK_NOEXCEPT;\n";
		InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => HeaderInteropEmitter.Source(Write("wxkit.h", header), [Write("thing.cpp", Definitions("wxk_Thing_IsOk"))]));
		Assert.Contains("bool", error.Message, StringComparison.Ordinal);
	}

	private static string Definitions(params string[] names) => string.Concat(names.Select(static name => $"WXK_FUNCTION(void) {name}() noexcept {{\n}}\n\n"));

	private string Write(string name, string text) {
		string path = Path.Combine(_folder, name);
		File.WriteAllText(path, text);
		return path;
	}
}
