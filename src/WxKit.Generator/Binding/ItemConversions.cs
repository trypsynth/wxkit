namespace WxKit.Generator.Binding;

// An item id that wraps one pointer, as wxTreeItemId and wxDataViewItem do, crosses as that
// pointer and becomes a value type that holds it, such as TreeItemId. One whose pointer is typed,
// as wxTreeListItem's is, is made from it after a cast. A tree's item that isn't one, such as the
// selection when nothing is selected, is null in the managed API. A data view's is an item that
// isn't valid, since a model makes its own and gives the root as one.
internal sealed class ItemIdConversion(string managedName, string cppName, string? pointerType = null, bool nullWhenNone = false) : Conversion {
	public static ItemIdConversion TreeItem { get; } = new("TreeItemId", "wxTreeItemId", nullWhenNone: true);

	public static ItemIdConversion DataViewItem { get; } = new("DataViewItem", "wxDataViewItem");

	public static ItemIdConversion TreeListItem { get; } = new("TreeListItem", "wxTreeListItem", "wxTreeListModelNode*", nullWhenNone: true);

	/// <summary>Whether a return that is no item is null.</summary>
	public bool NullWhenNone => nullWhenNone;

	/// <summary>The managed expression for a return that is null when it is no item.</summary>
	public string ManagedReturnOrNull(string expression) => $"global::WxKit.{managedName}.OrNull({expression})";

	public override string Suffix => managedName;

	public override string? CallbackCType => "void*";

	public override string CallbackInteropType => "nint";

	public override string FromCallback(string expression) => Make(expression);

	public override string ToCallback(string expression) => $"{expression}.Value";

	public override string CReturnType => "void*";

	public override string Fallback => "nullptr";

	public override string InteropReturnType => "nint";

	public override IEnumerable<string> CParameters(string name) => [$"void* {name}"];

	public override string ToCpp(string name) => Make(name);

	public override string FromCpp(string expression) => $"static_cast<void*>(({expression}).GetID())";

	public override IEnumerable<string> InteropParameters(string name) => [$"nint {name}"];

	public override string ManagedType(bool nullable) => nullable ? managedName + "?" : managedName;

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => [nullable ? $"({name}?.Value ?? 0)" : $"{name}.Value"];

	public override string ManagedReturn(string expression) => $"new global::WxKit.{managedName}({expression})";

	private string Make(string pointer) => pointerType is null ? $"{cppName}({pointer})" : $"{cppName}(static_cast<{pointerType}>({pointer}))";
}

// A wxVariant crosses as a handle to one, and is an object in the managed API: null, a string, a
// bool, a long, a double, or a bitmap, icon, or icon text. One going in is built for the call, and
// one coming back is read and freed.
internal sealed class VariantConversion : Conversion {
	public static VariantConversion Instance { get; } = new();

	public override string Suffix => "Variant";

	public override string CReturnType => "wxk_Variant*";

	public override string Fallback => "nullptr";

	public override string InteropReturnType => "WxkVariant*";

	public override IEnumerable<string> CParameters(string name) => [$"const wxk_Variant* {name}"];

	public override string ToCpp(string name) => $"*wxk::Unwrap(const_cast<wxk_Variant*>({name}))";

	public override string FromCpp(string expression) => $"wxk::Wrap(new wxVariant({expression}))";

	public override IEnumerable<string> InteropParameters(string name) => [$"WxkVariant* {name}"];

	public override string ManagedType(bool nullable) => "object?";

	public override string? Prepare(string name) => $"using NativeVariant {Native(name)} = new({name});";

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => [$"{Native(name)}.Handle"];

	public override string ManagedReturn(string expression) => $"NativeVariant.Take({expression})";

	public override bool AcceptsNull => true;

	private static string Native(string name) => name.TrimStart('@') + "Native";
}

// A wxVector<wxVariant>, such as a data view list row's values, is built from the managed values
// for the call.
// A wxClientData* or wxTreeItemData*, the data a control keeps with an item, is any object. It
// crosses as a GCHandle, which the shim wraps in data that frees it when the control deletes it.
// Only members whose control takes the data, and deletes what it replaces, may use it.
internal sealed class ItemDataConversion(string helper) : Conversion {
	public static ItemDataConversion Client { get; } = new("ToClientData");

	public static ItemDataConversion TreeItem { get; } = new("ToTreeItemData");

	public override string Suffix => "Data";

	public override string CReturnType => "void*";

	public override string Fallback => "nullptr";

	public override string InteropReturnType => "nint";

	public override IEnumerable<string> CParameters(string name) => [$"void* {name}"];

	public override string ToCpp(string name) => $"wxk::{helper}({name})";

	public override string FromCpp(string expression) => $"wxk::FromClientData({expression})";

	public override IEnumerable<string> InteropParameters(string name) => [$"nint {name}"];

	public override string ManagedType(bool nullable) => "object?";

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => [$"global::WxKit.ItemData.Alloc({name})"];

	public override string ManagedReturn(string expression) => $"global::WxKit.ItemData.Get({expression})";

	public override bool AcceptsNull => true;
}

internal sealed class VariantListConversion : Conversion {
	public static VariantListConversion Instance { get; } = new();

	public override string Suffix => "Variants";

	public override string CReturnType => throw new InvalidOperationException("Lists of variants are only parameters.");

	public override string Fallback => "nullptr";

	public override string InteropReturnType => throw new InvalidOperationException("Lists of variants are only parameters.");

	public override IEnumerable<string> CParameters(string name) => [$"const wxk_VariantList* {name}"];

	public override string ToCpp(string name) => $"*wxk::Unwrap(const_cast<wxk_VariantList*>({name}))";

	public override string FromCpp(string expression) => throw new InvalidOperationException("Lists of variants are only parameters.");

	public override IEnumerable<string> InteropParameters(string name) => [$"WxkVariantList* {name}"];

	public override string ManagedType(bool nullable) => "IEnumerable<object?>";

	public override string? Check(string name, bool nullable) => $"ArgumentNullException.ThrowIfNull({name});";

	public override string? Prepare(string name) => $"using NativeVariantList {Native(name)} = new({name});";

	public override IEnumerable<string> InteropArguments(string name, bool nullable) => [$"{Native(name)}.Handle"];

	public override string ManagedReturn(string expression) => throw new InvalidOperationException("Lists of variants are only parameters.");

	private static string Native(string name) => name.TrimStart('@') + "Native";
}
