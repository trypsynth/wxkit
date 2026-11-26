namespace WxKit.Generator.Tweaks;

// The styled text control's constants that are one choice or a set of flags, each group an enum
// named StyledText and what it chooses, and the members that take or give one. Scintilla takes
// them all as ints.
internal static class StyledText {
	public static void Apply(Binding.Tweaks tweaks) {
		Binding.ClassTweak control = tweaks.Class("wxStyledTextCtrl");
		tweaks.ConstantEnum("StyledTextLexer", "wxSTC_LEX_");
		control.ParameterEnum("GetLexer", "return", "StyledTextLexer").ParameterEnum("SetLexer", "lexer", "StyledTextLexer");
		tweaks.ConstantEnum("StyledTextCommand", "wxSTC_CMD_");
		control.ParameterEnum("CmdKeyAssign", "cmd", "StyledTextCommand").ParameterEnum("CmdKeyExecute", "cmd", "StyledTextCommand");
		tweaks.ConstantEnum("StyledTextKeyModifiers", "wxSTC_KEYMOD_", isFlags: true);
		control.ParameterEnum("CmdKeyAssign", "modifiers", "StyledTextKeyModifiers").ParameterEnum("CmdKeyClear", "modifiers", "StyledTextKeyModifiers");
		tweaks.ConstantEnum("StyledTextMarkerSymbol", "wxSTC_MARK_");
		control.ParameterEnum("MarkerDefine", "markerSymbol", "StyledTextMarkerSymbol").ParameterEnum("GetMarkerSymbolDefined", "return", "StyledTextMarkerSymbol");
		tweaks.ConstantEnum("StyledTextIndicatorStyle", "wxSTC_INDIC_", excludePrefixes: ["wxSTC_INDIC_CONTAINER", "wxSTC_INDIC_IME", "wxSTC_INDIC_MAX"]);
		control.ParameterEnum("IndicatorSetStyle", "indicatorStyle", "StyledTextIndicatorStyle").ParameterEnum("IndicatorGetStyle", "return", "StyledTextIndicatorStyle").ParameterEnum("IndicatorSetHoverStyle", "indicatorStyle", "StyledTextIndicatorStyle").ParameterEnum("IndicatorGetHoverStyle", "return", "StyledTextIndicatorStyle");
		tweaks.ConstantEnum("StyledTextIndicatorFlags", "wxSTC_INDICFLAG_", isFlags: true);
		control.ParameterEnum("IndicatorSetFlags", "flags", "StyledTextIndicatorFlags").ParameterEnum("IndicatorGetFlags", "return", "StyledTextIndicatorFlags");
		tweaks.ConstantEnum("StyledTextCharacterSet", "wxSTC_CHARSET_");
		control.ParameterEnum("StyleSetCharacterSet", "characterSet", "StyledTextCharacterSet").ParameterEnum("StyleGetCharacterSet", "return", "StyledTextCharacterSet");
		tweaks.ConstantEnum("StyledTextMarginType", "wxSTC_MARGIN_");
		control.ParameterEnum("SetMarginType", "marginType", "StyledTextMarginType").ParameterEnum("GetMarginType", "return", "StyledTextMarginType");
		tweaks.ConstantEnum("StyledTextCaretStyle", "wxSTC_CARETSTYLE_", isFlags: true);
		control.ParameterEnum("SetCaretStyle", "caretStyle", "StyledTextCaretStyle").ParameterEnum("GetCaretStyle", "return", "StyledTextCaretStyle");
		tweaks.ConstantEnum("StyledTextPrintColorMode", "wxSTC_PRINT_");
		control.ParameterEnum("SetPrintColourMode", "mode", "StyledTextPrintColorMode").ParameterEnum("GetPrintColourMode", "return", "StyledTextPrintColorMode");
		tweaks.ConstantEnum("StyledTextFoldFlags", "wxSTC_FOLDFLAG_", isFlags: true);
		control.ParameterEnum("SetFoldFlags", "flags", "StyledTextFoldFlags");
		tweaks.ConstantEnum("StyledTextFindFlags", "wxSTC_FIND_", isFlags: true);
		control.ParameterEnum("SetSearchFlags", "searchFlags", "StyledTextFindFlags").ParameterEnum("GetSearchFlags", "return", "StyledTextFindFlags").ParameterEnum("FindText", "flags", "StyledTextFindFlags").ParameterEnum("SearchNext", "searchFlags", "StyledTextFindFlags").ParameterEnum("SearchPrev", "searchFlags", "StyledTextFindFlags");
		tweaks.ConstantEnum("StyledTextStatus", "wxSTC_STATUS_");
		control.ParameterEnum("SetStatus", "status", "StyledTextStatus").ParameterEnum("GetStatus", "return", "StyledTextStatus");
		tweaks.ConstantEnum("StyledTextFontQuality", "wxSTC_EFF_QUALITY_", excludePrefixes: ["wxSTC_EFF_QUALITY_MASK"]);
		control.ParameterEnum("SetFontQuality", "fontQuality", "StyledTextFontQuality").ParameterEnum("GetFontQuality", "return", "StyledTextFontQuality");
		tweaks.ConstantEnum("StyledTextCompletionMethod", "wxSTC_AC_");
		tweaks.ConstantEnum("StyledTextWhiteSpace", "wxSTC_WS_");
		control.ParameterEnum("SetViewWhiteSpace", "viewWS", "StyledTextWhiteSpace").ParameterEnum("GetViewWhiteSpace", "return", "StyledTextWhiteSpace");
		tweaks.ConstantEnum("StyledTextTabDrawMode", "wxSTC_TD_");
		control.ParameterEnum("SetTabDrawMode", "tabDrawMode", "StyledTextTabDrawMode").ParameterEnum("GetTabDrawMode", "return", "StyledTextTabDrawMode");
		tweaks.ConstantEnum("StyledTextEolMode", "wxSTC_EOL_");
		control.ParameterEnum("SetEOLMode", "eolMode", "StyledTextEolMode").ParameterEnum("GetEOLMode", "return", "StyledTextEolMode").ParameterEnum("ConvertEOLs", "eolMode", "StyledTextEolMode");
		tweaks.ConstantEnum("StyledTextImeInteraction", "wxSTC_IME_");
		control.ParameterEnum("SetIMEInteraction", "imeInteraction", "StyledTextImeInteraction").ParameterEnum("GetIMEInteraction", "return", "StyledTextImeInteraction");
		tweaks.ConstantEnum("StyledTextIndentGuides", "wxSTC_IV_");
		control.ParameterEnum("SetIndentationGuides", "indentView", "StyledTextIndentGuides").ParameterEnum("GetIndentationGuides", "return", "StyledTextIndentGuides");
		tweaks.ConstantEnum("StyledTextFoldDisplayText", "wxSTC_FOLDDISPLAYTEXT_");
		control.ParameterEnum("FoldDisplayTextSetStyle", "style", "StyledTextFoldDisplayText").ParameterEnum("FoldDisplayTextGetStyle", "return", "StyledTextFoldDisplayText");
		tweaks.ConstantEnum("StyledTextFoldAction", "wxSTC_FOLDACTION_");
		control.ParameterEnum("FoldLine", "action", "StyledTextFoldAction").ParameterEnum("FoldChildren", "action", "StyledTextFoldAction").ParameterEnum("FoldAll", "action", "StyledTextFoldAction");
		tweaks.ConstantEnum("StyledTextAutomaticFold", "wxSTC_AUTOMATICFOLD_", isFlags: true);
		control.ParameterEnum("SetAutomaticFold", "automaticFold", "StyledTextAutomaticFold").ParameterEnum("GetAutomaticFold", "return", "StyledTextAutomaticFold");
		tweaks.ConstantEnum("StyledTextIdleStyling", "wxSTC_IDLESTYLING_");
		control.ParameterEnum("SetIdleStyling", "idleStyling", "StyledTextIdleStyling").ParameterEnum("GetIdleStyling", "return", "StyledTextIdleStyling");
		tweaks.ConstantEnum("StyledTextWrapMode", "wxSTC_WRAP_");
		control.ParameterEnum("SetWrapMode", "wrapMode", "StyledTextWrapMode").ParameterEnum("GetWrapMode", "return", "StyledTextWrapMode");
		tweaks.ConstantEnum("StyledTextWrapVisualFlags", "wxSTC_WRAPVISUALFLAG_", isFlags: true);
		control.ParameterEnum("SetWrapVisualFlags", "wrapVisualFlags", "StyledTextWrapVisualFlags").ParameterEnum("GetWrapVisualFlags", "return", "StyledTextWrapVisualFlags");
		tweaks.ConstantEnum("StyledTextWrapVisualLocation", "wxSTC_WRAPVISUALFLAGLOC_", isFlags: true);
		control.ParameterEnum("SetWrapVisualFlagsLocation", "wrapVisualFlagsLocation", "StyledTextWrapVisualLocation").ParameterEnum("GetWrapVisualFlagsLocation", "return", "StyledTextWrapVisualLocation");
		tweaks.ConstantEnum("StyledTextWrapIndentMode", "wxSTC_WRAPINDENT_");
		control.ParameterEnum("SetWrapIndentMode", "wrapIndentMode", "StyledTextWrapIndentMode").ParameterEnum("GetWrapIndentMode", "return", "StyledTextWrapIndentMode");
		tweaks.ConstantEnum("StyledTextLayoutCache", "wxSTC_CACHE_");
		control.ParameterEnum("SetLayoutCache", "cacheMode", "StyledTextLayoutCache").ParameterEnum("GetLayoutCache", "return", "StyledTextLayoutCache");
		tweaks.ConstantEnum("StyledTextPhasesDraw", "wxSTC_PHASES_");
		control.ParameterEnum("SetPhasesDraw", "phases", "StyledTextPhasesDraw").ParameterEnum("GetPhasesDraw", "return", "StyledTextPhasesDraw");
		tweaks.ConstantEnum("StyledTextMultiPaste", "wxSTC_MULTIPASTE_");
		control.ParameterEnum("SetMultiPaste", "multiPaste", "StyledTextMultiPaste").ParameterEnum("GetMultiPaste", "return", "StyledTextMultiPaste");
		tweaks.ConstantEnum("StyledTextAccessibility", "wxSTC_ACCESSIBILITY_");
		control.ParameterEnum("SetAccessibility", "accessibility", "StyledTextAccessibility").ParameterEnum("GetAccessibility", "return", "StyledTextAccessibility");
		tweaks.ConstantEnum("StyledTextEdgeMode", "wxSTC_EDGE_");
		control.ParameterEnum("SetEdgeMode", "edgeMode", "StyledTextEdgeMode").ParameterEnum("GetEdgeMode", "return", "StyledTextEdgeMode");
		tweaks.ConstantEnum("StyledTextPopUpMode", "wxSTC_POPUP_");
		control.ParameterEnum("UsePopUp", "popUpMode", "StyledTextPopUpMode");
		tweaks.ConstantEnum("StyledTextDocumentOptions", "wxSTC_DOCUMENTOPTION_", isFlags: true);
		control.ParameterEnum("GetDocumentOptions", "return", "StyledTextDocumentOptions");
		tweaks.ConstantEnum("StyledTextCaretPolicy", "wxSTC_CARET_", isFlags: true);
		control.ParameterEnum("SetXCaretPolicy", "caretPolicy", "StyledTextCaretPolicy").ParameterEnum("SetYCaretPolicy", "caretPolicy", "StyledTextCaretPolicy");
		tweaks.ConstantEnum("StyledTextVisiblePolicy", "wxSTC_VISIBLE_", isFlags: true);
		control.ParameterEnum("SetVisiblePolicy", "visiblePolicy", "StyledTextVisiblePolicy");
		tweaks.ConstantEnum("StyledTextSelectionMode", "wxSTC_SEL_");
		control.ParameterEnum("SetSelectionMode", "selectionMode", "StyledTextSelectionMode").ParameterEnum("GetSelectionMode", "return", "StyledTextSelectionMode");
		tweaks.ConstantEnum("StyledTextCaseInsensitiveBehavior", "wxSTC_CASEINSENSITIVEBEHAVIOUR_");
		control.ParameterEnum("AutoCompSetCaseInsensitiveBehaviour", "behaviour", "StyledTextCaseInsensitiveBehavior").ParameterEnum("AutoCompGetCaseInsensitiveBehaviour", "return", "StyledTextCaseInsensitiveBehavior");
		tweaks.ConstantEnum("StyledTextMultiAutoComplete", "wxSTC_MULTIAUTOC_");
		control.ParameterEnum("AutoCompSetMulti", "multi", "StyledTextMultiAutoComplete").ParameterEnum("AutoCompGetMulti", "return", "StyledTextMultiAutoComplete");
		tweaks.ConstantEnum("StyledTextAutoCompleteOrder", "wxSTC_ORDER_");
		control.ParameterEnum("AutoCompSetOrder", "order", "StyledTextAutoCompleteOrder").ParameterEnum("AutoCompGetOrder", "return", "StyledTextAutoCompleteOrder");
		tweaks.ConstantEnum("StyledTextCaretSticky", "wxSTC_CARETSTICKY_");
		control.ParameterEnum("SetCaretSticky", "useCaretStickyBehaviour", "StyledTextCaretSticky").ParameterEnum("GetCaretSticky", "return", "StyledTextCaretSticky");
		tweaks.ConstantEnum("StyledTextMarginOptions", "wxSTC_MARGINOPTION_", isFlags: true);
		control.ParameterEnum("SetMarginOptions", "marginOptions", "StyledTextMarginOptions").ParameterEnum("GetMarginOptions", "return", "StyledTextMarginOptions");
		tweaks.ConstantEnum("StyledTextAnnotationVisible", "wxSTC_ANNOTATION_");
		control.ParameterEnum("AnnotationSetVisible", "visible", "StyledTextAnnotationVisible").ParameterEnum("AnnotationGetVisible", "return", "StyledTextAnnotationVisible");
		tweaks.ConstantEnum("StyledTextEolAnnotationVisible", "wxSTC_EOLANNOTATION_");
		control.ParameterEnum("EOLAnnotationSetVisible", "visible", "StyledTextEolAnnotationVisible").ParameterEnum("EOLAnnotationGetVisible", "return", "StyledTextEolAnnotationVisible");
		tweaks.ConstantEnum("StyledTextUndoFlags", "wxSTC_UNDO_", isFlags: true);
		control.ParameterEnum("AddUndoAction", "flags", "StyledTextUndoFlags");
		tweaks.ConstantEnum("StyledTextVirtualSpace", "wxSTC_VS_", isFlags: true);
		control.ParameterEnum("SetVirtualSpaceOptions", "virtualSpaceOptions", "StyledTextVirtualSpace").ParameterEnum("GetVirtualSpaceOptions", "return", "StyledTextVirtualSpace");
		tweaks.ConstantEnum("StyledTextTechnology", "wxSTC_TECHNOLOGY_");
		control.ParameterEnum("SetTechnology", "technology", "StyledTextTechnology").ParameterEnum("GetTechnology", "return", "StyledTextTechnology");
		tweaks.ConstantEnum("StyledTextLineEndTypes", "wxSTC_LINE_END_TYPE_", isFlags: true);
		control.ParameterEnum("SetLineEndTypesAllowed", "lineEndBitSet", "StyledTextLineEndTypes").ParameterEnum("GetLineEndTypesAllowed", "return", "StyledTextLineEndTypes").ParameterEnum("GetLineEndTypesActive", "return", "StyledTextLineEndTypes").ParameterEnum("GetLineEndTypesSupported", "return", "StyledTextLineEndTypes");
		tweaks.ConstantEnum("StyledTextPropertyType", "wxSTC_TYPE_");
		control.ParameterEnum("PropertyType", "return", "StyledTextPropertyType");
		tweaks.ConstantEnum("StyledTextUpdateFlags", "wxSTC_UPDATE_", isFlags: true);
		tweaks.ConstantEnum("StyledTextCharacterSource", "wxSTC_CHARACTERSOURCE_");
		tweaks.ConstantEnum("StyledTextCase", "wxSTC_CASE_");
		control.ParameterEnum("StyleSetCase", "caseVisible", "StyledTextCase").ParameterEnum("StyleGetCase", "return", "StyledTextCase");
		// What changed a document, whose constants share no prefix.
		tweaks.ConstantEnumOf("StyledTextModificationFlags", isFlags: true, "wxSTC_MOD_NONE", "wxSTC_MOD_INSERTTEXT", "wxSTC_MOD_DELETETEXT", "wxSTC_MOD_CHANGESTYLE", "wxSTC_MOD_CHANGEFOLD", "wxSTC_PERFORMED_USER", "wxSTC_PERFORMED_UNDO", "wxSTC_PERFORMED_REDO", "wxSTC_MULTISTEPUNDOREDO", "wxSTC_LASTSTEPINUNDOREDO", "wxSTC_MOD_CHANGEMARKER", "wxSTC_MOD_BEFOREINSERT", "wxSTC_MOD_BEFOREDELETE", "wxSTC_MULTILINEUNDOREDO", "wxSTC_STARTACTION", "wxSTC_MOD_CHANGEINDICATOR", "wxSTC_MOD_CHANGELINESTATE", "wxSTC_MOD_CHANGEMARGIN", "wxSTC_MOD_CHANGEANNOTATION", "wxSTC_MOD_CONTAINER", "wxSTC_MOD_LEXERSTATE", "wxSTC_MOD_INSERTCHECK", "wxSTC_MOD_CHANGETABSTOPS", "wxSTC_MOD_CHANGEEOLANNOTATION", "wxSTC_MODEVENTMASKALL");
		tweaks.NameEnumMember("wxSTC_MOD_NONE", "None");
		tweaks.NameEnumMember("wxSTC_MOD_INSERTTEXT", "InsertText");
		tweaks.NameEnumMember("wxSTC_MOD_DELETETEXT", "DeleteText");
		tweaks.NameEnumMember("wxSTC_MOD_CHANGESTYLE", "ChangeStyle");
		tweaks.NameEnumMember("wxSTC_MOD_CHANGEFOLD", "ChangeFold");
		tweaks.NameEnumMember("wxSTC_PERFORMED_USER", "User");
		tweaks.NameEnumMember("wxSTC_PERFORMED_UNDO", "Undo");
		tweaks.NameEnumMember("wxSTC_PERFORMED_REDO", "Redo");
		tweaks.NameEnumMember("wxSTC_MULTISTEPUNDOREDO", "MultiStepUndoRedo");
		tweaks.NameEnumMember("wxSTC_LASTSTEPINUNDOREDO", "LastStepInUndoRedo");
		tweaks.NameEnumMember("wxSTC_MOD_CHANGEMARKER", "ChangeMarker");
		tweaks.NameEnumMember("wxSTC_MOD_BEFOREINSERT", "BeforeInsert");
		tweaks.NameEnumMember("wxSTC_MOD_BEFOREDELETE", "BeforeDelete");
		tweaks.NameEnumMember("wxSTC_MULTILINEUNDOREDO", "MultiLineUndoRedo");
		tweaks.NameEnumMember("wxSTC_STARTACTION", "StartAction");
		tweaks.NameEnumMember("wxSTC_MOD_CHANGEINDICATOR", "ChangeIndicator");
		tweaks.NameEnumMember("wxSTC_MOD_CHANGELINESTATE", "ChangeLineState");
		tweaks.NameEnumMember("wxSTC_MOD_CHANGEMARGIN", "ChangeMargin");
		tweaks.NameEnumMember("wxSTC_MOD_CHANGEANNOTATION", "ChangeAnnotation");
		tweaks.NameEnumMember("wxSTC_MOD_CONTAINER", "Container");
		tweaks.NameEnumMember("wxSTC_MOD_LEXERSTATE", "LexerState");
		tweaks.NameEnumMember("wxSTC_MOD_INSERTCHECK", "InsertCheck");
		tweaks.NameEnumMember("wxSTC_MOD_CHANGETABSTOPS", "ChangeTabStops");
		tweaks.NameEnumMember("wxSTC_MOD_CHANGEEOLANNOTATION", "ChangeEolAnnotation");
		tweaks.NameEnumMember("wxSTC_MODEVENTMASKALL", "All");
		control.ParameterEnum("SetModEventMask", "eventMask", "StyledTextModificationFlags").ParameterEnum("GetModEventMask", "return", "StyledTextModificationFlags");
		tweaks.ConstantEnumOf("StyledTextCursor", isFlags: false, "wxSTC_CURSORNORMAL", "wxSTC_CURSORARROW", "wxSTC_CURSORWAIT", "wxSTC_CURSORREVERSEARROW");
		tweaks.NameEnumMember("wxSTC_CURSORNORMAL", "Normal");
		tweaks.NameEnumMember("wxSTC_CURSORARROW", "Arrow");
		tweaks.NameEnumMember("wxSTC_CURSORWAIT", "Wait");
		tweaks.NameEnumMember("wxSTC_CURSORREVERSEARROW", "ReverseArrow");
		control.ParameterEnum("SetSTCCursor", "cursorType", "StyledTextCursor").ParameterEnum("GetSTCCursor", "return", "StyledTextCursor");
		tweaks.NameEnumMember("wxSTC_CHARSET_8859_15", "Iso885915");
		tweaks.NameEnumMember("wxSTC_KEYMOD_NORM", "None");
		tweaks.NameEnumMember("wxSTC_KEYMOD_CTRL", "Control");
		tweaks.NameEnumMember("wxSTC_CARETSTYLE_INS_MASK", "InsertMask");
		control.ParameterEnum("SetRectangularSelectionModifier", "modifier", "StyledTextKeyModifiers").ParameterEnum("GetRectangularSelectionModifier", "return", "StyledTextKeyModifiers");
		// Getters and setters Scintilla names with the verb in the middle are properties, a count
		// says it is one, and booleans are named as IsChecked is.
		control
			.Property("AnnotationVisible", "AnnotationGetVisible", "AnnotationSetVisible")
			.Property("EolAnnotationVisible", "EOLAnnotationGetVisible", "EOLAnnotationSetVisible")
			.Property("FoldDisplayTextStyle", "FoldDisplayTextGetStyle", "FoldDisplayTextSetStyle")
			.Property("AnnotationStyleOffset", "AnnotationGetStyleOffset", "AnnotationSetStyleOffset")
			.Property("EolAnnotationStyleOffset", "EOLAnnotationGetStyleOffset", "EOLAnnotationSetStyleOffset")
			.Property("MarginStyleOffset", "MarginGetStyleOffset", "MarginSetStyleOffset")
			.Property("SelectionCount", "GetSelections")
			.Property("IsSelectionEmpty", "GetSelectionEmpty")
			.Property("IsOvertype", "GetOvertype", "SetOvertype");
		// A recorded macro's message and its parameters are for SendMsg, which WxKit doesn't bind,
		// and where the pointer was is a Point, in StyledTextEventModifiers.cs.
		tweaks.Class("wxStyledTextEvent").Ignore("GetMessage", "GetWParam", "GetLParam").Internal("GetX", "GetY");
		// What an event says of itself.
		tweaks.Class("wxStyledTextEvent").ParameterEnum("GetModificationType", "return", "StyledTextModificationFlags").ParameterEnum("GetUpdated", "return", "StyledTextUpdateFlags").ParameterEnum("GetListCompletionMethod", "return", "StyledTextCompletionMethod");
	}
}
