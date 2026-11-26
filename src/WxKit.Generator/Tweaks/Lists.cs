namespace WxKit.Generator.Tweaks;

// Lists and trees, data views, and the controls made of others that show them.
internal static class Lists {
	public static void Apply(Binding.Tweaks tweaks) {
		// A virtual list asks for each row as it shows it, through members C# overrides. A getter
		// that can fail returns whether it could, as .NET's Try methods do.
		// A list's items and columns, and a text control's positions and lines, are ints, as .NET's
		// indexes are, where wxWidgets has longs.
		foreach (string indexed in (string[])["wxListCtrl", "wxListView", "wxListEvent", "wxListItem", "wxTextCtrl", "wxComboBox", "wxBitmapComboBox", "wxSearchCtrl", "wxSpinCtrl", "wxStyledTextCtrl"]) tweaks.Class(indexed, value => value.IntIndexes());
		// What wxWidgets takes as numbers, and names with constants, are enums.
		tweaks.ConstantEnum("ListItemState", "wxLIST_STATE_", isFlags: true);
		tweaks.ConstantEnum("ListItemMask", "wxLIST_MASK_", isFlags: true);
		tweaks.ConstantEnum("ListNextItem", "wxLIST_NEXT_");
		tweaks.ConstantEnum("ListArrangement", "wxLIST_ALIGN_");
		tweaks.ConstantEnum("ListRect", "wxLIST_RECT_");
		tweaks.ConstantEnum("ListFindDirection", "wxLIST_FIND_");
		tweaks.Class("wxListCtrl", list => list
			.ParameterEnum("SetItemState", "state", "ListItemState")
			.ParameterEnum("SetItemState", "stateMask", "ListItemState")
			.ParameterEnum("GetItemState", "stateMask", "ListItemState")
			.ParameterEnum("GetItemState", "return", "ListItemState")
			.ParameterEnum("GetNextItem", "geometry", "ListNextItem")
			.ParameterEnum("GetNextItem", "state", "ListItemState")
			.ParameterEnum("Arrange", "flag", "ListArrangement")
			.ParameterEnum("GetItemRect", "code", "ListRect")
			.ParameterEnum("GetSubItemRect", "code", "ListRect")
			.ParameterEnum("FindItem", "direction", "ListFindDirection")
			.ParameterEnum("InsertColumn", "format", "wxListColumnFormat")
			.ParameterEnum("SetSingleStyle", "style", "ListCtrlStyle")
			.ParameterEnum("SetWindowStyleFlag", "style", "ListCtrlStyle")
			.Director("OnGetItemText", "OnGetItemImage", "OnGetItemColumnImage", "OnGetItemIsChecked")
			// SetItemData takes the same data.
			.Ignore("SetItemPtrData")
			.Rename("GetItemRect", "TryGetItemRect")
			.Rename("GetSubItemRect", "TryGetSubItemRect"));
		tweaks.Class("wxListView");
		// SetData stays a method beside the data, as it was before the data could be read.
		tweaks.Class("wxListItem", item => item.ValueObject().Property("Data", "GetData")
			.ParameterEnum("GetState", "return", "ListItemState")
			.ParameterEnum("SetState", "state", "ListItemState")
			.ParameterEnum("SetStateMask", "stateMask", "ListItemState")
			.ParameterEnum("GetMask", "return", "ListItemMask")
			.ParameterEnum("SetMask", "mask", "ListItemMask"));
		tweaks.Class("wxListEvent", e => e.ParameterEnum("GetMask", "return", "ListItemMask"));
		// SetItemData is written by hand, since wxTreeCtrl does not delete the data it replaces.
		// SortChildren asks OnCompareItems for the order, which wxMSW only does of a class that isn't
		// wxTreeCtrl itself, so the director is one.
		tweaks.Class("wxTreeCtrl", tree => tree
			.Director("OnCompareItems")
			.DirectorClassInfo()
			.Ignore("SetItemData", "OnCompareItems")
			.ParameterName("GetSelections", "selection", "selections")
			.Rename("GetBoundingRect", "TryGetBoundingRect"));
		tweaks.Class("wxTreeEvent");
		// Data views show a model, whose classes are hand-written, in DataViewModel. A column is made
		// by adding one, since renderers are not bound, so the members that add a column object are
		// left out. Deleting columns is written by hand, since the control marks their objects destroyed.
		tweaks.Enum("wxDataViewColumnFlags", "DataViewColumnFlags", isFlags: true);
		tweaks.Class("wxDataViewColumn", column => column
			.InheritUnboundBases()
			.NoConstructors()
			.Ignore("GetRenderer", "SetFlag", "ClearFlag", "ToggleFlag", "ChangeFlag", "HasFlag")
			.ParameterEnum("SetFlags", "flags", "wxDataViewColumnFlags")
			.ParameterEnum("GetFlags", "return", "wxDataViewColumnFlags"));
		tweaks.Class("wxDataViewCtrl", view => view
			.ParameterName("GetSelections", "sel", "selections")
			.ParameterName("SetSelections", "sel", "selections")
			.DiscardUnboundReturns()
			.Internal("AssociateModel", "DeleteColumn", "ClearColumns")
			.Ignore("GetModel", "AppendColumn", "PrependColumn", "InsertColumn", "GetSortingColumns")
			.ParameterEnum("AppendTextColumn", "flags", "wxDataViewColumnFlags")
			.ParameterEnum("AppendToggleColumn", "flags", "wxDataViewColumnFlags")
			.ParameterEnum("AppendProgressColumn", "flags", "wxDataViewColumnFlags")
			.ParameterEnum("AppendIconTextColumn", "flags", "wxDataViewColumnFlags")
			.ParameterEnum("AppendBitmapColumn", "flags", "wxDataViewColumnFlags")
			.ParameterEnum("AppendDateColumn", "flags", "wxDataViewColumnFlags")
			.ParameterEnum("PrependTextColumn", "flags", "wxDataViewColumnFlags")
			.ParameterEnum("PrependToggleColumn", "flags", "wxDataViewColumnFlags")
			.ParameterEnum("PrependProgressColumn", "flags", "wxDataViewColumnFlags")
			.ParameterEnum("PrependIconTextColumn", "flags", "wxDataViewColumnFlags")
			.ParameterEnum("PrependBitmapColumn", "flags", "wxDataViewColumnFlags")
			.ParameterEnum("PrependDateColumn", "flags", "wxDataViewColumnFlags"));
		tweaks.Class("wxDataViewListCtrl", view => view
			.DiscardUnboundReturns()
			.Ignore("GetStore", "AppendColumn", "PrependColumn", "InsertColumn")
			.ParameterEnum("AppendTextColumn", "flags", "wxDataViewColumnFlags")
			.ParameterEnum("AppendToggleColumn", "flags", "wxDataViewColumnFlags")
			.ParameterEnum("AppendProgressColumn", "flags", "wxDataViewColumnFlags")
			.ParameterEnum("AppendIconTextColumn", "flags", "wxDataViewColumnFlags"));
		// Its items' icons are indexes into images, which wxWithImages gives it, though the interface
		// headers leave that base out. Its image list, which is not bound, is the old way to give them.
		tweaks.Class("wxDataViewTreeCtrl", view => view.Bases("wxDataViewCtrl", "wxWithImages").DiscardUnboundReturns().Ignore("GetStore", "GetImageList", "SetImageList", "GetUpdatedImageListFor"));
		// The interface headers document SetItem, which the real wxDataViewEvent does not have. Events
		// are only received, so the column is only read.
		tweaks.Class("wxDataViewEvent", dataViewEvent => dataViewEvent
			.Ignore("SetItem", "SetDataViewColumn")
			.ParameterEnum("SetDragFlags", "flags", "DragFlags")
			.ParameterEnum("GetDragFlags", "return", "DragFlags"));
		tweaks.Class("wxDataViewIconText").ValueObject();
		// The hand-written DataViewModel and VirtualListModel make directors, whose callbacks they
		// give. A model is reference counted, so .NET releases its reference with DecRef. The
		// virtual list model's base, wxDataViewListModel, answers the tree model's questions from
		// rows, so its director only overrides the rows' members.
		tweaks.Class("wxDataViewModel", model => model
			.Handwritten()
			.Plain()
			.InheritUnboundBases()
			.Internal("wxDataViewModel", "DecRef", "ItemAdded", "ItemDeleted", "ItemChanged", "ValueChanged", "Cleared", "Resort")
			.InternalDirector("GetValue", "SetValue", "GetParent", "IsContainer", "GetChildren", "IsEnabled", "HasContainerColumns"));
		tweaks.Class("wxDataViewVirtualListModel", model => model
			.Handwritten()
			.Plain("wxDataViewModel")
			.NoBase()
			.InheritUnboundBases()
			.Internal("wxDataViewVirtualListModel", "Reset", "RowAppended", "RowPrepended", "RowInserted", "RowDeleted", "RowChanged", "RowValueChanged", "GetCount", "GetItem", "GetRow")
			.InternalDirector("GetValueByRow", "SetValueByRow", "IsEnabledByRow"));
		// Controls made of others that screen readers know: a tree of items with columns, the file
		// system as a tree, a file chooser to put in a window, and a list that can be edited.
		foreach (string control in (string[])["wxTreeListCtrl", "wxTreeListEvent", "wxGenericDirCtrl", "wxFileCtrl", "wxFileCtrlEvent", "wxEditableListBox"]) {
			tweaks.Class(control);
		}
		// Init sets the control's members as its constructor does, which leaves it without its tree.
		tweaks.Class("wxGenericDirCtrl", tree => tree
			.Ignore("Init")
			.StylesPrefixed("DIRCTRL")
			.ParameterName("wxGenericDirCtrl", "dir", "directory"));
		tweaks.Class("wxFileCtrl").ParameterName("wxFileCtrl", "wildCard", "wildcard");
		tweaks.Class("wxEditableListBox").Rename("GetDelButton", "DeleteButton");
	}
}
