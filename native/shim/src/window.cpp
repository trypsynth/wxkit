#include "shim.h"

using wxk::Guard;
using wxk::Unwrap;

// Can't throw: it reads a member.
WXK_FUNCTION(void*) wxk_Window_GetHandle(wxk_Window* window) noexcept {
	return reinterpret_cast<void*>(Unwrap(window)->GetHandle());
}

WXK_FUNCTION(void*) wxk_DC_GetHandle(wxk_Object* dc) noexcept {
	return Guard<void*>(nullptr, [&] { return static_cast<wxDC*>(Unwrap(dc))->GetHandle(); });
}

// Can't throw: a vector's size.
WXK_FUNCTION(size_t) wxk_WindowList_GetCount(const wxk_WindowList* list) noexcept {
	return reinterpret_cast<const wxk::WindowList*>(list)->windows.size();
}

// Can't throw: reading a vector .NET checked the index of.
WXK_FUNCTION(wxk_Window*) wxk_WindowList_Item(const wxk_WindowList* list, size_t index) noexcept {
	return wxk::Wrap(reinterpret_cast<const wxk::WindowList*>(list)->windows[index]);
}

WXK_FUNCTION(void) wxk_WindowList_Free(wxk_WindowList* list) noexcept {
	delete reinterpret_cast<wxk::WindowList*>(list);
}

WXK_FUNCTION(wxk_Sizer*) wxk_Dialog_CreateStdDialogButtonSizer(wxk_Window* dialog, int64_t flags) noexcept {
	return wxk::Guard<wxk_Sizer*>(nullptr, [&] {
		wxSizer* created = static_cast<wxDialog*>(wxk::Unwrap(dialog))->CreateStdDialogButtonSizer(static_cast<long>(flags));
		return wxk::Wrap(created);
	});
}
