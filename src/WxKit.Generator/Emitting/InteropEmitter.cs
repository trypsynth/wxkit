using WxKit.Generator.Binding;

namespace WxKit.Generator.Emitting;

// Writes the LibraryImport declarations for the generated shim functions, as part of WxKitNative.
internal static class InteropEmitter {
	public static string Source(BindingResult result) {
		CodeWriter writer = new();
		writer.Line(ManagedEmitter.Banner)
			.Line("using System.Runtime.CompilerServices;")
			.Line("using System.Runtime.InteropServices;")
			.Line()
			.Line("namespace WxKit.Interop;")
			.Line();
		// A class that isn't a wxObject has a handle of its own type.
		foreach (BoundClass bound in result.Classes.Where(static bound => !bound.Tweak.IsHandwritten && bound.Handle.IsPlain)) {
			writer.Line($"/// <summary>A {bound.CppName}, which {bound.Handle.CType}'s functions take.</summary>");
			writer.Line($"public struct {bound.Handle.InteropType};").Line();
		}
		writer.Open("public static unsafe partial class WxKitNative");
		bool first = true;
		foreach (BoundClass bound in result.Classes) {
			foreach (BoundFunction function in bound.Functions) {
				if (!first) writer.Line();
				first = false;
				List<string> parameters = [];
				if (!function.IsConstructor && !function.IsStatic) parameters.Add($"{bound.Handle.InteropType}* self");
				parameters.AddRange(function.Parameters.SelectMany(static parameter => parameter.Conversion.InteropParameters(parameter.Name)));
				string returns = function.IsConstructor ? $"{bound.Handle.InteropType}*" : function.Returns.InteropReturnType;
				Import(writer, function.ShimName, $"public static partial {returns} {function.InteropName}({string.Join(", ", parameters)});");
			}
			if (ShimEmitter.HasDelete(bound)) {
				writer.Line();
				Import(writer, $"wxk_{Naming.Type(bound.CppName)}_delete", $"public static partial void {Naming.Type(bound.CppName)}Delete({bound.Handle.InteropType}* self);");
			}
			if (bound.Directors.Count > 0) {
				writer.Line();
				IEnumerable<string> callbacks = bound.Directors.Select(director => {
					IEnumerable<string> types = director.Parameters.Select(static parameter => parameter.Conversion.InteropReturnType).Prepend($"{bound.Handle.InteropType}*").Append(director.Returns.CallbackInteropType);
					return $"delegate* unmanaged[Cdecl]<{string.Join(", ", types)}> {char.ToLowerInvariant(director.Source.Name[0])}{director.Source.Name[1..]}";
				});
				Import(writer, $"wxk_{Naming.Type(bound.CppName)}_SetDirectors", $"public static partial void {Naming.Type(bound.CppName)}SetDirectors({string.Join(", ", callbacks)});");
			}
		}
		foreach (BoundEventType type in result.EventTypes) {
			writer.Line();
			Import(writer, $"wxk_EventType_{type.Name}", $"public static partial int EventType{type.Name}();");
		}
		writer.Close();
		return writer.ToString();
	}

	private static void Import(CodeWriter writer, string entryPoint, string declaration) {
		writer.Line($"[LibraryImport(Library, EntryPoint = \"{entryPoint}\")]");
		writer.Line("[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]");
		writer.Line(declaration);
	}
}
