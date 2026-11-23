#include "shim.h"
#include <wx/utils.h>

// Disabling the application's windows while a task runs. The busy cursor's functions are generated.

// Disables every top-level window but except, which may be null, until wxk_WindowDisabler_Free.
WXK_FUNCTION(void*) wxk_WindowDisabler_new(wxk_Window* except) noexcept {
	return wxk::Guard<void*>(nullptr, [except]() -> void* { return new wxWindowDisabler(static_cast<wxWindow*>(wxk::Unwrap(except))); });
}

WXK_FUNCTION(void) wxk_WindowDisabler_Free(void* disabler) noexcept {
	wxk::Guard([disabler] { delete static_cast<wxWindowDisabler*>(disabler); });
}
