#include "shim.h"
#include <wx/utils.h>

// Small things of the desktop that .NET has no way to ask the system for: its alert sound, its
// default browser and applications, and the keyboard and pointer's state.

WXK_FUNCTION(void) wxk_Desktop_Bell(void) noexcept {
	wxk::Guard([] { wxBell(); });
}

WXK_FUNCTION(uint8_t) wxk_Desktop_OpenUrl(const char16_t* url, size_t urlLength) noexcept {
	return wxk::Guard<uint8_t>(0, [&] { return wxLaunchDefaultBrowser(wxk::FromUtf16(url, urlLength)) ? uint8_t(1) : uint8_t(0); });
}

WXK_FUNCTION(uint8_t) wxk_Desktop_Open(const char16_t* document, size_t documentLength) noexcept {
	return wxk::Guard<uint8_t>(0, [&] { return wxLaunchDefaultApplication(wxk::FromUtf16(document, documentLength)) ? uint8_t(1) : uint8_t(0); });
}

WXK_FUNCTION(uint8_t) wxk_Keyboard_IsKeyDown(int32_t key) noexcept {
	return wxk::Guard<uint8_t>(0, [key] { return wxGetKeyState(static_cast<wxKeyCode>(key)) ? uint8_t(1) : uint8_t(0); });
}

WXK_FUNCTION(wxk_Point) wxk_Mouse_GetPosition(void) noexcept {
	return wxk::Guard<wxk_Point>(wxk_Point{-1, -1}, [] { return wxk::ToPoint(wxGetMousePosition()); });
}
