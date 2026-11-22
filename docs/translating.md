# Translating an application

WxKit translates an application with [gettext](https://www.gnu.org/software/gettext/), as wxWidgets translates its own text. You mark the strings to translate, the build extracts them into a template for translators, and each language's translations are compiled and embedded in the application. wxWidgets' own text, such as stock button labels, follows the same language, from catalogs WxKit ships for the 46 languages wxWidgets is translated into. The [settings sample](../samples/Settings), translated into German with a language picker, is where this guide's code comes from.

## Before you begin

Install the GNU gettext tools, and make sure that they're on your `PATH`. The build compiles translations with `msgfmt`, and makes and updates catalogs with `msginit` and `msgmerge`:

- Windows: `winget install mlocati.GetText`
- macOS: `brew install gettext`
- Debian and Ubuntu: `sudo apt install gettext`

Without them, an application still builds, with a warning, and its text stays in English.

## Mark the strings to translate

Import WxKit's gettext shortcuts, and pass each string a user sees through one of them:

```csharp
using static WxKit.Gettext;

Button save = new(panel, _("&Save"));
StaticText status = new(panel, _n("{0} file", "{0} files", count, count));
string state = _p("state", "Open");
```

- `_` translates a string.
- `_n` picks the form of a string for a count, in each language's own plural forms. Polish, for example, has three.
- `_p` translates a string in a context, which tells apart two strings that are the same in English, such as "Open" as a state and "Open" as a command.
- `_pn` does both.

Arguments after the string format the translation with them, as `string.Format` does, in the current culture. `_n` takes the count first, and then the arguments, which usually include the count again.

Pass string literals. The extractor reads your source code and doesn't run it, so it can't see a string in a variable, or translate an interpolated string such as `$"Hello, {name}"`. Use a placeholder instead, as in `_("Hello, {0}", name)`, so that a translator can move it. The extractor joins literals that you join with `+`.

To give translators context, put a comment that starts with `TRANSLATORS:` just above the call:

```csharp
// TRANSLATORS: Keep the tab and the shortcut after it, which the menu shows and the keyboard uses.
edit.Append(StandardId.Preferences, _("&Settings...\tCtrl+,"));
```

## Set up translations

Before you make any windows, set up translations with `Translations.Use`. It picks the best language the user prefers that there's a catalog for, and adds wxWidgets' catalog and the application's. You can also call it in `Main`, before `Application.Run`, so that a command-line mode's text is translated too:

```csharp
return Application.Run(() => {
	UILocale.UseDefault();
	Translations.Use("MyApp");
	MainFrame frame = new();
	frame.Show();
});
```

The first argument is the domain, the name of the application's catalogs, which is the assembly's name unless you set `WxKitTranslationDomain`. To use a language other than the user's, pass it second, as in `Translations.Use("MyApp", "de")`.

`UILocale.UseDefault` makes the numbers and dates in wxWidgets' controls follow the user's locale. It doesn't change the language of the text.

## Extract the strings

To extract the strings into a template, run the following command in the project's folder:

```sh
dotnet build -t:WxKitExtract
```

This writes `po/MyApp.pot`. The build warns about any call it can't read, such as one passed a variable, with the file and line. Commit the template with your source.

## Add a language

To add a language, make its catalog from the template with `msginit`. For example, for German:

```sh
msginit --input=po/MyApp.pot --output-file=po/de.po --locale=de --no-translator
```

Translators translate `po/de.po` in a text editor, or in a tool such as [Poedit](https://poedit.net/). The next build compiles it and embeds it in the application.

## Update the catalogs

When your strings change, update every catalog with the following command:

```sh
dotnet build -t:WxKitUpdateCatalogs
```

This extracts the strings again, and merges each catalog with the template, with `msgmerge`. A string that changed keeps the translation of the string it was, marked fuzzy, for a translator to review. As gettext does, the build leaves fuzzy entries out until a translator removes the `fuzzy` flag, so they stay in English until then. A new string is left untranslated, even when it looks like an old one, since `msgmerge`'s guesses at a translation are wrong more often than not. Set `WxKitFuzzyMatching` to `true` to have it guess, as fuzzy entries. The template and the catalogs keep the line endings they had.

## Check the translations

The build checks each catalog it compiles. `msgfmt` fails the build when a translation of a formatted string loses or adds a placeholder, since `string.Format` would throw. WxKit warns about translations that break what wxWidgets reads out of a label:

- WXKIT1001: The translation loses its original's accelerator, the `&` before the letter that chooses a menu item or control from the keyboard.
- WXKIT1002: The translation adds an accelerator that its original doesn't have. An ampersand that isn't one is written `&&`.
- WXKIT1003: The translation has more than one accelerator.
- WXKIT1004: The translation loses its original's shortcut, the tab and the text after it in a menu item, such as `\tCtrl+O`.
- WXKIT1005: The translation adds a shortcut that its original doesn't have.

A translated shortcut can use the language's names for the keys, such as `Strg+O` in German, since wxWidgets reads those too.

## Let users choose a language

A language picker lists the languages there are catalogs for, each named in itself, so that a user who can't read the current language finds their own. `LanguageInfo` gives each language's name:

```csharp
IEnumerable<string> codes = Translations.Current?.GetAvailableTranslations("MyApp") ?? [];
foreach (string code in codes.Prepend("en")) {
	string name = LanguageInfo.Find(code)?.NativeName ?? code;
	languages.Append(name);
}
```

The strings are in English already, so English has no catalog, and the list adds it.

A window's text is translated when the window is made. To change the language while the application runs, call `Translations.Use` with the new language, and make the windows again.

## Right-to-left languages

Hebrew, Arabic, and Persian are written from right to left, and windows in them lay out from the right. Windows follow the user's locale, so an application whose language is a setting of its own sets the direction itself. Set `Application.LayoutDirection` after `Translations.Use`, before you make any windows:

```csharp
string language = Translations.Current?.GetBestTranslation("MyApp") ?? "en";
Application.LayoutDirection = LanguageInfo.Find(language)?.LayoutDirection ?? LayoutDirection.Default;
```

New windows with no parent, and message boxes, are laid out in that direction, and a window with a parent takes its parent's. Windows that are already open keep theirs. To change the language while the application runs, set `Application.LayoutDirection` again, and set each open top-level window's `LayoutDirection` before you make its contents again. On macOS, Cocoa takes the direction from the languages the application is localized in, not from WxKit.

## Use F# or Visual Basic

Neither F# nor Visual Basic accepts `_` alone as a name. Both can use `_n`, `_p`, and `_pn`, and for a plain string, `Translations.Translate`, written with `Translations.` before it, which the extractor looks for.

## Ship wxWidgets' translations

The build embeds wxWidgets' catalog for each language the application has, which are the names of its catalogs in `po`, such as `de` for `po/de.po`. A language that wxWidgets has no catalog for uses its base language's, as `de_AT` uses `de`. Each of wxWidgets' catalogs is about 150 KB.

An application that doesn't translate its own text can still have wxWidgets' text follow the user's language. Set `WxKitStdCatalogs` to `all` to embed all 46 catalogs, about 6 MB, and call `Translations.Use` at start-up.

## Settings

Set these MSBuild properties and items in the project file:

- `WxKitTranslationDomain`: the name of the application's catalogs. The default is the assembly's name.
- `WxKitPoDirectory`: the folder of the application's catalogs. The default is `po`.
- `WxKitCatalog` items: catalogs laid out other than in the catalogs' folder, each with its language in `Language`, as in `<WxKitCatalog Include="locale/de/MyApp.po" Language="de" />`. When there are any, the build compiles and updates only these.
- `WxKitTranslationTemplate`: the template's path. The default is the domain's name in the catalogs' folder, as in `po/MyApp.pot`.
- `WxKitLanguage` items: the languages to embed wxWidgets' catalogs for, in place of the names of the catalogs in `po`.
- `WxKitStdCatalogs`: `all` to embed all of wxWidgets' catalogs, or `none` to embed none.
- `WxKitFuzzyMatching`: `true` to have `WxKitUpdateCatalogs` guess at new strings' translations from similar old ones, as fuzzy entries. The default is `false`.
- `WxKitMsgfmt` and `WxKitMsgmerge`: the paths of `msgfmt` and `msgmerge`, when they aren't on the `PATH`.

## How it works

The build embeds each catalog, compiled, as the resource `locale/<language>/<domain>.mo`, and wxWidgets' as `locale/<language>/wxstd.mo`. An `EmbeddedTranslationsLoader` reads them, and for a catalog that isn't embedded, wxWidgets looks for the same path next to the application. To load catalogs from somewhere else, derive from `TranslationsLoader`, and pass it to a `Translations` you set up yourself:

```csharp
Translations translations = new(new MyLoader()) { Language = "de" };
translations.AddStdCatalog();
translations.AddCatalog("MyApp");
Translations.Current = translations;
```

Looking strings up, as `_` does, works from any thread. Everything else is for the GUI thread.
