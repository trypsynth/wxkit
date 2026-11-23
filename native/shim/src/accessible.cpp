#include "shim.h"

#if wxUSE_ACCESSIBILITY

#include <wx/access.h>

// The statuses .NET returns are wxAccStatus's values.
static_assert(wxACC_FAIL == 0 && wxACC_FALSE == 1 && wxACC_OK == 2 && wxACC_NOT_IMPLEMENTED == 3 && wxACC_NOT_SUPPORTED == 4 && wxACC_INVALID_ARG == 5);

// The window owns the accessible object, and deletes the one it had, whose name, from
// SetAccessibleName, the new one keeps. wxMSW's SetNameOverride, which keeps it, isn't in the
// interface headers, so this isn't generated.
WXK_FUNCTION(uint8_t) wxk_Window_GiveAccessible(wxk_Window* window, wxk_Accessible* accessible) noexcept {
	return wxk::Guard<uint8_t>(0, [&]() -> uint8_t {
		wxWindow* owner = wxk::Unwrap(window);
		auto* given = reinterpret_cast<wxAccessible*>(accessible);
		if (wxAccessible* previous = owner->GetAccessible()) given->SetNameOverride(previous->GetNameOverride());
		owner->SetAccessible(given);
		return 1;
	});
}

#else

// wxAccessible is Microsoft Active Accessibility's, so elsewhere a window keeps the accessibility its
// port gives it, and nothing asks .NET.

WXK_FUNCTION(uint8_t) wxk_Window_GiveAccessible(wxk_Window*, wxk_Accessible*) noexcept {
	return 0;
}

#endif
