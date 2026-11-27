using System.Globalization;
using System.Text.RegularExpressions;
using WxKit.Generator.Doxygen;

namespace WxKit.Generator.Binding;

// How a C++ type crosses to managed code, and how a C++ default value is written in C#.
internal sealed partial class Binder {
	// What a hand-written callback gives back through a pointer or a reference: text, a long, an
	// enum, a rectangle, or an object's handle, such as an accessible object's name, state, role,
	// location, and child.
	private Conversion? CallbackOutFor(CppType type, BoundClass owner) {
		if (type is { Name: "wxVariant", IsReference: true, Pointers: 0 }) return new CallbackLentConversion("wxk_Variant*", "WxkVariant*", type.IsConst ? "wxk::Wrap(const_cast<wxVariant*>(&{0}))" : "wxk::Wrap(&{0})");
		if (type is { Name: "wxDataViewItemArray", IsReference: true, IsConst: false, Pointers: 0 }) return new CallbackLentConversion("wxk_DataViewItems*", "WxkDataViewItems*", "reinterpret_cast<wxk_DataViewItems*>(&{0})");
		if (type.IsConst) return null;
		if (type.IsReference && type.Pointers == 0) return type.Name == "wxRect" ? new CallbackOutConversion("wxRect", "wxk_Rect", "Rect", byReference: true) : null;
		if (type.IsReference) return null;
		if (type.Pointers == 2) return _classes.TryGetValue(type.Name, out BoundClass? target) && target.Handle != HandleKind.Static ? new CallbackOutConversion(type.Name + "*", target.Handle.CType + "*", target.Handle.InteropType + "*", byReference: false) : null;
		if (type.Pointers != 1) return null;
		if (type.Name == "wxString") return new CallbackOutConversion("wxString", "wxk_String*", "WxkString*", byReference: false);
		if (type.Name == "long") return new CallbackOutConversion("long", "int64_t", "long", byReference: false);
		string nested = Ancestors(owner.CppName).Select(ancestor => $"{ancestor}::{type.Name}").FirstOrDefault(api.Enums.ContainsKey) ?? type.Name;
		return api.Enums.ContainsKey(nested) ? new CallbackOutConversion(nested, "int32_t", "int", byReference: false) : null;
	}

	// A number a tweak says is milliseconds is a TimeSpan over whatever the number would have been.
	private Conversion? Map(CppType type, BoundClass owner, string member, string? parameter, bool isReturn) {
		Conversion? conversion = MapType(type, owner, member, parameter, isReturn);
		if (conversion == NumberConversion.Long && (owner.Tweak.HasIntIndexes || (parameter is not null && owner.Tweak.IntParameters.Contains((member, parameter)))) && !(parameter is not null && owner.Tweak.LongParameters.Contains((member, parameter)))) conversion = NumberConversion.Index;
		if (conversion == OutConversion.Long && owner.Tweak.HasIntIndexes) conversion = OutConversion.Index;
		return conversion is not null and not MillisecondsConversion && owner.Tweak.Milliseconds.Contains((member, isReturn ? "return" : parameter ?? string.Empty)) ? new MillisecondsConversion(conversion) : conversion;
	}

	private Conversion? MapType(CppType type, BoundClass owner, string member, string? parameter, bool isReturn) {
		bool isHandwrittenDirector = owner.Tweak.IsDirectorInternal && owner.Tweak.DirectorMembers.Contains(member);
		if (!isReturn && isHandwrittenDirector && CallbackOutFor(type, owner) is Conversion given) return given;
		if (!isReturn && _classes.TryGetValue(type.Name, out BoundClass? owned) && owned.Handle == HandleKind.Owned && (type.IsReference || type.Pointers == 1)) return new OwnedConversion(owned, type.IsReference);
		// UTF-8 text, as wxUIActionSimulator::Text takes it.
		if (!isReturn && type is { Name: "char", IsConst: true, Pointers: 1, IsReference: false }) return Utf8StringConversion.Instance;
		// A value object passed by a reference that can change, such as the bitmap a wxMemoryDC
		// draws into, is the managed object's own copy, so the change shows there.
		if (!isReturn && type.IsReference && !type.IsConst && _classes.TryGetValue(type.Name, out BoundClass? changed) && changed.Handle == HandleKind.Value) return new ValueConversion(changed, mutable: true);
		// A tweak names an enum by its managed name, or by its C++ one, which is then generated like
		// any other. It names a return value as the parameter "return".
		if (owner.Tweak.ParameterEnums.TryGetValue((member, isReturn ? "return" : parameter ?? string.Empty), out string? enumName)) {
			string managed = enumName.StartsWith("wx", StringComparison.Ordinal) ? EnumFor(enumName)?.Name ?? throw new InvalidOperationException($"A tweak gives {owner.CppName}::{member} the enum {enumName}, which the headers do not have.") : enumName;
			return new EnumConversion(managed, type.Name == "long" ? NumberConversion.Long : NumberConversion.Int);
		}
		// A class without a style table of its own, such as wxListView, takes its nearest ancestor's.
		BoundStyles? styles = null;
		for (BoundClass? current = owner; current is not null && styles is null; current = current.Base) styles = current.Styles;
		if (parameter == "style" && type.Name == "long" && type.Pointers == 0 && styles is not null && member == owner.CppName) return new EnumConversion(styles.Name, NumberConversion.Long);
		// A getter of the style, such as GetMessageDialogStyle, returns the class's style enum.
		if (isReturn && type.Name == "long" && type.Pointers == 0 && member.EndsWith("Style", StringComparison.Ordinal) && owner.Styles is BoundStyles ownStyles) return new EnumConversion(ownStyles.Name, NumberConversion.Long);
		if (type.IsVoid) return isReturn ? VoidConversion.Instance : null;
		// Numbers a function writes through a pointer become out parameters.
		if (!isReturn && type.Pointers == 1 && !type.IsConst && !type.IsReference) {
			switch (type.Name) {
				case "int" or "wxCoord":
					return OutConversion.Int;
				case "long":
					return OutConversion.Long;
				case "double":
					return OutConversion.Double;
			}
		}
		// A function that changes its object and returns it, as wxFont::MakeBold does.
		if (isReturn && type.IsReference && !type.IsConst && type.Pointers == 0 && type.Name == owner.CppName && owner.Handle.IsOwned) return new SelfConversion(owner);
		if (isReturn && type.Name is "wxWindowList" && type.Pointers == 0) return WindowListConversion.Instance;
		if (type.Pointers == 1 && !type.IsReference) {
			if (type.Name == "wxObject" && isReturn) return AnyObjectConversion.Instance;
			// Data a control keeps with an item, which it deletes with the item.
			if (type.Name == "wxClientData") return ItemDataConversion.Client;
			if (type.Name == "wxTreeItemData") return ItemDataConversion.TreeItem;
			// Events are only received, and a static class has no objects to pass.
			if (!_classes.TryGetValue(type.Name, out BoundClass? target) || target.Handle == HandleKind.Event || target.Handle == HandleKind.Static) return null;
			// Only event handlers are tracked, so only they can be wrapped when wxWidgets hands one back.
			// A tool cannot go in, since the only tools are a toolbar's own, which it has already.
			if (!isReturn && target.Handle == HandleKind.Tool) return null;
			// Hand-written code takes any object's handle from an internal member, such as a printout's
			// DC or a menu's item, and knows what it may do with it.
			if (owner.Tweak.InternalMembers.Contains(member)) return new ObjectConversion(target);
			// A menu item's handle, and a class's that isn't a wxObject, are only for hand-written code.
			if (target.Handle == HandleKind.MenuItem || target.Handle.IsPlain) return null;
			return isReturn && !target.Handle.IsEvtHandler && !target.Handle.IsKeptByOwner ? null : new ObjectConversion(target);
		}
		// A rectangle or strings a call fills by reference are out parameters. Only a getter's
		// rectangle is, since wxWindow::IsExposed takes one it reads by a reference that could change.
		if (!isReturn && type.Pointers == 0 && type.IsReference && !type.IsConst) {
			if (type.Name == "wxRect" && member.StartsWith("Get", StringComparison.Ordinal)) return RectOutConversion.Instance;
			if (type.Name == "wxArrayString") return StringArrayOutConversion.Instance;
		}
		// A list of ints or item ids, such as a list box's selections, comes back as an array, goes in
		// as any sequence, and is an out array when the call fills it.
		if (type.Pointers == 0 && NumberListKind.For(type.Name) is NumberListKind list) {
			if (isReturn) return new NumberListConversion(list);
			if (type.IsReference && !type.IsConst) return new NumberListOutConversion(list);
			return list.ToCpp is null ? null : new NumberListConversion(list);
		}
		// A reference that comes back, even one that could change, such as a color dialog's data,
		// is copied, as a value or a const reference is.
		// wxPen::SetColour's documentation takes a wxColour that could change, though wxWidgets' own
		// takes a const one, so a pen's color can be set.
		if (!isReturn && type is { Name: "wxColour", IsReference: true, IsConst: false, Pointers: 0 } && member.StartsWith("Set", StringComparison.Ordinal)) return ColorConversion.Instance;
		if (!type.IsValueOrConstReference && !(isReturn && type.IsReference && type.Pointers == 0)) return null;
		if (_classes.TryGetValue(type.Name, out BoundClass? value) && value.Handle == HandleKind.Value) return new ValueConversion(value);
		// Bundles of bitmaps or icons, which wxWidgets uses for every resolution, convert from one.
		if (!isReturn && type.Name == "wxBitmapBundle" && _classes.TryGetValue("wxBitmap", out BoundClass? bitmap)) return new ValueConversion(bitmap, "wxBitmapBundle");
		if (!isReturn && type.Name == "wxIconBundle" && _classes.TryGetValue("wxIcon", out BoundClass? icon)) return new ValueConversion(icon, "wxIconBundle");
		if (HandwrittenEnums.TryGetValue(type.Name, out string? handwritten)) return new EnumConversion(handwritten, NumberConversion.Int, type.Name);
		if (!isReturn && type.Name.Replace(" ", string.Empty, StringComparison.Ordinal) == "wxVector<wxVariant>") return VariantListConversion.Instance;
		if (!isReturn && type.Name.Replace(" ", string.Empty, StringComparison.Ordinal) == "wxVector<wxBitmapBundle>" && _classes.ContainsKey("wxBitmap")) return BitmapListConversion.Instance;
		// An enum a class nests, which its own members name without the class, as wxStaticBitmap's
		// SetScaleMode takes ScaleMode, is named with it, which the shim's cast needs.
		string nestedName = type.Name.Contains("::", StringComparison.Ordinal) ? type.Name : Ancestors(owner.CppName).Select(ancestor => $"{ancestor}::{type.Name}").FirstOrDefault(api.Enums.ContainsKey) ?? type.Name;
		// A hand-written callback's enum crosses as its number, such as wxAccStatus, unless a tweak binds
		// it, so the callback adds no enum to the managed API, nor one the shim checks where it isn't.
		if (isHandwrittenDirector && type.Pointers == 0 && !type.IsReference && api.Enums.ContainsKey(nestedName) && !tweaks.Enums.ContainsKey(nestedName)) return new EnumConversion("int", NumberConversion.Int, nestedName);
		if (EnumFor(nestedName) is BoundEnum bound) return new EnumConversion(bound.Name, NumberConversion.Int, nestedName);
		return type.Name switch {
			"bool" => BoolConversion.Instance,
			"int" or "wxWindowID" or "wxCoord" => NumberConversion.Int,
			// Only the list control's, since the data view list's appends would gain a parameter that code
			// built against WxKit 0.1.0 doesn't pass.
			"wxUIntPtr" when owner.CppName is "wxListCtrl" or "wxListView" or "wxListItem" or "wxListEvent" => NumberConversion.ItemData,
			"long" or "wxTextPos" => NumberConversion.Long,
			"double" => NumberConversion.Double,
			"float" => NumberConversion.Float,
			"unsigned char" or "wxByte" => NumberConversion.Byte,
			// A wxMessageDialog button label converts from a string, which is how WxKit gives it.
			// wxArtID and wxArtClient are typedefs of wxString.
			"wxString" or "ButtonLabel" or "wxArtID" or "wxArtClient" => StringConversion.Instance,
			// An unsigned long is 32 bits on Windows and 64 elsewhere, so it crosses as a size_t.
			"size_t" or "unsigned long" => CountConversion.Size,
			"unsigned" or "unsigned int" or "wxUint32" => CountConversion.Unsigned,
			"wxRect" => RectConversion.Instance,
			"wxTreeItemId" => ItemIdConversion.TreeItem,
			"wxDataViewItem" => ItemIdConversion.DataViewItem,
			"wxTreeListItem" => ItemIdConversion.TreeListItem,
			"wxVariant" => VariantConversion.Instance,
			"wxDateTime" => DateTimeConversion.Instance,
			"wxArrayString" => StringArrayConversion.Instance,
			"wxColour" => ColorConversion.Instance,
			"wxPoint" => GeometryConversion.Point,
			"wxSize" => GeometryConversion.Size,
			"wxGBPosition" => GeometryConversion.GridPosition,
			"wxGBSpan" => GeometryConversion.GridSpan,
			_ => null,
		};
	}

	private bool TryDefault(string cppDefault, Conversion conversion, out string? managed, out bool isNull) {
		managed = null;
		isNull = false;
		string value = cppDefault.Trim();
		if (conversion is GeometryConversion geometry && geometry.IsDefault(value) || value is "nullptr" or "NULL" or "0" && conversion.AcceptsNull || conversion is ColorConversion && value == "wxNullColour" || conversion is DateTimeConversion && value is "wxDefaultDateTime" or "wxInvalidDateTime" || conversion is ValueConversion && (value.StartsWith("wxNull", StringComparison.Ordinal) || value.EndsWith("()", StringComparison.Ordinal))) {
			managed = "null";
			isNull = true;
			return true;
		}
		switch (conversion) {
			case MillisecondsConversion when value == "0":
				managed = "default";
				return true;
			case BoolConversion when value is "true" or "false":
				managed = value;
				return true;
			case StringConversion when StringDefaults.TryGetValue(value, out string? text):
				managed = $"\"{text}\"";
				return true;
			case StringConversion when StringLiteralPattern().IsMatch(value):
				managed = value;
				return true;
			// An art id is written as the ArtId or ArtClient constant WxKit generates for it.
			case StringConversion when api.StringConstants.TryGetValue(value, out string? constant):
				managed = $"{(constant.EndsWith("_C", StringComparison.Ordinal) ? "ArtClient" : "ArtId")}.{Naming.Words(value["wxART_".Length..])}";
				return true;
			case NumberConversion number when number == NumberConversion.Int && value == "wxID_ANY":
				managed = "StandardId.Any";
				return true;
			case NumberConversion number when number == NumberConversion.Double || number == NumberConversion.Float:
				if (!double.TryParse(value.TrimEnd('f'), NumberStyles.Float, CultureInfo.InvariantCulture, out double floating)) return false;
				managed = number == NumberConversion.Float ? $"{floating.ToString(CultureInfo.InvariantCulture)}f" : floating.ToString(CultureInfo.InvariantCulture);
				return true;
			case NumberConversion or CountConversion:
				if (_constants.Evaluate(value) is not long integer) return false;
				managed = integer.ToString(CultureInfo.InvariantCulture);
				return true;
			case EnumConversion enumeration:
				managed = EnumDefault(value, enumeration);
				return managed is not null;
			default:
				return false;
		}
	}

	// A default made of constants the enum has, such as wxOK | wxCENTRE, is written with its
	// members, so it reads as MessageDialogStyle.Ok | MessageDialogStyle.Center.
	private string? EnumDefault(string value, EnumConversion enumeration) {
		BoundStyles? styles = _classes.Values.Select(static bound => bound.Styles).FirstOrDefault(found => found?.Name == enumeration.EnumName)
			?? _enums.Values.OfType<BoundEnum>().Where(found => found.Name == enumeration.EnumName).Select(static found => new BoundStyles(found.Name, found.Members)).FirstOrDefault();
		string[] parts = [.. value.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)];
		List<string> members = [];
		int shared = enumeration.CppType is string cppEnum && api.Enums.TryGetValue(cppEnum, out DoxygenEnum? source) ? SharedWords(source) : 0;
		foreach (string part in parts) {
			string? member = styles is not null
				? styles.Members.FirstOrDefault(style => Unqualified(style.Constant) == Unqualified(part))?.Name
				: ConstantPattern().IsMatch(part) ? MemberName(part, shared) : null;
			if (member is null) {
				members.Clear();
				break;
			}
			members.Add($"{enumeration.EnumName}.{member}");
		}
		if (members.Count > 0) return string.Join(" | ", members);
		if (_constants.Evaluate(value) is not long number) return null;
		if (styles?.Members.FirstOrDefault(style => style.Value == number) is BoundStyle exact) return $"{enumeration.EnumName}.{exact.Name}";
		return number == 0 ? "0" : $"({enumeration.EnumName})0x{number:X}";
	}

	// A constant without the class or enum class it is named in, which a default may leave out, as
	// a member of the class does, or give, as wxNavigationKeyEvent::IsForward.
	private static string Unqualified(string constant) => constant[(constant.LastIndexOf("::", StringComparison.Ordinal) is int colons and >= 0 ? colons + 2 : 0)..];

	// Known string constants used as defaults, and their values.
	private static readonly Dictionary<string, string> StringDefaults = new(StringComparer.Ordinal) {
		["wxEmptyString"] = string.Empty,
		["wxString()"] = string.Empty,
		["wxMessageBoxCaptionStr"] = "Message",
	};

	[GeneratedRegex("^\".*\"$")]
	private static partial Regex StringLiteralPattern();

	[GeneratedRegex(@"^wx[A-Z0-9_]+$")]
	private static partial Regex ConstantPattern();
}
