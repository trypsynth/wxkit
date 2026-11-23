namespace WxKit.Generator.Tweaks;

// Dialogs: the standard ones, and the data the find, font, and color dialogs keep.
internal static class Dialogs {
	public static void Apply(Binding.Tweaks tweaks) {
		// wxMSW's wxDialog has no GetToolBar, though the documentation gives it one.
		tweaks.Class("wxDialog").Ignore("GetToolBar");
		tweaks.Class("wxMessageDialog");
		foreach (string dialog in (string[])["wxTextEntryDialog", "wxPasswordEntryDialog", "wxFileDialog", "wxDirDialog", "wxProgressDialog", "wxSingleChoiceDialog", "wxMultiChoiceDialog", "wxNumberEntryDialog", "wxRichMessageDialog", "wxCredentialEntryDialog", "wxPropertySheetDialog", "wxWizard", "wxWizardPage", "wxWizardPageSimple"]) {
			tweaks.Class(dialog);
		}
		// The find dialog reads and writes its data for as long as it exists, so the data is
		// WxKit's, and the dialog keeps it alive. GetData would wrap the same data a second time.
		// The dialog sends its events to itself, and on to its parent.
		tweaks.Enum("wxFindReplaceFlags", "FindReplaceFlags", isFlags: true);
		tweaks.Enum("wxFindReplaceDialogStyles", "FindReplaceDialogStyle", isFlags: true);
		tweaks.Class("wxFindReplaceData", data => data
			.Owned()
			.ParameterEnum("wxFindReplaceData", "flags", "wxFindReplaceFlags")
			.ParameterEnum("SetFlags", "flags", "wxFindReplaceFlags")
			.ParameterEnum("GetFlags", "return", "wxFindReplaceFlags"));
		tweaks.Class("wxFindReplaceDialog", dialog => dialog
			.KeepAlive("data")
			.Ignore("GetData")
			.ParameterEnum("wxFindReplaceDialog", "style", "wxFindReplaceDialogStyles")
			.AddEvent("EVT_FIND", "wxEVT_FIND", "wxFindDialogEvent", "Find")
			.AddEvent("EVT_FIND_NEXT", "wxEVT_FIND_NEXT", "wxFindDialogEvent", "FindNext")
			.AddEvent("EVT_FIND_REPLACE", "wxEVT_FIND_REPLACE", "wxFindDialogEvent", "Replace")
			.AddEvent("EVT_FIND_REPLACE_ALL", "wxEVT_FIND_REPLACE_ALL", "wxFindDialogEvent", "ReplaceAll")
			.AddEvent("EVT_FIND_CLOSE", "wxEVT_FIND_CLOSE", "wxFindDialogEvent", "FindClose"));
		tweaks.Class("wxFindDialogEvent", findEvent => findEvent.ParameterEnum("GetFlags", "return", "wxFindReplaceFlags"));
		// The font dialog copies the data it is given, and gives a copy back.
		tweaks.Class("wxFontData", data => data.ValueObject().Property("EnableEffects", "GetEnableEffects", "EnableEffects").Property("RestrictSelection", "GetRestrictSelection", "RestrictSelection"));
		tweaks.Class("wxFontDialog");
		// The color dialog, and the event wxMSW sends as the color in it changes. Events are only
		// received, so the event's color is only read.
		// ToString and FromString write and read the custom colors, which would hide object.ToString.
		tweaks.Class("wxColourData", data => data.ValueObject().Ignore("ToString", "FromString"));
		tweaks.Class("wxColourDialog", dialog => dialog.AddEvent("EVT_COLOUR_CHANGED", "wxEVT_COLOUR_CHANGED", "wxColourDialogEvent", "ColorChanged"));
		tweaks.Class("wxColourDialogEvent", e => e.Ignore("SetColour"));
		// A progress dialog's styles are constants, not a style table.
		tweaks.ConstantEnum("ProgressDialogStyle", "wxPD_", isFlags: true);
		// Its progress members, such as Update and WasCancelled, are wxGenericProgressDialog's, which
		// wxMSW's native dialog overrides.
		tweaks.Class("wxProgressDialog", dialog => dialog
			.InheritUnboundBases()
			.ParameterEnum("wxProgressDialog", "style", "ProgressDialogStyle")
			.ParameterName("Update", "newmsg", "message")
			.ParameterName("Pulse", "newmsg", "message")
			.Property("IsCanceled", "WasCancelled")
			.Property("IsSkipped", "WasSkipped"));
		// A message dialog with a check box, such as "Don't ask again", and details that can be shown.
		// wxWidgets makes it from its own message dialog, not the platform's, so it is no
		// MessageDialog, and has that class's members through the base the two share.
		tweaks.Class("wxRichMessageDialog", dialog => dialog
			.Bases("wxDialog")
			.Borrow("wxMessageDialog", "wxMessageDialogBase")
			.ParameterEnum("wxRichMessageDialog", "style", "MessageDialogStyle")
			.ParameterEnum("GetMessageDialogStyle", "return", "MessageDialogStyle")
			.ParameterEnum("SetFooterIcon", "icon", "MessageDialogStyle")
			.ParameterEnum("GetFooterIcon", "return", "MessageDialogStyle"));
		// A password is a wxSecretValue in wxWebCredentials, so the user name and password are
		// strings written by hand.
		tweaks.Class("wxCredentialEntryDialog").Ignore("SetUser", "SetPassword");
		// A wizard's pages are each a WizardPageSimple, chained in order, or a WizardPage whose
		// Next and Previous a subclass decides.
		tweaks.Class("wxWizardPage").NoConstructors().Rename("GetPrev", "Previous");
		tweaks.Class("wxWizardPageSimple", page => page
			.Rename("SetPrev", "SetPrevious")
			.ParameterName("wxWizardPageSimple", "prev", "previous")
			.ParameterName("SetPrev", "prev", "previous"));
		tweaks.ConstantEnumOf("WizardBitmapPlacement", isFlags: true, "wxWIZARD_VALIGN_TOP", "wxWIZARD_VALIGN_CENTRE", "wxWIZARD_VALIGN_BOTTOM", "wxWIZARD_HALIGN_LEFT", "wxWIZARD_HALIGN_CENTRE", "wxWIZARD_HALIGN_RIGHT", "wxWIZARD_TILE");
		tweaks.Class("wxWizard", wizard => wizard
			.Rename("HasPrevPage", "HasPreviousPage")
			.ParameterEnum("SetBitmapPlacement", "placement", "WizardBitmapPlacement")
			.ParameterEnum("GetBitmapPlacement", "return", "WizardBitmapPlacement"));
		tweaks.Class("wxWizardEvent");
		// The buttons a dialog makes for itself, such as OK and Cancel.
		tweaks.ConstantEnumOf("DialogButtons", isFlags: true, "wxOK", "wxCANCEL", "wxYES", "wxNO", "wxAPPLY", "wxCLOSE", "wxHELP", "wxNO_DEFAULT");
		tweaks.ConstantEnumOf("PropertySheetStyle", isFlags: true, "wxPROPSHEET_DEFAULT", "wxPROPSHEET_NOTEBOOK", "wxPROPSHEET_TOOLBOOK", "wxPROPSHEET_CHOICEBOOK", "wxPROPSHEET_LISTBOOK", "wxPROPSHEET_BUTTONTOOLBOOK", "wxPROPSHEET_TREEBOOK", "wxPROPSHEET_SHRINKTOFIT");
		tweaks.Class("wxPropertySheetDialog", sheet => sheet
			.ParameterEnum("CreateButtons", "flags", "DialogButtons")
			.ParameterEnum("SetSheetStyle", "style", "PropertySheetStyle")
			.ParameterEnum("GetSheetStyle", "return", "PropertySheetStyle"));
	}
}
