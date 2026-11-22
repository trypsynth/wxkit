#include "shim.h"
#include <wx/settings.h>

// Light and dark: what the system uses, and which the application asks for.

WXK_FUNCTION(int32_t) wxk_App_SetAppearance(int32_t appearance) noexcept {
	return wxk::Guard<int32_t>(0, [appearance] {
		if (wxTheApp == nullptr) return int32_t(0);
		return static_cast<int32_t>(wxTheApp->SetAppearance(static_cast<wxApp::Appearance>(appearance)));
	});
}

// Whether the system is dark, its background is, and its applications are, as 1 bits of 1, 2, and 4.
WXK_FUNCTION(int32_t) wxk_SystemAppearance_GetFlags(void) noexcept {
	return wxk::Guard<int32_t>(0, [] {
		wxSystemAppearance appearance = wxSystemSettings::GetAppearance();
		return int32_t((appearance.IsDark() ? 1 : 0) | (appearance.IsUsingDarkBackground() ? 2 : 0) | (appearance.AreAppsDark() ? 4 : 0));
	});
}

WXK_FUNCTION(wxk_String*) wxk_SystemAppearance_GetName(void) noexcept {
	return wxk::Guard<wxk_String*>(nullptr, [] { return wxk::ToUtf16(wxSystemSettings::GetAppearance().GetName()); });
}
