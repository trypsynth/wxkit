#include "shim.h"

#include <wx/filefn.h>
#include <wx/log.h>
#include <wx/sound.h>

#include <memory>

#ifdef __WXMSW__
#include <wx/msw/wrapwin.h>

#include <mmsystem.h>

#include <utility>
#include <vector>
#endif

// Sounds loaded from a file or from bytes. Both keep wxWidgets from logging a failure, which would
// show the user an error dialog, since the caller is told.
namespace {

#ifdef __WXMSW__
// wxMSW plays a sound from memory out of the sound's own copy of the bytes, which goes when the
// sound is deleted, though Windows goes on reading it as it plays, as when the garbage collector
// frees a sound nothing refers to any more. So a sound made from bytes plays a copy of its own,
// which the last sound played keeps alive until another plays, so that it goes on playing once
// deleted, as it does on the other platforms.
class DataSound final : public wxSound {
public:
	DataSound(const uint8_t* data, size_t length) : wxSound(length, data), _data(std::make_shared<const std::vector<uint8_t>>(data, data + length)) {}

protected:
	bool DoPlay(unsigned flags) const override {
		if (!IsOk()) return false;
		// Windows only loops a sound it plays asynchronously.
		DWORD options = SND_MEMORY | SND_NODEFAULT | ((flags & (wxSOUND_ASYNC | wxSOUND_LOOP)) != 0 ? SND_ASYNC : SND_SYNC);
		if ((flags & wxSOUND_LOOP) != 0) options |= SND_LOOP;
		// Never freed, since Windows may still be reading the bytes as the process ends. The
		// bytes played before are kept until PlaySound has stopped them.
		static auto* playing = new std::shared_ptr<const std::vector<uint8_t>>();
		std::shared_ptr<const std::vector<uint8_t>> previous = std::exchange(*playing, _data);
		return ::PlaySound(reinterpret_cast<LPCTSTR>(_data->data()), nullptr, options) != FALSE;
	}

private:
	std::shared_ptr<const std::vector<uint8_t>> _data;
};
#endif

wxk_Object* Loaded(std::unique_ptr<wxSound> sound) {
	if (!sound->IsOk()) return nullptr;
	return wxk::Wrap(static_cast<wxObject*>(sound.release()));
}

} // namespace

WXK_FUNCTION(wxk_Object*) wxk_Sound_FromFile(const char16_t* path, size_t pathLength) noexcept {
	return wxk::Guard<wxk_Object*>(nullptr, [&]() -> wxk_Object* {
		wxLogNull quiet;
		wxString name = wxk::FromUtf16(path, pathLength);
		// wxMSW only checks that there is a name.
		if (!wxFileExists(name)) return nullptr;
		return Loaded(std::make_unique<wxSound>(name));
	});
}

WXK_FUNCTION(wxk_Object*) wxk_Sound_FromData(const uint8_t* data, size_t length) noexcept {
	return wxk::Guard<wxk_Object*>(nullptr, [&]() -> wxk_Object* {
		wxLogNull quiet;
#ifdef __WXMSW__
		return Loaded(std::make_unique<DataSound>(data, length));
#else
		return Loaded(std::make_unique<wxSound>(length, data));
#endif
	});
}
