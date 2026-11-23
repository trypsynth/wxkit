#include "shim.h"

#include <wx/dataview.h>

// A wxVariant holds one of the types .NET knows, and anything else reads as text. Bitmaps, icons,
// and icon texts are objects WxKit binds, which cross as heap copies.

WXK_FUNCTION(wxk_Variant*) wxk_Variant_new(void) noexcept {
	return wxk::Guard<wxk_Variant*>(nullptr, [] { return wxk::Wrap(new wxVariant()); });
}

// Can't throw: destructors don't throw.
WXK_FUNCTION(void) wxk_Variant_Free(wxk_Variant* variant) noexcept {
	delete wxk::Unwrap(variant);
}

WXK_FUNCTION(void) wxk_Variant_SetNull(wxk_Variant* variant) noexcept {
	wxk::Guard([&] { wxk::Unwrap(variant)->MakeNull(); });
}

WXK_FUNCTION(void) wxk_Variant_SetString(wxk_Variant* variant, const char16_t* text, size_t textLength) noexcept {
	wxk::Guard([&] { *wxk::Unwrap(variant) = wxk::FromUtf16(text, textLength); });
}

WXK_FUNCTION(void) wxk_Variant_SetBool(wxk_Variant* variant, uint8_t value) noexcept {
	wxk::Guard([&] { *wxk::Unwrap(variant) = value != 0; });
}

WXK_FUNCTION(void) wxk_Variant_SetLong(wxk_Variant* variant, int64_t value) noexcept {
	wxk::Guard([&] { *wxk::Unwrap(variant) = static_cast<long>(value); });
}

WXK_FUNCTION(void) wxk_Variant_SetDouble(wxk_Variant* variant, double value) noexcept {
	wxk::Guard([&] { *wxk::Unwrap(variant) = value; });
}

WXK_FUNCTION(uint8_t) wxk_Variant_SetObject(wxk_Variant* variant, wxk_Object* value) noexcept {
	return wxk::Guard<uint8_t>(0, [&]() -> uint8_t {
		wxObject* object = wxk::Unwrap(value);
		wxVariant& target = *wxk::Unwrap(variant);
		// wxIcon derives from wxBitmap on some ports, so it is checked first.
		if (auto* icon = wxDynamicCast(object, wxIcon)) {
			target << *icon;
		} else if (auto* bitmap = wxDynamicCast(object, wxBitmap)) {
			target << *bitmap;
		} else if (auto* iconText = wxDynamicCast(object, wxDataViewIconText)) {
			target << *iconText;
		} else {
			return 0;
		}
		return 1;
	});
}

WXK_FUNCTION(int32_t) wxk_Variant_GetType(const wxk_Variant* variant) noexcept {
	return wxk::Guard<int32_t>(WXK_VARIANT_STRING, [&]() -> int32_t {
		const wxVariant& value = *wxk::Unwrap(const_cast<wxk_Variant*>(variant));
		if (value.IsNull()) return WXK_VARIANT_NULL;
		const wxString type = value.GetType();
		if (type == "string") return WXK_VARIANT_STRING;
		if (type == "bool") return WXK_VARIANT_BOOL;
		if (type == "long" || type == "longlong" || type == "ulonglong" || type == "char") return WXK_VARIANT_LONG;
		if (type == "double") return WXK_VARIANT_DOUBLE;
		if (type == "wxIcon") return WXK_VARIANT_ICON;
		if (type == "wxBitmap") return WXK_VARIANT_BITMAP;
		if (type == "wxDataViewIconText") return WXK_VARIANT_ICON_TEXT;
		return WXK_VARIANT_STRING;
	});
}

WXK_FUNCTION(wxk_String*) wxk_Variant_GetString(const wxk_Variant* variant) noexcept {
	return wxk::Guard<wxk_String*>(nullptr, [&] { return wxk::ToUtf16(wxk::Unwrap(const_cast<wxk_Variant*>(variant))->MakeString()); });
}

WXK_FUNCTION(uint8_t) wxk_Variant_GetBool(const wxk_Variant* variant) noexcept {
	return wxk::Guard<uint8_t>(0, [&]() -> uint8_t { return wxk::Unwrap(const_cast<wxk_Variant*>(variant))->GetBool() ? 1 : 0; });
}

WXK_FUNCTION(int64_t) wxk_Variant_GetLong(const wxk_Variant* variant) noexcept {
	return wxk::Guard<int64_t>(0, [&]() -> int64_t {
		const wxVariant& value = *wxk::Unwrap(const_cast<wxk_Variant*>(variant));
		if (value.GetType() == "longlong") return value.GetLongLong().GetValue();
		if (value.GetType() == "ulonglong") return static_cast<int64_t>(value.GetULongLong().GetValue());
		if (value.GetType() == "char") return static_cast<int64_t>(static_cast<wxChar>(value.GetChar()));
		return value.GetLong();
	});
}

WXK_FUNCTION(double) wxk_Variant_GetDouble(const wxk_Variant* variant) noexcept {
	return wxk::Guard<double>(0, [&] { return wxk::Unwrap(const_cast<wxk_Variant*>(variant))->GetDouble(); });
}

// A heap copy of the bitmap, icon, or icon text the variant holds, which the caller owns.
WXK_FUNCTION(wxk_Object*) wxk_Variant_GetObject(const wxk_Variant* variant) noexcept {
	return wxk::Guard<wxk_Object*>(nullptr, [&]() -> wxk_Object* {
		const wxVariant& value = *wxk::Unwrap(const_cast<wxk_Variant*>(variant));
		const wxString type = value.GetType();
		if (type == "wxIcon") {
			wxIcon icon;
			icon << value;
			return wxk::CopyValue(icon);
		}
		if (type == "wxBitmap") {
			wxBitmap bitmap;
			bitmap << value;
			return wxk::CopyValue(bitmap);
		}
		if (type == "wxDataViewIconText") {
			wxDataViewIconText iconText;
			iconText << value;
			return wxk::CopyValue(iconText);
		}
		return nullptr;
	});
}

WXK_FUNCTION(wxk_VariantList*) wxk_VariantList_new(void) noexcept {
	return wxk::Guard<wxk_VariantList*>(nullptr, [] { return wxk::Wrap(new wxVector<wxVariant>()); });
}

WXK_FUNCTION(void) wxk_VariantList_Add(wxk_VariantList* list, const wxk_Variant* value) noexcept {
	wxk::Guard([&] { wxk::Unwrap(list)->push_back(*wxk::Unwrap(const_cast<wxk_Variant*>(value))); });
}

// Can't throw: destructors don't throw.
WXK_FUNCTION(void) wxk_VariantList_Free(wxk_VariantList* list) noexcept {
	delete wxk::Unwrap(list);
}
