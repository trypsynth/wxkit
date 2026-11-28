#include "shim.h"

#include <wx/listbase.h>

using wxk::Guard;
using wxk::Unwrap;

// The widths ListColumnWidth names.
static_assert(wxLIST_AUTOSIZE == -1 && wxLIST_AUTOSIZE_USEHEADER == -2);

// Can't throw: it reads a member.
WXK_FUNCTION(void*) wxk_Window_GetHandle(wxk_Window* window) noexcept {
	return reinterpret_cast<void*>(Unwrap(window)->GetHandle());
}

WXK_FUNCTION(void*) wxk_DC_GetHandle(wxk_Object* dc) noexcept {
	return Guard<void*>(nullptr, [&] { return static_cast<wxDC*>(Unwrap(dc))->GetHandle(); });
}

namespace {

// wxWidgets has no one call for a bitmap's handle: wxMSW's is GetHBITMAP, wxOSX's is too, which
// gives the CGContextRef the pixels are in, and wxGTK's is GetPixbuf. An empty bitmap has none,
// and wxOSX and wxGTK assert on one.
void* BitmapHandle(const wxBitmap& bitmap) {
	if (!bitmap.IsOk()) return nullptr;
#if defined(__WXMSW__) || defined(__WXOSX__)
	return bitmap.GetHBITMAP();
#elif defined(__WXGTK__)
	return bitmap.GetPixbuf();
#else
	return nullptr;
#endif
}

}

WXK_FUNCTION(void*) wxk_Bitmap_GetHandle(wxk_Object* bitmap) noexcept {
	return Guard<void*>(nullptr, [&] { return BitmapHandle(*static_cast<wxBitmap*>(Unwrap(bitmap))); });
}

// wxMSW's wxIcon is a GDI object of its own, and every other port's is a wxBitmap.
WXK_FUNCTION(void*) wxk_Icon_GetHandle(wxk_Object* icon) noexcept {
	return Guard<void*>(nullptr, [&]() -> void* {
		const wxIcon& value = *static_cast<wxIcon*>(Unwrap(icon));
#ifdef __WXMSW__
		return value.IsOk() ? value.GetHICON() : nullptr;
#else
		return BitmapHandle(value);
#endif
	});
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
