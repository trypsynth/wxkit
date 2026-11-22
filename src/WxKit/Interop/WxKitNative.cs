using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WxKit.Interop;

/// <summary>
/// The shim's C interface, one to one. The types in <see cref="WxKit"/> wrap it safely, and calling
/// it directly gives up their lifetime and threading checks. It can change between preview releases.
/// </summary>
public static unsafe partial class WxKitNative {
	public const string Library = "wxkitnative";

	// Application

	[LibraryImport(Library, EntryPoint = "wxk_App_Run")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial int AppRun(delegate* unmanaged[Cdecl]<void*, byte> onInit, void* userdata);

	[LibraryImport(Library, EntryPoint = "wxk_App_CanRunOnThisThread")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte AppCanRunOnThisThread();

	[LibraryImport(Library, EntryPoint = "wxk_App_CallAfter")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte AppCallAfter(delegate* unmanaged[Cdecl]<void*, void> callback, void* userdata);

	[LibraryImport(Library, EntryPoint = "wxk_App_ExitMainLoop")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void AppExitMainLoop();

	[LibraryImport(Library, EntryPoint = "wxk_App_SetAssertCallback")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void AppSetAssertCallback(delegate* unmanaged[Cdecl]<WxkString*, int, WxkString*, WxkString*, WxkString*, void> callback);

	[LibraryImport(Library, EntryPoint = "wxk_App_SetLayoutDirection")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void AppSetLayoutDirection(int direction);

	[LibraryImport(Library, EntryPoint = "wxk_Version_GetString")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial WxkString* VersionGetString();

	// Text

	[LibraryImport(Library, EntryPoint = "wxk_String_GetData")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial char* StringGetData(WxkString* text, nuint* length);

	[LibraryImport(Library, EntryPoint = "wxk_String_Free")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void StringFree(WxkString* text);

	[LibraryImport(Library, EntryPoint = "wxk_String_new")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial WxkString* StringNew(char* text, nuint textLength);

	[LibraryImport(Library, EntryPoint = "wxk_StringArray_new")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial WxkStringArray* StringArrayNew();

	[LibraryImport(Library, EntryPoint = "wxk_StringArray_Add")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void StringArrayAdd(WxkStringArray* array, char* text, nuint textLength);

	[LibraryImport(Library, EntryPoint = "wxk_StringArray_GetCount")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial nuint StringArrayGetCount(WxkStringArray* array);

	[LibraryImport(Library, EntryPoint = "wxk_StringArray_Item")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial WxkString* StringArrayItem(WxkStringArray* array, nuint index);

	[LibraryImport(Library, EntryPoint = "wxk_StringArray_Free")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void StringArrayFree(WxkStringArray* array);

	[LibraryImport(Library, EntryPoint = "wxk_WindowList_GetCount")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial nuint WindowListGetCount(WxkWindowList* list);

	[LibraryImport(Library, EntryPoint = "wxk_WindowList_Item")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial WxkWindow* WindowListItem(WxkWindowList* list, nuint index);

	[LibraryImport(Library, EntryPoint = "wxk_WindowList_Free")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void WindowListFree(WxkWindowList* list);

	[LibraryImport(Library, EntryPoint = "wxk_Clipboard_SetText")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte ClipboardSetText(char* text, nuint textLength);

	[LibraryImport(Library, EntryPoint = "wxk_Clipboard_GetText")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial WxkString* ClipboardGetText();

	[LibraryImport(Library, EntryPoint = "wxk_Clipboard_HasText")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte ClipboardHasText();

	[LibraryImport(Library, EntryPoint = "wxk_Clipboard_Clear")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void ClipboardClear();

	[LibraryImport(Library, EntryPoint = "wxk_ClipboardContent_new")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial WxkClipboardContent* ClipboardContentNew();

	[LibraryImport(Library, EntryPoint = "wxk_ClipboardContent_Free")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void ClipboardContentFree(WxkClipboardContent* content);

	[LibraryImport(Library, EntryPoint = "wxk_ClipboardContent_AddText")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void ClipboardContentAddText(WxkClipboardContent* content, char* text, nuint textLength);

	[LibraryImport(Library, EntryPoint = "wxk_ClipboardContent_AddHtml")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void ClipboardContentAddHtml(WxkClipboardContent* content, char* html, nuint htmlLength);

	[LibraryImport(Library, EntryPoint = "wxk_ClipboardContent_AddBitmap")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void ClipboardContentAddBitmap(WxkClipboardContent* content, WxkObject* bitmap);

	[LibraryImport(Library, EntryPoint = "wxk_ClipboardContent_AddFiles")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void ClipboardContentAddFiles(WxkClipboardContent* content, WxkStringArray* files);

	[LibraryImport(Library, EntryPoint = "wxk_ClipboardContent_AddData")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void ClipboardContentAddData(WxkClipboardContent* content, char* format, nuint formatLength, byte* data, nuint size);

	[LibraryImport(Library, EntryPoint = "wxk_Clipboard_SetContent")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte ClipboardSetContent(WxkClipboardContent* content);

	[LibraryImport(Library, EntryPoint = "wxk_Clipboard_Has")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte ClipboardHas(WxkClipboardFormat format);

	[LibraryImport(Library, EntryPoint = "wxk_Clipboard_HasData")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte ClipboardHasData(char* format, nuint formatLength);

	[LibraryImport(Library, EntryPoint = "wxk_Clipboard_GetHtml")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial WxkString* ClipboardGetHtml();

	[LibraryImport(Library, EntryPoint = "wxk_Clipboard_GetBitmap")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial WxkObject* ClipboardGetBitmap();

	[LibraryImport(Library, EntryPoint = "wxk_Clipboard_GetFiles")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial WxkStringArray* ClipboardGetFiles();

	[LibraryImport(Library, EntryPoint = "wxk_Clipboard_GetData")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte* ClipboardGetData(char* format, nuint formatLength, nuint* size);

	[LibraryImport(Library, EntryPoint = "wxk_Bytes_Free")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void BytesFree(byte* bytes);

	[LibraryImport(Library, EntryPoint = "wxk_Clipboard_Flush")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte ClipboardFlush();

	[LibraryImport(Library, EntryPoint = "wxk_Image_AddPngHandler")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void ImageAddPngHandler();

	[LibraryImport(Library, EntryPoint = "wxk_Image_AddJpegHandler")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void ImageAddJpegHandler();

	[LibraryImport(Library, EntryPoint = "wxk_Image_AddTiffHandler")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void ImageAddTiffHandler();

	[LibraryImport(Library, EntryPoint = "wxk_Image_AddGifHandler")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void ImageAddGifHandler();

	[LibraryImport(Library, EntryPoint = "wxk_Image_AddWebpHandler")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void ImageAddWebpHandler();

	[LibraryImport(Library, EntryPoint = "wxk_Image_AddPnmHandler")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void ImageAddPnmHandler();

	[LibraryImport(Library, EntryPoint = "wxk_Image_AddPcxHandler")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void ImageAddPcxHandler();

	[LibraryImport(Library, EntryPoint = "wxk_Image_AddIffHandler")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void ImageAddIffHandler();

	[LibraryImport(Library, EntryPoint = "wxk_Image_AddTgaHandler")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void ImageAddTgaHandler();

	[LibraryImport(Library, EntryPoint = "wxk_Image_AddXpmHandler")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void ImageAddXpmHandler();

	[LibraryImport(Library, EntryPoint = "wxk_Image_AddIconHandler")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void ImageAddIconHandler();

	[LibraryImport(Library, EntryPoint = "wxk_Image_AddAllHandlers")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void ImageAddAllHandlers();

	[LibraryImport(Library, EntryPoint = "wxk_Variant_new")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial WxkVariant* VariantNew();

	[LibraryImport(Library, EntryPoint = "wxk_Variant_Free")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void VariantFree(WxkVariant* variant);

	[LibraryImport(Library, EntryPoint = "wxk_Variant_SetNull")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void VariantSetNull(WxkVariant* variant);

	[LibraryImport(Library, EntryPoint = "wxk_Variant_SetString")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void VariantSetString(WxkVariant* variant, char* text, nuint textLength);

	[LibraryImport(Library, EntryPoint = "wxk_Variant_SetBool")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void VariantSetBool(WxkVariant* variant, byte value);

	[LibraryImport(Library, EntryPoint = "wxk_Variant_SetLong")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void VariantSetLong(WxkVariant* variant, long value);

	[LibraryImport(Library, EntryPoint = "wxk_Variant_SetDouble")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void VariantSetDouble(WxkVariant* variant, double value);

	[LibraryImport(Library, EntryPoint = "wxk_Variant_SetObject")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte VariantSetObject(WxkVariant* variant, WxkObject* value);

	[LibraryImport(Library, EntryPoint = "wxk_Variant_GetType")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial int VariantGetType(WxkVariant* variant);

	[LibraryImport(Library, EntryPoint = "wxk_Variant_GetString")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial WxkString* VariantGetString(WxkVariant* variant);

	[LibraryImport(Library, EntryPoint = "wxk_Variant_GetBool")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte VariantGetBool(WxkVariant* variant);

	[LibraryImport(Library, EntryPoint = "wxk_Variant_GetLong")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial long VariantGetLong(WxkVariant* variant);

	[LibraryImport(Library, EntryPoint = "wxk_Variant_GetDouble")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial double VariantGetDouble(WxkVariant* variant);

	[LibraryImport(Library, EntryPoint = "wxk_Variant_GetObject")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial WxkObject* VariantGetObject(WxkVariant* variant);

	[LibraryImport(Library, EntryPoint = "wxk_VariantList_new")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial WxkVariantList* VariantListNew();

	[LibraryImport(Library, EntryPoint = "wxk_VariantList_Add")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void VariantListAdd(WxkVariantList* list, WxkVariant* value);

	[LibraryImport(Library, EntryPoint = "wxk_VariantList_Free")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void VariantListFree(WxkVariantList* list);

	[LibraryImport(Library, EntryPoint = "wxk_DataViewModel_SetCallbacks")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void DataViewModelSetCallbacks(WxkDataViewModelCallbacks* callbacks);

	[LibraryImport(Library, EntryPoint = "wxk_VirtualListModel_SetCallbacks")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void VirtualListModelSetCallbacks(WxkVirtualListModelCallbacks* callbacks);

	[LibraryImport(Library, EntryPoint = "wxk_ManagedDataViewModel_new")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial WxkDataViewModel* ManagedDataViewModelNew(void* userdata);

	[LibraryImport(Library, EntryPoint = "wxk_ManagedVirtualListModel_new")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial WxkDataViewModel* ManagedVirtualListModelNew(void* userdata, uint count);

	[LibraryImport(Library, EntryPoint = "wxk_DataViewModel_DecRef")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void DataViewModelDecRef(WxkDataViewModel* model);

	[LibraryImport(Library, EntryPoint = "wxk_DataViewItems_Add")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void DataViewItemsAdd(WxkDataViewItems* items, nint item);

	[LibraryImport(Library, EntryPoint = "wxk_DataViewModel_ItemAdded")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte DataViewModelItemAdded(WxkDataViewModel* model, nint parent, nint item);

	[LibraryImport(Library, EntryPoint = "wxk_DataViewModel_ItemDeleted")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte DataViewModelItemDeleted(WxkDataViewModel* model, nint parent, nint item);

	[LibraryImport(Library, EntryPoint = "wxk_DataViewModel_ItemChanged")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte DataViewModelItemChanged(WxkDataViewModel* model, nint item);

	[LibraryImport(Library, EntryPoint = "wxk_DataViewModel_ValueChanged")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte DataViewModelValueChanged(WxkDataViewModel* model, nint item, uint column);

	[LibraryImport(Library, EntryPoint = "wxk_DataViewModel_Cleared")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte DataViewModelCleared(WxkDataViewModel* model);

	[LibraryImport(Library, EntryPoint = "wxk_DataViewModel_Resort")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void DataViewModelResort(WxkDataViewModel* model);

	[LibraryImport(Library, EntryPoint = "wxk_VirtualListModel_Reset")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void VirtualListModelReset(WxkDataViewModel* model, uint count);

	[LibraryImport(Library, EntryPoint = "wxk_VirtualListModel_RowAppended")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void VirtualListModelRowAppended(WxkDataViewModel* model);

	[LibraryImport(Library, EntryPoint = "wxk_VirtualListModel_RowPrepended")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void VirtualListModelRowPrepended(WxkDataViewModel* model);

	[LibraryImport(Library, EntryPoint = "wxk_VirtualListModel_RowInserted")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void VirtualListModelRowInserted(WxkDataViewModel* model, uint before);

	[LibraryImport(Library, EntryPoint = "wxk_VirtualListModel_RowDeleted")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void VirtualListModelRowDeleted(WxkDataViewModel* model, uint row);

	[LibraryImport(Library, EntryPoint = "wxk_VirtualListModel_RowChanged")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void VirtualListModelRowChanged(WxkDataViewModel* model, uint row);

	[LibraryImport(Library, EntryPoint = "wxk_VirtualListModel_RowValueChanged")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void VirtualListModelRowValueChanged(WxkDataViewModel* model, uint row, uint column);

	[LibraryImport(Library, EntryPoint = "wxk_VirtualListModel_GetCount")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial uint VirtualListModelGetCount(WxkDataViewModel* model);

	[LibraryImport(Library, EntryPoint = "wxk_VirtualListModel_GetItem")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial nint VirtualListModelGetItem(WxkDataViewModel* model, uint row);

	[LibraryImport(Library, EntryPoint = "wxk_VirtualListModel_GetRow")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial uint VirtualListModelGetRow(WxkDataViewModel* model, nint item);

	[LibraryImport(Library, EntryPoint = "wxk_DataViewCtrl_DeleteColumn")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte DataViewCtrlDeleteColumn(WxkWindow* control, WxkDataViewColumn* column);

	[LibraryImport(Library, EntryPoint = "wxk_DataViewCtrl_ClearColumns")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte DataViewCtrlClearColumns(WxkWindow* control);

	[LibraryImport(Library, EntryPoint = "wxk_DataViewCtrl_AssociateModel")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte DataViewCtrlAssociateModel(WxkWindow* control, WxkDataViewModel* model);

	[LibraryImport(Library, EntryPoint = "wxk_DropFilesEvent_GetFiles")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial WxkStringArray* DropFilesEventGetFiles(WxkEvent* @event);

	[LibraryImport(Library, EntryPoint = "wxk_Numbers_GetCount")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial nuint NumbersGetCount(WxkNumbers* numbers);

	[LibraryImport(Library, EntryPoint = "wxk_Numbers_CopyTo")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void NumbersCopyTo(WxkNumbers* numbers, long* values);

	[LibraryImport(Library, EntryPoint = "wxk_Numbers_Free")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void NumbersFree(WxkNumbers* numbers);

	[LibraryImport(Library, EntryPoint = "wxk_ManagedTaskBarIcon_new")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial WxkEvtHandler* ManagedTaskBarIconNew(int type);

	[LibraryImport(Library, EntryPoint = "wxk_ManagedTaskBarIcon_SetMenu")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void ManagedTaskBarIconSetMenu(WxkEvtHandler* icon, WxkMenu* menu);

	[LibraryImport(Library, EntryPoint = "wxk_ManagedTaskBarIcon_Destroy")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void ManagedTaskBarIconDestroy(WxkEvtHandler* icon);

	[LibraryImport(Library, EntryPoint = "wxk_NotificationMessage_Free")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void NotificationMessageFree(WxkEvtHandler* notification);

	[LibraryImport(Library, EntryPoint = "wxk_Window_SetAccessibleParts")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void WindowSetAccessibleParts(WxkWindow* window, WxkStringArray* labels, Rect* rects);

	[LibraryImport(Library, EntryPoint = "wxk_Window_SetAccessibleRows")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void WindowSetAccessibleRows(WxkWindow* window, WxkAccessibleRow* rows, nuint count, WxkStringArray* cellLabels, Rect* cellRects, long total);

	[LibraryImport(Library, EntryPoint = "wxk_Window_SetAccessibleCurrentRow")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void WindowSetAccessibleCurrentRow(WxkWindow* window, long row);

	[LibraryImport(Library, EntryPoint = "wxk_UIActionSimulator_MouseMove")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte UIActionSimulatorMouseMove(Point point);

	[LibraryImport(Library, EntryPoint = "wxk_UIActionSimulator_MouseDown")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte UIActionSimulatorMouseDown(int button);

	[LibraryImport(Library, EntryPoint = "wxk_UIActionSimulator_MouseUp")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte UIActionSimulatorMouseUp(int button);

	[LibraryImport(Library, EntryPoint = "wxk_UIActionSimulator_MouseClick")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte UIActionSimulatorMouseClick(int button);

	[LibraryImport(Library, EntryPoint = "wxk_UIActionSimulator_MouseDoubleClick")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte UIActionSimulatorMouseDoubleClick(int button);

	[LibraryImport(Library, EntryPoint = "wxk_UIActionSimulator_KeyDown")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte UIActionSimulatorKeyDown(int key, int modifiers);

	[LibraryImport(Library, EntryPoint = "wxk_UIActionSimulator_KeyUp")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte UIActionSimulatorKeyUp(int key, int modifiers);

	[LibraryImport(Library, EntryPoint = "wxk_UIActionSimulator_Char")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte UIActionSimulatorChar(int key, int modifiers);

	[LibraryImport(Library, EntryPoint = "wxk_UIActionSimulator_Text")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte UIActionSimulatorText(char* text, nuint textLength);

	[LibraryImport(Library, EntryPoint = "wxk_UIActionSimulator_Select")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte UIActionSimulatorSelect(char* text, nuint textLength);

	// Live regions

	[LibraryImport(Library, EntryPoint = "wxk_LiveRegion_Prepare")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte LiveRegionPrepare(WxkWindow* window);

	[LibraryImport(Library, EntryPoint = "wxk_LiveRegion_Announce")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte LiveRegionAnnounce(WxkWindow* window, char* message, nuint messageLength, int priority);

	// Images in memory

	[LibraryImport(Library, EntryPoint = "wxk_Image_LoadData")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte ImageLoadData(WxkObject* image, byte* data, nuint length, int type, int index);

	[LibraryImport(Library, EntryPoint = "wxk_Image_SaveData")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte* ImageSaveData(WxkObject* image, int type, nuint* size);

	// One running copy

	[LibraryImport(Library, EntryPoint = "wxk_SingleInstanceChecker_new")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial WxkSingleInstanceChecker* SingleInstanceCheckerNew(char* name, nuint nameLength, char* path, nuint pathLength);

	[LibraryImport(Library, EntryPoint = "wxk_SingleInstanceChecker_IsAnotherRunning")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte SingleInstanceCheckerIsAnotherRunning(WxkSingleInstanceChecker* checker);

	[LibraryImport(Library, EntryPoint = "wxk_SingleInstanceChecker_delete")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void SingleInstanceCheckerDelete(WxkSingleInstanceChecker* checker);

	// Translations

	[LibraryImport(Library, EntryPoint = "wxk_TranslationsLoader_SetCallbacks")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void TranslationsLoaderSetCallbacks(WxkTranslationsLoaderCallbacks* callbacks);

	[LibraryImport(Library, EntryPoint = "wxk_CatalogSink_Supply")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte CatalogSinkSupply(WxkCatalogSink* sink, byte* data, nuint length);

	[LibraryImport(Library, EntryPoint = "wxk_Translations_new")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial WxkTranslations* TranslationsNew(void* loaderUserdata);

	[LibraryImport(Library, EntryPoint = "wxk_Translations_delete")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void TranslationsDelete(WxkTranslations* translations);

	[LibraryImport(Library, EntryPoint = "wxk_Translations_SetCurrent")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void TranslationsSetCurrent(WxkTranslations* translations);

	[LibraryImport(Library, EntryPoint = "wxk_Translations_SetLanguage")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void TranslationsSetLanguage(WxkTranslations* translations, char* language, nuint languageLength);

	[LibraryImport(Library, EntryPoint = "wxk_Translations_AddStdCatalog")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte TranslationsAddStdCatalog(WxkTranslations* translations);

	[LibraryImport(Library, EntryPoint = "wxk_Translations_AddCatalog")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte TranslationsAddCatalog(WxkTranslations* translations, char* domain, nuint domainLength);

	[LibraryImport(Library, EntryPoint = "wxk_Translations_IsLoaded")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte TranslationsIsLoaded(WxkTranslations* translations, char* domain, nuint domainLength);

	[LibraryImport(Library, EntryPoint = "wxk_Translations_GetAvailableTranslations")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial WxkStringArray* TranslationsGetAvailableTranslations(WxkTranslations* translations, char* domain, nuint domainLength);

	[LibraryImport(Library, EntryPoint = "wxk_Translations_GetBestTranslation")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial WxkString* TranslationsGetBestTranslation(WxkTranslations* translations, char* domain, nuint domainLength);

	[LibraryImport(Library, EntryPoint = "wxk_Translations_GetHeaderValue")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial WxkString* TranslationsGetHeaderValue(WxkTranslations* translations, char* header, nuint headerLength, char* domain, nuint domainLength);

	[LibraryImport(Library, EntryPoint = "wxk_Translations_AddCatalogLookupPathPrefix")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void TranslationsAddCatalogLookupPathPrefix(char* prefix, nuint prefixLength);

	[LibraryImport(Library, EntryPoint = "wxk_Translations_GetString")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial WxkString* TranslationsGetString(char* text, nuint textLength, char* context, nuint contextLength);

	[LibraryImport(Library, EntryPoint = "wxk_Translations_GetPluralString")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial WxkString* TranslationsGetPluralString(char* singular, nuint singularLength, uint n, char* context, nuint contextLength);

	[LibraryImport(Library, EntryPoint = "wxk_UILocale_UseDefault")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte UILocaleUseDefault();

	[LibraryImport(Library, EntryPoint = "wxk_UILocale_GetCurrentName")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial WxkString* UILocaleGetCurrentName();

	[LibraryImport(Library, EntryPoint = "wxk_UILocale_GetPreferredUILanguages")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial WxkStringArray* UILocaleGetPreferredUILanguages();

	[LibraryImport(Library, EntryPoint = "wxk_LanguageInfo_Find")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte LanguageInfoFind(char* locale, nuint localeLength, WxkLanguageInfo* info);

	[LibraryImport(Library, EntryPoint = "wxk_Printout_SetCallbacks")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void PrintoutSetCallbacks(WxkPrintoutCallbacks* callbacks);

	[LibraryImport(Library, EntryPoint = "wxk_ManagedPrintout_new")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial WxkObject* ManagedPrintoutNew(char* title, nuint titleLength, void* userdata);

	[LibraryImport(Library, EntryPoint = "wxk_PrintPreview_Show")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial WxkWindow* PrintPreviewShow(WxkObject* printout, WxkObject* printoutForPrinting, WxkObject* data, WxkWindow* parent, char* title, nuint titleLength, Size size);

	[LibraryImport(Library, EntryPoint = "wxk_AboutBox_Show")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void AboutBoxShow(WxkAboutInfo* about, WxkWindow* parent);

	[LibraryImport(Library, EntryPoint = "wxk_ToolBarTool_GetToolBar")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial WxkWindow* ToolBarToolGetToolBar(WxkToolBarTool* tool);

	[LibraryImport(Library, EntryPoint = "wxk_ToolBar_DeleteTool")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte ToolBarDeleteTool(WxkWindow* toolBar, int toolId);

	[LibraryImport(Library, EntryPoint = "wxk_ToolBar_DeleteToolByPos")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte ToolBarDeleteToolByPos(WxkWindow* toolBar, nuint position);

	[LibraryImport(Library, EntryPoint = "wxk_ToolBar_ClearTools")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void ToolBarClearTools(WxkWindow* toolBar);

	[LibraryImport(Library, EntryPoint = "wxk_ClientData_SetFree")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void ClientDataSetFree(delegate* unmanaged[Cdecl]<nint, void> free);

	[LibraryImport(Library, EntryPoint = "wxk_TreeCtrl_SetItemData")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void TreeCtrlSetItemData(WxkWindow* tree, nint item, nint handle);

	[LibraryImport(Library, EntryPoint = "wxk_Accessible_SetCallbacks")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void AccessibleSetCallbacks(WxkAccessibleCallbacks* callbacks);

	[LibraryImport(Library, EntryPoint = "wxk_ManagedAccessible_new")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial WxkAccessible* ManagedAccessibleNew(WxkWindow* window, void* userdata);

	[LibraryImport(Library, EntryPoint = "wxk_Accessible_NotifyEvent")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void AccessibleNotifyEvent(int eventType, WxkWindow* window, int objectType, int objectId);

	[LibraryImport(Library, EntryPoint = "wxk_ManagedValidator_new")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial WxkObject* ManagedValidatorNew(delegate* unmanaged[Cdecl]<void*, WxkWindow*, WxkWindow*, byte> validate, delegate* unmanaged[Cdecl]<void*, WxkWindow*, byte> toWindow, delegate* unmanaged[Cdecl]<void*, WxkWindow*, byte> fromWindow, delegate* unmanaged[Cdecl]<void*, void> retain, delegate* unmanaged[Cdecl]<void*, void> release, delegate* unmanaged[Cdecl]<void*, void> onFree, void* userdata);

	[LibraryImport(Library, EntryPoint = "wxk_Window_SetValidator")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void WindowSetValidator(WxkWindow* window, WxkObject* validator);

	// Objects

	[LibraryImport(Library, EntryPoint = "wxk_EvtHandler_Track")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte EvtHandlerTrack(WxkEvtHandler* handler, delegate* unmanaged[Cdecl]<void*, void> onDestroy, void* userdata);

	[LibraryImport(Library, EntryPoint = "wxk_Object_GetClassInfo")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial WxkClassInfo* ObjectGetClassInfo(WxkObject* obj);

	[LibraryImport(Library, EntryPoint = "wxk_Object_Free")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void ObjectFree(WxkObject* obj);

	[LibraryImport(Library, EntryPoint = "wxk_ClassInfo_GetClassName")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial WxkString* ClassInfoGetClassName(WxkClassInfo* info);

	[LibraryImport(Library, EntryPoint = "wxk_ClassInfo_GetBaseClass")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial WxkClassInfo* ClassInfoGetBaseClass(WxkClassInfo* info);

	// Events

	[LibraryImport(Library, EntryPoint = "wxk_EvtHandler_Bind")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte EvtHandlerBind(WxkEvtHandler* handler, int eventType, int id, int lastId, delegate* unmanaged[Cdecl]<void*, WxkEvent*, void> callback, void* userdata, delegate* unmanaged[Cdecl]<void*, void> onFree);

	[LibraryImport(Library, EntryPoint = "wxk_Event_GetEventType")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial int EventGetEventType(WxkEvent* e);

	[LibraryImport(Library, EntryPoint = "wxk_CloseEvent_Veto")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void CloseEventVeto(WxkEvent* e, byte veto);

	// Windows

	[LibraryImport(Library, EntryPoint = "wxk_Window_GetHandle")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void* WindowGetHandle(WxkWindow* window);

	[LibraryImport(Library, EntryPoint = "wxk_Window_SetSizer")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void WindowSetSizer(WxkWindow* window, WxkSizer* sizer);

	[LibraryImport(Library, EntryPoint = "wxk_Frame_SetMenuBar")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void FrameSetMenuBar(WxkWindow* frame, WxkWindow* menuBar);

	// Dialogs

	// Sizers

	[LibraryImport(Library, EntryPoint = "wxk_Sizer_AddWindow")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void SizerAddWindow(WxkSizer* sizer, WxkWindow* window, int proportion, int flags, int border);

	[LibraryImport(Library, EntryPoint = "wxk_GridBagSizer_AddWindow")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte GridBagSizerAddWindow(WxkSizer* sizer, WxkWindow* window, GridPosition position, GridSpan span, int flags, int border);

	[LibraryImport(Library, EntryPoint = "wxk_GridBagSizer_AddSizer")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte GridBagSizerAddSizer(WxkSizer* sizer, WxkSizer* child, GridPosition position, GridSpan span, int flags, int border);

	[LibraryImport(Library, EntryPoint = "wxk_GridBagSizer_AddSpacer")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte GridBagSizerAddSpacer(WxkSizer* sizer, Size size, GridPosition position, GridSpan span, int flags, int border);

	[LibraryImport(Library, EntryPoint = "wxk_Sizer_AddSizer")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void SizerAddSizer(WxkSizer* sizer, WxkSizer* child, int proportion, int flags, int border);

	[LibraryImport(Library, EntryPoint = "wxk_Sizer_AddSpacer")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void SizerAddSpacer(WxkSizer* sizer, int size);

	[LibraryImport(Library, EntryPoint = "wxk_Sizer_AddStretchSpacer")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void SizerAddStretchSpacer(WxkSizer* sizer, int proportion);

	[LibraryImport(Library, EntryPoint = "wxk_Sizer_Free")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void SizerFree(WxkSizer* sizer);

	// Menus

	[LibraryImport(Library, EntryPoint = "wxk_Menu_new")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial WxkMenu* MenuNew();

	[LibraryImport(Library, EntryPoint = "wxk_Menu_Append")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial WxkMenuItem* MenuAppend(WxkMenu* menu, int id, char* text, nuint textLength, char* help, nuint helpLength, int kind);

	[LibraryImport(Library, EntryPoint = "wxk_Menu_AppendSeparator")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial WxkMenuItem* MenuAppendSeparator(WxkMenu* menu);

	[LibraryImport(Library, EntryPoint = "wxk_Menu_Free")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void MenuFree(WxkMenu* menu);

	[LibraryImport(Library, EntryPoint = "wxk_Menu_AppendSubMenu")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial WxkMenuItem* MenuAppendSubMenu(WxkMenu* menu, WxkMenu* submenu, char* text, nuint textLength, char* help, nuint helpLength);

	[LibraryImport(Library, EntryPoint = "wxk_Menu_Enable")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void MenuEnable(WxkMenu* menu, int id, byte enable);

	[LibraryImport(Library, EntryPoint = "wxk_Menu_IsEnabled")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte MenuIsEnabled(WxkMenu* menu, int id);

	[LibraryImport(Library, EntryPoint = "wxk_Menu_Check")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void MenuCheck(WxkMenu* menu, int id, byte check);

	[LibraryImport(Library, EntryPoint = "wxk_Menu_IsChecked")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte MenuIsChecked(WxkMenu* menu, int id);

	[LibraryImport(Library, EntryPoint = "wxk_Menu_SetLabel")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void MenuSetLabel(WxkMenu* menu, int id, char* text, nuint textLength);

	[LibraryImport(Library, EntryPoint = "wxk_Menu_GetLabel")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial WxkString* MenuGetLabel(WxkMenu* menu, int id);

	[LibraryImport(Library, EntryPoint = "wxk_MenuItem_GetId")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial int MenuItemGetId(WxkMenuItem* item);

	// Timers

	[LibraryImport(Library, EntryPoint = "wxk_Timer_new")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial WxkTimer* TimerNew();

	[LibraryImport(Library, EntryPoint = "wxk_Timer_Start")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte TimerStart(WxkTimer* timer, int milliseconds, byte oneShot);

	[LibraryImport(Library, EntryPoint = "wxk_Timer_Stop")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void TimerStop(WxkTimer* timer);

	[LibraryImport(Library, EntryPoint = "wxk_Timer_IsRunning")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial byte TimerIsRunning(WxkTimer* timer);

	[LibraryImport(Library, EntryPoint = "wxk_Timer_Free")]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	public static partial void TimerFree(WxkTimer* timer);
}
