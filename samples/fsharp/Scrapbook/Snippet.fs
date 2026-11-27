/// Something kept in the scrapbook: text, or a list of files, as the clipboard and a drag carry them.
module Scrapbook.Snippet

open System
open System.IO
open WxKit

type Snippet =
    | Text of string
    | Files of string list

let private fileName (path: string) = Path.GetFileName path

/// What the list shows for a snippet: text's first line, cut short when it's long, or the files' names.
let describe snippet =
    match snippet with
    | Text text ->
        let line = text.Split('\n') |> Array.head |> _.Trim()
        if line.Length > 60 then line[..59] + "..." else line
    | Files [ file ] -> "File: " + fileName file
    | Files files -> $"{files.Length} files: " + String.Join(", ", files |> List.map fileName)

/// A snippet as the clipboard and a drag take it. Files come with their paths as text too, for an
/// application that only takes text.
let toContent snippet =
    match snippet with
    | Text text -> ClipboardContent(Text = text)
    | Files files -> ClipboardContent(Files = files, Text = String.Join(Environment.NewLine, files))

/// A snippet from dropped or pasted files or text, preferring files, as a file manager copies them.
let from (files: string array | null) (text: string | null) =
    match files, text with
    | NonNull files, _ when files.Length > 0 -> Some(Files(List.ofArray files))
    | _, NonNull text when text <> "" -> Some(Text text)
    | _ -> None

/// What's on the clipboard, if it's files or text.
let fromClipboard () =
    from (if Clipboard.HasFiles then Clipboard.GetFiles() else null) (if Clipboard.HasText then Clipboard.GetText() else null)
