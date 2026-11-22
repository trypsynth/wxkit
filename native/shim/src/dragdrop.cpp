#include "shim.h"

#include <wx/dataobj.h>
#include <wx/dnd.h>
#include <wx/imagpng.h>

#include <cstring>
#include <vector>

// Dragging content out of a window, and dropping content on one. The content to drag is a
// wxDataObjectComposite, as the clipboard's is, and a drop target accepts the formats it is made
// with, and asks .NET what to do as a drag passes over it.

namespace {

wxk_DropTargetCallbacks callbacks{};

// The kind of data a format is, as .NET knows it: the standard ones, and a format an application names.
int32_t KindOf(const wxDataFormat& format) {
	if (format.GetType() == wxDF_INVALID) return WXK_DROP_NONE;
	switch (format.GetType()) {
	case wxDF_TEXT:
	case wxDF_OEMTEXT:
	case wxDF_UNICODETEXT:
		return WXK_DROP_TEXT;
	// A bitmap's data object also takes a device-independent bitmap, as Windows offers one, and PNG.
	case wxDF_BITMAP:
	case wxDF_DIB:
	case wxDF_PNG:
		return WXK_DROP_BITMAP;
	case wxDF_FILENAME:
		return WXK_DROP_FILES;
	case wxDF_HTML:
		return WXK_DROP_HTML;
	default:
		return WXK_DROP_NAMED;
	}
}

// What was dropped, which .NET reads while its handler runs: one part of the target's content,
// in the format it arrived in.
struct DropData {
	int32_t kind;
	wxDataObjectSimple* part;
};

class ManagedDropTarget final : public wxDropTarget {
public:
	ManagedDropTarget(uint32_t kinds, const wxArrayString& named, void* userdata) : wxDropTarget(new wxDataObjectComposite()), _userdata(userdata) {
		auto* content = Content();
		// The composite takes ownership of each part. Text comes first, so a drag of several
		// formats arrives as the first of these the target accepts.
		if ((kinds & (1u << WXK_DROP_TEXT)) != 0) content->Add(new wxTextDataObject());
		if ((kinds & (1u << WXK_DROP_HTML)) != 0) content->Add(new wxHTMLDataObject());
		if ((kinds & (1u << WXK_DROP_BITMAP)) != 0) {
#ifdef __WXGTK__
			wxk::AddImageHandler<wxPNGHandler>(wxBITMAP_TYPE_PNG);
#endif
			content->Add(new wxBitmapDataObject());
		}
		if ((kinds & (1u << WXK_DROP_FILES)) != 0) content->Add(new wxFileDataObject());
		for (const wxString& name : named) content->Add(new wxCustomDataObject(wxDataFormat(name)));
	}

	~ManagedDropTarget() override {
		if (callbacks.onFree != nullptr) callbacks.onFree(_userdata);
	}

	wxDragResult OnEnter(wxCoord x, wxCoord y, wxDragResult suggested) override {
		return Ask(WXK_DROP_ENTER, x, y, suggested, Matching());
	}

	wxDragResult OnDragOver(wxCoord x, wxCoord y, wxDragResult suggested) override {
		return Ask(WXK_DROP_OVER, x, y, suggested, Matching());
	}

	// The system asks whether the drop can land, which on macOS reads the drag's pasteboard, and a
	// test drags without one.
	bool OnDrop(wxCoord x, wxCoord y) override {
		return _testFormat.GetType() != wxDF_INVALID || wxDropTarget::OnDrop(x, y);
	}

	void OnLeave() override {
		if (callbacks.onLeave != nullptr) callbacks.onLeave(_userdata);
	}

	wxDragResult OnData(wxCoord x, wxCoord y, wxDragResult suggested) override {
		if (!GetData()) return wxDragNone;
		return Dropped(x, y, suggested);
	}

	// Gives .NET what arrived, which the content now holds, and returns what it did with it.
	wxDragResult Dropped(wxCoord x, wxCoord y, wxDragResult suggested) {
		auto* content = Content();
		wxDataFormat format = content->GetReceivedFormat();
		DropData data{KindOf(format), content->GetObject(format)};
		if (callbacks.onDrop == nullptr || data.part == nullptr) return wxDragNone;
		std::u16string name = NameOf(data.kind, format);
		if (data.kind == WXK_DROP_NAMED && name.empty()) return wxDragNone;
		return static_cast<wxDragResult>(callbacks.onDrop(_userdata, x, y, suggested, data.kind, name.data(), name.size(), reinterpret_cast<wxk_DropData*>(&data)));
	}

	wxDataObjectComposite* Content() {
		return static_cast<wxDataObjectComposite*>(GetDataObject());
	}

	// For a test, which drags without the system: the format the system would have matched.
	void SetTestFormat(const wxDataFormat& format) {
		_testFormat = format;
	}

private:
	wxDataFormat Matching() {
		return _testFormat.GetType() != wxDF_INVALID ? _testFormat : GetMatchingPair();
	}

	static std::u16string NameOf(int32_t kind, const wxDataFormat& format) {
		if (kind != WXK_DROP_NAMED) return std::u16string();
		wxk_String* id = wxk::ToUtf16(format.GetId());
		std::u16string name = id->text;
		delete id;
		return name;
	}

	// A drag the target cannot take any format of is not offered to .NET.
	wxDragResult Ask(int32_t stage, wxCoord x, wxCoord y, wxDragResult suggested, const wxDataFormat& format) {
		int32_t kind = KindOf(format);
		if (kind == WXK_DROP_NONE) return wxDragNone;
		if (callbacks.onDrag == nullptr) return suggested;
		std::u16string name = NameOf(kind, format);
		// A standard format WxKit has no kind for, which has no name, is not offered either.
		if (kind == WXK_DROP_NAMED && name.empty()) return wxDragNone;
		return static_cast<wxDragResult>(callbacks.onDrag(_userdata, stage, x, y, suggested, kind, name.data(), name.size()));
	}

	void* _userdata;
	wxDataFormat _testFormat;
};

wxDataObjectComposite* Composite(wxk_ClipboardContent* content) {
	return reinterpret_cast<wxDataObjectComposite*>(content);
}

DropData* Unwrap(wxk_DropData* data) {
	return reinterpret_cast<DropData*>(data);
}

} // namespace

WXK_FUNCTION(void) wxk_DropTarget_SetCallbacks(const wxk_DropTargetCallbacks* set) noexcept {
	callbacks = *set;
}

// Starts dragging the content from the window, and returns once it is dropped or the drag is
// cancelled, with what the target did. The content is freed afterwards.
WXK_FUNCTION(int32_t) wxk_DragDrop_DoDragDrop(wxk_Window* source, wxk_ClipboardContent* content, int32_t flags) noexcept {
	wxDataObjectComposite* data = Composite(content);
	int32_t result = wxk::Guard<int32_t>(wxDragError, [&]() -> int32_t {
		wxDropSource drag(*data, wxk::Unwrap(source));
		return drag.DoDragDrop(flags);
	});
	delete data;
	return result;
}

// Makes the window a drop target for the kinds of data in kinds, a bit for each wxk_DropKind, and
// the formats named, in place of any target it had. The window owns the target, and frees it,
// which tells .NET through onFree.
WXK_FUNCTION(uint8_t) wxk_DropTarget_Set(wxk_Window* window, uint32_t kinds, const wxk_StringArray* named, void* userdata) noexcept {
	return wxk::Guard<uint8_t>(0, [&]() -> uint8_t {
		wxArrayString names;
		if (named != nullptr) names = *wxk::Unwrap(const_cast<wxk_StringArray*>(named));
		wxk::Unwrap(window)->SetDropTarget(new ManagedDropTarget(kinds, names, userdata));
		return 1;
	});
}

WXK_FUNCTION(void) wxk_DropTarget_Remove(wxk_Window* window) noexcept {
	wxk::Guard([&] { wxk::Unwrap(window)->SetDropTarget(nullptr); });
}

WXK_FUNCTION(wxk_String*) wxk_DropData_GetText(wxk_DropData* data) noexcept {
	return wxk::Guard<wxk_String*>(nullptr, [&]() -> wxk_String* {
		DropData* dropped = Unwrap(data);
		if (dropped->kind == WXK_DROP_TEXT) return wxk::ToUtf16(static_cast<wxTextDataObject*>(dropped->part)->GetText());
		if (dropped->kind == WXK_DROP_HTML) return wxk::ToUtf16(static_cast<wxHTMLDataObject*>(dropped->part)->GetHTML());
		return nullptr;
	});
}

WXK_FUNCTION(wxk_Object*) wxk_DropData_GetBitmap(wxk_DropData* data) noexcept {
	return wxk::Guard<wxk_Object*>(nullptr, [&]() -> wxk_Object* {
		DropData* dropped = Unwrap(data);
		return dropped->kind == WXK_DROP_BITMAP ? wxk::CopyValue(static_cast<wxBitmapDataObject*>(dropped->part)->GetBitmap()) : nullptr;
	});
}

WXK_FUNCTION(wxk_StringArray*) wxk_DropData_GetFiles(wxk_DropData* data) noexcept {
	return wxk::Guard<wxk_StringArray*>(nullptr, [&]() -> wxk_StringArray* {
		DropData* dropped = Unwrap(data);
		return dropped->kind == WXK_DROP_FILES ? wxk::Wrap(new wxArrayString(static_cast<wxFileDataObject*>(dropped->part)->GetFilenames())) : nullptr;
	});
}

// A copy of data in a format an application names, which the caller frees with wxk_Bytes_Free.
WXK_FUNCTION(uint8_t*) wxk_DropData_GetBytes(wxk_DropData* data, size_t* size) noexcept {
	*size = 0;
	return wxk::Guard<uint8_t*>(nullptr, [&]() -> uint8_t* {
		DropData* dropped = Unwrap(data);
		if (dropped->kind != WXK_DROP_NAMED) return nullptr;
		auto* custom = static_cast<wxCustomDataObject*>(dropped->part);
		auto* copy = new uint8_t[custom->GetSize() == 0 ? 1 : custom->GetSize()];
		if (custom->GetSize() > 0) std::memcpy(copy, custom->GetData(), custom->GetSize());
		*size = custom->GetSize();
		return copy;
	});
}

// For tests: drags the content over the window's drop target and drops it, as the system does,
// without the system's own drag loop, which a test cannot drive. The content is freed afterwards.
WXK_FUNCTION(int32_t) wxk_DropTarget_TestDrop(wxk_Window* window, wxk_ClipboardContent* content, int32_t x, int32_t y, int32_t suggested) noexcept {
	wxDataObjectComposite* offered = Composite(content);
	int32_t result = wxk::Guard<int32_t>(wxDragError, [&]() -> int32_t {
		auto* target = dynamic_cast<ManagedDropTarget*>(wxk::Unwrap(window)->GetDropTarget());
		if (target == nullptr) return wxDragError;
		// The first format the target takes that the content has, as the system would choose.
		size_t count = target->Content()->GetFormatCount(wxDataObject::Set);
		std::vector<wxDataFormat> accepted(count);
		target->Content()->GetAllFormats(accepted.data(), wxDataObject::Set);
		for (const wxDataFormat& format : accepted) {
			if (!offered->IsSupported(format, wxDataObject::Get)) continue;
			target->SetTestFormat(format);
			auto drag = static_cast<wxDragResult>(suggested);
			wxDragResult entered = target->OnEnter(x, y, drag);
			if (entered == wxDragNone) {
				target->OnLeave();
				return wxDragNone;
			}
			drag = target->OnDragOver(x, y, entered);
			if (drag == wxDragNone || !target->OnDrop(x, y)) {
				target->OnLeave();
				return wxDragNone;
			}
			std::vector<unsigned char> bytes(offered->GetDataSize(format) + 1);
			offered->GetDataHere(format, bytes.data());
			target->Content()->SetData(format, offered->GetDataSize(format), bytes.data());
			wxDragResult dropped = target->Dropped(x, y, drag);
			target->SetTestFormat(wxDataFormat());
			return dropped;
		}
		return wxDragNone;
	});
	delete offered;
	return result;
}
