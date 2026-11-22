#include "shim.h"
#include <atomic>
#include <cstdio>

#ifdef __WXMSW__
	#include <wx/msw/private.h>
#endif
#ifdef __WXOSX__
	#include <pthread.h>
#endif
#ifdef __WXGTK__
// The shim is built without GTK's headers, so the calls it makes are declared here. A
// GtkTextDirection is none, left to right, or right to left.
extern "C" {
int gtk_widget_get_default_direction(void);
void gtk_widget_set_default_direction(int direction);
}
#endif

namespace {

// Where a failed wxWidgets assertion goes: .NET's Application.AssertFailed, once it registers.
std::atomic<wxk_AssertCallback> assertCallback{nullptr};
thread_local bool inAssert = false;

// Replaces wxWidgets' handler, which would show a dialog, for assertions on any thread, before the
// application runs too. One that fails while another is reported, such as in .NET's handler, is
// left out rather than recursing.
void OnAssert(const wxString& file, int line, const wxString& function, const wxString& condition, const wxString& message) {
	if (inAssert) return;
	inAssert = true;
	wxk_AssertCallback callback = assertCallback.load();
	if (callback != nullptr) {
		callback(wxk::ToUtf16(file), line, wxk::ToUtf16(function), wxk::ToUtf16(condition), wxk::ToUtf16(message));
	} else {
		wxString text = wxString::Format("wxWidgets assertion \"%s\" failed in %s at %s:%d: %s\n", condition, function, file, line, message);
#ifdef __WXMSW__
		::OutputDebugStringW(text.wc_str());
#else
		fputs(text.utf8_str(), stderr);
#endif
	}
	inAssert = false;
}

// Installed when the library loads, so that no assertion ever shows wxWidgets' dialog.
const struct AssertHandlerInstaller {
	AssertHandlerInstaller() {
		wxSetAssertHandler(OnAssert);
	}
} assertHandlerInstaller;

// The direction the application gives what has no window to take it from, such as a new frame or
// a message box with no parent, or wxLayout_Default to follow the UI locale.
wxLayoutDirection layoutDirection = wxLayout_Default;
#ifdef __WXGTK__
// GTK's own default, from its translations, which wxLayout_Default goes back to.
int gtkDirection = 0;

// GTK lays out a window it makes, such as a message box, in its default direction. It is set
// once GTK has started.
void ApplyGtkDirection() {
	if (gtkDirection == 0) gtkDirection = gtk_widget_get_default_direction();
	gtk_widget_set_default_direction(layoutDirection == wxLayout_RightToLeft ? 2 : layoutDirection == wxLayout_LeftToRight ? 1 : gtkDirection);
}
#endif

class App final : public wxApp {
public:
	App(wxk_AppInitCallback onInit, void* userdata) : _onInit(onInit), _userdata(userdata) {}

	// wxApp::OnInit parses the command line, which belongs to .NET, so it is not called.
	bool OnInit() override {
#ifdef __WXGTK__
		if (layoutDirection != wxLayout_Default) ApplyGtkDirection();
#endif
		return _onInit(_userdata) != 0;
	}

	wxLayoutDirection GetLayoutDirection() const override {
		return layoutDirection != wxLayout_Default ? layoutDirection : wxApp::GetLayoutDirection();
	}

private:
	wxk_AppInitCallback _onInit;
	void* _userdata;
};

#ifdef __WXMSW__
// wxWidgets finds its resources in the module it is told it runs in, and otherwise assumes the
// executable. That is right when it is linked into the application, but not when it is inside
// wxkitnative.dll, so the module is looked up from an address in this file either way.
HINSTANCE ThisModule() {
	HMODULE module = nullptr;
	::GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT, reinterpret_cast<LPCWSTR>(&ThisModule), &module);
	return module;
}
#endif

} // namespace

WXK_FUNCTION(int32_t) wxk_App_Run(wxk_AppInitCallback onInit, void* userdata) noexcept {
	return wxk::Guard(-1, [&] {
#ifdef __WXMSW__
		wxSetInstance(ThisModule());
#endif
		wxApp::SetInstance(new App(onInit, userdata));
		int argc = 0;
		char* argv[] = {nullptr};
		return static_cast<int32_t>(wxEntry(argc, argv));
	});
}

WXK_FUNCTION(uint8_t) wxk_App_CanRunOnThisThread(void) noexcept {
#ifdef __WXOSX__
	// Cocoa only runs a user interface on the process's main thread.
	return pthread_main_np() != 0 ? 1 : 0;
#else
	return 1;
#endif
}

WXK_FUNCTION(uint8_t) wxk_App_CallAfter(wxk_Callback callback, void* userdata) noexcept {
	return wxk::Guard<uint8_t>(0, [&]() -> uint8_t {
		if (wxTheApp == nullptr) return 0;
		wxTheApp->CallAfter([callback, userdata] { callback(userdata); });
		return 1;
	});
}

WXK_FUNCTION(void) wxk_App_ExitMainLoop(void) noexcept {
	wxk::Guard([] {
		if (wxTheApp != nullptr) wxTheApp->ExitMainLoop();
	});
}

WXK_FUNCTION(void) wxk_App_SetAssertCallback(wxk_AssertCallback callback) noexcept {
	assertCallback.store(callback);
}

WXK_FUNCTION(void) wxk_App_SetLayoutDirection(int32_t direction) noexcept {
	wxk::Guard([direction] {
		layoutDirection = static_cast<wxLayoutDirection>(direction);
#ifdef __WXGTK__
		if (wxTheApp != nullptr) ApplyGtkDirection();
#endif
	});
}

WXK_FUNCTION(wxk_String*) wxk_Version_GetString(void) noexcept {
	return wxk::Guard<wxk_String*>(nullptr, [] { return wxk::ToUtf16(wxVERSION_STRING); });
}
