// The flat C interface over wxWidgets that WxKit calls. Every function is safe to call from C:
// none lets a C++ exception escape, and only C types cross the boundary.
#pragma once

#include <stddef.h>
#include <stdint.h>

#if defined(WXK_BUILDING_SHARED) && defined(_WIN32)
	#define WXK_API __declspec(dllexport)
#elif defined(WXK_BUILDING_SHARED)
	#define WXK_API __attribute__((visibility("default")))
#else
	#define WXK_API
#endif

#ifdef __cplusplus
	#define WXK_EXTERN extern "C"
	#define WXK_NOEXCEPT noexcept
#else
	#define WXK_EXTERN
	#define WXK_NOEXCEPT
#endif

#define WXK_FUNCTION(type) WXK_EXTERN WXK_API type

// Every handle points at the start of the wxWidgets object. wxObject, wxEvtHandler, and wxWindow
// form a chain of primary bases, so the same address is a valid handle to any of the classes an
// object derives from: a frame's handle is also a window's and an event handler's.
typedef struct wxk_Object wxk_Object;
// An item in a tree or a data view, as the pointer wxWidgets identifies it by, which only
// wxWidgets reads, so it crosses as a number.
typedef intptr_t wxk_ItemId;
typedef struct wxk_EvtHandler wxk_EvtHandler;
typedef struct wxk_Window wxk_Window;
typedef struct wxk_Sizer wxk_Sizer;
typedef struct wxk_DataViewColumn wxk_DataViewColumn;
typedef struct wxk_ToolBarTool wxk_ToolBarTool;
typedef struct wxk_Menu wxk_Menu;
typedef struct wxk_MenuItem wxk_MenuItem;
typedef struct wxk_Event wxk_Event;
typedef struct wxk_ClassInfo wxk_ClassInfo;
// An accessible object, which describes a window to screen readers. The window owns it.
typedef struct wxk_Accessible wxk_Accessible;

// Text that the shim hands out, which the caller frees with wxk_String_Free.
typedef struct wxk_String wxk_String;

// A list of ints or item ids, as 64-bit numbers, which the caller frees with wxk_Numbers_Free.
typedef struct wxk_Numbers wxk_Numbers;

// A wxArrayString, which the caller frees with wxk_StringArray_Free.
typedef struct wxk_StringArray wxk_StringArray;

// A list of windows, such as a window's children, which the caller frees with wxk_WindowList_Free.
typedef struct wxk_WindowList wxk_WindowList;

// A wxVariant, which holds one value of any type, and which the caller frees with wxk_Variant_Free.
typedef struct wxk_Variant wxk_Variant;

// A wxVector<wxVariant>, such as a data view list row's values, freed with wxk_VariantList_Free.
typedef struct wxk_VariantList wxk_VariantList;

// The types of value a wxVariant crosses with. Any other type reads as text.
typedef enum wxk_VariantType {
	WXK_VARIANT_NULL = 0,
	WXK_VARIANT_STRING = 1,
	WXK_VARIANT_BOOL = 2,
	WXK_VARIANT_LONG = 3,
	WXK_VARIANT_DOUBLE = 4,
	WXK_VARIANT_BITMAP = 5,
	WXK_VARIANT_ICON = 6,
	WXK_VARIANT_ICON_TEXT = 7
} wxk_VariantType;

// A wxDataViewModel, which is reference counted.
typedef struct wxk_DataViewModel wxk_DataViewModel;

// The wxDataViewItemArray a model fills with an item's children.
typedef struct wxk_DataViewItems wxk_DataViewItems;

// Clipboard content in several formats, which wxk_Clipboard_SetContent takes ownership of.
typedef struct wxk_ClipboardContent wxk_ClipboardContent;

// The standard clipboard formats.
typedef enum wxk_ClipboardFormat {
	WXK_CLIPBOARD_TEXT = 0,
	WXK_CLIPBOARD_BITMAP = 1,
	WXK_CLIPBOARD_FILES = 2,
	WXK_CLIPBOARD_HTML = 3
} wxk_ClipboardFormat;

typedef struct wxk_Point {
	int32_t x;
	int32_t y;
} wxk_Point;

typedef struct wxk_Size {
	int32_t width;
	int32_t height;
} wxk_Size;

// A cell of a grid bag sizer, and how many rows and columns an item there spans.
typedef struct wxk_GridPosition {
	int32_t row;
	int32_t column;
} wxk_GridPosition;

typedef struct wxk_GridSpan {
	int32_t rows;
	int32_t columns;
} wxk_GridSpan;

typedef struct wxk_Rect {
	int32_t x;
	int32_t y;
	int32_t width;
	int32_t height;
} wxk_Rect;

// A wxColour's channels, and whether it is set: wxNullColour has valid 0.
typedef struct wxk_Colour {
	uint8_t red;
	uint8_t green;
	uint8_t blue;
	uint8_t alpha;
	uint8_t valid;
} wxk_Colour;

// A date and time, in local time, as wxDateTime gives its parts. The month is 1 to 12. valid is 0
// for wxInvalidDateTime, which stands for no date.
typedef struct wxk_DateTime {
	int32_t year;
	int32_t month;
	int32_t day;
	int32_t hour;
	int32_t minute;
	int32_t second;
	int32_t millisecond;
	uint8_t valid;
} wxk_DateTime;

typedef uint8_t (*wxk_AppInitCallback)(void* userdata);
typedef uint8_t (*wxk_ValidateCallback)(void* userdata, wxk_Window* window, wxk_Window* parent);
typedef uint8_t (*wxk_TransferCallback)(void* userdata, wxk_Window* window);
typedef void (*wxk_Callback)(void* userdata);
typedef void (*wxk_EventCallback)(void* userdata, wxk_Event* event);

// The application

// Runs the application: initializes wxWidgets, calls onInit, and runs the event loop until it
// ends. Returns the exit code, or -1 when wxWidgets could not start.
WXK_FUNCTION(int32_t) wxk_App_Run(wxk_AppInitCallback onInit, void* userdata) WXK_NOEXCEPT;
// Whether the event loop can run on the calling thread, which on macOS has to be the main thread.
WXK_FUNCTION(uint8_t) wxk_App_CanRunOnThisThread(void) WXK_NOEXCEPT;

// Queues callback to run on the GUI thread. Safe from any thread. Returns 0 when no application is running.
WXK_FUNCTION(uint8_t) wxk_App_CallAfter(wxk_Callback callback, void* userdata) WXK_NOEXCEPT;

WXK_FUNCTION(void) wxk_App_ExitMainLoop(void) WXK_NOEXCEPT;

// Receives a failed wxWidgets assertion, on the thread it failed on, with its file, line, function,
// condition, and message, each a string the callback takes and frees.
typedef void (*wxk_AssertCallback)(wxk_String* file, int32_t line, wxk_String* function, wxk_String* condition, wxk_String* message);
// Sends failed assertions to callback, or, when it is null, to the debug output.
WXK_FUNCTION(void) wxk_App_SetAssertCallback(wxk_AssertCallback callback) WXK_NOEXCEPT;

// Sets the direction, a wxLayoutDirection, that windows with nothing to take one from are laid
// out in, such as new frames and message boxes with no parent. wxLayout_Default follows the UI locale.
WXK_FUNCTION(void) wxk_App_SetLayoutDirection(int32_t direction) WXK_NOEXCEPT;

// The wxWidgets version, such as "wxWidgets 3.3.4".
WXK_FUNCTION(wxk_String*) wxk_Version_GetString(void) WXK_NOEXCEPT;

// Objects and events

// Calls onDestroy with userdata when wxWidgets destroys the handler, after its own destructor has
// run and every event handler bound to it has been released.
WXK_FUNCTION(uint8_t) wxk_EvtHandler_Track(wxk_EvtHandler* handler, wxk_Callback onDestroy, void* userdata) WXK_NOEXCEPT;

WXK_FUNCTION(const wxk_ClassInfo*) wxk_Object_GetClassInfo(wxk_Object* object) WXK_NOEXCEPT;

// Deletes an object the shim copied out, such as a font or a bitmap, which releases its reference.
WXK_FUNCTION(void) wxk_Object_Free(wxk_Object* object) WXK_NOEXCEPT;

WXK_FUNCTION(wxk_String*) wxk_ClassInfo_GetClassName(const wxk_ClassInfo* info) WXK_NOEXCEPT;

// The first base class, or null for wxObject.
WXK_FUNCTION(const wxk_ClassInfo*) wxk_ClassInfo_GetBaseClass(const wxk_ClassInfo* info) WXK_NOEXCEPT;

// Binds callback to events of eventType from ids in [id, lastId], where -1 means any. When
// wxWidgets releases the binding, which it does when the handler is destroyed, it calls onFree.
WXK_FUNCTION(uint8_t) wxk_EvtHandler_Bind(wxk_EvtHandler* handler, int32_t eventType, int32_t id, int32_t lastId, wxk_EventCallback callback, void* userdata, wxk_Callback onFree) WXK_NOEXCEPT;

WXK_FUNCTION(int32_t) wxk_Event_GetEventType(wxk_Event* event) WXK_NOEXCEPT;

WXK_FUNCTION(void) wxk_CloseEvent_Veto(wxk_Event* event, uint8_t veto) WXK_NOEXCEPT;

// The paths of the files dropped on a window, which the caller frees.
WXK_FUNCTION(wxk_StringArray*) wxk_DropFilesEvent_GetFiles(wxk_Event* event) WXK_NOEXCEPT;

// Text, lists, and bytes

WXK_FUNCTION(const char16_t*) wxk_String_GetData(const wxk_String* text, size_t* length) WXK_NOEXCEPT;

WXK_FUNCTION(void) wxk_String_Free(wxk_String* text) WXK_NOEXCEPT;

// Text that .NET hands to the shim, such as what a director's callback returns. The shim frees it.
WXK_FUNCTION(wxk_String*) wxk_String_new(const char16_t* text, size_t textLength) WXK_NOEXCEPT;

WXK_FUNCTION(size_t) wxk_Numbers_GetCount(const wxk_Numbers* numbers) WXK_NOEXCEPT;
// Copies the numbers into values, which has room for all of them.
WXK_FUNCTION(void) wxk_Numbers_CopyTo(const wxk_Numbers* numbers, int64_t* values) WXK_NOEXCEPT;
WXK_FUNCTION(void) wxk_Numbers_Free(wxk_Numbers* numbers) WXK_NOEXCEPT;

WXK_FUNCTION(wxk_StringArray*) wxk_StringArray_new(void) WXK_NOEXCEPT;
WXK_FUNCTION(void) wxk_StringArray_Add(wxk_StringArray* array, const char16_t* text, size_t textLength) WXK_NOEXCEPT;
WXK_FUNCTION(size_t) wxk_StringArray_GetCount(const wxk_StringArray* array) WXK_NOEXCEPT;
WXK_FUNCTION(wxk_String*) wxk_StringArray_Item(const wxk_StringArray* array, size_t index) WXK_NOEXCEPT;
WXK_FUNCTION(void) wxk_StringArray_Free(wxk_StringArray* array) WXK_NOEXCEPT;

WXK_FUNCTION(size_t) wxk_WindowList_GetCount(const wxk_WindowList* list) WXK_NOEXCEPT;
WXK_FUNCTION(wxk_Window*) wxk_WindowList_Item(const wxk_WindowList* list, size_t index) WXK_NOEXCEPT;
WXK_FUNCTION(void) wxk_WindowList_Free(wxk_WindowList* list) WXK_NOEXCEPT;

// Frees bytes the shim hands out, such as the clipboard's data or an image's file.
WXK_FUNCTION(void) wxk_Bytes_Free(uint8_t* bytes) WXK_NOEXCEPT;

// Variants

WXK_FUNCTION(wxk_Variant*) wxk_Variant_new(void) WXK_NOEXCEPT;
WXK_FUNCTION(void) wxk_Variant_Free(wxk_Variant* variant) WXK_NOEXCEPT;
WXK_FUNCTION(void) wxk_Variant_SetNull(wxk_Variant* variant) WXK_NOEXCEPT;
WXK_FUNCTION(void) wxk_Variant_SetString(wxk_Variant* variant, const char16_t* text, size_t textLength) WXK_NOEXCEPT;
WXK_FUNCTION(void) wxk_Variant_SetBool(wxk_Variant* variant, uint8_t value) WXK_NOEXCEPT;
WXK_FUNCTION(void) wxk_Variant_SetLong(wxk_Variant* variant, int64_t value) WXK_NOEXCEPT;
WXK_FUNCTION(void) wxk_Variant_SetDouble(wxk_Variant* variant, double value) WXK_NOEXCEPT;
// Copies a bitmap, an icon, or a wxDataViewIconText into the variant. Returns 0 for anything else.
WXK_FUNCTION(uint8_t) wxk_Variant_SetObject(wxk_Variant* variant, wxk_Object* value) WXK_NOEXCEPT;
// A wxk_VariantType.
WXK_FUNCTION(int32_t) wxk_Variant_GetType(const wxk_Variant* variant) WXK_NOEXCEPT;
// The value as text, which works for any type.
WXK_FUNCTION(wxk_String*) wxk_Variant_GetString(const wxk_Variant* variant) WXK_NOEXCEPT;
WXK_FUNCTION(uint8_t) wxk_Variant_GetBool(const wxk_Variant* variant) WXK_NOEXCEPT;
WXK_FUNCTION(int64_t) wxk_Variant_GetLong(const wxk_Variant* variant) WXK_NOEXCEPT;
WXK_FUNCTION(double) wxk_Variant_GetDouble(const wxk_Variant* variant) WXK_NOEXCEPT;
// A heap copy of the bitmap, icon, or icon text the variant holds, which the caller owns.
WXK_FUNCTION(wxk_Object*) wxk_Variant_GetObject(const wxk_Variant* variant) WXK_NOEXCEPT;
WXK_FUNCTION(wxk_VariantList*) wxk_VariantList_new(void) WXK_NOEXCEPT;
WXK_FUNCTION(void) wxk_VariantList_Add(wxk_VariantList* list, const wxk_Variant* value) WXK_NOEXCEPT;
WXK_FUNCTION(void) wxk_VariantList_Free(wxk_VariantList* list) WXK_NOEXCEPT;

// Windows and controls

// The platform's own handle: an HWND on Windows, an NSView* on macOS, and a GtkWidget* on Linux.
WXK_FUNCTION(void*) wxk_Window_GetHandle(wxk_Window* window) WXK_NOEXCEPT;

// A validator whose checks call back into .NET. retain and release are called as windows take
// and drop copies of it, and onFree once the last copy is gone.
WXK_FUNCTION(wxk_Object*) wxk_ManagedValidator_new(wxk_ValidateCallback validate, wxk_TransferCallback toWindow, wxk_TransferCallback fromWindow, wxk_Callback retain, wxk_Callback release, wxk_Callback onFree, void* userdata) WXK_NOEXCEPT;
// Gives the window a copy of the validator. Null removes the window's validator.
WXK_FUNCTION(void) wxk_Window_SetValidator(wxk_Window* window, wxk_Object* validator) WXK_NOEXCEPT;

// The tool's toolbar. The interface headers don't document the wxToolBarBase it returns, so it
// isn't generated, as the toolbar's other members are.
WXK_FUNCTION(wxk_Window*) wxk_ToolBarTool_GetToolBar(wxk_ToolBarTool* tool) WXK_NOEXCEPT;

// A credential dialog's user name and password, as strings.
WXK_FUNCTION(wxk_String*) wxk_CredentialEntryDialog_GetUserName(wxk_Window* dialog) WXK_NOEXCEPT;
WXK_FUNCTION(void) wxk_CredentialEntryDialog_SetUserName(wxk_Window* dialog, const char16_t* user, size_t userLength) WXK_NOEXCEPT;
WXK_FUNCTION(wxk_String*) wxk_CredentialEntryDialog_GetPassword(wxk_Window* dialog) WXK_NOEXCEPT;
WXK_FUNCTION(void) wxk_CredentialEntryDialog_SetPassword(wxk_Window* dialog, const char16_t* password, size_t passwordLength) WXK_NOEXCEPT;

// Shows a preview of printout in a frame of its own, and returns the frame, or null when the
// preview could not be made. The preview takes the printouts, and deletes them when it closes.
WXK_FUNCTION(wxk_Window*) wxk_PrintPreview_Show(wxk_Object* printout, wxk_Object* printoutForPrinting, wxk_Object* data, wxk_Window* parent, const char16_t* title, size_t titleLength, wxk_Size size) WXK_NOEXCEPT;

// Sets the function that frees the GCHandle of a .NET object a control kept with an item, which
// is called when the control deletes the item.
WXK_FUNCTION(void) wxk_ClientData_SetFree(void (*free)(void* handle)) WXK_NOEXCEPT;
// Gives a tree item the .NET object, or none when the handle is null, deleting the data it had,
// which wxTreeCtrl::SetItemData does not.
WXK_FUNCTION(void) wxk_TreeCtrl_SetItemData(wxk_Window* tree, wxk_ItemId item, void* handle) WXK_NOEXCEPT;

// Adds a child to the array a model's GetChildren fills. The models' members and directors are generated.
WXK_FUNCTION(void) wxk_DataViewItems_Add(wxk_DataViewItems* items, wxk_ItemId item) WXK_NOEXCEPT;

// Accessibility

// Gives the window an accessible object made with wxk_Accessible_new, which replaces the one it had
// and keeps its name. Returns 0 where there's no Microsoft Active Accessibility, or when it fails.
// The accessible object's members, and its director, are generated.
WXK_FUNCTION(uint8_t) wxk_Window_GiveAccessible(wxk_Window* window, wxk_Accessible* accessible) WXK_NOEXCEPT;

// Parts and rows of a window that draws itself, which wxWidgets shows screen readers on macOS and
// GTK 3, as it does its own list control's and status bar's. They do nothing elsewhere.

// A row, whose cells are cellCount of the cell labels and rectangles, from firstCell on.
typedef struct wxk_AccessibleRow {
	int64_t index;
	wxk_Rect rect;
	uint8_t selected;
	int32_t firstCell;
	int32_t cellCount;
} wxk_AccessibleRow;

// Gives the window parts that screen readers read as text, a label and a rectangle for each,
// replacing the ones it had. None removes them.
WXK_FUNCTION(void) wxk_Window_SetAccessibleParts(wxk_Window* window, const wxk_StringArray* labels, const wxk_Rect* rects) WXK_NOEXCEPT;
// Shows the window as a list or table of the given rows, which are the ones shown, of total.
WXK_FUNCTION(void) wxk_Window_SetAccessibleRows(wxk_Window* window, const wxk_AccessibleRow* rows, size_t count, const wxk_StringArray* cellLabels, const wxk_Rect* cellRects, int64_t total) WXK_NOEXCEPT;
// Tells screen readers which row, by its index, is the current one, or none for -1.
WXK_FUNCTION(void) wxk_Window_SetAccessibleCurrentRow(wxk_Window* window, int64_t row) WXK_NOEXCEPT;

// Live regions

// Gets a window ready to announce through, and returns whether announcements can work, which on
// Windows gives it a UI Automation provider of its own.
WXK_FUNCTION(uint8_t) wxk_LiveRegion_Prepare(wxk_Window* window) WXK_NOEXCEPT;
// Has screen readers speak a message, at priority 0, 1, or 2, for low, medium, and high, and
// returns whether one could be told, which on Windows is false when no UI Automation client listens.
WXK_FUNCTION(uint8_t) wxk_LiveRegion_Announce(wxk_Window* window, const char16_t* message, size_t messageLength, int32_t priority) WXK_NOEXCEPT;

// What screen readers learn of a window

// Asks the platform's accessibility API about a window, as a screen reader would: MSAA on Windows,
// NSAccessibility on macOS, and ATK on Linux. Fills in its role, as a wxAccRole, its states, as
// the bits 1 for enabled, 2 for focused, and 4 for checked, its name, and the platform's name for
// its role, and returns whether it could ask.
WXK_FUNCTION(uint8_t) wxk_AccessibilityInfo_Get(wxk_Window* window, int32_t* role, uint32_t* states, wxk_String** name, wxk_String** platformRole) WXK_NOEXCEPT;

// Gives a control the label before it, as screen readers read it, where the platform does not.
WXK_FUNCTION(void) wxk_Window_LinkLabel(wxk_Window* window) WXK_NOEXCEPT;
// Names a window for screen readers, through ATK and NSAccessibility on Linux and macOS, where
// wxWidgets' SetAccessibleName does nothing.
WXK_FUNCTION(void) wxk_Window_SetAccessibleName(wxk_Window* window, const char16_t* name, size_t nameLength) WXK_NOEXCEPT;
// Sets a window's tooltip, which names a button that shows only an icon, unless it has a name of its own.
WXK_FUNCTION(void) wxk_Window_SetToolTip(wxk_Window* window, const char16_t* text, size_t textLength) WXK_NOEXCEPT;
// Names the icon buttons inside a window by their tooltips, such as an editable list box's.
WXK_FUNCTION(void) wxk_Window_NameIconButtons(wxk_Window* window) WXK_NOEXCEPT;

// The clipboard. Each call opens the clipboard and closes it again. The getters return null when
// the clipboard does not have their format.

WXK_FUNCTION(uint8_t) wxk_Clipboard_SetText(const char16_t* text, size_t textLength) WXK_NOEXCEPT;
WXK_FUNCTION(wxk_String*) wxk_Clipboard_GetText(void) WXK_NOEXCEPT;
WXK_FUNCTION(uint8_t) wxk_Clipboard_HasText(void) WXK_NOEXCEPT;
WXK_FUNCTION(void) wxk_Clipboard_Clear(void) WXK_NOEXCEPT;
WXK_FUNCTION(wxk_ClipboardContent*) wxk_ClipboardContent_new(void) WXK_NOEXCEPT;
WXK_FUNCTION(void) wxk_ClipboardContent_Free(wxk_ClipboardContent* content) WXK_NOEXCEPT;
WXK_FUNCTION(void) wxk_ClipboardContent_AddText(wxk_ClipboardContent* content, const char16_t* text, size_t textLength) WXK_NOEXCEPT;
WXK_FUNCTION(void) wxk_ClipboardContent_AddHtml(wxk_ClipboardContent* content, const char16_t* html, size_t htmlLength) WXK_NOEXCEPT;
WXK_FUNCTION(void) wxk_ClipboardContent_AddBitmap(wxk_ClipboardContent* content, wxk_Object* bitmap) WXK_NOEXCEPT;
WXK_FUNCTION(void) wxk_ClipboardContent_AddFiles(wxk_ClipboardContent* content, const wxk_StringArray* files) WXK_NOEXCEPT;
// Data in a format the application names, such as "MyApp.Shape", which the system registers.
WXK_FUNCTION(void) wxk_ClipboardContent_AddData(wxk_ClipboardContent* content, const char16_t* format, size_t formatLength, const uint8_t* data, size_t size) WXK_NOEXCEPT;
// Takes ownership of the content, whether or not it succeeds.
WXK_FUNCTION(uint8_t) wxk_Clipboard_SetContent(wxk_ClipboardContent* content) WXK_NOEXCEPT;
WXK_FUNCTION(uint8_t) wxk_Clipboard_Has(wxk_ClipboardFormat format) WXK_NOEXCEPT;
WXK_FUNCTION(uint8_t) wxk_Clipboard_HasData(const char16_t* format, size_t formatLength) WXK_NOEXCEPT;
WXK_FUNCTION(wxk_String*) wxk_Clipboard_GetHtml(void) WXK_NOEXCEPT;
WXK_FUNCTION(wxk_Object*) wxk_Clipboard_GetBitmap(void) WXK_NOEXCEPT;
WXK_FUNCTION(wxk_StringArray*) wxk_Clipboard_GetFiles(void) WXK_NOEXCEPT;
// The caller frees the data with wxk_Bytes_Free.
WXK_FUNCTION(uint8_t*) wxk_Clipboard_GetData(const char16_t* format, size_t formatLength, size_t* size) WXK_NOEXCEPT;
// Keeps the clipboard's content after the application exits.
WXK_FUNCTION(uint8_t) wxk_Clipboard_Flush(void) WXK_NOEXCEPT;

// Drag and drop

// Data a drag carries, which a drop target's handler reads while it runs.
typedef struct wxk_DropData wxk_DropData;

// The kinds of data a drop target takes, as bits of the kinds it is made with: the standard ones,
// and formats an application names.
typedef enum wxk_DropKind {
	WXK_DROP_NONE = -1,
	WXK_DROP_TEXT = 0,
	WXK_DROP_BITMAP = 1,
	WXK_DROP_FILES = 2,
	WXK_DROP_HTML = 3,
	WXK_DROP_NAMED = 4
} wxk_DropKind;

// Where a drag is: entering the target, or moving over it.
typedef enum wxk_DropStage {
	WXK_DROP_ENTER = 0,
	WXK_DROP_OVER = 1
} wxk_DropStage;

// What a drop target asks .NET. Each gets the target's userdata, and the drag's position in the
// window, the result wxWidgets suggests, a wxDragResult, and the kind of data that would arrive,
// with a named format's name, and returns the result to show or give. onFree tells .NET the
// target is gone.
typedef struct wxk_DropTargetCallbacks {
	int32_t (*onDrag)(void* userdata, int32_t stage, int32_t x, int32_t y, int32_t suggested, int32_t kind, const char16_t* name, size_t nameLength);
	void (*onLeave)(void* userdata);
	int32_t (*onDrop)(void* userdata, int32_t x, int32_t y, int32_t suggested, int32_t kind, const char16_t* name, size_t nameLength, wxk_DropData* data);
	void (*onFree)(void* userdata);
} wxk_DropTargetCallbacks;

WXK_FUNCTION(void) wxk_DropTarget_SetCallbacks(const wxk_DropTargetCallbacks* callbacks) WXK_NOEXCEPT;
// Drags content from a window, which takes ownership of it, with wxDrag flags, and returns the
// wxDragResult once it is dropped or cancelled.
WXK_FUNCTION(int32_t) wxk_DragDrop_DoDragDrop(wxk_Window* source, wxk_ClipboardContent* content, int32_t flags) WXK_NOEXCEPT;
// Makes a window a drop target for the kinds in kinds, a bit for each wxk_DropKind, and the
// formats named, which may be null.
WXK_FUNCTION(uint8_t) wxk_DropTarget_Set(wxk_Window* window, uint32_t kinds, const wxk_StringArray* named, void* userdata) WXK_NOEXCEPT;
WXK_FUNCTION(void) wxk_DropTarget_Remove(wxk_Window* window) WXK_NOEXCEPT;
// What was dropped: the text or HTML, the bitmap, the files, or a copy of a named format's bytes,
// which wxk_Bytes_Free frees. Each is null when the data is of another kind.
WXK_FUNCTION(wxk_String*) wxk_DropData_GetText(wxk_DropData* data) WXK_NOEXCEPT;
WXK_FUNCTION(wxk_Object*) wxk_DropData_GetBitmap(wxk_DropData* data) WXK_NOEXCEPT;
WXK_FUNCTION(wxk_StringArray*) wxk_DropData_GetFiles(wxk_DropData* data) WXK_NOEXCEPT;
WXK_FUNCTION(uint8_t*) wxk_DropData_GetBytes(wxk_DropData* data, size_t* size) WXK_NOEXCEPT;
// For tests: drags content, which it takes ownership of, over a window's drop target and drops
// it, without the system's drag loop, and returns the wxDragResult.
WXK_FUNCTION(int32_t) wxk_DropTarget_TestDrop(wxk_Window* window, wxk_ClipboardContent* content, int32_t x, int32_t y, int32_t suggested) WXK_NOEXCEPT;

// Images. Each file format is in a file of its own, so a statically linked application only
// links the formats it adds. BMP is always there.

WXK_FUNCTION(void) wxk_Image_AddPngHandler(void) WXK_NOEXCEPT;
WXK_FUNCTION(void) wxk_Image_AddJpegHandler(void) WXK_NOEXCEPT;
WXK_FUNCTION(void) wxk_Image_AddTiffHandler(void) WXK_NOEXCEPT;
WXK_FUNCTION(void) wxk_Image_AddGifHandler(void) WXK_NOEXCEPT;
WXK_FUNCTION(void) wxk_Image_AddWebpHandler(void) WXK_NOEXCEPT;
WXK_FUNCTION(void) wxk_Image_AddPnmHandler(void) WXK_NOEXCEPT;
WXK_FUNCTION(void) wxk_Image_AddPcxHandler(void) WXK_NOEXCEPT;
WXK_FUNCTION(void) wxk_Image_AddIffHandler(void) WXK_NOEXCEPT;
WXK_FUNCTION(void) wxk_Image_AddTgaHandler(void) WXK_NOEXCEPT;
WXK_FUNCTION(void) wxk_Image_AddXpmHandler(void) WXK_NOEXCEPT;
WXK_FUNCTION(void) wxk_Image_AddIconHandler(void) WXK_NOEXCEPT;
WXK_FUNCTION(void) wxk_Image_AddAllHandlers(void) WXK_NOEXCEPT;

// Loads an image from the bytes of a file in a format whose handler was added, and returns whether
// it could. A failure is not logged.
WXK_FUNCTION(uint8_t) wxk_Image_LoadData(wxk_Object* image, const uint8_t* data, size_t length, int32_t type, int32_t index) WXK_NOEXCEPT;
// The image as the bytes of a file in a format, which the caller frees with wxk_Bytes_Free, or null
// when it cannot be saved in it. A failure is not logged.
WXK_FUNCTION(uint8_t*) wxk_Image_SaveData(wxk_Object* image, int32_t type, size_t* size) WXK_NOEXCEPT;

// The desktop. Its free functions, such as wxBell, are generated, as wxk_Desktop_Bell, and so
// are wxDisplay's and the other classes' that aren't wxObjects, each a handle of its own type.

// Asks for the system's appearance, 0, light, 1, or dark, 2, and returns 1 when it could, and 0 when not.
WXK_FUNCTION(int32_t) wxk_App_SetAppearance(int32_t appearance) WXK_NOEXCEPT;
// Whether the system is dark, its background is, and its applications are, as the bits 1, 2, and 4.
WXK_FUNCTION(int32_t) wxk_SystemAppearance_GetFlags(void) WXK_NOEXCEPT;
WXK_FUNCTION(wxk_String*) wxk_SystemAppearance_GetName(void) WXK_NOEXCEPT;

// Makes a taskbar icon whose menu is the one wxk_ManagedTaskBarIcon_SetMenu gives it, which
// wxWidgets shows when the icon is right-clicked, or on macOS clicked, and does not delete. A
// negative type is wxTBI_DEFAULT_TYPE.
WXK_FUNCTION(wxk_EvtHandler*) wxk_ManagedTaskBarIcon_new(int32_t type) WXK_NOEXCEPT;
WXK_FUNCTION(void) wxk_ManagedTaskBarIcon_SetMenu(wxk_EvtHandler* icon, wxk_Menu* menu) WXK_NOEXCEPT;
// Removes the icon and deletes it once the event loop is idle. Call it only once.
WXK_FUNCTION(void) wxk_ManagedTaskBarIcon_Destroy(wxk_EvtHandler* icon) WXK_NOEXCEPT;

// Web views

// Whether the system has the browser engine a web view needs, such as Edge's WebView2 runtime.
WXK_FUNCTION(uint8_t) wxk_WebView_IsAvailable(void) WXK_NOEXCEPT;
// Creates a web view showing url, whose browser engine keeps its data in dataPath, or where it
// chooses when that is empty. Returns null when the engine could not start.
WXK_FUNCTION(wxk_Window*) wxk_WebView_new(wxk_Window* parent, int32_t id, const char16_t* url, size_t urlLength, wxk_Point position, wxk_Size size, int64_t style, const char16_t* dataPath, size_t dataPathLength) WXK_NOEXCEPT;
WXK_FUNCTION(void) wxk_WebView_LoadURL(wxk_Window* self, const char16_t* url, size_t urlLength) WXK_NOEXCEPT;
WXK_FUNCTION(wxk_String*) wxk_WebView_GetCurrentURL(wxk_Window* self) WXK_NOEXCEPT;
// Shows html, whose relative links are resolved against baseUrl.
WXK_FUNCTION(void) wxk_WebView_SetPage(wxk_Window* self, const char16_t* html, size_t htmlLength, const char16_t* baseUrl, size_t baseUrlLength) WXK_NOEXCEPT;
// Runs a script, whose result comes in a script result event, with request as its client data.
WXK_FUNCTION(void) wxk_WebView_RunScriptAsync(wxk_Window* self, const char16_t* script, size_t scriptLength, int64_t request) WXK_NOEXCEPT;
// The request a script result event answers.
WXK_FUNCTION(int64_t) wxk_WebViewEvent_GetRequest(wxk_Event* event) WXK_NOEXCEPT;

// Translations

typedef struct wxk_Translations wxk_Translations;
// Where a loader puts the catalog it found, through wxk_CatalogSink_Supply.
typedef struct wxk_CatalogSink wxk_CatalogSink;

// The callbacks every managed translations loader calls, with the userdata its wxk_Translations was
// made with. loadCatalog takes the strings it is given, and supplies the catalog to the sink, or
// nothing when it has none, so that a file is looked for. availableTranslations adds the languages
// it has to the array.
typedef struct wxk_TranslationsLoaderCallbacks {
	void (*loadCatalog)(void* userdata, wxk_String* domain, wxk_String* language, wxk_CatalogSink* sink);
	void (*availableTranslations)(void* userdata, wxk_String* domain, wxk_StringArray* languages);
	void (*onFree)(void* userdata);
} wxk_TranslationsLoaderCallbacks;

// A language, as wxLanguageInfo describes it. The strings are the caller's to free.
typedef struct wxk_LanguageInfo {
	int32_t language;
	wxk_String* canonicalName;
	wxk_String* localeTag;
	wxk_String* description;
	wxk_String* descriptionNative;
	int32_t layoutDirection;
} wxk_LanguageInfo;

WXK_FUNCTION(void) wxk_TranslationsLoader_SetCallbacks(const wxk_TranslationsLoaderCallbacks* callbacks) WXK_NOEXCEPT;
// Gives the sink a catalog read from the bytes of a .mo file, and returns whether they were one.
WXK_FUNCTION(uint8_t) wxk_CatalogSink_Supply(wxk_CatalogSink* sink, const uint8_t* data, size_t length) WXK_NOEXCEPT;
// Makes a wxTranslations whose loader asks .NET first, and frees the userdata when it is deleted.
WXK_FUNCTION(wxk_Translations*) wxk_Translations_new(void* loaderUserdata) WXK_NOEXCEPT;
WXK_FUNCTION(void) wxk_Translations_delete(wxk_Translations* translations) WXK_NOEXCEPT;
// Makes translations the global one, which wxWidgets then owns, and deletes the one before it.
WXK_FUNCTION(void) wxk_Translations_SetCurrent(wxk_Translations* translations) WXK_NOEXCEPT;
// Sets the language, or the user's preferred languages when language is null.
WXK_FUNCTION(void) wxk_Translations_SetLanguage(wxk_Translations* translations, const char16_t* language, size_t languageLength) WXK_NOEXCEPT;
WXK_FUNCTION(uint8_t) wxk_Translations_AddStdCatalog(wxk_Translations* translations) WXK_NOEXCEPT;
WXK_FUNCTION(uint8_t) wxk_Translations_AddCatalog(wxk_Translations* translations, const char16_t* domain, size_t domainLength) WXK_NOEXCEPT;
WXK_FUNCTION(uint8_t) wxk_Translations_IsLoaded(wxk_Translations* translations, const char16_t* domain, size_t domainLength) WXK_NOEXCEPT;
WXK_FUNCTION(wxk_StringArray*) wxk_Translations_GetAvailableTranslations(wxk_Translations* translations, const char16_t* domain, size_t domainLength) WXK_NOEXCEPT;
// The best language there is a catalog for, or null when there is none.
WXK_FUNCTION(wxk_String*) wxk_Translations_GetBestTranslation(wxk_Translations* translations, const char16_t* domain, size_t domainLength) WXK_NOEXCEPT;
WXK_FUNCTION(wxk_String*) wxk_Translations_GetHeaderValue(wxk_Translations* translations, const char16_t* header, size_t headerLength, const char16_t* domain, size_t domainLength) WXK_NOEXCEPT;
WXK_FUNCTION(void) wxk_Translations_AddCatalogLookupPathPrefix(const char16_t* prefix, size_t prefixLength) WXK_NOEXCEPT;
// The global translations' translation of text, in context when it is not empty, from any catalog,
// or null when there is none.
WXK_FUNCTION(wxk_String*) wxk_Translations_GetString(const char16_t* text, size_t textLength, const char16_t* context, size_t contextLength) WXK_NOEXCEPT;
// The same for the plural form for n of the string whose singular is singular.
WXK_FUNCTION(wxk_String*) wxk_Translations_GetPluralString(const char16_t* singular, size_t singularLength, uint32_t n, const char16_t* context, size_t contextLength) WXK_NOEXCEPT;
WXK_FUNCTION(uint8_t) wxk_UILocale_UseDefault(void) WXK_NOEXCEPT;
WXK_FUNCTION(wxk_String*) wxk_UILocale_GetCurrentName(void) WXK_NOEXCEPT;
WXK_FUNCTION(wxk_StringArray*) wxk_UILocale_GetPreferredUILanguages(void) WXK_NOEXCEPT;
// Fills info for a locale such as "de" or "pt-BR", and returns whether wxWidgets knows it.
WXK_FUNCTION(uint8_t) wxk_LanguageInfo_Find(const char16_t* locale, size_t localeLength, wxk_LanguageInfo* info) WXK_NOEXCEPT;

// The About box

// What an About box shows. Each string is the shim's to free, and a null one, or a null list,
// is left out.
typedef struct wxk_AboutInfo {
	wxk_String* name;
	wxk_String* version;
	wxk_String* longVersion;
	wxk_String* description;
	wxk_String* copyright;
	wxk_String* license;
	wxk_String* webSite;
	wxk_String* webSiteDescription;
	const wxk_StringArray* developers;
	const wxk_StringArray* docWriters;
	const wxk_StringArray* artists;
	const wxk_StringArray* translators;
	const wxk_Object* icon;
} wxk_AboutInfo;

// Shows the platform's About box, or wxWidgets's own when the platform's cannot show everything.
WXK_FUNCTION(void) wxk_AboutBox_Show(const wxk_AboutInfo* about, wxk_Window* parent) WXK_NOEXCEPT;

// The functions generated from the wxWidgets interface headers.
#include "wxkit_generated.h"
