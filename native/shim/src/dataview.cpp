#include "shim.h"

#include <wx/dataview.h>

namespace {

wxk_DataViewModelCallbacks treeCallbacks{};
wxk_VirtualListModelCallbacks listCallbacks{};

wxDataViewModel* Unwrap(wxk_DataViewModel* model) {
	return reinterpret_cast<wxDataViewModel*>(model);
}

wxDataViewVirtualListModel* UnwrapList(wxk_DataViewModel* model) {
	return static_cast<wxDataViewVirtualListModel*>(Unwrap(model));
}

wxk_DataViewModel* Wrap(wxDataViewModel* model) {
	return reinterpret_cast<wxk_DataViewModel*>(model);
}

// A model whose items, values, and hierarchy come from .NET. An item is the id .NET gave it, and
// the invisible root is the null item. The model is reference counted: .NET holds one reference,
// and each control it is associated with holds another.
class ManagedDataViewModel final : public wxDataViewModel {
public:
	explicit ManagedDataViewModel(void* userdata) : _userdata(userdata) {}

	~ManagedDataViewModel() override {
		if (treeCallbacks.onFree != nullptr) treeCallbacks.onFree(_userdata);
	}

	void GetValue(wxVariant& variant, const wxDataViewItem& item, unsigned int col) const override {
		if (treeCallbacks.getValue != nullptr) treeCallbacks.getValue(_userdata, item.GetID(), col, wxk::Wrap(&variant));
	}

	bool SetValue(const wxVariant& variant, const wxDataViewItem& item, unsigned int col) override {
		return treeCallbacks.setValue != nullptr && treeCallbacks.setValue(_userdata, wxk::Wrap(const_cast<wxVariant*>(&variant)), item.GetID(), col) != 0;
	}

	wxDataViewItem GetParent(const wxDataViewItem& item) const override {
		return wxDataViewItem(treeCallbacks.getParent != nullptr ? treeCallbacks.getParent(_userdata, item.GetID()) : nullptr);
	}

	bool IsContainer(const wxDataViewItem& item) const override {
		return treeCallbacks.isContainer != nullptr && treeCallbacks.isContainer(_userdata, item.GetID()) != 0;
	}

	unsigned int GetChildren(const wxDataViewItem& item, wxDataViewItemArray& children) const override {
		children.clear();
		if (treeCallbacks.getChildren != nullptr) treeCallbacks.getChildren(_userdata, item.GetID(), reinterpret_cast<wxk_DataViewItems*>(&children));
		return static_cast<unsigned int>(children.size());
	}

	bool IsEnabled(const wxDataViewItem& item, unsigned int col) const override {
		return treeCallbacks.isEnabled == nullptr || treeCallbacks.isEnabled(_userdata, item.GetID(), col) != 0;
	}

	bool HasContainerColumns(const wxDataViewItem& item) const override {
		return treeCallbacks.hasContainerColumns != nullptr && treeCallbacks.hasContainerColumns(_userdata, item.GetID()) != 0;
	}

private:
	void* _userdata;
};

// A list whose rows .NET produces as the control shows them, so it can have millions. The model
// only knows how many rows there are.
class ManagedVirtualListModel final : public wxDataViewVirtualListModel {
public:
	ManagedVirtualListModel(void* userdata, unsigned int count) : wxDataViewVirtualListModel(count), _userdata(userdata) {}

	~ManagedVirtualListModel() override {
		if (listCallbacks.onFree != nullptr) listCallbacks.onFree(_userdata);
	}

	void GetValueByRow(wxVariant& variant, unsigned int row, unsigned int col) const override {
		if (listCallbacks.getValue != nullptr) listCallbacks.getValue(_userdata, row, col, wxk::Wrap(&variant));
	}

	bool SetValueByRow(const wxVariant& variant, unsigned int row, unsigned int col) override {
		return listCallbacks.setValue != nullptr && listCallbacks.setValue(_userdata, wxk::Wrap(const_cast<wxVariant*>(&variant)), row, col) != 0;
	}

	bool IsEnabledByRow(unsigned int row, unsigned int col) const override {
		return listCallbacks.isEnabled == nullptr || listCallbacks.isEnabled(_userdata, row, col) != 0;
	}

private:
	void* _userdata;
};

} // namespace

WXK_FUNCTION(void) wxk_DataViewModel_SetCallbacks(const wxk_DataViewModelCallbacks* callbacks) noexcept {
	treeCallbacks = *callbacks;
}

WXK_FUNCTION(void) wxk_VirtualListModel_SetCallbacks(const wxk_VirtualListModelCallbacks* callbacks) noexcept {
	listCallbacks = *callbacks;
}

WXK_FUNCTION(wxk_DataViewModel*) wxk_ManagedDataViewModel_new(void* userdata) noexcept {
	return wxk::Guard<wxk_DataViewModel*>(nullptr, [&] { return Wrap(new ManagedDataViewModel(userdata)); });
}

WXK_FUNCTION(wxk_DataViewModel*) wxk_ManagedVirtualListModel_new(void* userdata, uint32_t count) noexcept {
	return wxk::Guard<wxk_DataViewModel*>(nullptr, [&] { return Wrap(new ManagedVirtualListModel(userdata, count)); });
}

WXK_FUNCTION(void) wxk_DataViewModel_DecRef(wxk_DataViewModel* model) noexcept {
	wxk::Guard([&] { Unwrap(model)->DecRef(); });
}

WXK_FUNCTION(void) wxk_DataViewItems_Add(wxk_DataViewItems* items, wxk_ItemId item) noexcept {
	wxk::Guard([&] { reinterpret_cast<wxDataViewItemArray*>(items)->push_back(wxDataViewItem(wxk::ItemPointer(item))); });
}

WXK_FUNCTION(uint8_t) wxk_DataViewModel_ItemAdded(wxk_DataViewModel* model, wxk_ItemId parent, wxk_ItemId item) noexcept {
	return wxk::Guard<uint8_t>(0, [&]() -> uint8_t { return Unwrap(model)->ItemAdded(wxDataViewItem(wxk::ItemPointer(parent)), wxDataViewItem(wxk::ItemPointer(item))) ? 1 : 0; });
}

WXK_FUNCTION(uint8_t) wxk_DataViewModel_ItemDeleted(wxk_DataViewModel* model, wxk_ItemId parent, wxk_ItemId item) noexcept {
	return wxk::Guard<uint8_t>(0, [&]() -> uint8_t { return Unwrap(model)->ItemDeleted(wxDataViewItem(wxk::ItemPointer(parent)), wxDataViewItem(wxk::ItemPointer(item))) ? 1 : 0; });
}

WXK_FUNCTION(uint8_t) wxk_DataViewModel_ItemChanged(wxk_DataViewModel* model, wxk_ItemId item) noexcept {
	return wxk::Guard<uint8_t>(0, [&]() -> uint8_t { return Unwrap(model)->ItemChanged(wxDataViewItem(wxk::ItemPointer(item))) ? 1 : 0; });
}

WXK_FUNCTION(uint8_t) wxk_DataViewModel_ValueChanged(wxk_DataViewModel* model, wxk_ItemId item, uint32_t column) noexcept {
	return wxk::Guard<uint8_t>(0, [&]() -> uint8_t { return Unwrap(model)->ValueChanged(wxDataViewItem(wxk::ItemPointer(item)), column) ? 1 : 0; });
}

WXK_FUNCTION(uint8_t) wxk_DataViewModel_Cleared(wxk_DataViewModel* model) noexcept {
	return wxk::Guard<uint8_t>(0, [&]() -> uint8_t { return Unwrap(model)->Cleared() ? 1 : 0; });
}

WXK_FUNCTION(void) wxk_DataViewModel_Resort(wxk_DataViewModel* model) noexcept {
	wxk::Guard([&] { Unwrap(model)->Resort(); });
}

WXK_FUNCTION(void) wxk_VirtualListModel_Reset(wxk_DataViewModel* model, uint32_t count) noexcept {
	wxk::Guard([&] { UnwrapList(model)->Reset(count); });
}

WXK_FUNCTION(void) wxk_VirtualListModel_RowAppended(wxk_DataViewModel* model) noexcept {
	wxk::Guard([&] { UnwrapList(model)->RowAppended(); });
}

WXK_FUNCTION(void) wxk_VirtualListModel_RowPrepended(wxk_DataViewModel* model) noexcept {
	wxk::Guard([&] { UnwrapList(model)->RowPrepended(); });
}

WXK_FUNCTION(void) wxk_VirtualListModel_RowInserted(wxk_DataViewModel* model, uint32_t before) noexcept {
	wxk::Guard([&] { UnwrapList(model)->RowInserted(before); });
}

WXK_FUNCTION(void) wxk_VirtualListModel_RowDeleted(wxk_DataViewModel* model, uint32_t row) noexcept {
	wxk::Guard([&] { UnwrapList(model)->RowDeleted(row); });
}

WXK_FUNCTION(void) wxk_VirtualListModel_RowChanged(wxk_DataViewModel* model, uint32_t row) noexcept {
	wxk::Guard([&] { UnwrapList(model)->RowChanged(row); });
}

WXK_FUNCTION(void) wxk_VirtualListModel_RowValueChanged(wxk_DataViewModel* model, uint32_t row, uint32_t column) noexcept {
	wxk::Guard([&] { UnwrapList(model)->RowValueChanged(row, column); });
}

WXK_FUNCTION(uint32_t) wxk_VirtualListModel_GetCount(wxk_DataViewModel* model) noexcept {
	return wxk::Guard<uint32_t>(0, [&] { return UnwrapList(model)->GetCount(); });
}

WXK_FUNCTION(wxk_ItemId) wxk_VirtualListModel_GetItem(wxk_DataViewModel* model, uint32_t row) noexcept {
	return wxk::Guard<wxk_ItemId>(0, [&] { return wxk::ItemId(UnwrapList(model)->GetItem(row).GetID()); });
}

WXK_FUNCTION(uint32_t) wxk_VirtualListModel_GetRow(wxk_DataViewModel* model, wxk_ItemId item) noexcept {
	return wxk::Guard<uint32_t>(0, [&] { return UnwrapList(model)->GetRow(wxDataViewItem(wxk::ItemPointer(item))); });
}

WXK_FUNCTION(uint8_t) wxk_DataViewCtrl_DeleteColumn(wxk_Window* control, wxk_DataViewColumn* column) noexcept {
	return wxk::Guard<uint8_t>(0, [&]() -> uint8_t { return static_cast<wxDataViewCtrl*>(wxk::Unwrap(control))->DeleteColumn(wxk::Unwrap(column)) ? 1 : 0; });
}

WXK_FUNCTION(uint8_t) wxk_DataViewCtrl_ClearColumns(wxk_Window* control) noexcept {
	return wxk::Guard<uint8_t>(0, [&]() -> uint8_t { return static_cast<wxDataViewCtrl*>(wxk::Unwrap(control))->ClearColumns() ? 1 : 0; });
}

WXK_FUNCTION(uint8_t) wxk_DataViewCtrl_AssociateModel(wxk_Window* control, wxk_DataViewModel* model) noexcept {
	return wxk::Guard<uint8_t>(0, [&]() -> uint8_t { return static_cast<wxDataViewCtrl*>(wxk::Unwrap(control))->AssociateModel(Unwrap(model)) ? 1 : 0; });
}
