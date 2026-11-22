#include "shim.h"
#include <wx/utils.h>

// The busy cursor, which nests, and disabling the application's windows while a task runs.

WXK_FUNCTION(void) wxk_BusyCursor_Begin(void) noexcept {
	wxk::Guard([] { wxBeginBusyCursor(); });
}

WXK_FUNCTION(void) wxk_BusyCursor_End(void) noexcept {
	wxk::Guard([] { wxEndBusyCursor(); });
}

WXK_FUNCTION(uint8_t) wxk_BusyCursor_IsBusy(void) noexcept {
	return wxk::Guard<uint8_t>(0, [] { return wxIsBusy() ? uint8_t(1) : uint8_t(0); });
}

// Disables every top-level window but except, which may be null, until wxk_WindowDisabler_Free.
WXK_FUNCTION(void*) wxk_WindowDisabler_new(wxk_Window* except) noexcept {
	return wxk::Guard<void*>(nullptr, [except]() -> void* { return new wxWindowDisabler(static_cast<wxWindow*>(wxk::Unwrap(except))); });
}

WXK_FUNCTION(void) wxk_WindowDisabler_Free(void* disabler) noexcept {
	wxk::Guard([disabler] { delete static_cast<wxWindowDisabler*>(disabler); });
}
