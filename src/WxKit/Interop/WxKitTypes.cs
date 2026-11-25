namespace WxKit.Interop;

// Each handle type is only ever used through a pointer. As in the shim, every handle points at the
// start of the wxWidgets object, so a pointer to a derived class's handle can be cast to a base's.

/// <summary>Any wxObject.</summary>
internal struct WxkObject;

/// <summary>A wxEvtHandler, or any class derived from it.</summary>
internal struct WxkEvtHandler;

/// <summary>A wxWindow, or any class derived from it.</summary>
internal struct WxkWindow;

/// <summary>A wxSizer, or any class derived from it.</summary>
internal struct WxkSizer;

/// <summary>A wxDataViewColumn.</summary>
internal struct WxkDataViewColumn;

/// <summary>A wxToolBarToolBase.</summary>
internal struct WxkToolBarTool;

/// <summary>A row of a window that draws itself, as the shim's wxk_AccessibleRow lays it out.</summary>
internal struct WxkAccessibleRow {
	public long Index;
	public Rect Bounds;
	public byte Selected;
	public int FirstCell;
	public int CellCount;
}

/// <summary>A wxTranslations.</summary>
internal struct WxkTranslations;

/// <summary>Where a translations loader puts the catalog it found.</summary>
internal struct WxkCatalogSink;

/// <summary>The callbacks every managed translations loader calls, as the shim's wxk_TranslationsLoaderCallbacks lays them out.</summary>
internal unsafe struct WxkTranslationsLoaderCallbacks {
	public delegate* unmanaged[Cdecl]<void*, WxkString*, WxkString*, WxkCatalogSink*, void> LoadCatalog;
	public delegate* unmanaged[Cdecl]<void*, WxkString*, WxkStringArray*, void> AvailableTranslations;
	public delegate* unmanaged[Cdecl]<void*, void> OnFree;
}

/// <summary>A language, as the shim's wxk_LanguageInfo lays it out.</summary>
internal unsafe struct WxkLanguageInfo {
	public int Language;
	public WxkString* CanonicalName;
	public WxkString* LocaleTag;
	public WxkString* Description;
	public WxkString* DescriptionNative;
	public int LayoutDirection;
}

/// <summary>What an About box shows, as the shim's wxk_AboutInfo lays it out.</summary>
internal unsafe struct WxkAboutInfo {
	public WxkString* Name;
	public WxkString* Version;
	public WxkString* LongVersion;
	public WxkString* Description;
	public WxkString* Copyright;
	public WxkString* License;
	public WxkString* WebSite;
	public WxkString* WebSiteDescription;
	public WxkStringArray* Developers;
	public WxkStringArray* DocWriters;
	public WxkStringArray* Artists;
	public WxkStringArray* Translators;
	public WxkObject* Icon;
}

/// <summary>A wxMenu.</summary>
internal struct WxkMenu;

/// <summary>A wxMenuItem.</summary>
internal struct WxkMenuItem;

/// <summary>A wxEvent, or any class derived from it. Only valid while its handlers run.</summary>
internal struct WxkEvent;

/// <summary>A wxClassInfo, which lives as long as the program.</summary>
internal struct WxkClassInfo;

/// <summary>
/// Text the shim hands out, as the shim's wxk_String lays it out: its length, and then its UTF-16
/// text in the same block. Freed with <see cref="WxKitNative.StringFree"/>.
/// </summary>
internal struct WxkString {
	public nuint Length;
}

/// <summary>A wxColour's channels, and whether it is set, as the shim passes them.</summary>
internal readonly record struct WxkColor(byte Red, byte Green, byte Blue, byte Alpha, byte Valid);

/// <summary>A wxDateTime's parts, in local time, and whether it is set, as the shim passes them.</summary>
internal readonly record struct WxkDateTime(int Year, int Month, int Day, int Hour, int Minute, int Second, int Millisecond, byte Valid);

/// <summary>A list of ints or item ids, freed with <see cref="WxKitNative.NumbersFree"/>.</summary>
internal struct WxkNumbers;

/// <summary>A wxArrayString, freed with <see cref="WxKitNative.StringArrayFree"/>.</summary>
internal struct WxkStringArray;

/// <summary>A list of windows, freed with <see cref="WxKitNative.WindowListFree"/>.</summary>
internal struct WxkWindowList;

/// <summary>A wxVariant, freed with <see cref="WxKitNative.VariantFree"/>.</summary>
internal struct WxkVariant;

/// <summary>A wxVector of wxVariant, freed with <see cref="WxKitNative.VariantListFree"/>.</summary>
internal struct WxkVariantList;

/// <summary>A wxDataViewModel, which is reference counted.</summary>
internal struct WxkDataViewModel;

/// <summary>The wxDataViewItemArray a model fills with an item's children.</summary>
internal struct WxkDataViewItems;

/// <summary>The types of value a wxVariant crosses with, as the shim numbers them.</summary>
internal enum WxkVariantType {
	Null = 0,
	String = 1,
	Bool = 2,
	Long = 3,
	Double = 4,
	Bitmap = 5,
	Icon = 6,
	IconText = 7,
}

/// <summary>An accessible object, which its window owns.</summary>
internal struct WxkAccessible;

/// <summary>Clipboard content in several formats, which <see cref="WxKitNative.ClipboardSetContent"/> takes ownership of.</summary>
internal struct WxkClipboardContent;

/// <summary>Data a drag carries, which a drop target's handler reads while it runs.</summary>
internal struct WxkDropData;

/// <summary>The callbacks every drop target calls, as the shim's wxk_DropTargetCallbacks lays them out.</summary>
internal unsafe struct WxkDropTargetCallbacks {
	public delegate* unmanaged[Cdecl]<void*, int, int, int, int, int, char*, nuint, int> OnDrag;
	public delegate* unmanaged[Cdecl]<void*, void> OnLeave;
	public delegate* unmanaged[Cdecl]<void*, int, int, int, int, char*, nuint, WxkDropData*, int> OnDrop;
	public delegate* unmanaged[Cdecl]<void*, void> OnFree;
}

/// <summary>The standard clipboard formats, as the shim numbers them.</summary>
internal enum WxkClipboardFormat {
	Text = 0,
	Bitmap = 1,
	Files = 2,
	Html = 3,
}
