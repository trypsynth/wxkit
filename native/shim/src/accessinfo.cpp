#include "shim.h"
#include <wx/access.h>
#include <utility>

// What the platform's screen readers learn of a window: its role, as one of wxAccRole's, and the
// platform's own, its name, and its states. It asks the platform's accessibility API, as a screen
// reader would, rather than WxKit, so a test of it shows what NVDA, VoiceOver, or Orca reads:
// MSAA on Windows, NSAccessibility on macOS, and ATK on Linux.

namespace {

// The roles, numbered as wxAccRole numbers them, and so WxKit's AccessibleRole, which wxWidgets only
// defines where it builds its accessibility support, on Windows.
enum class Role : int32_t {
	None = 0,
	Pushbutton = 44,
	Checkbutton = 12,
	Radiobutton = 45,
	Text = 56,
	Statictext = 53,
	Combobox = 17,
	Slider = 50,
	Progressbar = 42,
	List = 32,
	Outline = 37,
	Grouping = 27,
	Window = 61,
	Pagetablist = 40,
	Link = 31,
	Spinbutton = 52,
	Menubar = 34,
	Toolbar = 58,
	Graphic = 25,
	Pane = 41,
	Dialog = 21,
	Separator = 49,
	Statusbar = 54,
};

struct Found {
	Role role = Role::None;
	wxString platformRole;
	wxString name;
	uint32_t states = 0;
};

// The states, as bits.
constexpr uint32_t Enabled = 1;
constexpr uint32_t Focused = 2;
constexpr uint32_t Checked = 4;

}

#ifdef __WXMSW__

#include <oleacc.h>
#pragma comment(lib, "oleacc.lib")

// wxWidgets' own, in its MSAA code, which the static library links in.
int wxConvertToWindowsRole(wxAccRole role);

namespace {

wxAccRole FromWindowsRole(long role) {
	for (int candidate = wxROLE_NONE; candidate <= wxROLE_SYSTEM_WINDOW; candidate++) {
		if (wxConvertToWindowsRole(static_cast<wxAccRole>(candidate)) == role) return static_cast<wxAccRole>(candidate);
	}
	return wxROLE_NONE;
}

bool Describe(wxWindow* window, Found& found) {
	IAccessible* accessible = nullptr;
	if (FAILED(::AccessibleObjectFromWindow(window->GetHWND(), static_cast<DWORD>(OBJID_CLIENT), IID_IAccessible, reinterpret_cast<void**>(&accessible))) || accessible == nullptr) return false;
	VARIANT self;
	::VariantInit(&self);
	self.vt = VT_I4;
	self.lVal = CHILDID_SELF;
	VARIANT role;
	::VariantInit(&role);
	if (SUCCEEDED(accessible->get_accRole(self, &role)) && role.vt == VT_I4) {
		found.role = static_cast<Role>(FromWindowsRole(role.lVal));
		wchar_t text[128] = {};
		if (::GetRoleTextW(static_cast<DWORD>(role.lVal), text, 128) > 0) found.platformRole = text;
	}
	::VariantClear(&role);
	BSTR name = nullptr;
	if (SUCCEEDED(accessible->get_accName(self, &name)) && name != nullptr) {
		found.name = name;
		::SysFreeString(name);
	}
	VARIANT state;
	::VariantInit(&state);
	if (SUCCEEDED(accessible->get_accState(self, &state)) && state.vt == VT_I4) {
		if ((state.lVal & STATE_SYSTEM_UNAVAILABLE) == 0) found.states |= Enabled;
		if (state.lVal & STATE_SYSTEM_FOCUSED) found.states |= Focused;
		if (state.lVal & STATE_SYSTEM_CHECKED) found.states |= Checked;
	}
	::VariantClear(&state);
	accessible->Release();
	return true;
}

}

#elif defined(__WXOSX__)

#include <CoreFoundation/CoreFoundation.h>
#include <objc/message.h>
#include <objc/runtime.h>

// AppKit's, declared here, since the shim is C++, not Objective-C: the element a screen reader
// reads for a view, which for a control is often its cell.
extern "C" void* NSAccessibilityUnignoredDescendant(void* element);

namespace {

using Object = void*;

bool Responds(Object target, const char* selector) {
	return target != nullptr && class_respondsToSelector(object_getClass(static_cast<id>(target)), sel_registerName(selector));
}

Object Send(Object target, const char* selector) {
	if (!Responds(target, selector)) return nullptr;
	return reinterpret_cast<Object (*)(Object, SEL)>(objc_msgSend)(target, sel_registerName(selector));
}

// An attribute of the accessibility API VoiceOver reads, by its name, such as AXRole.
Object Attribute(Object element, const char* name) {
	if (!Responds(element, "accessibilityAttributeValue:")) return nullptr;
	CFStringRef key = CFStringCreateWithCString(kCFAllocatorDefault, name, kCFStringEncodingUTF8);
	Object value = reinterpret_cast<Object (*)(Object, SEL, Object)>(objc_msgSend)(element, sel_registerName("accessibilityAttributeValue:"), const_cast<void*>(static_cast<const void*>(key)));
	CFRelease(key);
	return value;
}

wxString Text(Object value) {
	if (value == nullptr || !Responds(value, "UTF8String")) return wxString();
	const char* utf8 = reinterpret_cast<const char* (*)(Object, SEL)>(objc_msgSend)(value, sel_registerName("UTF8String"));
	return utf8 == nullptr ? wxString() : wxString::FromUTF8(utf8);
}

bool Flag(Object value) {
	return value != nullptr && Responds(value, "boolValue") && reinterpret_cast<signed char (*)(Object, SEL)>(objc_msgSend)(value, sel_registerName("boolValue")) != 0;
}

Role FromAXRole(const wxString& role) {
	static const std::pair<const char*, Role> roles[] = {
		{"AXButton", Role::Pushbutton}, {"AXCheckBox", Role::Checkbutton}, {"AXRadioButton", Role::Radiobutton},
		{"AXTextField", Role::Text}, {"AXTextArea", Role::Text}, {"AXStaticText", Role::Statictext},
		{"AXPopUpButton", Role::Combobox}, {"AXComboBox", Role::Combobox}, {"AXSlider", Role::Slider},
		{"AXProgressIndicator", Role::Progressbar}, {"AXList", Role::List}, {"AXTable", Role::List},
		{"AXOutline", Role::Outline}, {"AXGroup", Role::Grouping}, {"AXWindow", Role::Window},
		{"AXTabGroup", Role::Pagetablist}, {"AXLink", Role::Link}, {"AXIncrementor", Role::Spinbutton},
		{"AXMenuBar", Role::Menubar}, {"AXToolbar", Role::Toolbar}, {"AXImage", Role::Graphic},
		{"AXScrollArea", Role::Pane}, {"AXSplitGroup", Role::Pane},
	};
	for (const auto& [name, value] : roles) {
		if (role == name) return value;
	}
	return Role::None;
}

// The element VoiceOver reads for a window: a list box's or a multi-line text box's is the
// control inside its scroll view, and a control's is often its cell.
Object Element(wxWindow* window) {
	Object view = window->GetHandle();
	if (view == nullptr) return nullptr;
	if (Text(Attribute(view, "AXRole")) == "AXScrollArea" && Send(view, "documentView") != nullptr) view = Send(view, "documentView");
	Object element = NSAccessibilityUnignoredDescendant(view);
	return element != nullptr ? element : view;
}

bool Describe(wxWindow* window, Found& found) {
	Object element = Element(window);
	if (element == nullptr) return false;
	found.platformRole = Text(Attribute(element, "AXRole"));
	found.role = FromAXRole(found.platformRole);
	// VoiceOver names an element by its title, its description, or the element that titles it,
	// such as the label before a text field.
	found.name = Text(Attribute(element, "AXTitle"));
	if (found.name.empty()) found.name = Text(Attribute(element, "AXDescription"));
	if (found.name.empty()) found.name = Text(Attribute(Send(element, "accessibilityTitleUIElement"), "AXValue"));
	if (found.name.empty()) found.name = Text(Attribute(Attribute(element, "AXTitleUIElement"), "AXValue"));
	// A label's text is its value.
	if (found.name.empty() && found.role == Role::Statictext) found.name = Text(Attribute(element, "AXValue"));
	// An element without the attribute, such as a text area, can't be disabled.
	Object enabled = Attribute(element, "AXEnabled");
	if (enabled == nullptr || Flag(enabled)) found.states |= Enabled;
	if (Flag(Attribute(element, "AXFocused"))) found.states |= Focused;
	if (found.role == Role::Checkbutton || found.role == Role::Radiobutton) {
		Object value = Attribute(element, "AXValue");
		if (value != nullptr && Responds(value, "intValue") && reinterpret_cast<int (*)(Object, SEL)>(objc_msgSend)(value, sel_registerName("intValue")) == 1) found.states |= Checked;
	}
	return true;
}

}

// For labels.cpp: the element VoiceOver reads for a window, and its title.
void* wxkMacAccessibleElement(wxWindow* window) {
	return Element(window);
}

bool wxkMacHasTitle(void* element) {
	return !Text(Attribute(element, "AXTitle")).empty();
}

#elif defined(__WXGTK__)

// The shim is built without GTK's and ATK's headers, so the calls it makes are declared here.
extern "C" {
void* gtk_widget_get_accessible(void* widget);
int atk_object_get_role(void* accessible);
const char* atk_role_get_name(int role);
const char* atk_object_get_name(void* accessible);
void* atk_object_ref_state_set(void* accessible);
int atk_state_set_contains_state(void* set, int state);
int atk_state_type_for_name(const char* name);
void* atk_object_ref_relation_set(void* accessible);
int atk_relation_type_for_name(const char* name);
void* atk_relation_set_get_relation_by_type(void* set, int type);
void* atk_relation_get_target(void* relation);
int atk_object_get_n_accessible_children(void* accessible);
void* atk_object_ref_accessible_child(void* accessible, int index);
void g_object_unref(void* object);
}

// GLib's GPtrArray, whose first members are its items and their count.
struct PointerArray {
	void** items;
	unsigned int count;
};

namespace {

Role FromAtkRole(const wxString& role) {
	static const std::pair<const char*, Role> roles[] = {
		{"push button", Role::Pushbutton}, {"toggle button", Role::Pushbutton}, {"check box", Role::Checkbutton},
		{"radio button", Role::Radiobutton}, {"text", Role::Text}, {"entry", Role::Text},
		{"password text", Role::Text}, {"label", Role::Statictext}, {"combo box", Role::Combobox},
		{"slider", Role::Slider}, {"progress bar", Role::Progressbar}, {"list", Role::List},
		{"list box", Role::List}, {"table", Role::List}, {"tree table", Role::Outline},
		{"tree", Role::Outline}, {"panel", Role::Pane}, {"filler", Role::Pane},
		{"scroll pane", Role::Pane}, {"frame", Role::Window}, {"dialog", Role::Dialog},
		{"page tab list", Role::Pagetablist}, {"link", Role::Link}, {"spin button", Role::Spinbutton},
		{"menu bar", Role::Menubar}, {"tool bar", Role::Toolbar}, {"image", Role::Graphic},
		{"separator", Role::Separator}, {"status bar", Role::Statusbar},
	};
	for (const auto& [name, value] : roles) {
		if (role == name) return value;
	}
	return Role::None;
}

wxString RoleName(void* accessible) {
	const char* role = atk_role_get_name(atk_object_get_role(accessible));
	return role == nullptr ? wxString() : wxString::FromUTF8(role);
}

wxString Name(void* accessible) {
	const char* name = atk_object_get_name(accessible);
	return name == nullptr ? wxString() : wxString::FromUTF8(name);
}

// The name of the label an object is labelled by, which Orca reads for one without a name of its
// own, as GTK gives a label's mnemonic widget.
wxString LabelledBy(void* accessible) {
	wxString name;
	void* relations = atk_object_ref_relation_set(accessible);
	if (relations == nullptr) return name;
	void* relation = atk_relation_set_get_relation_by_type(relations, atk_relation_type_for_name("labelled-by"));
	PointerArray* targets = relation == nullptr ? nullptr : static_cast<PointerArray*>(atk_relation_get_target(relation));
	if (targets != nullptr && targets->count > 0) name = Name(targets->items[0]);
	g_object_unref(relations);
	return name;
}

bool Describe(wxWindow* window, Found& found) {
	// The widget a reader reads, such as a multi-line text box's text view inside its scrolled window.
	void* widget = window->GetConnectWidget();
	if (widget == nullptr) widget = window->GetHandle();
	void* accessible = widget == nullptr ? nullptr : gtk_widget_get_accessible(widget);
	if (accessible == nullptr) return false;
	// A box around the control, such as a slider's around its scale, which a reader goes into.
	void* inner = nullptr;
	if (RoleName(accessible) == "filler") {
		for (int index = 0; index < atk_object_get_n_accessible_children(accessible) && inner == nullptr; index++) {
			void* child = atk_object_ref_accessible_child(accessible, index);
			if (child == nullptr) continue;
			wxString childRole = RoleName(child);
			if (childRole != "filler" && childRole != "label") inner = child;
			else g_object_unref(child);
		}
		if (inner != nullptr) accessible = inner;
	}
	found.platformRole = RoleName(accessible);
	found.role = FromAtkRole(found.platformRole);
	found.name = Name(accessible);
	if (found.name.empty()) found.name = LabelledBy(accessible);
	void* states = atk_object_ref_state_set(accessible);
	if (states != nullptr) {
		if (atk_state_set_contains_state(states, atk_state_type_for_name("enabled"))) found.states |= Enabled;
		if (atk_state_set_contains_state(states, atk_state_type_for_name("focused"))) found.states |= Focused;
		if (atk_state_set_contains_state(states, atk_state_type_for_name("checked"))) found.states |= Checked;
		g_object_unref(states);
	}
	if (inner != nullptr) g_object_unref(inner);
	return true;
}

}

#else

namespace {

bool Describe(wxWindow*, Found&) {
	return false;
}

}

#endif

WXK_FUNCTION(uint8_t) wxk_AccessibilityInfo_Get(wxk_Window* window, int32_t* role, uint32_t* states, wxk_String** name, wxk_String** platformRole) noexcept {
	return wxk::Guard<uint8_t>(0, [&]() -> uint8_t {
		Found found;
		if (!Describe(static_cast<wxWindow*>(wxk::Unwrap(window)), found)) return 0;
		*role = static_cast<int32_t>(found.role);
		*states = found.states;
		*name = wxk::ToUtf16(found.name);
		*platformRole = wxk::ToUtf16(found.platformRole);
		return 1;
	});
}
