#include "shim.h"

// Speech for screen readers, from a live region: a window an application announces through,
// such as an empty label, as accessware/live-region does for wxDragon. Each platform's backend
// follows that crate's, which was tested against NVDA, VoiceOver, and Orca. The priorities are
// 0, 1, and 2, for low, medium, and high.

#ifdef __WXMSW__

#include <windows.h>
#include <commctrl.h>
#include <ole2.h>
#include <uiautomation.h>

#include <unordered_map>

#pragma comment(lib, "uiautomationcore.lib")
#pragma comment(lib, "comctl32.lib")
#pragma comment(lib, "oleaut32.lib")

namespace {

// Only has to be unique among the subclasses on a window.
constexpr UINT_PTR SubclassId = 0x77786b6c;

// The smallest server-side provider there can be: it identifies itself, supports no patterns,
// and leaves every property to the window's host provider, so the window keeps what it was. A
// notification needs one, since raising it on the provider UiaHostProviderFromHwnd gives
// succeeds and reaches no client at all.
class Announcer final : public IRawElementProviderSimple {
public:
	explicit Announcer(HWND hwnd) : _hwnd(hwnd) {}

	ULONG STDMETHODCALLTYPE AddRef() override { return static_cast<ULONG>(InterlockedIncrement(&_references)); }

	ULONG STDMETHODCALLTYPE Release() override {
		LONG left = InterlockedDecrement(&_references);
		if (left == 0) delete this;
		return static_cast<ULONG>(left);
	}

	HRESULT STDMETHODCALLTYPE QueryInterface(REFIID id, void** result) override {
		if (result == nullptr) return E_POINTER;
		if (id == __uuidof(IUnknown) || id == __uuidof(IRawElementProviderSimple)) {
			*result = static_cast<IRawElementProviderSimple*>(this);
			AddRef();
			return S_OK;
		}
		*result = nullptr;
		return E_NOINTERFACE;
	}

	HRESULT STDMETHODCALLTYPE get_ProviderOptions(ProviderOptions* options) override {
		*options = static_cast<ProviderOptions>(ProviderOptions_ServerSideProvider | ProviderOptions_UseComThreading);
		return S_OK;
	}

	HRESULT STDMETHODCALLTYPE GetPatternProvider(PATTERNID, IUnknown** pattern) override {
		*pattern = nullptr;
		return S_OK;
	}

	// An empty value leaves each property to the host provider.
	HRESULT STDMETHODCALLTYPE GetPropertyValue(PROPERTYID, VARIANT* value) override {
		value->vt = VT_EMPTY;
		return S_OK;
	}

	HRESULT STDMETHODCALLTYPE get_HostRawElementProvider(IRawElementProviderSimple** host) override {
		return UiaHostProviderFromHwnd(_hwnd, host);
	}

private:
	LONG _references = 1;
	HWND _hwnd;
};

// One provider for each live region's window, which the subclass hands out on every
// WM_GETOBJECT, as UI Automation holds on to it across events. The GUI thread owns them.
std::unordered_map<HWND, Announcer*> providers;

LRESULT CALLBACK SubclassProc(HWND hwnd, UINT message, WPARAM wParam, LPARAM lParam, UINT_PTR, DWORD_PTR) {
	if (message == WM_GETOBJECT && static_cast<LONG>(lParam) == static_cast<LONG>(UiaRootObjectId)) {
		auto found = providers.find(hwnd);
		if (found != providers.end()) return UiaReturnRawElementProvider(hwnd, wParam, lParam, found->second);
	}
	// The window's last message, so the provider and the subclass go with it.
	if (message == WM_NCDESTROY) {
		auto found = providers.find(hwnd);
		if (found != providers.end()) {
			found->second->Release();
			providers.erase(found);
		}
		RemoveWindowSubclass(hwnd, SubclassProc, SubclassId);
	}
	return DefSubclassProc(hwnd, message, wParam, lParam);
}

// The window's provider, which is made and served the first time, or null when it cannot be.
Announcer* Provider(HWND hwnd) {
	auto found = providers.find(hwnd);
	if (found != providers.end()) return found->second;
	// The provider is there before the subclass, since installing it can draw a WM_GETOBJECT.
	auto* provider = new Announcer(hwnd);
	providers.emplace(hwnd, provider);
	if (!SetWindowSubclass(hwnd, SubclassProc, SubclassId, 0)) {
		providers.erase(hwnd);
		provider->Release();
		return nullptr;
	}
	return provider;
}

// Low queues behind everything pending, medium lets what is being said finish and then drops
// anything staler, and high cuts off what is being said.
NotificationProcessing Processing(int32_t priority) {
	switch (priority) {
		case 0: return NotificationProcessing_All;
		case 1: return NotificationProcessing_CurrentThenMostRecent;
		default: return NotificationProcessing_ImportantMostRecent;
	}
}

} // namespace

WXK_FUNCTION(uint8_t) wxk_LiveRegion_Prepare(wxk_Window* window) noexcept {
	return wxk::Guard<uint8_t>(0, [&]() -> uint8_t {
		HWND hwnd = static_cast<HWND>(wxk::Unwrap(window)->GetHWND());
		return hwnd != nullptr && Provider(hwnd) != nullptr ? 1 : 0;
	});
}

WXK_FUNCTION(uint8_t) wxk_LiveRegion_Announce(wxk_Window* window, const char16_t* message, size_t messageLength, int32_t priority) noexcept {
	return wxk::Guard<uint8_t>(0, [&]() -> uint8_t {
		HWND hwnd = static_cast<HWND>(wxk::Unwrap(window)->GetHWND());
		Announcer* provider = hwnd != nullptr ? Provider(hwnd) : nullptr;
		// With no client listening, the event would be raised and reach no one, which is not
		// an announcement made.
		if (provider == nullptr || !UiaClientsAreListening()) return 0;
		BSTR text = SysAllocStringLen(reinterpret_cast<const OLECHAR*>(message), static_cast<UINT>(messageLength));
		BSTR activity = SysAllocString(L"");
		HRESULT result = UiaRaiseNotificationEvent(provider, NotificationKind_Other, Processing(priority), text, activity);
		SysFreeString(activity);
		SysFreeString(text);
		return SUCCEEDED(result) ? 1 : 0;
	});
}

#elif defined(__WXOSX__)

#include <CoreFoundation/CoreFoundation.h>
#include <dispatch/dispatch.h>
#include <objc/message.h>
#include <objc/runtime.h>

// AppKit's, which wxOSX links. An NSString and an NSDictionary are a CFString and a CFDictionary.
extern "C" void NSAccessibilityPostNotificationWithUserInfo(void* element, CFStringRef notification, CFDictionaryRef userInfo);

namespace {

struct PendingAnnouncement {
	CFStringRef message;
	long priority;
};

// Posted on the next pass of the main run loop, rather than at once, so that VoiceOver speech
// the current pass makes, such as for a caret move just after it, does not drown it out.
void PostAnnouncement(void* context) {
	auto* pending = static_cast<PendingAnnouncement*>(context);
	using SendId = void* (*)(void*, SEL);
	void* application = reinterpret_cast<SendId>(objc_msgSend)(reinterpret_cast<void*>(objc_getClass("NSApplication")), sel_registerName("sharedApplication"));
	CFNumberRef priority = CFNumberCreate(nullptr, kCFNumberLongType, &pending->priority);
	const void* keys[] = {CFSTR("AXAnnouncementKey"), CFSTR("AXPriorityKey")};
	const void* values[] = {pending->message, priority};
	CFDictionaryRef userInfo = CFDictionaryCreate(nullptr, keys, values, 2, &kCFTypeDictionaryKeyCallBacks, &kCFTypeDictionaryValueCallBacks);
	NSAccessibilityPostNotificationWithUserInfo(application, CFSTR("AXAnnouncementRequested"), userInfo);
	CFRelease(userInfo);
	CFRelease(priority);
	CFRelease(pending->message);
	delete pending;
}

} // namespace

// VoiceOver needs nothing set up.
WXK_FUNCTION(uint8_t) wxk_LiveRegion_Prepare(wxk_Window*) noexcept {
	return 1;
}

// NSAccessibilityPriorityLow, Medium, and High.
WXK_FUNCTION(uint8_t) wxk_LiveRegion_Announce(wxk_Window*, const char16_t* message, size_t messageLength, int32_t priority) noexcept {
	return wxk::Guard<uint8_t>(0, [&]() -> uint8_t {
		CFStringRef text = CFStringCreateWithCharacters(nullptr, reinterpret_cast<const UniChar*>(message), static_cast<CFIndex>(messageLength));
		if (text == nullptr) return 0;
		dispatch_async_f(dispatch_get_main_queue(), new PendingAnnouncement{text, priority == 0 ? 10L : priority == 1 ? 50L : 90L}, PostAnnouncement);
		return 1;
	});
}

#elif defined(__WXGTK__)

#include <cstddef>

// GTK's and GObject's, which wxGTK links. A GObject starts with its class, whose type is its first field.
extern "C" {
void* gtk_widget_get_accessible(void* widget);
unsigned int g_signal_lookup(const char* name, std::size_t type);
void g_signal_emit_by_name(void* instance, const char* signal, ...);
}

namespace {

// AtkLive's polite and assertive.
constexpr int AtkLivePolite = 1;
constexpr int AtkLiveAssertive = 2;

std::size_t TypeOf(void* instance) {
	auto* type = *static_cast<std::size_t**>(instance);
	return *type;
}

void* Accessible(wxk_Window* window) {
	void* widget = wxk::Unwrap(window)->GetHandle();
	return widget != nullptr ? gtk_widget_get_accessible(widget) : nullptr;
}

} // namespace

// AT-SPI keeps no live region setting, since each announcement carries its politeness, so this
// only says whether announcing can work.
WXK_FUNCTION(uint8_t) wxk_LiveRegion_Prepare(wxk_Window* window) noexcept {
	return wxk::Guard<uint8_t>(0, [&]() -> uint8_t { return Accessible(window) != nullptr ? 1 : 0; });
}

// ATK's notification signal, from 2.50, carries a politeness: polite for low and medium, and
// assertive for high. Before 2.50, the announcement signal carries none.
WXK_FUNCTION(uint8_t) wxk_LiveRegion_Announce(wxk_Window* window, const char16_t* message, size_t messageLength, int32_t priority) noexcept {
	return wxk::Guard<uint8_t>(0, [&]() -> uint8_t {
		void* accessible = Accessible(window);
		if (accessible == nullptr) return 0;
		wxScopedCharBuffer text = wxk::FromUtf16(message, messageLength).utf8_str();
		std::size_t type = TypeOf(accessible);
		if (g_signal_lookup("notification", type) != 0) {
			g_signal_emit_by_name(accessible, "notification", text.data(), priority >= 2 ? AtkLiveAssertive : AtkLivePolite);
			return 1;
		}
		if (g_signal_lookup("announcement", type) != 0) {
			g_signal_emit_by_name(accessible, "announcement", text.data());
			return 1;
		}
		return 0;
	});
}

#else

WXK_FUNCTION(uint8_t) wxk_LiveRegion_Prepare(wxk_Window*) noexcept {
	return 0;
}

WXK_FUNCTION(uint8_t) wxk_LiveRegion_Announce(wxk_Window*, const char16_t*, size_t, int32_t) noexcept {
	return 0;
}

#endif
