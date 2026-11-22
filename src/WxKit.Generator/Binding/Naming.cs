using System.Globalization;
using System.Text;

namespace WxKit.Generator.Binding;

// The rules that turn wxWidgets names into .NET ones.
internal static class Naming {
	// British spellings wxWidgets uses, and the .NET ones WxKit uses instead, by whole word.
	private static readonly Dictionary<string, string> Respellings = new(StringComparer.Ordinal) {
		["Centre"] = "Center",
		["Colour"] = "Color",
		["Colours"] = "Colors",
		["Grey"] = "Gray",
		["Behaviour"] = "Behavior",
		["Initialise"] = "Initialize",
		["Normalise"] = "Normalize",
		["Customise"] = "Customize",
		["Favourite"] = "Favorite",
		["Neighbour"] = "Neighbor",
	};

	// Words that wxWidgets writes run together in upper case, so they cannot be split on their own.
	private static readonly Dictionary<string, string> CompoundWords = new(StringComparer.Ordinal) {
		["MULTILINE"] = "MultiLine",
		["READONLY"] = "ReadOnly",
		["NOHIDESEL"] = "NoHideSelection",
		["DONTWRAP"] = "DontWrap",
		["CHARWRAP"] = "CharWrap",
		["WORDWRAP"] = "WordWrap",
		["BESTWRAP"] = "BestWrap",
		// The pickers, their styles, and their events.
		["COLOURPICKER"] = "ColorPicker",
		["FILEPICKER"] = "FilePicker",
		["DIRPICKER"] = "DirPicker",
		["FONTPICKER"] = "FontPicker",
		["TEXTCTRL"] = "TextCtrl",
		["FONTDESC"] = "FontDesc",
		["USEFONT"] = "UseFont",
		["STATUSITEM"] = "StatusItem",
		// The mouse buttons, as keys.
		["LBUTTON"] = "LeftButton",
		["RBUTTON"] = "RightButton",
		["MBUTTON"] = "MiddleButton",
		["NUMLOCK"] = "NumLock",
		["ALTGR"] = "AltGr",
		// The system colors, which run their words together as Windows's COLOR_ constants do.
		["ACTIVECAPTION"] = "ActiveCaption",
		["INACTIVECAPTION"] = "InactiveCaption",
		["INACTIVECAPTIONTEXT"] = "InactiveCaptionText",
		["GRADIENTACTIVECAPTION"] = "GradientActiveCaption",
		["GRADIENTINACTIVECAPTION"] = "GradientInactiveCaption",
		["CAPTIONTEXT"] = "CaptionText",
		["ACTIVEBORDER"] = "ActiveBorder",
		["INACTIVEBORDER"] = "InactiveBorder",
		["WINDOWFRAME"] = "WindowFrame",
		["WINDOWTEXT"] = "WindowText",
		["MENUTEXT"] = "MenuText",
		["MENUHILIGHT"] = "MenuHighlight",
		["APPWORKSPACE"] = "AppWorkspace",
		["HIGHLIGHTTEXT"] = "HighlightText",
		["HOTLIGHT"] = "HotLight",
		["GRAYTEXT"] = "GrayText",
		["INFOTEXT"] = "InfoText",
		["INFOBK"] = "InfoBackground",
		["BTNFACE"] = "ButtonFace",
		["BTNSHADOW"] = "ButtonShadow",
		["BTNTEXT"] = "ButtonText",
		["BTNHIGHLIGHT"] = "ButtonHighlight",
		["BTNHILIGHT"] = "ButtonHilight",
		["3DDKSHADOW"] = "ThreeDDarkShadow",
		["3DLIGHT"] = "ThreeDLight",
		["3DFACE"] = "ThreeDFace",
		["3DSHADOW"] = "ThreeDShadow",
		["3DHIGHLIGHT"] = "ThreeDHighlight",
		["LISTBOX"] = "ListBox",
		["LISTBOXTEXT"] = "ListBoxText",
		["LISTBOXHIGHLIGHT"] = "ListBoxHighlight",
		["LISTBOXHIGHLIGHTTEXT"] = "ListBoxHighlightText",
		["GRIDLINES"] = "GridLines",
		["WRAPSIZER"] = "WrapSizer",
		["CONTEXTHELP"] = "ContextHelp",
		// The scroll events.
		["SCROLLWIN"] = "ScrollWin",
		["LINEUP"] = "LineUp",
		["LINEDOWN"] = "LineDown",
		["PAGEUP"] = "PageUp",
		["PAGEDOWN"] = "PageDown",
		["THUMBTRACK"] = "ThumbTrack",
		["THUMBRELEASE"] = "ThumbRelease",
		// The full screen styles and the update UI flags.
		["FULLSCREEN"] = "FullScreen",
		["NOMENUBAR"] = "NoMenuBar",
		["NOTOOLBAR"] = "NoToolBar",
		["NOSTATUSBAR"] = "NoStatusBar",
		["NOBORDER"] = "NoBorder",
		["NOCAPTION"] = "NoCaption",
		["FROMIDLE"] = "FromIdle",
		["EXACTFIT"] = "ExactFit",
		["NOTEXT"] = "NoText",
		["ICONIZE"] = "Iconize",
		["2STATE"] = "TwoState",
		["3STATE"] = "ThreeState",
		["3D"] = "ThreeD",
		["3DSASH"] = "ThreeDSash",
		["3DBORDER"] = "ThreeDBorder",
		["DATAVIEW"] = "DataView",
		// So do the standard ids.
		["SAVEAS"] = "SaveAs",
		["SELECTALL"] = "SelectAll",
		["LARGEICONS"] = "LargeIcons",
		["SMALLICONS"] = "SmallIcons",
		["SORTDATE"] = "SortDate",
		["SORTNAME"] = "SortName",
		["SORTSIZE"] = "SortSize",
		["SORTTYPE"] = "SortType",
		["YESTOALL"] = "YesToAll",
		["NOTOALL"] = "NoToAll",
		["CDROM"] = "CdRom",
		["HARDDISK"] = "HardDisk",
		// The find dialog's flags and styles run theirs together too.
		["MATCHCASE"] = "MatchCase",
		["WHOLEWORD"] = "WholeWord",
		["REPLACEDIALOG"] = "ReplaceDialog",
		["NOUPDOWN"] = "NoUpDown",
		["NOMATCHCASE"] = "NoMatchCase",
		["NOWHOLEWORD"] = "NoWholeWord",
		// The accessibility roles, states, and events run their words together.
		["BUTTONDROPDOWN"] = "ButtonDropDown",
		["BUTTONDROPDOWNGRID"] = "ButtonDropDownGrid",
		["BUTTONMENU"] = "ButtonMenu",
		["CHECKBUTTON"] = "CheckButton",
		["COLUMNHEADER"] = "ColumnHeader",
		["COMBOBOX"] = "ComboBox",
		["DROPLIST"] = "DropList",
		["HELPBALLOON"] = "HelpBalloon",
		["HOTKEYFIELD"] = "HotkeyField",
		["LISTITEM"] = "ListItem",
		["MENUBAR"] = "MenuBar",
		["MENUITEM"] = "MenuItem",
		["MENUPOPUP"] = "MenuPopup",
		["OUTLINEITEM"] = "OutlineItem",
		["PAGETAB"] = "PageTab",
		["PAGETABLIST"] = "PageTabList",
		["PROGRESSBAR"] = "ProgressBar",
		["PROPERTYPAGE"] = "PropertyPage",
		["PUSHBUTTON"] = "PushButton",
		["RADIOBUTTON"] = "RadioButton",
		["ROWHEADER"] = "RowHeader",
		["SCROLLBAR"] = "ScrollBar",
		["SPINBUTTON"] = "SpinButton",
		["STATICTEXT"] = "StaticText",
		["STATUSBAR"] = "StatusBar",
		["TITLEBAR"] = "TitleBar",
		["TOOLBAR"] = "ToolBar",
		["TOOLTIP"] = "ToolTip",
		["WHITESPACE"] = "WhiteSpace",
		["SYSMENU"] = "SystemMenu",
		["VSCROLL"] = "VerticalScrollBar",
		["HSCROLL"] = "HorizontalScrollBar",
		["SIZEGRIP"] = "SizeGrip",
		["FIRSTCHILD"] = "FirstChild",
		["LASTCHILD"] = "LastChild",
		["TAKEFOCUS"] = "TakeFocus",
		["TAKESELECTION"] = "TakeSelection",
		["EXTENDSELECTION"] = "ExtendSelection",
		["ADDSELECTION"] = "AddSelection",
		["REMOVESELECTION"] = "RemoveSelection",
		["EXTSELECTABLE"] = "ExtSelectable",
		["HOTTRACKED"] = "HotTracked",
		["MULTISELECTABLE"] = "MultiSelectable",
		["SELFVOICING"] = "SelfVoicing",
		["MENUSTART"] = "MenuStart",
		["MENUEND"] = "MenuEnd",
		["MENUPOPUPSTART"] = "MenuPopupStart",
		["MENUPOPUPEND"] = "MenuPopupEnd",
		["CAPTURESTART"] = "CaptureStart",
		["CAPTUREEND"] = "CaptureEnd",
		["MOVESIZESTART"] = "MoveSizeStart",
		["MOVESIZEEND"] = "MoveSizeEnd",
		["CONTEXTHELPSTART"] = "ContextHelpStart",
		["CONTEXTHELPEND"] = "ContextHelpEnd",
		["DRAGDROPSTART"] = "DragDropStart",
		["DRAGDROPEND"] = "DragDropEnd",
		["DIALOGSTART"] = "DialogStart",
		["DIALOGEND"] = "DialogEnd",
		["SCROLLINGSTART"] = "ScrollingStart",
		["SCROLLINGEND"] = "ScrollingEnd",
		["SWITCHSTART"] = "SwitchStart",
		["SWITCHEND"] = "SwitchEnd",
		["MINIMIZESTART"] = "MinimizeStart",
		["MINIMIZEEND"] = "MinimizeEnd",
		["SELECTIONADD"] = "SelectionAdd",
		["SELECTIONREMOVE"] = "SelectionRemove",
		["SELECTIONWITHIN"] = "SelectionWithin",
		["STATECHANGE"] = "StateChange",
		["LOCATIONCHANGE"] = "LocationChange",
		["NAMECHANGE"] = "NameChange",
		["DESCRIPTIONCHANGE"] = "DescriptionChange",
		["VALUECHANGE"] = "ValueChange",
		["PARENTCHANGE"] = "ParentChange",
		["HELPCHANGE"] = "HelpChange",
		["DEFACTIONCHANGE"] = "DefaultActionChange",
		["ACCELERATORCHANGE"] = "AcceleratorChange",
	};

	private static readonly Dictionary<string, string> ParameterNames = new(StringComparer.Ordinal) {
		["pos"] = "position",
		["orient"] = "orientation",
		["winid"] = "id",
		["dir"] = "direction",
		["sz"] = "size",
		["pt"] = "point",
		["win"] = "window",
		["string"] = "text",
		["str"] = "text",
		["n"] = "index",
		["img"] = "image",
		["col"] = "column",
		["bmp"] = "bitmap",
		["idx"] = "index",
	};

	// What the common events are called, in the way .NET names events. An event the table leaves
	// out is named after its macro, as EVT_TEXT_ENTER becomes TextEnter.
	private static readonly Dictionary<string, string> EventNames = new(StringComparer.Ordinal) {
		["EVT_ACTIVATE"] = "Activated",
		["EVT_CHILD_FOCUS"] = "ChildFocused",
		["EVT_CLOSE"] = "Closing",
		["EVT_DPI_CHANGED"] = "DpiChanged",
		["EVT_ENTER_WINDOW"] = "MouseEnter",
		["EVT_FULLSCREEN"] = "FullScreenChanged",
		["EVT_HELP"] = "HelpRequested",
		["EVT_ICONIZE"] = "Iconized",
		["EVT_KILL_FOCUS"] = "LostFocus",
		["EVT_LEAVE_WINDOW"] = "MouseLeave",
		["EVT_MAXIMIZE"] = "Maximized",
		["EVT_MENU_CLOSE"] = "MenuClosed",
		["EVT_MENU_HIGHLIGHT"] = "MenuHighlighted",
		["EVT_MENU_OPEN"] = "MenuOpened",
		["EVT_MOUSEWHEEL"] = "MouseWheel",
		["EVT_MOVE"] = "Moved",
		["EVT_MOVE_END"] = "MoveEnded",
		["EVT_MOVE_START"] = "MoveStarted",
		["EVT_SET_FOCUS"] = "GotFocus",
		["EVT_SHOW"] = "VisibilityChanged",
		["EVT_SIZE"] = "SizeChanged",
		["EVT_SYS_COLOUR_CHANGED"] = "SystemColorsChanged",
		["EVT_TEXT_MAXLEN"] = "MaxLengthReached",
		["EVT_TEXT_URL"] = "UrlClicked",
		// The controls' own events.
		["EVT_BOOKCTRL_PAGE_CHANGED"] = "PageChanged",
		["EVT_BOOKCTRL_PAGE_CHANGING"] = "PageChanging",
		["EVT_CHECKBOX"] = "CheckedChanged",
		["EVT_CHECKLISTBOX"] = "ItemCheckedChanged",
		["EVT_CHOICE"] = "SelectionChanged",
		["EVT_COLLAPSIBLEPANE_CHANGED"] = "CollapsedChanged",
		["EVT_COMBOBOX"] = "SelectionChanged",
		["EVT_COMBOBOX_CLOSEUP"] = "DropDownClosed",
		["EVT_COMBOBOX_DROPDOWN"] = "DropDownOpened",
		["EVT_HYPERLINK"] = "Click",
		["EVT_LISTBOX"] = "SelectionChanged",
		["EVT_LISTBOX_DCLICK"] = "ItemDoubleClicked",
		["EVT_NOTEBOOK_PAGE_CHANGED"] = "PageChanged",
		["EVT_NOTEBOOK_PAGE_CHANGING"] = "PageChanging",
		["EVT_LISTBOOK_PAGE_CHANGED"] = "PageChanged",
		["EVT_LISTBOOK_PAGE_CHANGING"] = "PageChanging",
		["EVT_CHOICEBOOK_PAGE_CHANGED"] = "PageChanged",
		["EVT_CHOICEBOOK_PAGE_CHANGING"] = "PageChanging",
		["EVT_TOOLBOOK_PAGE_CHANGED"] = "PageChanged",
		["EVT_TOOLBOOK_PAGE_CHANGING"] = "PageChanging",
		["EVT_TREEBOOK_PAGE_CHANGED"] = "PageChanged",
		["EVT_TREEBOOK_PAGE_CHANGING"] = "PageChanging",
		["EVT_TREEBOOK_NODE_COLLAPSED"] = "NodeCollapsed",
		["EVT_TREEBOOK_NODE_EXPANDED"] = "NodeExpanded",
		["EVT_RADIOBOX"] = "SelectionChanged",
		["EVT_RADIOBUTTON"] = "Selected",
		["EVT_SEARCH"] = "SearchRequested",
		["EVT_SEARCH_CANCEL"] = "SearchCanceled",
		["EVT_SLIDER"] = "ValueChanged",
		["EVT_SPINCTRL"] = "ValueChanged",
		["EVT_SPINCTRLDOUBLE"] = "ValueChanged",
		["EVT_SPLITTER_DCLICK"] = "SashDoubleClicked",
		["EVT_SPLITTER_SASH_POS_CHANGED"] = "SashPositionChanged",
		["EVT_SPLITTER_SASH_POS_CHANGING"] = "SashPositionChanging",
		["EVT_SPLITTER_SASH_POS_RESIZE"] = "SashPositionResizing",
		["EVT_SPLITTER_UNSPLIT"] = "Unsplit",
		["EVT_TOGGLEBUTTON"] = "Toggled",
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
	public static string EventType(string constant) => Words(string.Join('_', constant["wxEVT_".Length..].Split('_').Select(static word => EventTypeWords.GetValueOrDefault(word, word))));

	private static readonly Dictionary<string, string> EventTypeWords = new(StringComparer.Ordinal) {
		["SEL"] = "SELECTION",
		["COL"] = "COLUMN",
		["POS"] = "POSITION",
	};

	/// <summary>
	/// A style constant's name inside its class's enum. wxTE_MULTILINE becomes MultiLine: the prefix
	/// most of the class's styles share is dropped, as is one naming the class, and
	/// wxDEFAULT_FRAME_STYLE becomes Default.
	/// </summary>
	public static string Style(string constant, string? sharedPrefix, string className) {
		string[] words = constant[2..].Split('_', StringSplitOptions.RemoveEmptyEntries);
		string classWord = Type(className).ToUpperInvariant();
		if (words.Length > 1 && (words[0] == sharedPrefix || words[0] == classWord)) words = words[1..];
		if (words is ["DEFAULT", .., "STYLE"]) return "Default";
		return Words(string.Join('_', words));
	}

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
			if (CompoundWords.TryGetValue(word, out string? compound)) {
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
	/// Url and FromDIP is FromDip. Two letters stay upper case, as UI and DC do in .NET. An acronym
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
			result.Append(run.Length >= 3 ? run[0] + run[1..].ToLower(CultureInfo.InvariantCulture) : run);
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
