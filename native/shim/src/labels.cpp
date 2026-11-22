#include "shim.h"
#include <wx/stattext.h>
#include <cstddef>

// A label names the control after it, as Windows has screen readers read it. wxGTK makes the
// label the control's mnemonic label when it next goes idle, which gives the control a labelled-by
// relation, but it gives a control made of more than one widget to the outer one: a list box's or
// a multi-line text box's scrolled window, or a slider's box. The widget that gets the focus, the
// list, the text, or the scale, then has none, and Orca reads it without its label. This adds the
// relation to the widget that gets the focus.

#ifdef __WXGTK__

// The shim is built without GTK's and ATK's headers, so the calls it makes are declared here.
extern "C" {
void* gtk_widget_get_accessible(void* widget);
int gtk_widget_get_can_focus(void* widget);
std::size_t gtk_container_get_type(void);
void* gtk_container_get_children(void* container);
int g_type_check_instance_is_a(void* instance, std::size_t type);
void g_list_free(void* list);
int atk_relation_type_for_name(const char* name);
int atk_object_add_relationship(void* accessible, int relationship, void* target);
}

namespace {

// GLib's GList, a node of a linked list.
struct ListNode {
	void* data;
	ListNode* next;
	ListNode* previous;
};

// The widget, or the first one inside it, that can take the focus.
void* Focusable(void* widget) {
	if (gtk_widget_get_can_focus(widget)) return widget;
	if (!g_type_check_instance_is_a(widget, gtk_container_get_type())) return nullptr;
	ListNode* children = static_cast<ListNode*>(gtk_container_get_children(widget));
	void* found = nullptr;
	for (ListNode* node = children; node != nullptr && found == nullptr; node = node->next) found = Focusable(node->data);
	g_list_free(children);
	return found;
}

}

WXK_FUNCTION(void) wxk_Window_LinkLabel(wxk_Window* window) noexcept {
	wxk::Guard([window] {
		wxWindow* control = static_cast<wxWindow*>(wxk::Unwrap(window));
		wxWindow* label = control->GetPrevSibling();
		if (label == nullptr || wxDynamicCast(label, wxStaticText) == nullptr) return;
		GtkWidget* connect = control->GetConnectWidget();
		void* inner = connect == nullptr ? nullptr : Focusable(connect);
		// wxGTK's own mnemonic names a control whose outer widget gets the focus, such as an entry.
		if (inner == nullptr || inner == control->GetHandle()) return;
		void* target = gtk_widget_get_accessible(inner);
		void* source = gtk_widget_get_accessible(label->GetHandle());
		if (target == nullptr || source == nullptr) return;
		atk_object_add_relationship(target, atk_relation_type_for_name("labelled-by"), source);
		atk_object_add_relationship(source, atk_relation_type_for_name("label-for"), target);
	});
}

#elif defined(__WXOSX__)

#include <CoreFoundation/CoreFoundation.h>
#include <objc/message.h>
#include <objc/runtime.h>

// In accessinfo.cpp: the element VoiceOver reads for a window, and whether it has a title.
void* wxkMacAccessibleElement(wxWindow* window);
bool wxkMacHasTitle(void* element);

// VoiceOver names an element by its title element, which nothing sets for a label before a
// control, so a text field or a list after a label is read without it. The label is made the
// title element of a control that has no title of its own, such as a button's, on the element
// VoiceOver reads, which for a cell-based control is its cell: through NSAccessibility's property,
// and the attribute override the older API reads, since the two aren't bridged for cells.
WXK_FUNCTION(void) wxk_Window_LinkLabel(wxk_Window* window) noexcept {
	wxk::Guard([window] {
		wxWindow* control = static_cast<wxWindow*>(wxk::Unwrap(window));
		wxWindow* label = control->GetPrevSibling();
		if (label == nullptr || wxDynamicCast(label, wxStaticText) == nullptr) return;
		void* target = wxkMacAccessibleElement(control);
		void* source = wxkMacAccessibleElement(label);
		if (target == nullptr || source == nullptr || wxkMacHasTitle(target)) return;
		Class type = object_getClass(static_cast<id>(target));
		SEL property = sel_registerName("setAccessibilityTitleUIElement:");
		if (class_respondsToSelector(type, property)) reinterpret_cast<void (*)(void*, SEL, void*)>(objc_msgSend)(target, property, source);
		SEL override = sel_registerName("accessibilitySetOverrideValue:forAttribute:");
		if (class_respondsToSelector(type, override)) reinterpret_cast<signed char (*)(void*, SEL, void*, const void*)>(objc_msgSend)(target, override, source, CFSTR("AXTitleUIElement"));
	});
}

#else

// Windows names a control by the label before it itself.
WXK_FUNCTION(void) wxk_Window_LinkLabel(wxk_Window*) noexcept {
}

#endif
