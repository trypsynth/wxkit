#include "shim.h"

using wxk::Guard;
using wxk::Unwrap;

WXK_FUNCTION(wxk_Timer*) wxk_Timer_new(void) noexcept {
	return Guard<wxk_Timer*>(nullptr, [] {
		auto* timer = new wxTimer();
		// A timer without an owner has nowhere to send its events, so it sends them to itself,
		// where WxKit binds them like any other handler's.
		timer->SetOwner(timer);
		return wxk::Wrap(timer);
	});
}

WXK_FUNCTION(uint8_t) wxk_Timer_Start(wxk_Timer* timer, int32_t milliseconds, uint8_t oneShot) noexcept {
	return Guard<uint8_t>(0, [&] { return Unwrap(timer)->Start(milliseconds, oneShot != 0) ? 1 : 0; });
}

WXK_FUNCTION(void) wxk_Timer_Stop(wxk_Timer* timer) noexcept {
	Guard([&] { Unwrap(timer)->Stop(); });
}

WXK_FUNCTION(uint8_t) wxk_Timer_IsRunning(wxk_Timer* timer) noexcept {
	return wxk::Guard<uint8_t>(0, [&]() -> uint8_t { return Unwrap(timer)->IsRunning() ? 1 : 0; });
}

WXK_FUNCTION(void) wxk_Timer_Free(wxk_Timer* timer) noexcept {
	Guard([&] { delete Unwrap(timer); });
}
