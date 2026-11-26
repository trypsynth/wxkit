namespace WxKit.Generator.Binding;

// A pointer to a class WxKit wraps, which crosses as the class's handle. One coming back is wrapped
// in its managed object, which is made if WxKit has not seen the native one before.
internal sealed class ObjectConversion(BoundClass target) : Conversion {
	public BoundClass Target { get; } = target;

	public override string Suffix => Target.Name;

	public override string CReturnType => $"{Target.Handle.CType}*";

	public override string Fallback => "nullptr";

	public override string InteropReturnType => $"{Target.Handle.InteropType}*";

	public override IEnumerable<string> CParameters(string name) => [$"{Target.Handle.CType}* {name}"];

	public override string ToCpp(string name) => $"static_cast<{Target.CppName}*>(wxk::Unwrap({name}))";

	public override string FromCpp(string expression) => $"wxk::Wrap(static_cast<{Target.Handle.CppBase}*>({expression}))";

	public override IEnumerable<string> InteropParameters(string name) => [$"{Target.Handle.InteropType}* {name}"];

	public override string ManagedType(bool nullable) => nullable ? Target.Name + "?" : Target.Name;

	public override string? Check(string name, bool nullable) => nullable ? null : $"ArgumentNullException.ThrowIfNull({name});";

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => [nullable ? $"{name} is null ? null : ({Target.Handle.InteropType}*){name}.{Target.Handle.Accessor}" : $"({Target.Handle.InteropType}*){name}.{Target.Handle.Accessor}"];

	// A column or a tool comes back as the one managed object its control keeps for it.
	public override string ManagedReturn(string expression) => Target.Handle.IsKeptByOwner ? $"{Target.Name}.Wrap({expression})" : $"ObjectRegistry.Wrap<{Target.Name}>({expression})";

	public override bool AcceptsNull => true;
}

// A wxObject that may or may not be an event handler, such as an event's source. It crosses as
// itself, and comes back wrapped only when it is a class WxKit wraps.
internal sealed class AnyObjectConversion : Conversion {
	public static AnyObjectConversion Instance { get; } = new();

	public override string Suffix => "Object";

	public override string CReturnType => "wxk_Object*";

	public override string Fallback => "nullptr";

	public override string InteropReturnType => "WxkObject*";

	public override IEnumerable<string> CParameters(string name) => throw new InvalidOperationException("wxObject is only bound as a return type.");

	public override string ToCpp(string name) => throw new InvalidOperationException("wxObject is only bound as a return type.");

	public override string FromCpp(string expression) => $"wxk::Wrap({expression})";

	public override IEnumerable<string> InteropParameters(string name) => throw new InvalidOperationException("wxObject is only bound as a return type.");

	public override string ManagedType(bool nullable) => "EvtHandler?";

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => throw new InvalidOperationException("wxObject is only bound as a return type.");

	public override string ManagedReturn(string expression) => $"ObjectRegistry.Wrap<EvtHandler>({expression})";
}

// A value object, such as a font, crosses as a handle to a heap copy the managed side owns. One
// going in can be null, for the class's empty object; one coming back empty is null. A bundle of
// bitmaps or icons is made from one, as C++ would convert it.
internal sealed class ValueConversion(BoundClass target, string? wrapper = null, bool mutable = false) : Conversion {
	public BoundClass Target { get; } = target;

	public override string Suffix => Target.Name;

	public override string CReturnType => "wxk_Object*";

	public override string Fallback => "nullptr";

	public override string InteropReturnType => "WxkObject*";

	public override IEnumerable<string> CParameters(string name) => [mutable ? $"wxk_Object* {name}" : $"const wxk_Object* {name}"];

	public override string ToCpp(string name) => mutable ? $"wxk::ValueRef<{Target.CppName}>({name})" : wrapper is null ? $"wxk::ValueOf<{Target.CppName}>({name})" : $"{wrapper}(wxk::ValueOf<{Target.CppName}>({name}))";

	public override string FromCpp(string expression) => $"wxk::CopyValue<{Target.CppName}>({expression})";

	public override IEnumerable<string> InteropParameters(string name) => [$"WxkObject* {name}"];

	public override string ManagedType(bool nullable) => nullable ? Target.Name + "?" : Target.Name;

	public override string ManagedReturnType(bool nullable) => Target.Name + "?";

	public override string? Check(string name, bool nullable) => nullable && !mutable ? null : $"ArgumentNullException.ThrowIfNull({name});";

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => [nullable && !mutable ? $"{name} is null ? null : {name}.Native" : $"{name}.Native"];

	public override string ManagedReturn(string expression) => $"global::WxKit.{Target.Name}.Take({expression})";

	public override bool AcceptsNull => true;
}

// An object the managed side owns, such as a drawing context, passed by reference or pointer.
internal sealed class OwnedConversion(BoundClass target, bool byReference) : Conversion {
	public override string Suffix => target.Name;

	public override string CReturnType => throw new InvalidOperationException("Owned objects are only bound as parameters.");

	public override string Fallback => "nullptr";

	public override string InteropReturnType => throw new InvalidOperationException("Owned objects are only bound as parameters.");

	public override IEnumerable<string> CParameters(string name) => [$"wxk_Object* {name}"];

	public override string ToCpp(string name) => byReference ? $"*static_cast<{target.CppName}*>(wxk::Unwrap({name}))" : $"static_cast<{target.CppName}*>(wxk::Unwrap({name}))";

	public override string FromCpp(string expression) => throw new InvalidOperationException("Owned objects are only bound as parameters.");

	public override IEnumerable<string> InteropParameters(string name) => [$"WxkObject* {name}"];

	public override string ManagedType(bool nullable) => nullable ? target.Name + "?" : target.Name;

	public override string? Check(string name, bool nullable) => nullable ? null : $"ArgumentNullException.ThrowIfNull({name});";

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => [nullable ? $"{name} is null ? null : {name}.Native" : $"{name}.Native"];

	public override string ManagedReturn(string expression) => throw new InvalidOperationException("Owned objects are only bound as parameters.");

	public override bool AcceptsNull => !byReference;
}

// A function that changes its object and returns it, such as wxFont::MakeBold, returns the same
// managed object, so calls chain.
internal sealed class SelfConversion(BoundClass owner) : Conversion {
	public override string Suffix => "Self";

	public override string CReturnType => "void";

	public override string Fallback => string.Empty;

	public override string InteropReturnType => "void";

	public override IEnumerable<string> CParameters(string name) => throw new InvalidOperationException("Self is only a return type.");

	public override string ToCpp(string name) => throw new InvalidOperationException("Self is only a return type.");

	public override string FromCpp(string expression) => expression;

	public override IEnumerable<string> InteropParameters(string name) => throw new InvalidOperationException("Self is only a return type.");

	public override string ManagedType(bool nullable) => owner.Name;

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => throw new InvalidOperationException("Self is only a return type.");

	public override string ManagedReturn(string expression) => expression;
}

// A list of windows, such as a window's children, which comes back as an array of their managed objects.
internal sealed class WindowListConversion : Conversion {
	public static WindowListConversion Instance { get; } = new();

	public override string Suffix => "Windows";

	public override string CReturnType => "wxk_WindowList*";

	public override string Fallback => "nullptr";

	public override string InteropReturnType => "WxkWindowList*";

	public override IEnumerable<string> CParameters(string name) => throw new InvalidOperationException("Window lists are only return types.");

	public override string ToCpp(string name) => throw new InvalidOperationException("Window lists are only return types.");

	public override string FromCpp(string expression) => $"wxk::ToWindowList({expression})";

	public override IEnumerable<string> InteropParameters(string name) => throw new InvalidOperationException("Window lists are only return types.");

	public override string ManagedType(bool nullable) => "Window[]";

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => throw new InvalidOperationException("Window lists are only return types.");

	public override string ManagedReturn(string expression) => $"global::WxKit.NativeWindowList.Take({expression})";
}

// A wxVector<wxBitmapBundle>, such as a tree's images, is a sequence of bitmaps, which cross as an
// array of their handles that the shim makes bundles of. The bitmaps stay alive for the call.
internal sealed class BitmapListConversion : Conversion {
	public static BitmapListConversion Instance { get; } = new();

	public override string Suffix => "Bitmaps";

	public override string CReturnType => throw new InvalidOperationException("Lists of bitmaps are only parameters.");

	public override string Fallback => "nullptr";

	public override string InteropReturnType => throw new InvalidOperationException("Lists of bitmaps are only parameters.");

	public override IEnumerable<string> CParameters(string name) => [$"wxk_Object* const* {name}", $"size_t {name}Count"];

	public override string ToCpp(string name) => $"wxk::ToBitmapBundles({name}, {name}Count)";

	public override string FromCpp(string expression) => throw new InvalidOperationException("Lists of bitmaps are only parameters.");

	public override IEnumerable<string> InteropParameters(string name) => [$"WxkObject** {name}", $"nuint {name}Count"];

	public override string ManagedType(bool nullable) => "IEnumerable<Bitmap>";

	public override string? Check(string name, bool nullable) => $"ArgumentNullException.ThrowIfNull({name});";

	public override string? Prepare(string name) => $"using NativeBitmapList {Native(name)} = new({name});";

	public override string? Fixed(string name) => $"fixed (nint* {Pointer(name)} = {Native(name)}.Handles)";

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => [$"(WxkObject**){Pointer(name)}", $"(nuint){Native(name)}.Handles.Length"];

	public override string ManagedReturn(string expression) => throw new InvalidOperationException("Lists of bitmaps are only parameters.");

	private static string Native(string name) => name.TrimStart('@') + "Native";

	private static string Pointer(string name) => name.TrimStart('@') + "Pointer";
}
