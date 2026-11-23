#include "shim.h"

#include <memory>

namespace {

// wxWidgets copies a bound functor, and deletes the copies when the binding goes away, so the
// callback's state is shared between them and released once, when the last copy is deleted.
struct Binding {
	wxk_EventCallback callback;
	void* userdata;
	wxk_Callback onFree;

	~Binding() {
		if (onFree != nullptr) onFree(userdata);
	}
};

class Functor {
public:
	explicit Functor(std::shared_ptr<Binding> binding) : _binding(std::move(binding)) {}

	void operator()(wxEvent& event) const {
		_binding->callback(_binding->userdata, wxk::Wrap(&event));
	}

private:
	std::shared_ptr<Binding> _binding;
};

} // namespace

WXK_FUNCTION(uint8_t) wxk_EvtHandler_Bind(wxk_EvtHandler* handler, int32_t eventType, int32_t id, int32_t lastId, wxk_EventCallback callback, void* userdata, wxk_Callback onFree) noexcept {
	return wxk::Guard<uint8_t>(0, [&]() -> uint8_t {
		auto binding = std::make_shared<Binding>(Binding{callback, userdata, nullptr});
		wxk::Unwrap(handler)->Bind(wxEventTypeTag<wxEvent>(eventType), Functor(binding), id, lastId);
		// Only once wxWidgets holds the functor does releasing it become its job.
		binding->onFree = onFree;
		return 1;
	});
}

// Can't throw: it reads a member.
WXK_FUNCTION(int32_t) wxk_Event_GetEventType(wxk_Event* event) noexcept {
	return wxk::Unwrap(event)->GetEventType();
}

WXK_FUNCTION(void) wxk_CloseEvent_Veto(wxk_Event* event, uint8_t veto) noexcept {
	wxk::Guard([&] { wxk::As<wxCloseEvent>(event)->Veto(veto != 0); });
}
