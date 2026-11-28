#include "shim.h"

#include <cstdlib>
#include <cstring>

#include <wx/image.h>

// An image's pixels in bulk, where wxWidgets' members take and give a pointer to them. wxImage
// frees what it is given with free(), so each copy is made with malloc().

namespace {

unsigned char* Copy(const uint8_t* bytes, size_t length) {
	auto* copy = static_cast<unsigned char*>(std::malloc(length > 0 ? length : 1));
	if (copy == nullptr) throw std::bad_alloc();
	std::memcpy(copy, bytes, length);
	return copy;
}

wxImage* Unwrap(wxk_Object* image) {
	return static_cast<wxImage*>(wxk::Unwrap(image));
}

size_t Pixels(const wxImage& image) {
	return static_cast<size_t>(image.GetWidth()) * static_cast<size_t>(image.GetHeight());
}

} // namespace

WXK_FUNCTION(wxk_Object*) wxk_Image_NewData(int32_t width, int32_t height, const uint8_t* rgb, const uint8_t* alpha) noexcept {
	return wxk::Guard<wxk_Object*>(nullptr, [&]() -> wxk_Object* {
		size_t pixels = static_cast<size_t>(width) * static_cast<size_t>(height);
		unsigned char* data = Copy(rgb, pixels * 3);
		unsigned char* opacity = alpha != nullptr ? Copy(alpha, pixels) : nullptr;
		wxObject* created = new wxImage(width, height, data, opacity, false);
		return wxk::Wrap(created);
	});
}

WXK_FUNCTION(const uint8_t*) wxk_Image_GetData(wxk_Object* image) noexcept {
	return wxk::Guard<const uint8_t*>(nullptr, [&] { return static_cast<const uint8_t*>(Unwrap(image)->GetData()); });
}

WXK_FUNCTION(void) wxk_Image_SetData(wxk_Object* image, const uint8_t* rgb) noexcept {
	wxk::Guard([&] {
		wxImage* self = Unwrap(image);
		self->SetData(Copy(rgb, Pixels(*self) * 3));
	});
}

WXK_FUNCTION(const uint8_t*) wxk_Image_GetAlphaData(wxk_Object* image) noexcept {
	return wxk::Guard<const uint8_t*>(nullptr, [&] { return static_cast<const uint8_t*>(Unwrap(image)->GetAlpha()); });
}

WXK_FUNCTION(void) wxk_Image_SetAlphaData(wxk_Object* image, const uint8_t* alpha) noexcept {
	wxk::Guard([&] {
		wxImage* self = Unwrap(image);
		self->SetAlpha(Copy(alpha, Pixels(*self)));
	});
}
