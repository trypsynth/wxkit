/// Finding text the way the find dialog asks: down or up, matching case or not, and whole words
/// or not. These only look at strings, so they don't need the editor.
module Editor.Search

open System
open System.Text
open WxKit

let private has flag (flags: FindReplaceFlags) = flags &&& flag = flag

let comparison flags =
    if has FindReplaceFlags.MatchCase flags then StringComparison.CurrentCulture else StringComparison.CurrentCultureIgnoreCase

let private isWordCharacter character = Char.IsLetterOrDigit character || character = '_'

let private isWholeWord (text: string) start length =
    let before = start = 0 || not (isWordCharacter text[start - 1])
    let after = start + length = text.Length || not (isWordCharacter text[start + length])
    before && after

/// The first match at or after start, or, going up, the last one that ends at or before it, which
/// is a whole word when the flags say so.
let rec find (text: string) (needle: string) start flags =
    let down = has FindReplaceFlags.Down flags
    let found =
        if down then
            if start > text.Length then -1 else text.IndexOf(needle, max start 0, comparison flags)
        elif start < 0 || text.Length = 0 then
            -1
        else
            text.LastIndexOf(needle, min (start + needle.Length - 1) (text.Length - 1), comparison flags)
    match found with
    | -1 -> None
    | found when not (has FindReplaceFlags.WholeWord flags) || isWholeWord text found needle.Length -> Some found
    | found -> find text needle (if down then found + 1 else found - 1) flags

/// The text with every match replaced, and how many there were.
let replaceAll (text: string) (needle: string) (replacement: string) flags =
    let downward = flags ||| FindReplaceFlags.Down
    let result = StringBuilder()
    let rec replace start count =
        match find text needle start downward with
        | Some found ->
            result.Append(text, start, found - start).Append(replacement) |> ignore
            replace (found + needle.Length) (count + 1)
        | None ->
            result.Append(text, start, text.Length - start) |> ignore
            count
    let count = replace 0 0
    result.ToString(), count
