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

WXK_FUNCTION(uint8_t) wxk_PrintDialogData_GetEnable(wxk_Object* data, int32_t part) noexcept {
	return wxk::Guard<uint8_t>(0, [&]() -> uint8_t {
		auto* dialogData = static_cast<wxPrintDialogData*>(wxk::Unwrap(data));
		switch (part) {
			case 0: return dialogData->GetEnablePrintToFile() ? 1 : 0;
			case 1: return dialogData->GetEnableSelection() ? 1 : 0;
			case 2: return dialogData->GetEnableCurrentPage() ? 1 : 0;
			case 3: return dialogData->GetEnablePageNumbers() ? 1 : 0;
			default: return dialogData->GetEnableHelp() ? 1 : 0;
		}
	});
}
