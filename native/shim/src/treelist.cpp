#include "shim.h"

#include <wx/treelist.h>

// A tree list's selected items, which wxWidgets fills in a list it is given.
WXK_FUNCTION(wxk_Numbers*) wxk_TreeListCtrl_GetSelections(wxk_Window* control) noexcept {
	return wxk::Guard<wxk_Numbers*>(nullptr, [&] {
		wxTreeListItems items;
		static_cast<wxTreeListCtrl*>(wxk::Unwrap(control))->GetSelections(items);
		auto* numbers = new wxk_Numbers;
		for (const wxTreeListItem& item : items) numbers->values.push_back(reinterpret_cast<intptr_t>(item.GetID()));
		return numbers;
	});
}
