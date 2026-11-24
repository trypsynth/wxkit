/// The languages the sample is translated into, from its catalogs in po, which its build compiles
/// and embeds, with wxWidgets' own for the same languages.
module Settings.Languages

open System
open WxKit

// The name of the sample's catalogs, its assembly's name, as in po/de.po's compiled Settings.mo.
let private domain = "Settings"

/// A language's name in itself, as a language picker names it, or its code when WxKit has none.
let nativeName code =
    LanguageInfo.Find code |> Option.ofObj |> Option.map _.NativeName |> Option.defaultValue code

/// The language the sample's text is in, such as de, or en for its original English.
let current () =
    Translations.Current
    |> Option.ofObj
    |> Option.bind (fun translations -> translations.GetBestTranslation domain |> Option.ofObj)
    |> Option.defaultValue "en"

/// Whether that language is written right to left, as Hebrew and Arabic are, so that windows lay
/// out from the right. A window takes the direction before its contents are made.
let isRightToLeft () =
    LanguageInfo.Find(current ()) |> Option.ofObj |> Option.exists (fun info -> info.LayoutDirection = LayoutDirection.RightToLeft)

/// Translates the sample, and wxWidgets' own text, such as stock button labels, into a language,
/// or into the user's preferred one that there is a catalog for when there's none. Windows made
/// before keep the text they were made with.
let select (language: string option) =
    Translations.Use(domain, Option.toObj language) |> ignore

/// The languages there are catalogs for, and English, each as its code and its name in itself, so
/// that a user who can't read the current one finds their own.
let available () =
    let codes =
        match Translations.Current with
        | null -> []
        | translations -> List.ofSeq (translations.GetAvailableTranslations domain)
    "en" :: codes
    |> List.distinct
    |> List.map (fun code -> code, nativeName code)
    |> List.sortWith (fun (_, first) (_, second) -> String.Compare(first, second, StringComparison.CurrentCultureIgnoreCase))
