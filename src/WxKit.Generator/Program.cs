using WxKit.Generator.Binding;
using WxKit.Generator.Doxygen;
using WxKit.Generator.Emitting;

// Usage: WxKit.Generator <Doxygen XML directory> <repository root>
if (args.Length != 2) {
	Console.Error.WriteLine("Usage: WxKit.Generator <Doxygen XML directory> <repository root>");
	return 2;
}
string xml = args[0];
string root = args[1];
DoxygenApi read = DoxygenReader.Read(xml);
// The interface headers use a few constants only the real headers define, such as wxDEFAULT.
Dictionary<string, string> constants = new(read.Constants);
foreach ((string name, string value) in HeaderConstants.ReadDeprecatedGuiConstants(Path.Combine(root, "native", "wxWidgets", "include", "wx", "defs.h"))) constants.TryAdd(name, value);
// The real headers are what the shim compiles against, so their enum values win. The interface
// headers leave some out, such as wxTextValidatorStyle's flags, and Doxygen cuts others short at a
// continued line, such as wxEVT_CATEGORY_ALL.
// The styled text control's headers, in wx/stc, define its constants.
string include = Path.Combine(root, "native", "wxWidgets", "include", "wx");
HeaderValues header = HeaderConstants.ReadValues(include, Path.Combine(include, "stc"));
foreach ((string name, string value) in header.Values) constants[name] = value;
// A constant whose value depends on the platform has no one value to give a C# constant, so it is
// left out, and the C++ default applies where it would have been one.
foreach (string name in header.Conditional) constants.Remove(name);
Dictionary<string, string> artIds = HeaderConstants.ReadArtIds(Path.Combine(root, "native", "wxWidgets", "include", "wx", "artprov.h"));
DoxygenApi api = read with { Constants = constants, StringConstants = artIds, PlatformConstants = header.Conditional, HeaderDefines = header.Defines };
Tweaks tweaks = new();
WxKit.Generator.Tweaks.Core.Apply(tweaks);
BindingResult result = new Binder(api, tweaks).Bind();
string shim = Path.Combine(root, "native", "shim");
string managed = Path.Combine(root, "src", "WxKit", "Generated");
Write(Path.Combine(shim, "include", "wxkit_generated.h"), ShimEmitter.Header(result));
// As with the managed files, a shim file for a class no longer bound must not linger.
string generated = Path.Combine(shim, "generated");
if (Directory.Exists(generated)) {
	foreach (string stale in Directory.GetFiles(generated)) File.Delete(stale);
}
foreach ((string fileName, string text) in ShimEmitter.Files(result)) Write(Path.Combine(generated, fileName), text);
Write(Path.Combine(root, "src", "WxKit", "Interop", "WxKitNative.g.cs"), InteropEmitter.Source(result));
// The hand-written functions' imports come from wxkit.h, which declares them, and which every
// function the shim's sources define has to be declared in.
string[] handwritten = Directory.GetFiles(Path.Combine(shim, "src"), "*.cpp", SearchOption.AllDirectories);
Write(Path.Combine(root, "src", "WxKit", "Interop", "WxKitNative.Header.g.cs"), HeaderInteropEmitter.Source(Path.Combine(shim, "include", "wxkit.h"), handwritten));
// Every generated class file is rewritten, so one for a class no longer bound must not linger.
if (Directory.Exists(managed)) {
	foreach (string stale in Directory.GetFiles(managed, "*.g.cs")) File.Delete(stale);
}
foreach ((string fileName, string text) in ManagedEmitter.Files(result)) Write(Path.Combine(managed, fileName), text);
int functions = result.Classes.Sum(static bound => bound.Functions.Count());
Console.WriteLine($"Bound {result.Classes.Count(static bound => !bound.Tweak.IsHandwritten)} classes, {functions} functions, and {result.EventTypes.Count} event types.");
string report = Path.Combine(root, "artifacts", "generator-report.txt");
Write(report, string.Join('\n', result.Skipped) + "\n");
Console.WriteLine($"Skipped {result.Skipped.Count} members; see {report}.");
return 0;

static void Write(string path, string text) {
	Directory.CreateDirectory(Path.GetDirectoryName(path)!);
	File.WriteAllText(path, text.ReplaceLineEndings("\n"));
}
