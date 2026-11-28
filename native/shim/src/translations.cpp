#include "shim.h"

#include <wx/app.h>
#include <wx/translation.h>
#include <wx/uilocale.h>

struct wxk_Translations {};

struct wxk_CatalogSink {
	wxString domain;
	wxMsgCatalog* catalog = nullptr;
};

namespace {

wxk_TranslationsLoaderCallbacks callbacks{};

wxTranslations* Unwrap(wxk_Translations* translations) {
	return reinterpret_cast<wxTranslations*>(translations);
}

// A loader that asks .NET for each catalog first, as the bytes of a .mo file, such as one embedded
// in the application, and looks for a file when .NET has none. Before the application starts,
// it looks for no file, since wxWidgets' file loader asks wxStandardPaths for its folders, which
// reads the application's name and vendor from wxTheApp.
class ManagedTranslationsLoader final : public wxTranslationsLoader {
public:
	explicit ManagedTranslationsLoader(void* userdata) : _userdata(userdata) {}

	~ManagedTranslationsLoader() override {
		if (callbacks.onFree != nullptr) callbacks.onFree(_userdata);
	}

	wxMsgCatalog* LoadCatalog(const wxString& domain, const wxString& lang) override {
		wxk_CatalogSink sink{domain};
		if (callbacks.loadCatalog != nullptr) callbacks.loadCatalog(_userdata, wxk::ToUtf16(domain), wxk::ToUtf16(lang), &sink);
		if (sink.catalog != nullptr || wxTheApp == nullptr) return sink.catalog;
		return _files.LoadCatalog(domain, lang);
	}

	wxArrayString GetAvailableTranslations(const wxString& domain) const override {
		wxArrayString languages = wxTheApp != nullptr ? _files.GetAvailableTranslations(domain) : wxArrayString();
		if (callbacks.availableTranslations != nullptr) callbacks.availableTranslations(_userdata, wxk::ToUtf16(domain), wxk::Wrap(&languages));
		languages.Sort();
		for (size_t index = languages.size(); index > 1; index--) {
			if (languages[index - 1] == languages[index - 2]) languages.RemoveAt(index - 1);
		}
		return languages;
	}

private:
	void* _userdata;
	wxFileTranslationsLoader _files;
};

const wxString* Translated(const char16_t* text, size_t textLength, const char16_t* context, size_t contextLength, const unsigned* n) {
	wxTranslations* translations = wxTranslations::Get();
	if (translations == nullptr) return nullptr;
	wxString original = wxk::FromUtf16(text, textLength);
	wxString inContext = wxk::FromUtf16(context, contextLength);
	return n != nullptr ? translations->GetTranslatedString(original, *n, wxString(), inContext) : translations->GetTranslatedString(original, wxString(), inContext);
}

} // namespace

WXK_FUNCTION(void) wxk_TranslationsLoader_SetCallbacks(const wxk_TranslationsLoaderCallbacks* set) noexcept {
	callbacks = *set;
}

WXK_FUNCTION(uint8_t) wxk_CatalogSink_Supply(wxk_CatalogSink* sink, const uint8_t* data, size_t length) noexcept {
	return wxk::Guard<uint8_t>(0, [&]() -> uint8_t {
		// The catalog copies what it needs out of the data, which only lives for this call.
		delete sink->catalog;
		sink->catalog = wxMsgCatalog::CreateFromData(wxScopedCharBuffer::CreateNonOwned(reinterpret_cast<const char*>(data), length), sink->domain);
		return sink->catalog != nullptr ? 1 : 0;
	});
}

WXK_FUNCTION(wxk_Translations*) wxk_Translations_new(void* loaderUserdata) noexcept {
	return wxk::Guard<wxk_Translations*>(nullptr, [&]() -> wxk_Translations* {
		auto* translations = new wxTranslations();
		translations->SetLoader(new ManagedTranslationsLoader(loaderUserdata));
		return reinterpret_cast<wxk_Translations*>(translations);
	});
}

WXK_FUNCTION(void) wxk_Translations_delete(wxk_Translations* translations) noexcept {
	wxk::Guard([&] { delete Unwrap(translations); });
}

WXK_FUNCTION(void) wxk_Translations_SetCurrent(wxk_Translations* translations) noexcept {
	wxk::Guard([&] { wxTranslations::Set(Unwrap(translations)); });
}

WXK_FUNCTION(void) wxk_Translations_SetLanguage(wxk_Translations* translations, const char16_t* language, size_t languageLength) noexcept {
	wxk::Guard([&] {
		if (language == nullptr) Unwrap(translations)->SetLanguage(wxLANGUAGE_DEFAULT);
		else Unwrap(translations)->SetLanguage(wxk::FromUtf16(language, languageLength));
	});
}

WXK_FUNCTION(uint8_t) wxk_Translations_AddStdCatalog(wxk_Translations* translations) noexcept {
	return wxk::Guard<uint8_t>(0, [&]() -> uint8_t { return Unwrap(translations)->AddStdCatalog() ? 1 : 0; });
}

WXK_FUNCTION(uint8_t) wxk_Translations_AddCatalog(wxk_Translations* translations, const char16_t* domain, size_t domainLength) noexcept {
	return wxk::Guard<uint8_t>(0, [&]() -> uint8_t { return Unwrap(translations)->AddCatalog(wxk::FromUtf16(domain, domainLength)) ? 1 : 0; });
}

WXK_FUNCTION(uint8_t) wxk_Translations_IsLoaded(wxk_Translations* translations, const char16_t* domain, size_t domainLength) noexcept {
	return wxk::Guard<uint8_t>(0, [&]() -> uint8_t { return Unwrap(translations)->IsLoaded(wxk::FromUtf16(domain, domainLength)) ? 1 : 0; });
}

WXK_FUNCTION(wxk_StringArray*) wxk_Translations_GetAvailableTranslations(wxk_Translations* translations, const char16_t* domain, size_t domainLength) noexcept {
	return wxk::Guard<wxk_StringArray*>(nullptr, [&] { return wxk::CopyStrings(Unwrap(translations)->GetAvailableTranslations(wxk::FromUtf16(domain, domainLength))); });
}

WXK_FUNCTION(wxk_String*) wxk_Translations_GetBestTranslation(wxk_Translations* translations, const char16_t* domain, size_t domainLength) noexcept {
	return wxk::Guard<wxk_String*>(nullptr, [&]() -> wxk_String* {
		// GetBestAvailableTranslation leaves out English, which the strings are in and so has no
		// catalog, and names the next language the user prefers when English comes first, though
		// AddCatalog then loads none. GetBestTranslation counts English, as AddCatalog does.
		wxString best = Unwrap(translations)->GetBestTranslation(wxk::FromUtf16(domain, domainLength));
		return best.empty() ? nullptr : wxk::ToUtf16(best);
	});
}

WXK_FUNCTION(wxk_String*) wxk_Translations_GetHeaderValue(wxk_Translations* translations, const char16_t* header, size_t headerLength, const char16_t* domain, size_t domainLength) noexcept {
	return wxk::Guard<wxk_String*>(nullptr, [&]() -> wxk_String* {
		wxString value = Unwrap(translations)->GetHeaderValue(wxk::FromUtf16(header, headerLength), wxk::FromUtf16(domain, domainLength));
		return value.empty() ? nullptr : wxk::ToUtf16(value);
	});
}

WXK_FUNCTION(void) wxk_Translations_AddCatalogLookupPathPrefix(const char16_t* prefix, size_t prefixLength) noexcept {
	wxk::Guard([&] { wxFileTranslationsLoader::AddCatalogLookupPathPrefix(wxk::FromUtf16(prefix, prefixLength)); });
}

WXK_FUNCTION(wxk_String*) wxk_Translations_GetString(const char16_t* text, size_t textLength, const char16_t* context, size_t contextLength) noexcept {
	return wxk::Guard<wxk_String*>(nullptr, [&]() -> wxk_String* {
		const wxString* translated = Translated(text, textLength, context, contextLength, nullptr);
		return translated != nullptr ? wxk::ToUtf16(*translated) : nullptr;
	});
}

WXK_FUNCTION(wxk_String*) wxk_Translations_GetPluralString(const char16_t* singular, size_t singularLength, uint32_t n, const char16_t* context, size_t contextLength) noexcept {
	return wxk::Guard<wxk_String*>(nullptr, [&]() -> wxk_String* {
		unsigned count = n;
		const wxString* translated = Translated(singular, singularLength, context, contextLength, &count);
		return translated != nullptr ? wxk::ToUtf16(*translated) : nullptr;
	});
}

WXK_FUNCTION(uint8_t) wxk_UILocale_UseDefault(void) noexcept {
	return wxk::Guard<uint8_t>(0, []() -> uint8_t { return wxUILocale::UseDefault() ? 1 : 0; });
}

WXK_FUNCTION(wxk_String*) wxk_UILocale_GetCurrentName(void) noexcept {
	return wxk::Guard<wxk_String*>(nullptr, [] { return wxk::ToUtf16(wxUILocale::GetCurrent().GetName()); });
}

WXK_FUNCTION(wxk_StringArray*) wxk_UILocale_GetPreferredUILanguages(void) noexcept {
	return wxk::Guard<wxk_StringArray*>(nullptr, [] {
		wxArrayString languages;
		for (const wxString& language : wxUILocale::GetPreferredUILanguages()) languages.Add(language);
		return wxk::CopyStrings(languages);
	});
}

WXK_FUNCTION(uint8_t) wxk_LanguageInfo_Find(const char16_t* locale, size_t localeLength, wxk_LanguageInfo* info) noexcept {
	return wxk::Guard<uint8_t>(0, [&]() -> uint8_t {
		// The string overload only takes an underscore before the region, so a hyphen means a BCP 47
		// tag, such as the user's preferred languages are.
		wxString name = wxk::FromUtf16(locale, localeLength);
		const wxLanguageInfo* found = name.Contains('-') ? wxUILocale::FindLanguageInfo(wxLocaleIdent::FromTag(name)) : wxUILocale::FindLanguageInfo(name);
		if (found == nullptr) return 0;
		info->language = found->Language;
		info->canonicalName = wxk::ToUtf16(found->CanonicalName);
		info->localeTag = wxk::ToUtf16(found->LocaleTag);
		info->description = wxk::ToUtf16(found->Description);
		info->descriptionNative = wxk::ToUtf16(found->DescriptionNative);
		info->layoutDirection = static_cast<int32_t>(found->LayoutDirection);
		return 1;
	});
}
