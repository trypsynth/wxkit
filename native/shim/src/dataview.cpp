#include "shim.h"

#include <wx/dataview.h>

// Adds a child to the array a model's GetChildren fills, whose director is generated. The array
// is a wxVector the interface headers don't give members of.
WXK_FUNCTION(void) wxk_DataViewItems_Add(wxk_DataViewItems* items, wxk_ItemId item) noexcept {
	wxk::Guard([&] { reinterpret_cast<wxDataViewItemArray*>(items)->push_back(wxDataViewItem(wxk::ItemPointer(item))); });
}
