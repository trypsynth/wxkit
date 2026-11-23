#include "shim.h"

#include <wx/clntdata.h>
#include <wx/treectrl.h>

namespace {

void (*freeHandle)(void* handle) = nullptr;

// What both kinds of item data from .NET hold: the GCHandle of the object, which is freed with it.
class ManagedHandle {
public:
	explicit ManagedHandle(void* handle) : _handle(handle) {}
	ManagedHandle(const ManagedHandle&) = delete;
	ManagedHandle& operator=(const ManagedHandle&) = delete;
	virtual ~ManagedHandle() {
		if (freeHandle != nullptr) freeHandle(_handle);
	}
	void* Handle() const { return _handle; }
private:
	void* _handle;
};

// The data an item container, such as a list box or choice, keeps with an item.
class ManagedClientData final : public wxClientData, public ManagedHandle {
public:
	using ManagedHandle::ManagedHandle;
};

// The data a tree keeps with an item, which wxTreeCtrl needs to be a wxTreeItemData.
class ManagedTreeItemData final : public wxTreeItemData, public ManagedHandle {
public:
	using ManagedHandle::ManagedHandle;
};

}

namespace wxk {

wxClientData* ToClientData(void* handle) {
	return handle == nullptr ? nullptr : new ManagedClientData(handle);
}

wxTreeItemData* ToTreeItemData(void* handle) {
	return handle == nullptr ? nullptr : new ManagedTreeItemData(handle);
}

void* FromClientData(const wxClientData* data) {
	const auto* managed = dynamic_cast<const ManagedHandle*>(data);
	return managed == nullptr ? nullptr : managed->Handle();
}

}

WXK_FUNCTION(void) wxk_ClientData_SetFree(void (*free)(void* handle)) noexcept {
	freeHandle = free;
}

WXK_FUNCTION(void) wxk_TreeCtrl_SetItemData(wxk_Window* tree, wxk_ItemId item, void* handle) noexcept {
	wxk::Guard([&] {
		auto* control = static_cast<wxTreeCtrl*>(wxk::Unwrap(tree));
		wxTreeItemId id(wxk::ItemPointer(item));
		wxTreeItemData* old = control->GetItemData(id);
		control->SetItemData(id, wxk::ToTreeItemData(handle));
		delete old;
	});
}
