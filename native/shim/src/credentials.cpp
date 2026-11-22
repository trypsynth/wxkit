#include "shim.h"

#include <wx/creddlg.h>
#include <wx/textctrl.h>

// A credential dialog's user name and password, as strings. wxWidgets gives the password as a
// wxSecretValue, which wipes its memory when it goes, but a .NET string cannot be wiped, so there
// is nothing to gain from it on the managed side.
namespace {

wxCredentialEntryDialog* Unwrap(wxk_Window* dialog) {
	return static_cast<wxCredentialEntryDialog*>(wxk::Unwrap(dialog));
}

// The text box the password is typed in, which is read directly: without the secret store, as on
// Linux, wxWidgets' stand-in for wxSecretValue gives back the password's UTF-8 bytes as if each
// were a character.
wxTextCtrl* PasswordBox(wxWindow* window) {
	for (wxWindow* child : window->GetChildren()) {
		wxTextCtrl* text = wxDynamicCast(child, wxTextCtrl);
		if (text != nullptr && text->HasFlag(wxTE_PASSWORD)) return text;
		if (wxTextCtrl* found = PasswordBox(child)) return found;
	}
	return nullptr;
}

} // namespace

WXK_FUNCTION(wxk_String*) wxk_CredentialEntryDialog_GetUserName(wxk_Window* dialog) noexcept {
	return wxk::Guard<wxk_String*>(nullptr, [&] { return wxk::ToUtf16(Unwrap(dialog)->GetCredentials().GetUser()); });
}

WXK_FUNCTION(void) wxk_CredentialEntryDialog_SetUserName(wxk_Window* dialog, const char16_t* user, size_t userLength) noexcept {
	wxk::Guard([&] { Unwrap(dialog)->SetUser(wxk::FromUtf16(user, userLength)); });
}

WXK_FUNCTION(wxk_String*) wxk_CredentialEntryDialog_GetPassword(wxk_Window* dialog) noexcept {
	return wxk::Guard<wxk_String*>(nullptr, [&] {
		wxTextCtrl* password = PasswordBox(Unwrap(dialog));
		return wxk::ToUtf16(password == nullptr ? wxString() : password->GetValue());
	});
}

WXK_FUNCTION(void) wxk_CredentialEntryDialog_SetPassword(wxk_Window* dialog, const char16_t* password, size_t passwordLength) noexcept {
	wxk::Guard([&] { Unwrap(dialog)->SetPassword(wxk::FromUtf16(password, passwordLength)); });
}
