#include "shim.h"
#include <wx/display.h>

// The displays connected to the system, each by its index, from 0 to the count.

WXK_FUNCTION(int32_t) wxk_Display_GetCount(void) noexcept {
	return wxk::Guard<int32_t>(0, [] { return static_cast<int32_t>(wxDisplay::GetCount()); });
}

WXK_FUNCTION(int32_t) wxk_Display_GetFromWindow(wxk_Window* window) noexcept {
	return wxk::Guard<int32_t>(-1, [window] { return static_cast<int32_t>(wxDisplay::GetFromWindow(static_cast<wxWindow*>(wxk::Unwrap(window)))); });
}

WXK_FUNCTION(int32_t) wxk_Display_GetFromPoint(wxk_Point point) noexcept {
	return wxk::Guard<int32_t>(-1, [point] { return static_cast<int32_t>(wxDisplay::GetFromPoint(wxk::FromPoint(point))); });
}

WXK_FUNCTION(wxk_Rect) wxk_Display_GetGeometry(int32_t index) noexcept {
	return wxk::Guard<wxk_Rect>(wxk_Rect{0, 0, 0, 0}, [index] { return wxk::ToRect(wxDisplay(static_cast<unsigned int>(index)).GetGeometry()); });
}

WXK_FUNCTION(wxk_Rect) wxk_Display_GetClientArea(int32_t index) noexcept {
	return wxk::Guard<wxk_Rect>(wxk_Rect{0, 0, 0, 0}, [index] { return wxk::ToRect(wxDisplay(static_cast<unsigned int>(index)).GetClientArea()); });
}

WXK_FUNCTION(wxk_String*) wxk_Display_GetName(int32_t index) noexcept {
	return wxk::Guard<wxk_String*>(nullptr, [index] { return wxk::ToUtf16(wxDisplay(static_cast<unsigned int>(index)).GetName()); });
}

WXK_FUNCTION(uint8_t) wxk_Display_IsPrimary(int32_t index) noexcept {
	return wxk::Guard<uint8_t>(0, [index] { return wxDisplay(static_cast<unsigned int>(index)).IsPrimary() ? uint8_t(1) : uint8_t(0); });
}

WXK_FUNCTION(double) wxk_Display_GetScaleFactor(int32_t index) noexcept {
	return wxk::Guard<double>(1.0, [index] { return wxDisplay(static_cast<unsigned int>(index)).GetScaleFactor(); });
}
