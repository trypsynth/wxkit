using System.Globalization;
using System.Text;

namespace WxKit.Generator.Binding;

// The rules that turn wxWidgets names into .NET ones.
internal static class Naming {
	// British spellings wxWidgets uses, and the .NET ones WxKit uses instead, by whole word.
	// Each table is filled with Add, which throws on a duplicate key, rather than with the
	// indexer, which would let a later entry replace an earlier one unnoticed.
	private static readonly Dictionary<string, string> Respellings = new(StringComparer.Ordinal) {
		{ "Centre", "Center" },
		{ "Colour", "Color" },
		{ "Colours", "Colors" },
		{ "Grey", "Gray" },
		{ "Behaviour", "Behavior" },
		{ "Initialise", "Initialize" },
		{ "Normalise", "Normalize" },
		{ "Customise", "Customize" },
		{ "Favourite", "Favorite" },
		{ "Neighbour", "Neighbor" },
		{ "Cancelled", "Canceled" },
		{ "Cancelling", "Canceling" },
		{ "Greyscale", "Grayscale" },
		{ "Colourise", "Colorize" },
		// Words wxWidgets spells two ways, spelled one way: as the class is, or as .NET does.
		{ "Scrollbar", "ScrollBar" },
		{ "Scrollbars", "ScrollBars" },
		{ "Filename", "FileName" },
		{ "Filenames", "FileNames" },
		{ "Autoscroll", "AutoScroll" },
		// Abbreviations .NET spells out, as in PreviousSibling.
		{ "Prev", "Previous" },
		{ "Desc", "Description" },
		{ "Cols", "Columns" },
		{ "Freq", "Frequency" },
		{ "Phys", "Physical" },
		{ "Fg", "Foreground" },
		{ "Bg", "Background" },
		// A misspelling.
		{ "Resizeable", "Resizable" },
	};

	// Words that wxWidgets writes run together in upper case, so they cannot be split on their own.
	private static readonly Dictionary<string, string> CompoundWords = new(StringComparer.Ordinal) {
		{ "MULTILINE", "MultiLine" },
		{ "CHECKBOX", "CheckBox" },
		{ "DIR", "Directory" },
		{ "NOSHOWHIDDEN", "NoShowHidden" },
		{ "READONLY", "ReadOnly" },
		{ "NOHIDESEL", "NoHideSelection" },
		{ "DONTWRAP", "DontWrap" },
		{ "CHARWRAP", "CharWrap" },
		{ "WORDWRAP", "WordWrap" },
		{ "BESTWRAP", "BestWrap" },
		// The pickers, their styles, and their events.
		{ "COLOURPICKER", "ColorPicker" },
		{ "FILEPICKER", "FilePicker" },
		{ "DIRPICKER", "DirPicker" },
		{ "FONTPICKER", "FontPicker" },
		{ "TEXTCTRL", "TextCtrl" },
		{ "FONTDESC", "FontDescription" },
		{ "USEFONT", "UseFont" },
		{ "STATUSITEM", "StatusItem" },
		// The mouse buttons, as keys.
		{ "LBUTTON", "LeftButton" },
		{ "RBUTTON", "RightButton" },
		{ "MBUTTON", "MiddleButton" },
		{ "NUMLOCK", "NumLock" },
		{ "ALTGR", "AltGr" },
		// The system colors, which run their words together as Windows's COLOR_ constants do.
		{ "ACTIVECAPTION", "ActiveCaption" },
		{ "INACTIVECAPTION", "InactiveCaption" },
		{ "INACTIVECAPTIONTEXT", "InactiveCaptionText" },
		{ "GRADIENTACTIVECAPTION", "GradientActiveCaption" },
		{ "GRADIENTINACTIVECAPTION", "GradientInactiveCaption" },
		{ "CAPTIONTEXT", "CaptionText" },
		{ "ACTIVEBORDER", "ActiveBorder" },
		{ "INACTIVEBORDER", "InactiveBorder" },
		{ "WINDOWFRAME", "WindowFrame" },
		{ "WINDOWTEXT", "WindowText" },
		{ "MENUTEXT", "MenuText" },
		{ "MENUHILIGHT", "MenuHighlight" },
		{ "APPWORKSPACE", "AppWorkspace" },
		{ "HIGHLIGHTTEXT", "HighlightText" },
		{ "HOTLIGHT", "HotLight" },
		{ "GRAYTEXT", "GrayText" },
		{ "INFOTEXT", "InfoText" },
		{ "INFOBK", "InfoBackground" },
		{ "BTNFACE", "ButtonFace" },
		{ "BTNSHADOW", "ButtonShadow" },
		{ "BTNTEXT", "ButtonText" },
		{ "BTNHIGHLIGHT", "ButtonHighlight" },
		{ "BTNHILIGHT", "ButtonHilight" },
		{ "3DDKSHADOW", "ThreeDDarkShadow" },
		{ "3DLIGHT", "ThreeDLight" },
		{ "3DFACE", "ThreeDFace" },
		{ "3DSHADOW", "ThreeDShadow" },
		{ "3DHIGHLIGHT", "ThreeDHighlight" },
		{ "LISTBOX", "ListBox" },
		{ "LISTBOXTEXT", "ListBoxText" },
		{ "LISTBOXHIGHLIGHT", "ListBoxHighlight" },
		{ "LISTBOXHIGHLIGHTTEXT", "ListBoxHighlightText" },
		{ "GRIDLINES", "GridLines" },
		{ "WRAPSIZER", "WrapSizer" },
		{ "CONTEXTHELP", "ContextHelp" },
		// The scroll events.
		{ "SCROLLWIN", "ScrollWin" },
		{ "LINEUP", "LineUp" },
		{ "LINEDOWN", "LineDown" },
		{ "PAGEUP", "PageUp" },
		{ "PAGEDOWN", "PageDown" },
		{ "THUMBTRACK", "ThumbTrack" },
		{ "THUMBRELEASE", "ThumbRelease" },
		// The full screen styles and the update UI flags.
		{ "FULLSCREEN", "FullScreen" },
		{ "NOMENUBAR", "NoMenuBar" },
		{ "NOTOOLBAR", "NoToolBar" },
		{ "NOSTATUSBAR", "NoStatusBar" },
		{ "NOBORDER", "NoBorder" },
		{ "NOCAPTION", "NoCaption" },
		{ "FROMIDLE", "FromIdle" },
		{ "EXACTFIT", "ExactFit" },
		{ "NOTEXT", "NoText" },
		{ "ICONIZE", "Iconize" },
		{ "2STATE", "TwoState" },
		{ "3STATE", "ThreeState" },
		{ "3D", "ThreeD" },
		{ "3DSASH", "ThreeDSash" },
		{ "3DBORDER", "ThreeDBorder" },
		{ "DATAVIEW", "DataView" },
		// So do the standard ids.
		{ "SAVEAS", "SaveAs" },
		{ "SELECTALL", "SelectAll" },
		{ "LARGEICONS", "LargeIcons" },
		{ "SMALLICONS", "SmallIcons" },
		{ "SORTDATE", "SortDate" },
		{ "SORTNAME", "SortName" },
		{ "SORTSIZE", "SortSize" },
		{ "SORTTYPE", "SortType" },
		{ "YESTOALL", "YesToAll" },
		{ "NOTOALL", "NoToAll" },
		{ "CDROM", "CdRom" },
		{ "HARDDISK", "HardDisk" },
		// The find dialog's flags and styles run theirs together too.
		{ "MATCHCASE", "MatchCase" },
		{ "WHOLEWORD", "WholeWord" },
		{ "REPLACEDIALOG", "ReplaceDialog" },
		{ "NOUPDOWN", "NoUpDown" },
		{ "NOMATCHCASE", "NoMatchCase" },
		{ "NOWHOLEWORD", "NoWholeWord" },
		// The accessibility roles, states, and events run their words together.
		{ "BUTTONDROPDOWN", "ButtonDropDown" },
		{ "BUTTONDROPDOWNGRID", "ButtonDropDownGrid" },
		{ "BUTTONMENU", "ButtonMenu" },
		{ "CHECKBUTTON", "CheckButton" },
		{ "COLUMNHEADER", "ColumnHeader" },
		{ "COMBOBOX", "ComboBox" },
		{ "DROPLIST", "DropList" },
		{ "HELPBALLOON", "HelpBalloon" },
		{ "HOTKEYFIELD", "HotkeyField" },
		{ "LISTITEM", "ListItem" },
		{ "MENUBAR", "MenuBar" },
		{ "MENUITEM", "MenuItem" },
		{ "MENUPOPUP", "MenuPopup" },
		{ "OUTLINEITEM", "OutlineItem" },
		{ "PAGETAB", "PageTab" },
		{ "PAGETABLIST", "PageTabList" },
		{ "PROGRESSBAR", "ProgressBar" },
		{ "PROPERTYPAGE", "PropertyPage" },
		{ "PUSHBUTTON", "PushButton" },
		{ "RADIOBUTTON", "RadioButton" },
		{ "ROWHEADER", "RowHeader" },
		{ "SCROLLBAR", "ScrollBar" },
		{ "SPINBUTTON", "SpinButton" },
		{ "STATICTEXT", "StaticText" },
		{ "STATUSBAR", "StatusBar" },
		{ "TITLEBAR", "TitleBar" },
		{ "TOOLBAR", "ToolBar" },
		{ "TOOLTIP", "ToolTip" },
		{ "WHITESPACE", "WhiteSpace" },
		{ "SYSMENU", "SystemMenu" },
		{ "VSCROLL", "VerticalScrollBar" },
		{ "HSCROLL", "HorizontalScrollBar" },
		{ "SIZEGRIP", "SizeGrip" },
		{ "FIRSTCHILD", "FirstChild" },
		{ "LASTCHILD", "LastChild" },
		{ "TAKEFOCUS", "TakeFocus" },
		{ "TAKESELECTION", "TakeSelection" },
		{ "EXTENDSELECTION", "ExtendSelection" },
		{ "ADDSELECTION", "AddSelection" },
		{ "REMOVESELECTION", "RemoveSelection" },
		{ "EXTSELECTABLE", "ExtendedSelectable" },
		{ "HOTTRACKED", "HotTracked" },
		{ "MULTISELECTABLE", "MultiSelectable" },
		{ "SELFVOICING", "SelfVoicing" },
		{ "MENUSTART", "MenuStart" },
		{ "MENUEND", "MenuEnd" },
		{ "MENUPOPUPSTART", "MenuPopupStart" },
		{ "MENUPOPUPEND", "MenuPopupEnd" },
		{ "CAPTURESTART", "CaptureStart" },
		{ "CAPTUREEND", "CaptureEnd" },
		{ "MOVESIZESTART", "MoveSizeStart" },
		{ "MOVESIZEEND", "MoveSizeEnd" },
		{ "CONTEXTHELPSTART", "ContextHelpStart" },
		{ "CONTEXTHELPEND", "ContextHelpEnd" },
		{ "DRAGDROPSTART", "DragDropStart" },
		{ "DRAGDROPEND", "DragDropEnd" },
		{ "DIALOGSTART", "DialogStart" },
		{ "DIALOGEND", "DialogEnd" },
		{ "SCROLLINGSTART", "ScrollingStart" },
		{ "SCROLLINGEND", "ScrollingEnd" },
		{ "SWITCHSTART", "SwitchStart" },
		{ "SWITCHEND", "SwitchEnd" },
		{ "MINIMIZESTART", "MinimizeStart" },
		{ "MINIMIZEEND", "MinimizeEnd" },
		{ "SELECTIONADD", "SelectionAdd" },
		{ "SELECTIONREMOVE", "SelectionRemove" },
		{ "SELECTIONWITHIN", "SelectionWithin" },
		{ "STATECHANGE", "StateChange" },
		{ "LOCATIONCHANGE", "LocationChange" },
		{ "NAMECHANGE", "NameChange" },
		{ "DESCRIPTIONCHANGE", "DescriptionChange" },
		{ "VALUECHANGE", "ValueChange" },
		{ "PARENTCHANGE", "ParentChange" },
		{ "HELPCHANGE", "HelpChange" },
		{ "DEFACTIONCHANGE", "DefaultActionChange" },
		{ "ACCELERATORCHANGE", "AcceleratorChange" },
		// Words in enum members that the headers run together.
		{ "DROPHILITED", "DropHighlighted" },
		{ "FIXEDPITCH", "FixedPitch" },
		// A list item state of no state, which is what a flags enum calls None.
		{ "DONTCARE", "None" },
		{ "VERTICALCENTRECARET", "VerticalCenterCaret" },
		{ "GRADIENTCENTRE", "GradientCenter" },
		{ "COLOURONWHITE", "ColorOnWhite" },
		{ "COLOURONWHITEDEFAULTBG", "ColorOnWhiteDefaultBackground" },
		{ "SCREENCOLOURS", "ScreenColors" },
		{ "ALLOWNONE", "AllowNone" },
		{ "AUTOARRANGE", "AutoArrange" },
		{ "AUTORESIZE", "AutoResize" },
		{ "AUTOTICKS", "AutoTicks" },
		{ "BDIAGONAL", "BackwardDiagonal" },
		{ "FDIAGONAL", "ForwardDiagonal" },
		{ "CROSSDIAG", "CrossDiagonal" },
		{ "BUTTONBAR", "ButtonBar" },
		{ "BUTTONTOOLBOOK", "ButtonToolbook" },
		{ "CONTEXTMENU", "ContextMenu" },
		{ "DECMONTH", "DecrementMonth" },
		{ "INCMONTH", "IncrementMonth" },
		{ "DROPDOWN", "DropDown" },
		{ "ENVMANUAL", "EnvelopeManual" },
		{ "EXTRABOLD", "ExtraBold" },
		{ "EXTRAHEAVY", "ExtraHeavy" },
		{ "EXTRALIGHT", "ExtraLight" },
		{ "SEMIBOLD", "SemiBold" },
		{ "FIXEDWIDTH", "FixedWidth" },
		{ "FORMSOURCE", "FormSource" },
		{ "FRAMEBK", "FrameBackground" },
		{ "FRAMESIZE", "FrameSize" },
		{ "ICONSPACING", "IconSpacing" },
		{ "LARGECAPACITY", "LargeCapacity" },
		{ "LARGEFMT", "LargeFormat" },
		{ "SMALLFMT", "SmallFormat" },
		{ "LETTERSMALL", "LetterSmall" },
		{ "A4SMALL", "A4Small" },
		{ "P32KBIG", "P32kBig" },
		{ "CSHEET", "CSheet" },
		{ "DSHEET", "DSheet" },
		{ "ESHEET", "ESheet" },
		{ "LOMETRIC", "LowMetric" },
		{ "NOALIGN", "NoAlign" },
		{ "NODIVIDER", "NoDivider" },
		{ "NOICONS", "NoIcons" },
		{ "NOPAGETHEME", "NoPageTheme" },
		{ "ONLYONE", "OnlyOne" },
		{ "PENWINDOWS", "PenWindows" },
		{ "SELRANGE", "SelectionRange" },
		{ "SHOWCENTURY", "ShowCentury" },
		{ "SHRINKTOFIT", "ShrinkToFit" },
		{ "SIZENESW", "SizeNesw" },
		{ "SIZENWSE", "SizeNwse" },
		{ "SIZENS", "SizeNS" },
		{ "SIZEWE", "SizeWE" },
		{ "SPRAYCAN", "SprayCan" },
		{ "IBEAM", "IBeam" },
		{ "SMALLICON", "SmallIcon" },
		{ "WINDOWMIN", "WindowMin" },
		{ "XDIGITS", "HexDigits" },
		{ "HRULES", "HorizontalRules" },
		{ "VRULES", "VerticalRules" },
		{ "DCLICK", "DoubleClick" },
		{ "HTHUMB", "HorizontalThumb" },
		{ "VTHUMB", "VerticalThumb" },
		{ "TOOLTIPS", "ToolTips" },
		{ "10X11", "Standard10x11" },
		{ "10X14", "Standard10x14" },
		{ "11X17", "Standard11x17" },
		{ "12X11", "Standard12x11" },
		{ "15X11", "Standard15x11" },
		{ "9X11", "Standard9x11" },
		// The Mac's font encodings, and its cursor files.
		{ "MACARABIC", "MacArabic" },
		{ "MACARABICEXT", "MacArabicExt" },
		{ "MACARMENIAN", "MacArmenian" },
		{ "MACBENGALI", "MacBengali" },
		{ "MACBURMESE", "MacBurmese" },
		{ "MACCELTIC", "MacCeltic" },
		{ "MACCENTRALEUR", "MacCentralEuropean" },
		{ "MACCHINESESIMP", "MacChineseSimplified" },
		{ "MACCHINESETRAD", "MacChineseTraditional" },
		{ "MACCROATIAN", "MacCroatian" },
		{ "MACCYRILLIC", "MacCyrillic" },
		{ "MACDEVANAGARI", "MacDevanagari" },
		{ "MACDINGBATS", "MacDingbats" },
		{ "MACETHIOPIC", "MacEthiopic" },
		{ "MACGAELIC", "MacGaelic" },
		{ "MACGEORGIAN", "MacGeorgian" },
		{ "MACGREEK", "MacGreek" },
		{ "MACGUJARATI", "MacGujarati" },
		{ "MACGURMUKHI", "MacGurmukhi" },
		{ "MACHEBREW", "MacHebrew" },
		{ "MACICELANDIC", "MacIcelandic" },
		{ "MACJAPANESE", "MacJapanese" },
		{ "MACKANNADA", "MacKannada" },
		{ "MACKEYBOARD", "MacKeyboard" },
		{ "MACKHMER", "MacKhmer" },
		{ "MACKOREAN", "MacKorean" },
		{ "MACLAOTIAN", "MacLaotian" },
		{ "MACMALAJALAM", "MacMalayalam" },
		{ "MACMAX", "MacMax" },
		{ "MACMIN", "MacMin" },
		{ "MACMONGOLIAN", "MacMongolian" },
		{ "MACORIYA", "MacOriya" },
		{ "MACROMAN", "MacRoman" },
		{ "MACROMANIAN", "MacRomanian" },
		{ "MACSINHALESE", "MacSinhalese" },
		{ "MACSYMBOL", "MacSymbol" },
		{ "MACTAMIL", "MacTamil" },
		{ "MACTELUGU", "MacTelugu" },
		{ "MACTHAI", "MacThai" },
		{ "MACTIBETAN", "MacTibetan" },
		{ "MACTURKISH", "MacTurkish" },
		{ "MACVIATNAMESE", "MacVietnamese" },
		{ "MACVIETNAMESE", "MacVietnamese" },
		{ "MACCURSOR", "MacCursor" },
		// Abbreviations, spelled out as the rest are.
		{ "SB", "ScrollBar" },
		{ "HORIZ", "Horizontal" },
		{ "HORZ", "Horizontal" },
		{ "VERT", "Vertical" },
		{ "SEL", "Selection" },
		{ "HALIGN", "HorizontalAlign" },
		{ "VALIGN", "VerticalAlign" },
		{ "TLW", "TopLevelWindow" },
		{ "CMN", "Common" },
		{ "DEL", "Delete" },
		{ "VAR", "Variable" },
		{ "AUTH", "Authentication" },
		{ "GOTO", "GoTo" },
		{ "3RD", "Third" },
	};

	private static readonly Dictionary<string, string> ParameterNames = new(StringComparer.Ordinal) {
		{ "pos", "position" },
		{ "orient", "orientation" },
		{ "winid", "id" },
		{ "colFg", "foreground" },
		{ "colBg", "background" },
		// Hungarian and abbreviated names, spelled out, and one name for one thing.
		{ "bCase", "caseSensitive" },
		{ "sa", "startAngle" },
		{ "ea", "endAngle" },
		{ "xs", "xScale" },
		{ "ys", "yScale" },
		{ "noUnitsX", "unitsX" },
		{ "noUnitsY", "unitsY" },
		{ "xPos", "xPosition" },
		{ "yPos", "yPosition" },
		{ "inc", "increment" },
		{ "bitmap1", "bitmap" },
		{ "dim", "dimension" },
		{ "tsize", "textSize" },
		{ "but", "button" },
		{ "bmpDisabled", "disabledBitmap" },
		{ "colForDark", "darkColor" },
		{ "colForLight", "lightColor" },
		{ "colname", "colorName" },
		{ "defaultDir", "defaultDirectory" },
		{ "defaultRes", "defaultResult" },
		{ "dstHeight", "destinationHeight" },
		{ "dstWidth", "destinationWidth" },
		{ "srcHeight", "sourceHeight" },
		{ "srcWidth", "sourceWidth" },
		{ "xdest", "xDestination" },
		{ "ydest", "yDestination" },
		{ "xsrc", "xSource" },
		{ "ysrc", "ySource" },
		{ "xsrcMask", "xSourceMask" },
		{ "ysrcMask", "ySourceMask" },
		{ "exFlag", "extraFlag" },
		{ "helptext", "helpText" },
		{ "hflag", "horizontal" },
		{ "vflag", "vertical" },
		{ "horz", "horizontal" },
		{ "vert", "vertical" },
		{ "hgap", "horizontalGap" },
		{ "vgap", "verticalGap" },
		{ "minW", "minWidth" },
		{ "minH", "minHeight" },
		{ "maxW", "maxWidth" },
		{ "maxH", "maxHeight" },
		{ "incW", "widthIncrement" },
		{ "incH", "heightIncrement" },
		{ "len", "length" },
		{ "lineNo", "lineNumber" },
		{ "logicalFunc", "logicalFunction" },
		{ "maxVal", "maxValue" },
		{ "minVal", "minValue" },
		{ "menubar", "menuBar" },
		{ "mimetype", "mimeType" },
		{ "nDirection", "direction" },
		{ "nPage", "page" },
		{ "ptStart", "start" },
		{ "ptEnd", "end" },
		{ "pt1", "point1" },
		{ "pt2", "point2" },
		{ "retCode", "returnCode" },
		{ "selImage", "selectedImage" },
		{ "styleNum", "styleNumber" },
		{ "val", "value" },
		{ "winNew", "newWindow" },
		{ "winOld", "oldWindow" },
		{ "cmd", "command" },
		{ "xoff", "xOffset" },
		{ "yoff", "yOffset" },
		{ "hotspot", "hotSpot" },
		{ "tooltip", "toolTip" },
		{ "indexAccel", "acceleratorIndex" },
		{ "startPos", "startPosition" },
		{ "endPos", "endPosition" },
		{ "minPos", "minPosition" },
		{ "maxPos", "maxPosition" },
		{ "pagePos", "pagePosition" },
		{ "tickPos", "tickPosition" },
		{ "posStart", "startPosition" },
		{ "keyWords", "keywords" },
		{ "keyWordSet", "keywordSet" },
		{ "dir", "direction" },
		{ "sz", "size" },
		{ "pt", "point" },
		{ "win", "window" },
		{ "string", "text" },
		{ "str", "text" },
		{ "n", "index" },
		{ "img", "image" },
		{ "col", "column" },
		{ "bmp", "bitmap" },
		{ "idx", "index" },
	};

	// What the common events are called, in the way .NET names events. An event the table leaves
	// out is named after its macro, as EVT_TEXT_ENTER becomes TextEnter.
	private static readonly Dictionary<string, string> EventNames = new(StringComparer.Ordinal) {
		{ "EVT_ACTIVATE", "Activated" },
		{ "EVT_CHILD_FOCUS", "ChildFocused" },
		{ "EVT_CLOSE", "Closing" },
		{ "EVT_DPI_CHANGED", "DpiChanged" },
		{ "EVT_ENTER_WINDOW", "MouseEnter" },
		{ "EVT_FULLSCREEN", "FullScreenChanged" },
		{ "EVT_HELP", "HelpRequested" },
		{ "EVT_ICONIZE", "Iconized" },
		{ "EVT_KILL_FOCUS", "LostFocus" },
		{ "EVT_LEAVE_WINDOW", "MouseLeave" },
		{ "EVT_MAXIMIZE", "Maximized" },
		{ "EVT_MENU_CLOSE", "MenuClosed" },
		{ "EVT_MENU_HIGHLIGHT", "MenuHighlighted" },
		{ "EVT_MENU_OPEN", "MenuOpened" },
		{ "EVT_MOUSEWHEEL", "MouseWheel" },
		{ "EVT_MOVE", "Moved" },
		{ "EVT_MOVE_END", "MoveEnded" },
		{ "EVT_MOVE_START", "MoveStarted" },
		{ "EVT_SET_FOCUS", "GotFocus" },
		{ "EVT_SHOW", "VisibilityChanged" },
		{ "EVT_SIZE", "SizeChanged" },
		{ "EVT_SYS_COLOUR_CHANGED", "SystemColorsChanged" },
		{ "EVT_TEXT_MAXLEN", "MaxLengthReached" },
		{ "EVT_TEXT_URL", "UrlClicked" },
		// The controls' own events.
		{ "EVT_BOOKCTRL_PAGE_CHANGED", "PageChanged" },
		{ "EVT_BOOKCTRL_PAGE_CHANGING", "PageChanging" },
		{ "EVT_CHECKBOX", "CheckedChanged" },
		{ "EVT_CHECKLISTBOX", "ItemCheckedChanged" },
		{ "EVT_CHOICE", "SelectionChanged" },
		{ "EVT_COLLAPSIBLEPANE_CHANGED", "CollapsedChanged" },
		{ "EVT_COMBOBOX", "SelectionChanged" },
		{ "EVT_COMBOBOX_CLOSEUP", "DropDownClosed" },
		{ "EVT_COMBOBOX_DROPDOWN", "DropDownOpened" },
		{ "EVT_HYPERLINK", "Click" },
		// A control's events don't repeat its name, so wxListCtrl's EVT_LIST_ITEM_SELECTED is ItemSelected.
		{ "EVT_LIST_BEGIN_DRAG", "BeginDrag" },
		{ "EVT_LIST_BEGIN_RDRAG", "BeginRightDrag" },
		{ "EVT_LIST_BEGIN_LABEL_EDIT", "BeginLabelEdit" },
		{ "EVT_LIST_END_LABEL_EDIT", "EndLabelEdit" },
		{ "EVT_LIST_CACHE_HINT", "CacheHint" },
		{ "EVT_LIST_COL_BEGIN_DRAG", "ColumnBeginDrag" },
		{ "EVT_LIST_COL_CLICK", "ColumnClick" },
		{ "EVT_LIST_COL_DRAGGING", "ColumnDragging" },
		{ "EVT_LIST_COL_END_DRAG", "ColumnEndDrag" },
		{ "EVT_LIST_COL_RIGHT_CLICK", "ColumnRightClick" },
		{ "EVT_LIST_DELETE_ALL_ITEMS", "AllItemsDeleted" },
		{ "EVT_LIST_DELETE_ITEM", "ItemDeleted" },
		{ "EVT_LIST_INSERT_ITEM", "ItemInserted" },
		{ "EVT_LIST_ITEM_ACTIVATED", "ItemActivated" },
		{ "EVT_LIST_ITEM_CHECKED", "ItemChecked" },
		{ "EVT_LIST_ITEM_UNCHECKED", "ItemUnchecked" },
		{ "EVT_LIST_ITEM_DESELECTED", "ItemDeselected" },
		{ "EVT_LIST_ITEM_FOCUSED", "ItemFocused" },
		{ "EVT_LIST_ITEM_MIDDLE_CLICK", "ItemMiddleClick" },
		{ "EVT_LIST_ITEM_RIGHT_CLICK", "ItemRightClick" },
		{ "EVT_LIST_ITEM_SELECTED", "ItemSelected" },
		{ "EVT_LIST_KEY_DOWN", "ItemKeyDown" },
		{ "EVT_TREE_BEGIN_DRAG", "BeginDrag" },
		{ "EVT_TREE_BEGIN_RDRAG", "BeginRightDrag" },
		{ "EVT_TREE_END_DRAG", "EndDrag" },
		{ "EVT_TREE_BEGIN_LABEL_EDIT", "BeginLabelEdit" },
		{ "EVT_TREE_END_LABEL_EDIT", "EndLabelEdit" },
		{ "EVT_TREE_DELETE_ITEM", "ItemDeleted" },
		{ "EVT_TREE_GET_INFO", "ItemInfoRequested" },
		{ "EVT_TREE_SET_INFO", "ItemInfoSet" },
		{ "EVT_TREE_ITEM_ACTIVATED", "ItemActivated" },
		{ "EVT_TREE_ITEM_COLLAPSED", "ItemCollapsed" },
		{ "EVT_TREE_ITEM_COLLAPSING", "ItemCollapsing" },
		{ "EVT_TREE_ITEM_EXPANDED", "ItemExpanded" },
		{ "EVT_TREE_ITEM_EXPANDING", "ItemExpanding" },
		{ "EVT_TREE_ITEM_GETTOOLTIP", "ItemToolTipRequested" },
		{ "EVT_TREE_ITEM_MENU", "ItemContextMenu" },
		{ "EVT_TREE_ITEM_MIDDLE_CLICK", "ItemMiddleClick" },
		{ "EVT_TREE_ITEM_RIGHT_CLICK", "ItemRightClick" },
		{ "EVT_TREE_KEY_DOWN", "ItemKeyDown" },
		{ "EVT_TREE_SEL_CHANGED", "SelectionChanged" },
		{ "EVT_TREE_SEL_CHANGING", "SelectionChanging" },
		{ "EVT_TREE_STATE_IMAGE_CLICK", "StateImageClick" },
		{ "EVT_DATAVIEW_CACHE_HINT", "CacheHint" },
		{ "EVT_DATAVIEW_COLUMN_HEADER_CLICK", "ColumnHeaderClick" },
		{ "EVT_DATAVIEW_COLUMN_HEADER_RIGHT_CLICK", "ColumnHeaderRightClick" },
		{ "EVT_DATAVIEW_COLUMN_REORDERED", "ColumnReordered" },
		{ "EVT_DATAVIEW_COLUMN_SORTED", "ColumnSorted" },
		{ "EVT_DATAVIEW_ITEM_ACTIVATED", "ItemActivated" },
		{ "EVT_DATAVIEW_ITEM_BEGIN_DRAG", "ItemBeginDrag" },
		{ "EVT_DATAVIEW_ITEM_COLLAPSED", "ItemCollapsed" },
		{ "EVT_DATAVIEW_ITEM_COLLAPSING", "ItemCollapsing" },
		{ "EVT_DATAVIEW_ITEM_EXPANDED", "ItemExpanded" },
		{ "EVT_DATAVIEW_ITEM_EXPANDING", "ItemExpanding" },
		{ "EVT_DATAVIEW_ITEM_CONTEXT_MENU", "ItemContextMenu" },
		{ "EVT_DATAVIEW_ITEM_DROP", "ItemDrop" },
		{ "EVT_DATAVIEW_ITEM_DROP_POSSIBLE", "ItemDropPossible" },
		{ "EVT_DATAVIEW_ITEM_EDITING_DONE", "ItemEditingDone" },
		{ "EVT_DATAVIEW_ITEM_EDITING_STARTED", "ItemEditingStarted" },
		{ "EVT_DATAVIEW_ITEM_START_EDITING", "ItemEditingStarting" },
		{ "EVT_DATAVIEW_ITEM_VALUE_CHANGED", "ItemValueChanged" },
		{ "EVT_DATAVIEW_SELECTION_CHANGED", "SelectionChanged" },
		{ "EVT_TOOL", "ToolClicked" },
		{ "EVT_TOOL_RCLICKED", "ToolRightClicked" },
		{ "EVT_TOOL_DROPDOWN", "ToolDropDown" },
		{ "EVT_TOOL_ENTER", "ToolHovered" },
		{ "EVT_INIT_DIALOG", "Initializing" },
		{ "EVT_TREELIST_COLUMN_SORTED", "ColumnSorted" },
		{ "EVT_TREELIST_ITEM_ACTIVATED", "ItemActivated" },
		{ "EVT_TREELIST_ITEM_CHECKED", "ItemChecked" },
		{ "EVT_TREELIST_ITEM_CONTEXT_MENU", "ItemContextMenu" },
		{ "EVT_TREELIST_ITEM_EXPANDED", "ItemExpanded" },
		{ "EVT_TREELIST_ITEM_EXPANDING", "ItemExpanding" },
		{ "EVT_TREELIST_SELECTION_CHANGED", "SelectionChanged" },
		{ "EVT_DIRCTRL_FILEACTIVATED", "FileActivated" },
		{ "EVT_DIRCTRL_SELECTIONCHANGED", "SelectionChanged" },
		{ "EVT_FILECTRL_FILEACTIVATED", "FileActivated" },
		{ "EVT_FILECTRL_FILTERCHANGED", "FilterChanged" },
		{ "EVT_FILECTRL_FOLDERCHANGED", "FolderChanged" },
		{ "EVT_FILECTRL_SELECTIONCHANGED", "SelectionChanged" },
		{ "EVT_WIZARD_BEFORE_PAGE_CHANGED", "BeforePageChanged" },
		{ "EVT_WIZARD_CANCEL", "Canceling" },
		{ "EVT_WIZARD_FINISHED", "Finished" },
		{ "EVT_WIZARD_HELP", "HelpRequested" },
		{ "EVT_WIZARD_PAGE_CHANGED", "PageChanged" },
		{ "EVT_WIZARD_PAGE_CHANGING", "PageChanging" },
		{ "EVT_WIZARD_PAGE_SHOWN", "PageShown" },
		{ "EVT_FIND_CLOSE", "FindClosed" },
		{ "EVT_LISTBOX", "SelectionChanged" },
		{ "EVT_LISTBOX_DCLICK", "ItemDoubleClicked" },
		{ "EVT_NOTEBOOK_PAGE_CHANGED", "PageChanged" },
		{ "EVT_NOTEBOOK_PAGE_CHANGING", "PageChanging" },
		{ "EVT_LISTBOOK_PAGE_CHANGED", "PageChanged" },
		{ "EVT_LISTBOOK_PAGE_CHANGING", "PageChanging" },
		{ "EVT_CHOICEBOOK_PAGE_CHANGED", "PageChanged" },
		{ "EVT_CHOICEBOOK_PAGE_CHANGING", "PageChanging" },
		{ "EVT_TOOLBOOK_PAGE_CHANGED", "PageChanged" },
		{ "EVT_TOOLBOOK_PAGE_CHANGING", "PageChanging" },
		{ "EVT_TREEBOOK_PAGE_CHANGED", "PageChanged" },
		{ "EVT_TREEBOOK_PAGE_CHANGING", "PageChanging" },
		{ "EVT_TREEBOOK_NODE_COLLAPSED", "NodeCollapsed" },
		{ "EVT_TREEBOOK_NODE_EXPANDED", "NodeExpanded" },
		{ "EVT_RADIOBOX", "SelectionChanged" },
		{ "EVT_RADIOBUTTON", "Selected" },
		{ "EVT_SEARCH", "SearchRequested" },
		{ "EVT_SEARCH_CANCEL", "SearchCanceled" },
		{ "EVT_SLIDER", "ValueChanged" },
		{ "EVT_SPINCTRL", "ValueChanged" },
		{ "EVT_SPINCTRLDOUBLE", "ValueChanged" },
		{ "EVT_SPLITTER_DCLICK", "SashDoubleClicked" },
		{ "EVT_SPLITTER_SASH_POS_CHANGED", "SashPositionChanged" },
		{ "EVT_SPLITTER_SASH_POS_CHANGING", "SashPositionChanging" },
		{ "EVT_SPLITTER_SASH_POS_RESIZE", "SashPositionResizing" },
		{ "EVT_SPLITTER_UNSPLIT", "WindowRemoved" },
		{ "EVT_TOGGLEBUTTON", "Toggled" },
	};

	private static readonly HashSet<string> Keywords = ["base", "checked", "class", "default", "event", "fixed", "lock", "object", "operator", "out", "params", "ref", "string", "this"];

	/// <summary>wxButton to Button.</summary>
	public static string Type(string name) => Acronyms(Respell(name.StartsWith("wx", StringComparison.Ordinal) ? name[2..] : name));

	/// <summary>A method or property name, respelled. A leading digit is spelled out, as Get3StateValue gives ThreeStateValue.</summary>
	public static string Member(string name) => Acronyms(Respell(name.Length > 0 && char.IsDigit(name[0]) ? Digits[name[0] - '0'] + name[1..] : name));

	private static readonly string[] Digits = ["Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine"];

	public static string Parameter(string name) {
		string renamed = ParameterNames.GetValueOrDefault(name, name);
		// A snake_case name, such as model_column, becomes modelColumn.
		if (renamed.Contains('_', StringComparison.Ordinal)) renamed = string.Concat(renamed.Split('_', StringSplitOptions.RemoveEmptyEntries).Select(static (word, index) => index == 0 ? word : char.ToUpperInvariant(word[0]) + word[1..]));
		string pascal = Acronyms(Respell(char.ToUpperInvariant(renamed[0]) + renamed[1..]));
		string camel = char.ToLowerInvariant(pascal[0]) + pascal[1..];
		return Keywords.Contains(camel) ? "@" + camel : camel;
	}

	/// <summary>The name of the event a macro such as EVT_SIZE handles, such as SizeChanged.</summary>
	public static string Event(string macro) => EventNames.TryGetValue(macro, out string? name) ? name : Words(macro["EVT_".Length..]);

	/// <summary>wxEVT_CLOSE_WINDOW to CloseWindow, with abbreviations spelled out, so wxEVT_TREE_SEL_CHANGED is TreeSelectionChanged.</summary>
	public static string EventType(string constant) => EventTypeNames.TryGetValue(constant, out string? name) ? name : Words(string.Join('_', constant["wxEVT_".Length..].Split('_').Select(static word => EventTypeWords.GetValueOrDefault(word, word))));

	// Event types named for what wxWidgets does, rather than for what happened, named as their
	// events are.
	private static readonly Dictionary<string, string> EventTypeNames = new(StringComparer.Ordinal) {
		{ "wxEVT_SET_FOCUS", "GotFocus" },
		{ "wxEVT_KILL_FOCUS", "LostFocus" },
		{ "wxEVT_SET_CURSOR", "CursorRequested" },
		{ "wxEVT_COMMAND_SET_FOCUS", "CommandGotFocus" },
		{ "wxEVT_COMMAND_KILL_FOCUS", "CommandLostFocus" },
		{ "wxEVT_UPDATE_UI", "UpdateUI" },
		{ "wxEVT_STC_UPDATEUI", "StyledTextUpdateUI" },
		{ "wxEVT_DATAVIEW_ITEM_START_EDITING", "DataViewItemEditingStarting" },
		{ "wxEVT_STC_DO_DROP", "StyledTextTextDropping" },
		{ "wxEVT_TREE_ITEM_MENU", "TreeItemContextMenu" },
		{ "wxEVT_SPLITTER_UNSPLIT", "SplitterWindowRemoved" },
		{ "wxEVT_CALENDAR_WEEKDAY_CLICKED", "CalendarWeekDayClicked" },
		{ "wxEVT_CALENDAR_DOUBLECLICKED", "CalendarDateActivated" },
		{ "wxEVT_FIND", "FindRequested" },
		{ "wxEVT_FIND_NEXT", "FindNextRequested" },
		{ "wxEVT_FIND_REPLACE", "ReplaceRequested" },
		{ "wxEVT_FIND_REPLACE_ALL", "ReplaceAllRequested" },
		{ "wxEVT_FIND_CLOSE", "FindClosed" },
	};

	private static readonly Dictionary<string, string> EventTypeWords = new(StringComparer.Ordinal) {
		{ "SEL", "SELECTION" },
		{ "COL", "COLUMN" },
		{ "POS", "POSITION" },
		// Words wxWidgets runs together in some event types, such as wxEVT_DIRCTRL_FILEACTIVATED.
		{ "DIRCTRL", "GenericDirCtrl" },
		{ "FILECTRL", "FILE_CTRL" },
		{ "TREELIST", "TREE_LIST" },
		{ "FILEACTIVATED", "FILE_ACTIVATED" },
		{ "SELECTIONCHANGED", "SELECTION_CHANGED" },
		{ "FILTERCHANGED", "FILTER_CHANGED" },
		{ "FOLDERCHANGED", "FOLDER_CHANGED" },
		{ "DCLICK", "DOUBLE_CLICK" },
		{ "DOUBLECLICK", "DOUBLE_CLICK" },
		{ "DOUBLECLICKED", "DOUBLE_CLICKED" },
		{ "RCLICKED", "RIGHT_CLICKED" },
		{ "RDRAG", "RIGHT_DRAG" },
		{ "MOUSEWHEEL", "MOUSE_WHEEL" },
		{ "CLOSEUP", "CLOSE_UP" },
		{ "DROPDOWN", "DROP_DOWN" },
		{ "MAXLEN", "MAX_LENGTH" },
		{ "GETTOOLTIP", "GET_TOOL_TIP" },
		{ "NEWWINDOW", "NEW_WINDOW" },
		// Controls, spelled as their classes are.
		{ "CHECKLISTBOX", "CheckListBox" },
		{ "COLLAPSIBLEPANE", "CollapsiblePane" },
		{ "RADIOBOX", "RadioBox" },
		{ "SPINCTRL", "SpinCtrl" },
		{ "SPINCTRLDOUBLE", "SpinCtrlDouble" },
		{ "TOGGLEBUTTON", "ToggleButton" },
		{ "WEBVIEW", "WebView" },
		{ "TASKBAR", "TaskBar" },
		{ "STC", "StyledText" },
		// The styled text control's, which Scintilla runs together.
		{ "AUTOCOMP", "AutoComp" },
		{ "CALLTIP", "CALL_TIP" },
		{ "CHARADDED", "CHAR_ADDED" },
		{ "DWELLEND", "DWELL_END" },
		{ "DWELLSTART", "DWELL_START" },
		{ "MACRORECORD", "MACRO_RECORD" },
		{ "MARGINCLICK", "MARGIN_CLICK" },
		{ "NEEDSHOWN", "NEED_SHOWN" },
		{ "ROMODIFYATTEMPT", "READ_ONLY_MODIFY_ATTEMPT" },
		{ "SAVEPOINTLEFT", "SAVE_POINT_LEFT" },
		{ "SAVEPOINTREACHED", "SAVE_POINT_REACHED" },
		{ "STYLENEEDED", "STYLE_NEEDED" },
		{ "UPDATEUI", "UPDATE_UI" },
		{ "USERLISTSELECTION", "USER_LIST_SELECTION" },
	};

	/// <summary>
	/// A style constant's name inside its class's enum. wxTE_MULTILINE becomes MultiLine: the prefix
	/// most of the class's styles share is dropped, as is one naming the class, and
	/// wxDEFAULT_FRAME_STYLE becomes Default.
	/// </summary>
	public static string Style(string constant, string? sharedPrefix, string className) {
		string[] words = constant[2..].Split('_', StringSplitOptions.RemoveEmptyEntries);
		string classWord = Type(className).ToUpperInvariant();
		// A style borrowed from another control, such as a combo box's wxTE_PROCESS_ENTER, loses that
		// control's prefix too.
		if (words.Length > 1 && (words[0] == sharedPrefix || words[0] == classWord || BorrowedStylePrefixes.Contains(words[0]))) words = words[1..];
		if (words is ["DEFAULT", .., "STYLE"]) return "Default";
		return Words(string.Join('_', words));
	}

	private static readonly HashSet<string> BorrowedStylePrefixes = ["TE", "CB", "PB"];

	/// <summary>
	/// The short prefix a class's own styles share, such as TE for wxTE_MULTILINE or SP for
	/// wxSP_ARROW_KEYS. Longer words, such as ICON in wxICON_ERROR, are part of the name, and so is a
	/// word that is a constant itself, such as YES in wxYES_NO, since wxYES exists.
	/// </summary>
	public static string? SharedStylePrefix(IEnumerable<string> constants, Func<string, bool>? isConstant = null) {
		List<string> prefixes = [.. constants.Select(static name => name[2..].Split('_')).Where(words => words.Length > 1 && words[0].Length <= 3 && isConstant?.Invoke("wx" + words[0]) != true).Select(static words => words[0])];
		if (prefixes.Count == 0) return null;
		IGrouping<string, string> common = prefixes.GroupBy(static prefix => prefix).OrderByDescending(static group => group.Count()).First();
		return common.Count() >= 2 ? common.Key : null;
	}

	/// <summary>UPPER_CASE_WORDS to PascalCase, respelled.</summary>
	public static string Words(string upper) {
		StringBuilder result = new();
		foreach (string word in upper.Split('_', StringSplitOptions.RemoveEmptyEntries)) {
			if (CompoundWords.TryGetValue(word, out string? compound) || StyledTextWords.Compounds.TryGetValue(word, out compound)) {
				result.Append(compound);
				continue;
			}
			// A word already in mixed case, such as RightToLeft in wxLayout_RightToLeft, keeps its own
			// casing. A leading digit is spelled out, since an identifier cannot start with one, but
			// only at the start, so ZOOM_100 is Zoom100.
			string rest = word.Any(char.IsLower) ? word[1..] : word[1..].ToLower(CultureInfo.InvariantCulture);
			if (char.IsDigit(word[0]) && result.Length == 0) {
				result.Append(Digits[word[0] - '0']);
				if (rest.Length > 0) result.Append(char.ToUpperInvariant(rest[0])).Append(rest[1..]);
				continue;
			}
			// A number after a number keeps the underscore between them, so ISO8859_10 is Iso8859_10.
			if (char.IsDigit(word[0]) && char.IsDigit(result[^1])) result.Append('_');
			result.Append(char.ToUpperInvariant(word[0])).Append(rest);
		}
		return Respell(result.ToString());
	}

	/// <summary>
	/// An acronym of three letters or more in Pascal case, as .NET writes them, so GetURL's URL is
	/// Url and FromDIP is FromDip. Two letters stay upper case, as UI and DC do in .NET, except OK,
	/// which is Ok, as StandardId.Ok is. An acronym
	/// ends where a capital starts a lowercase word, so RGBAImage is RgbaImage and VCHome keeps VC.
	/// </summary>
	public static string Acronyms(string identifier) {
		StringBuilder result = new();
		int index = 0;
		while (index < identifier.Length) {
			if (!char.IsUpper(identifier[index])) {
				result.Append(identifier[index++]);
				continue;
			}
			int end = index;
			while (end < identifier.Length && char.IsUpper(identifier[end])) end++;
			// The last capital of a run that a lowercase letter follows starts the next word.
			int acronymEnd = end < identifier.Length && char.IsLower(identifier[end]) && end - index > 1 ? end - 1 : end;
			string run = identifier[index..acronymEnd];
			result.Append(run.Length >= 3 || run == "OK" ? run[0] + run[1..].ToLower(CultureInfo.InvariantCulture) : run);
			index = acronymEnd;
		}
		return result.ToString();
	}

	private static string Respell(string identifier) {
		StringBuilder result = new();
		int start = 0;
		for (int index = 1; index <= identifier.Length; index++) {
			if (index < identifier.Length && !char.IsUpper(identifier[index])) continue;
			string word = identifier[start..index];
			result.Append(Respellings.GetValueOrDefault(word, word));
			start = index;
		}
		return result.ToString();
	}
}
