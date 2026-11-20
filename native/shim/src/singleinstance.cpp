#include "shim.h"

#include <wx/snglinst.h>

#include <memory>

struct wxk_SingleInstanceChecker {};

namespace {

wxSingleInstanceChecker* Unwrap(wxk_SingleInstanceChecker* checker) {
	return reinterpret_cast<wxSingleInstanceChecker*>(checker);
}

} // namespace

WXK_FUNCTION(wxk_SingleInstanceChecker*) wxk_SingleInstanceChecker_new(const char16_t* name, size_t nameLength, const char16_t* path, size_t pathLength) noexcept {
	return wxk::Guard<wxk_SingleInstanceChecker*>(nullptr, [&]() -> wxk_SingleInstanceChecker* {
		auto checker = std::make_unique<wxSingleInstanceChecker>();
		if (!checker->Create(wxk::FromUtf16(name, nameLength), path != nullptr ? wxk::FromUtf16(path, pathLength) : wxString())) return nullptr;
		return reinterpret_cast<wxk_SingleInstanceChecker*>(checker.release());
	});
}

WXK_FUNCTION(uint8_t) wxk_SingleInstanceChecker_IsAnotherRunning(wxk_SingleInstanceChecker* checker) noexcept {
	return wxk::Guard<uint8_t>(0, [&]() -> uint8_t { return Unwrap(checker)->IsAnotherRunning() ? 1 : 0; });
}

WXK_FUNCTION(void) wxk_SingleInstanceChecker_delete(wxk_SingleInstanceChecker* checker) noexcept {
	wxk::Guard([&] { delete Unwrap(checker); });
}
