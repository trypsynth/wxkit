/// Asking for a new book's title, author, and year.
module Library.AddBook

open System
open WxKit
open WxKit.FSharp

// Keeps the dialog open, and says what's missing, while a field is empty.
let private required what =
    { new CustomValidator() with
        override _.OnValidate(window, parent) =
            match window with
            | :? TextCtrl as text when text.Value.Trim() <> "" -> true
            | _ ->
                let icon = MessageDialogStyle.Ok ||| MessageDialogStyle.IconWarning
                MessageBox.Show($"Enter {what} for the book.", "Add Book", icon, parent) |> ignore
                window.SetFocus()
                false }

/// Shows the dialog, and gives the book once it's closed with OK. The dialog stays open until the
/// title and the author are filled in.
let ask (parent: Window) =
    use dialog = new Dialog(parent, "Add Book")
    // Each label is made just before its field, so a screen reader names the field after it.
    let titleLabel = new StaticText(dialog, "&Title:")
    let title = new TextCtrl(dialog, size = Size(260, -1), Validator = required "a title")
    let authorLabel = new StaticText(dialog, "&Author:")
    let author = new TextCtrl(dialog, Validator = required "an author")
    let yearLabel = new StaticText(dialog, "&Year:")
    let year = new SpinCtrl(dialog, min = 1450, max = DateTime.Now.Year, initial = DateTime.Now.Year)
    // OK and Cancel, in the order and with the labels the platform uses.
    let buttons = dialog.CreateStdDialogButtonSizer(DialogButtons.Ok ||| DialogButtons.Cancel)
    dialog.Sizer <-
        column {
            form {
                field titleLabel title
                field authorLabel author
                field yearLabel year
            }
            section buttons |> fill
        }
    dialog.Fit()
    title.SetFocus()
    if dialog.ShowModal() = StandardId.Ok then
        Some
            { Title = title.Value.Trim()
              Author = author.Value.Trim()
              Year = year.Value
              IsRead = false }
    else
        None
