#include "shim.h"

using wxk::Guard;
using wxk::Unwrap;

WXK_FUNCTION(wxk_Menu*) wxk_Menu_new(void) noexcept {
	return Guard<wxk_Menu*>(nullptr, [] { return wxk::Wrap(new wxMenu()); });
}

WXK_FUNCTION(wxk_MenuItem*) wxk_Menu_Append(wxk_Menu* menu, int32_t id, const char16_t* text, size_t textLength, const char16_t* help, size_t helpLength, int32_t kind) noexcept {
	return Guard<wxk_MenuItem*>(nullptr, [&] {
		return wxk::Wrap(Unwrap(menu)->Append(id, wxk::FromUtf16(text, textLength), wxk::FromUtf16(help, helpLength), static_cast<wxItemKind>(kind)));
	});
}

WXK_FUNCTION(wxk_MenuItem*) wxk_Menu_AppendSeparator(wxk_Menu* menu) noexcept {
	return Guard<wxk_MenuItem*>(nullptr, [&] { return wxk::Wrap(Unwrap(menu)->AppendSeparator()); });
}

WXK_FUNCTION(wxk_MenuItem*) wxk_Menu_AppendSubMenu(wxk_Menu* menu, wxk_Menu* submenu, const char16_t* text, size_t textLength, const char16_t* help, size_t helpLength) noexcept {
	return Guard<wxk_MenuItem*>(nullptr, [&] {
		return wxk::Wrap(Unwrap(menu)->AppendSubMenu(Unwrap(submenu), wxk::FromUtf16(text, textLength), wxk::FromUtf16(help, helpLength)));
	});
}

// The item with an id, in the menu or one of its submenus, as wxMenu's own by-id members find it.
WXK_FUNCTION(void) wxk_Menu_Enable(wxk_Menu* menu, int32_t id, uint8_t enable) noexcept {
	Guard([&] { Unwrap(menu)->Enable(id, enable != 0); });
}

WXK_FUNCTION(uint8_t) wxk_Menu_IsEnabled(wxk_Menu* menu, int32_t id) noexcept {
	return Guard<uint8_t>(0, [&]() -> uint8_t { return Unwrap(menu)->IsEnabled(id) ? 1 : 0; });
}

WXK_FUNCTION(void) wxk_Menu_Check(wxk_Menu* menu, int32_t id, uint8_t check) noexcept {
	Guard([&] { Unwrap(menu)->Check(id, check != 0); });
}

WXK_FUNCTION(uint8_t) wxk_Menu_IsChecked(wxk_Menu* menu, int32_t id) noexcept {
	return Guard<uint8_t>(0, [&]() -> uint8_t { return Unwrap(menu)->IsChecked(id) ? 1 : 0; });
}

WXK_FUNCTION(void) wxk_Menu_SetLabel(wxk_Menu* menu, int32_t id, const char16_t* text, size_t textLength) noexcept {
	Guard([&] { Unwrap(menu)->SetLabel(id, wxk::FromUtf16(text, textLength)); });
}

WXK_FUNCTION(wxk_String*) wxk_Menu_GetLabel(wxk_Menu* menu, int32_t id) noexcept {
	return Guard<wxk_String*>(nullptr, [&] { return wxk::ToUtf16(Unwrap(menu)->GetLabel(id)); });
}

WXK_FUNCTION(void) wxk_Menu_Free(wxk_Menu* menu) noexcept {
	Guard([&] { delete Unwrap(menu); });
}

// Can't throw: it reads a member.
WXK_FUNCTION(int32_t) wxk_MenuItem_GetId(wxk_MenuItem* item) noexcept {
	return Unwrap(item)->GetId();
}
