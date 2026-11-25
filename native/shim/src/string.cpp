#include "shim.h"

// Can't throw: freeing memory.
WXK_FUNCTION(void) wxk_String_Free(wxk_String* text) noexcept {
	wxk::FreeString(text);
}

WXK_FUNCTION(wxk_String*) wxk_String_new(const char16_t* text, size_t textLength) noexcept {
	return wxk::Guard<wxk_String*>(nullptr, [&] { return wxk::NewString(text, textLength); });
}

WXK_FUNCTION(wxk_StringArray*) wxk_StringArray_new(void) noexcept {
	return wxk::Guard<wxk_StringArray*>(nullptr, [] { return wxk::Wrap(new wxArrayString()); });
}

WXK_FUNCTION(void) wxk_StringArray_Add(wxk_StringArray* array, const char16_t* text, size_t textLength) noexcept {
	wxk::Guard([&] { wxk::Unwrap(array)->Add(wxk::FromUtf16(text, textLength)); });
}

// Can't throw: an array's size.
WXK_FUNCTION(size_t) wxk_StringArray_GetCount(const wxk_StringArray* array) noexcept {
	return wxk::Unwrap(array)->GetCount();
}

WXK_FUNCTION(wxk_String*) wxk_StringArray_Item(const wxk_StringArray* array, size_t index) noexcept {
	return wxk::Guard<wxk_String*>(nullptr, [&] { return wxk::ToUtf16(wxk::Unwrap(array)->Item(index)); });
}

// Can't throw: destructors don't throw.
WXK_FUNCTION(void) wxk_StringArray_Free(wxk_StringArray* array) noexcept {
	delete wxk::Unwrap(array);
}
