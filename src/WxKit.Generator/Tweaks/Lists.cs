namespace WxKit.Generator.Tweaks;

// Lists and trees, data views, and the controls made of others that show them.
internal static class Lists {
	public static void Apply(Binding.Tweaks tweaks) {
		// A virtual list asks for each row as it shows it, through members C# overrides. A getter
		// that can fail returns whether it could, as .NET's Try methods do.
		tweaks.Class("wxListCtrl", list => list
			.ParameterEnum("InsertColumn", "format", "wxListColumnFormat")
			.ParameterEnum("SetSingleStyle", "style", "ListCtrlStyle")
			.ParameterEnum("SetWindowStyleFlag", "style", "ListCtrlStyle")
			.Director("OnGetItemText", "OnGetItemImage", "OnGetItemColumnImage", "OnGetItemIsChecked")
			.Rename("GetItemRect", "TryGetItemRect")
			.Rename("GetSubItemRect", "TryGetSubItemRect"));
		tweaks.Class("wxListView");
		tweaks.Class("wxListItem").ValueObject();
		tweaks.Class("wxListEvent");
		// SetItemData is written by hand, since wxTreeCtrl does not delete the data it replaces.
		tweaks.Class("wxTreeCtrl", tree => tree
			.Ignore("SetItemData")
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
			.Ignore("AssociateModel", "GetModel", "AppendColumn", "PrependColumn", "InsertColumn", "DeleteColumn", "ClearColumns", "GetSortingColumns")
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
		// Its images are an image list, which is not bound.
		tweaks.Class("wxDataViewTreeCtrl", view => view.DiscardUnboundReturns().Ignore("GetStore", "GetImageList"));
		// The interface headers document SetItem, which the real wxDataViewEvent does not have. Events
		// are only received, so the column is only read.
		tweaks.Class("wxDataViewEvent", dataViewEvent => dataViewEvent
			.Ignore("SetItem", "SetDataViewColumn")
			.ParameterEnum("SetDragFlags", "flags", "DragFlags")
			.ParameterEnum("GetDragFlags", "return", "DragFlags"));
		tweaks.Class("wxDataViewIconText").ValueObject();
		// Controls made of others that screen readers know: a tree of items with columns, the file
		// system as a tree, a file chooser to put in a window, and a list that can be edited.
		foreach (string control in (string[])["wxTreeListCtrl", "wxTreeListEvent", "wxGenericDirCtrl", "wxFileCtrl", "wxFileCtrlEvent", "wxEditableListBox"]) {
			tweaks.Class(control);
		}
		tweaks.Class("wxGenericDirCtrl", tree => tree
			.StylesPrefixed("DIRCTRL")
			.ParameterName("wxGenericDirCtrl", "dir", "directory"));
		tweaks.Class("wxFileCtrl").ParameterName("wxFileCtrl", "wildCard", "wildcard");
		tweaks.Class("wxEditableListBox").Rename("GetDelButton", "DeleteButton");
	}
}
