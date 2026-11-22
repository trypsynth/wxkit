#include "shim.h"
#include <wx/webview.h>

// The web view's members that generated code can't give .NET: creating it with a configuration,
// which says where its data goes, and pages and scripts. The shared library on Linux leaves this
// file out, and the web view's own library has it.

WXK_FUNCTION(uint8_t) wxk_WebView_IsAvailable(void) noexcept {
	return wxk::Guard<uint8_t>(0, [] { return wxWebView::IsBackendAvailable(wxWebViewBackendDefault) ? uint8_t(1) : uint8_t(0); });
}

WXK_FUNCTION(wxk_Window*) wxk_WebView_new(wxk_Window* parent, int32_t id, const char16_t* url, size_t urlLength, wxk_Point position, wxk_Size size, int64_t style, const char16_t* dataPath, size_t dataPathLength) noexcept {
	return wxk::Guard<wxk_Window*>(nullptr, [&]() -> wxk_Window* {
		wxWebViewConfiguration configuration = wxWebView::NewConfiguration(wxWebViewBackendDefault);
		if (dataPathLength > 0) configuration.SetDataPath(wxk::FromUtf16(dataPath, dataPathLength));
		wxWebView* view = wxWebView::New(configuration);
		if (view == nullptr) return nullptr;
		if (!view->Create(static_cast<wxWindow*>(wxk::Unwrap(parent)), static_cast<int>(id), wxk::FromUtf16(url, urlLength), wxk::FromPoint(position), wxk::FromSize(size), static_cast<long>(style))) {
			delete view;
			return nullptr;
		}
		return wxk::Wrap(static_cast<wxWindow*>(view));
	});
}

WXK_FUNCTION(void) wxk_WebView_LoadURL(wxk_Window* self, const char16_t* url, size_t urlLength) noexcept {
	wxk::Guard([&] { static_cast<wxWebView*>(wxk::Unwrap(self))->LoadURL(wxk::FromUtf16(url, urlLength)); });
}

WXK_FUNCTION(wxk_String*) wxk_WebView_GetCurrentURL(wxk_Window* self) noexcept {
	return wxk::Guard<wxk_String*>(nullptr, [&] { return wxk::ToUtf16(static_cast<wxWebView*>(wxk::Unwrap(self))->GetCurrentURL()); });
}

WXK_FUNCTION(void) wxk_WebView_SetPage(wxk_Window* self, const char16_t* html, size_t htmlLength, const char16_t* baseUrl, size_t baseUrlLength) noexcept {
	wxk::Guard([&] { static_cast<wxWebView*>(wxk::Unwrap(self))->SetPage(wxk::FromUtf16(html, htmlLength), wxk::FromUtf16(baseUrl, baseUrlLength)); });
}

// The script's result comes in a wxEVT_WEBVIEW_SCRIPT_RESULT event, whose client data is the
// number given here, which .NET matches to the task that waits for it.
WXK_FUNCTION(void) wxk_WebView_RunScriptAsync(wxk_Window* self, const char16_t* script, size_t scriptLength, int64_t request) noexcept {
	wxk::Guard([&] { static_cast<wxWebView*>(wxk::Unwrap(self))->RunScriptAsync(wxk::FromUtf16(script, scriptLength), reinterpret_cast<void*>(static_cast<intptr_t>(request))); });
}

WXK_FUNCTION(int64_t) wxk_WebViewEvent_GetRequest(wxk_Event* event) noexcept {
	return wxk::Guard<int64_t>(0, [&] { return static_cast<int64_t>(reinterpret_cast<intptr_t>(static_cast<wxCommandEvent*>(wxk::Unwrap(event))->GetClientData())); });
}
