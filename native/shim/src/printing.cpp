#include "shim.h"

#include <wx/print.h>

WXK_FUNCTION(wxk_Window*) wxk_PrintPreview_Show(wxk_Object* printout, wxk_Object* printoutForPrinting, wxk_Object* data, wxk_Window* parent, const char16_t* title, size_t titleLength, wxk_Size size) noexcept {
	return wxk::Guard<wxk_Window*>(nullptr, [&]() -> wxk_Window* {
		auto* preview = new wxPrintPreview(static_cast<wxPrintout*>(wxk::Unwrap(printout)), printoutForPrinting != nullptr ? static_cast<wxPrintout*>(wxk::Unwrap(printoutForPrinting)) : nullptr, data != nullptr ? static_cast<wxPrintDialogData*>(wxk::Unwrap(data)) : nullptr);
		// A preview that is not ready, as when there is no printer, deletes its printouts with it.
		if (!preview->IsOk()) {
			delete preview;
			return nullptr;
		}
		auto* frame = new wxPreviewFrame(preview, wxk::Unwrap(parent), wxk::FromUtf16(title, titleLength), wxDefaultPosition, wxk::FromSize(size));
		frame->Initialize();
		frame->Show();
		return wxk::Wrap(static_cast<wxWindow*>(frame));
	});
}
