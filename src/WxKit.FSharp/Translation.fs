namespace WxKit.FSharp

open WxKit

/// <summary>
/// Text for translators, as Gettext's <c>_</c>, <c>_n</c>, <c>_p</c>, and <c>_pn</c> are in C#,
/// whose names F# cannot call as functions. WxKit's string extraction finds a call with string
/// literals, such as <c>tr "&amp;Open"</c>. Format with String.Format, such as
/// <c>String.Format(tr "Hello, {0}!", name)</c>, since printf's formats are not gettext's.
/// </summary>
[<AutoOpen>]
module Translation =
    /// <summary>The translation of <paramref name="text"/> into the language in use, or the text itself.</summary>
    let tr (text: string) : string = Translations.Translate text

    /// <summary>The singular or plural form, for <paramref name="n"/> things, such as <c>trn "{0} file" "{0} files" count</c>.</summary>
    let trn (singular: string) (plural: string) (n: int64) : string = Translations.TranslatePlural(singular, plural, n)

    /// <summary>The translation of <paramref name="text"/> in a context, which tells apart two uses of the same words, such as <c>trp "verb" "Open"</c>.</summary>
    let trp (context: string) (text: string) : string = Translations.TranslateInContext(context, text)

    /// <summary>The singular or plural form, in a context.</summary>
    let trpn (context: string) (singular: string) (plural: string) (n: int64) : string = Translations.TranslatePluralInContext(context, singular, plural, n)
