#include "shim.h"

#include <wx/image.h>
#include <wx/log.h>
#include <wx/mstream.h>

// Images read from and written to memory, which .NET's streams go through. Both keep wxWidgets
// from logging a failure, which would show the user an error dialog, since the caller is told.

WXK_FUNCTION(uint8_t) wxk_Image_LoadData(wxk_Object* image, const uint8_t* data, size_t length, int32_t type, int32_t index) noexcept {
	return wxk::Guard<uint8_t>(0, [&]() -> uint8_t {
		wxLogNull quiet;
		wxMemoryInputStream stream(data, length);
		return static_cast<wxImage*>(wxk::Unwrap(image))->LoadFile(stream, static_cast<wxBitmapType>(type), index) ? 1 : 0;
	});
}

// Hands out the image in a format, which the caller frees with wxk_Bytes_Free, or null when it
// cannot be saved in it.
WXK_FUNCTION(uint8_t*) wxk_Image_SaveData(wxk_Object* image, int32_t type, size_t* size) noexcept {
	*size = 0;
	return wxk::Guard<uint8_t*>(nullptr, [&]() -> uint8_t* {
		wxLogNull quiet;
		wxMemoryOutputStream stream;
		if (!static_cast<wxImage*>(wxk::Unwrap(image))->SaveFile(stream, static_cast<wxBitmapType>(type))) return nullptr;
		size_t length = stream.GetSize();
		auto* copy = new uint8_t[length > 0 ? length : 1];
		stream.CopyTo(copy, length);
		*size = length;
		return copy;
	});
}

// Turns wxWidgets' logging on or off, for loading a file the caller is told the failure of, and
// returns whether it was on. A failure wxWidgets logs shows the user an error dialog, on Linux and
// macOS when the event loop is next idle, or as the application exits.
WXK_FUNCTION(uint8_t) wxk_Log_Enable(uint8_t enable) noexcept {
	return wxk::Guard<uint8_t>(1, [&]() -> uint8_t { return wxLog::EnableLogging(enable != 0) ? 1 : 0; });
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
	return wxk::Guard<void*>(nullptr, [&] { return BitmapHandle(*static_cast<wxBitmap*>(wxk::Unwrap(bitmap))); });
}

// wxMSW's wxIcon is a GDI object of its own, and every other port's is a wxBitmap.
WXK_FUNCTION(void*) wxk_Icon_GetHandle(wxk_Object* icon) noexcept {
	return wxk::Guard<void*>(nullptr, [&]() -> void* {
		const wxIcon& value = *static_cast<wxIcon*>(wxk::Unwrap(icon));
#ifdef __WXMSW__
		return value.IsOk() ? value.GetHICON() : nullptr;
#else
		return BitmapHandle(value);
#endif
	});
}