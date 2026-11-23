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

	// A function that calls into C++ runs the call in Guard, or says why it can't throw, so no
	// exception ends the process. One that only stores or returns needs neither.
	[Fact]
	public void RefusesACallOutsideGuard() {
		string header = """
			WXK_FUNCTION(void) wxk_Thing_Guarded(wxk_Thing* thing) WXK_NOEXCEPT;
			WXK_FUNCTION(int32_t) wxk_Thing_Marked(wxk_Thing* thing) WXK_NOEXCEPT;
			WXK_FUNCTION(void) wxk_Thing_SetCallbacks(const wxk_Thing* set) WXK_NOEXCEPT;
			WXK_FUNCTION(void) wxk_Thing_Unguarded(wxk_Thing* thing) WXK_NOEXCEPT;
			""";
		string source = """
			WXK_FUNCTION(void) wxk_Thing_Guarded(wxk_Thing* thing) noexcept {
				wxk::Guard([&] { Unwrap(thing)->Refresh("{ a brace in a string"); });
			}

			// Can't throw: it reads a member.
			WXK_FUNCTION(int32_t) wxk_Thing_Marked(wxk_Thing* thing) noexcept {
				return Unwrap(thing)->GetId();
			}

			WXK_FUNCTION(void) wxk_Thing_SetCallbacks(const wxk_Thing* set) noexcept {
				// Stores them for later (as a copy).
				callbacks = *set;
			}

			WXK_FUNCTION(void) wxk_Thing_Unguarded(wxk_Thing* thing) noexcept {
				Unwrap(thing)->Destroy();
			}
			""";
		InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => HeaderInteropEmitter.Source(Write("wxkit.h", header), [Write("thing.cpp", source)]));
		Assert.Contains("wxk_Thing_Unguarded", error.Message, StringComparison.Ordinal);
		Assert.DoesNotContain("wxk_Thing_Guarded", error.Message, StringComparison.Ordinal);
		Assert.DoesNotContain("wxk_Thing_Marked", error.Message, StringComparison.Ordinal);
		Assert.DoesNotContain("wxk_Thing_SetCallbacks", error.Message, StringComparison.Ordinal);
	}

	private static string Definitions(params string[] names) => string.Concat(names.Select(static name => $"WXK_FUNCTION(void) {name}() noexcept {{\n}}\n\n"));

	private string Write(string name, string text) {
		string path = Path.Combine(_folder, name);
		File.WriteAllText(path, text);
		return path;
	}
}
