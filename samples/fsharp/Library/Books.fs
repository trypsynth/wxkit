namespace Library

open System
open System.Globalization
open WxKit

/// A book in the library. Marking it read makes a changed copy.
type Book =
    { Title: string
      Author: string
      Year: int
      IsRead: bool }

module Book =
    /// Whether the title or the author has the text in it, whatever its case.
    let matches (text: string) book =
        let has (field: string) = field.Contains(text, StringComparison.CurrentCultureIgnoreCase)
        has book.Title || has book.Author

/// The order the list shows the books in.
type Order =
    | ByTitle
    | ByAuthor
    | ByYear

module Order =
    /// Every order, as the choice lists them.
    let all = [ ByTitle; ByAuthor; ByYear ]

    let name order =
        match order with
        | ByTitle -> "Title"
        | ByAuthor -> "Author"
        | ByYear -> "Year"

    let sort order (books: Book seq) =
        let byText (field: Book -> string) = Seq.sortWith (fun first second -> String.Compare(field first, field second, StringComparison.CurrentCulture))
        match order with
        | ByTitle -> books |> byText _.Title
        | ByAuthor -> books |> Seq.sortBy _.Year |> byText _.Author
        | ByYear -> books |> Seq.sortBy _.Year

/// The columns of the list, which are the model's column numbers.
module Column =
    let [<Literal>] Title = 0
    let [<Literal>] Author = 1
    let [<Literal>] Year = 2
    let [<Literal>] IsRead = 3

module BookModel =
    /// A model of the books the list shows, by row, which the data view asks for each cell of as it
    /// draws it, so it only reads what's on screen, however long the list. The Read column's check
    /// box is the one cell the user changes, which markRead is told of.
    let create (shown: unit -> Book[]) (markRead: int -> bool -> unit) =
        { new DataViewVirtualListModel(0) with
            override _.GetValue(row, column) =
                let book = shown () |> Array.item row
                match column with
                | Column.Title -> box book.Title
                | Column.Author -> box book.Author
                | Column.Year -> box (book.Year.ToString CultureInfo.CurrentCulture)
                | Column.IsRead -> box book.IsRead
                | _ -> null

            override _.SetValue(value, row, column) =
                match column, value with
                | Column.IsRead, (:? bool as read) ->
                    markRead row read
                    true
                | _ -> false }
