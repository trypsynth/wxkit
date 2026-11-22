namespace WxKit.Interop;

// Each handle type is only ever used through a pointer. As in the shim, every handle points at the
// start of the wxWidgets object, so a pointer to a derived class's handle can be cast to a base's.

/// <summary>Any wxObject.</summary>
public struct WxkObject;

/// <summary>A wxEvtHandler, or any class derived from it.</summary>
public struct WxkEvtHandler;

/// <summary>A wxWindow, or any class derived from it.</summary>
public struct WxkWindow;

/// <summary>A wxSizer, or any class derived from it.</summary>
public struct WxkSizer;

/// <summary>A wxDataViewColumn.</summary>
public struct WxkDataViewColumn;

/// <summary>A wxToolBarToolBase.</summary>
public struct WxkToolBarTool;

/// <summary>A row of a window that draws itself, as the shim's wxk_AccessibleRow lays it out.</summary>
public struct WxkAccessibleRow {
	public long Index;
	public Rect Bounds;
	public byte Selected;
	public int FirstCell;
	public int CellCount;
}

/// <summary>The callbacks every managed printout calls, as the shim's wxk_PrintoutCallbacks lays them out.</summary>
public unsafe struct WxkPrintoutCallbacks {
	public delegate* unmanaged[Cdecl]<void*, int, WxkObject*, byte> PrintPage;
	public delegate* unmanaged[Cdecl]<void*, int, byte> HasPage;
	public delegate* unmanaged[Cdecl]<void*, int*, int*, int*, int*, void> GetPageInfo;
	public delegate* unmanaged[Cdecl]<void*, void> PreparePrinting;
	public delegate* unmanaged[Cdecl]<void*, void> BeginPrinting;
	public delegate* unmanaged[Cdecl]<void*, void> EndPrinting;
	public delegate* unmanaged[Cdecl]<void*, int, int, byte> BeginDocument;
	public delegate* unmanaged[Cdecl]<void*, void> EndDocument;
	public delegate* unmanaged[Cdecl]<void*, void> OnFree;
}

/// <summary>A wxSingleInstanceChecker.</summary>
public struct WxkSingleInstanceChecker;

/// <summary>A wxTranslations.</summary>
public struct WxkTranslations;

/// <summary>Where a translations loader puts the catalog it found.</summary>
public struct WxkCatalogSink;

/// <summary>The callbacks every managed translations loader calls, as the shim's wxk_TranslationsLoaderCallbacks lays them out.</summary>
public unsafe struct WxkTranslationsLoaderCallbacks {
	public delegate* unmanaged[Cdecl]<void*, WxkString*, WxkString*, WxkCatalogSink*, void> LoadCatalog;
	public delegate* unmanaged[Cdecl]<void*, WxkString*, WxkStringArray*, void> AvailableTranslations;
	public delegate* unmanaged[Cdecl]<void*, void> OnFree;
}

/// <summary>A language, as the shim's wxk_LanguageInfo lays it out.</summary>
public unsafe struct WxkLanguageInfo {
	public int Language;
	public WxkString* CanonicalName;
	public WxkString* LocaleTag;
	public WxkString* Description;
	public WxkString* DescriptionNative;
	public int LayoutDirection;
}

/// <summary>What an About box shows, as the shim's wxk_AboutInfo lays it out.</summary>
public unsafe struct WxkAboutInfo {
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
public struct WxkMenu;

/// <summary>A wxMenuItem.</summary>
public struct WxkMenuItem;

/// <summary>A wxTimer.</summary>
public struct WxkTimer;

/// <summary>A wxEvent, or any class derived from it. Only valid while its handlers run.</summary>
public struct WxkEvent;

/// <summary>A wxClassInfo, which lives as long as the program.</summary>
public struct WxkClassInfo;

/// <summary>Text the shim hands out, freed with <see cref="WxKitNative.StringFree"/>.</summary>
public struct WxkString;

/// <summary>A wxColour's channels, and whether it is set, as the shim passes them.</summary>
public readonly record struct WxkColor(byte Red, byte Green, byte Blue, byte Alpha, byte Valid);

/// <summary>A wxDateTime's parts, in local time, and whether it is set, as the shim passes them.</summary>
public readonly record struct WxkDateTime(int Year, int Month, int Day, int Hour, int Minute, int Second, int Millisecond, byte Valid);

/// <summary>A list of ints or item ids, freed with <see cref="WxKitNative.NumbersFree"/>.</summary>
public struct WxkNumbers;

/// <summary>A wxArrayString, freed with <see cref="WxKitNative.StringArrayFree"/>.</summary>
public struct WxkStringArray;

/// <summary>A list of windows, freed with <see cref="WxKitNative.WindowListFree"/>.</summary>
public struct WxkWindowList;

/// <summary>A wxVariant, freed with <see cref="WxKitNative.VariantFree"/>.</summary>
public struct WxkVariant;

/// <summary>A wxVector of wxVariant, freed with <see cref="WxKitNative.VariantListFree"/>.</summary>
public struct WxkVariantList;

/// <summary>A wxDataViewModel, which is reference counted.</summary>
public struct WxkDataViewModel;

/// <summary>The wxDataViewItemArray a model fills with an item's children.</summary>
public struct WxkDataViewItems;

/// <summary>The types of value a wxVariant crosses with, as the shim numbers them.</summary>
public enum WxkVariantType {
	Null = 0,
	String = 1,
	Bool = 2,
	Long = 3,
	Double = 4,
	Bitmap = 5,
	Icon = 6,
	IconText = 7,
}

/// <summary>The callbacks of every model whose items come from .NET, as wxk_DataViewModelCallbacks lays them out.</summary>
public unsafe struct WxkDataViewModelCallbacks {
	public delegate* unmanaged[Cdecl]<void*, nint, uint, WxkVariant*, void> GetValue;
	public delegate* unmanaged[Cdecl]<void*, WxkVariant*, nint, uint, byte> SetValue;
	public delegate* unmanaged[Cdecl]<void*, nint, nint> GetParent;
	public delegate* unmanaged[Cdecl]<void*, nint, byte> IsContainer;
	public delegate* unmanaged[Cdecl]<void*, nint, WxkDataViewItems*, void> GetChildren;
	public delegate* unmanaged[Cdecl]<void*, nint, uint, byte> IsEnabled;
	public delegate* unmanaged[Cdecl]<void*, nint, byte> HasContainerColumns;
	public delegate* unmanaged[Cdecl]<void*, void> OnFree;
}

/// <summary>The callbacks of every virtual list model whose rows come from .NET, as wxk_VirtualListModelCallbacks lays them out.</summary>
public unsafe struct WxkVirtualListModelCallbacks {
	public delegate* unmanaged[Cdecl]<void*, uint, uint, WxkVariant*, void> GetValue;
	public delegate* unmanaged[Cdecl]<void*, WxkVariant*, uint, uint, byte> SetValue;
	public delegate* unmanaged[Cdecl]<void*, uint, uint, byte> IsEnabled;
	public delegate* unmanaged[Cdecl]<void*, void> OnFree;
}

/// <summary>An accessible object, which its window owns.</summary>
public struct WxkAccessible;

/// <summary>The callbacks every managed accessible object calls, as the shim's wxk_AccessibleCallbacks lays them out.</summary>
public unsafe struct WxkAccessibleCallbacks {
	public delegate* unmanaged[Cdecl]<void*, int, int, WxkString**, int> GetText;
	public delegate* unmanaged[Cdecl]<void*, int, int*, int> GetRole;
	public delegate* unmanaged[Cdecl]<void*, int, long*, int> GetState;
	public delegate* unmanaged[Cdecl]<void*, int*, int> GetChildCount;
	public delegate* unmanaged[Cdecl]<void*, int, Rect*, int> GetLocation;
	public delegate* unmanaged[Cdecl]<void*, int, int, int, Point, int*, WxkAccessible**, int> GetElement;
	public delegate* unmanaged[Cdecl]<void*, int, int, int, int> Act;
	public delegate* unmanaged[Cdecl]<void*, void> OnFree;
}

/// <summary>Clipboard content in several formats, which <see cref="WxKitNative.ClipboardSetContent"/> takes ownership of.</summary>
public struct WxkClipboardContent;

/// <summary>Data a drag carries, which a drop target's handler reads while it runs.</summary>
public struct WxkDropData;

/// <summary>The callbacks every drop target calls, as the shim's wxk_DropTargetCallbacks lays them out.</summary>
public unsafe struct WxkDropTargetCallbacks {
	public delegate* unmanaged[Cdecl]<void*, int, int, int, int, int, char*, nuint, int> OnDrag;
	public delegate* unmanaged[Cdecl]<void*, void> OnLeave;
	public delegate* unmanaged[Cdecl]<void*, int, int, int, int, char*, nuint, WxkDropData*, int> OnDrop;
	public delegate* unmanaged[Cdecl]<void*, void> OnFree;
}

/// <summary>The standard clipboard formats, as the shim numbers them.</summary>
public enum WxkClipboardFormat {
	Text = 0,
	Bitmap = 1,
	Files = 2,
	Html = 3,
}
