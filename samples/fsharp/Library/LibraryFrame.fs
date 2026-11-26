namespace Library

open WxKit
open WxKit.FSharp

/// The library's window: a search box, a choice of order, the list of books, and buttons that add a
/// book, remove one, and mark one read. The list shows the books that match the search, in the
/// order chosen, which the model reads by row.
type LibraryFrame() as this =
    inherit Frame(null, "Library", size = Size(680, 480))

    let book title author year isRead =
        { Title = title
          Author = author
          Year = year
          IsRead = isRead }

    let mutable books =
        [ book "Pride and Prejudice" "Jane Austen" 1813 true
          book "Frankenstein" "Mary Shelley" 1818 false
          book "Moby-Dick" "Herman Melville" 1851 false
          book "Middlemarch" "George Eliot" 1871 true
          book "The Adventures of Huckleberry Finn" "Mark Twain" 1884 false
          book "Dracula" "Bram Stoker" 1897 false
          book "The Time Machine" "H. G. Wells" 1895 true
          book "Emma" "Jane Austen" 1815 false ]

    let mutable shown: Book[] = [||]
    let panel = new Panel(this)
    // Each label is made just before the control it names, so a screen reader names the control
    // after it, and the tab order follows.
    let searchLabel = new StaticText(panel, "&Search:")
    let search = new SearchCtrl(panel, DescriptiveText = "Title or author")
    let orderLabel = new StaticText(panel, "&Order by:")
    let order = new Choice(panel, List.map Order.name Order.all, Selection = 0)
    let listLabel = new StaticText(panel, "&Books:")
    let list = new DataViewCtrl(panel, style = DataViewCtrlStyle.RowLines)
    let add = new Button(panel, "&Add Book...")
    let remove = new Button(panel, "&Remove")
    let markRead = new Button(panel, "&Mark Read or Unread")

    let showCounts () =
        let read = shown |> Array.filter _.IsRead |> Array.length
        this.SetStatusText $"{shown.Length} of {books.Length} books shown, {read} of them read"

    // Puts a changed copy of the book in a row in place of the book, in the list and the library.
    let replace row changed =
        let old = shown[row]
        books <- books |> List.map (fun book -> if LanguagePrimitives.PhysicalEquality book old then changed else book)
        shown[row] <- changed
        showCounts ()

    let model = BookModel.create (fun () -> shown) (fun row read -> replace row { shown[row] with IsRead = read })

    // The row of the selected book, if one is.
    let selectedRow () =
        if list.HasSelection then Some(model.GetRow list.Selection) else None

    // Shows the books that match the search, in the order chosen, and says how many there are.
    let showBooks () =
        let text = search.Value.Trim()
        shown <-
            books
            |> Seq.filter (fun book -> text = "" || Book.matches text book)
            |> Order.sort Order.all[max order.Selection 0]
            |> Array.ofSeq
        model.Reset shown.Length
        showCounts ()

    let addBook () =
        match AddBook.ask this with
        | None -> ()
        | Some added ->
            books <- books @ [ added ]
            // The search is cleared, so the new book is shown, and selected, so a screen reader reads it.
            search.ChangeValue ""
            showBooks ()
            let item = model.GetItem(shown |> Array.findIndex (LanguagePrimitives.PhysicalEquality added))
            list.Select item
            list.EnsureVisible item
            list.SetFocus()

    let removeSelected () =
        selectedRow ()
        |> Option.iter (fun row ->
            let old = shown[row]
            books <- books |> List.filter (fun book -> not (LanguagePrimitives.PhysicalEquality book old))
            shown <- shown |> Array.removeAt row
            model.NotifyRowDeleted row
            showCounts ())

    let toggleSelected () =
        selectedRow ()
        |> Option.iter (fun row ->
            replace row { shown[row] with IsRead = not shown[row].IsRead }
            model.NotifyRowChanged row)

    do
        list.Model <- model
        list.AppendTextColumn("Title", Column.Title, width = 260) |> ignore
        list.AppendTextColumn("Author", Column.Author, width = 160) |> ignore
        list.AppendTextColumn("Year", Column.Year, width = 60) |> ignore
        list.AppendToggleColumn("Read", Column.IsRead, DataViewCellMode.Activatable, width = 60) |> ignore
        search.TextChanged.Add(fun _ -> showBooks ())
        order.SelectionChanged.Add(fun _ -> showBooks ())
        add.Click.Add(fun _ -> addBook ())
        remove.Click.Add(fun _ -> removeSelected ())
        markRead.Click.Add(fun _ -> toggleSelected ())
        // The buttons that act on a book only apply while one is selected.
        this.Bind(
            EventType.UpdateUi,
            fun _ (e: UpdateUIEvent) ->
                if e.Id = remove.Id || e.Id = markRead.Id then
                    e.IsEnabled <- selectedRow () |> Option.isSome
        )
        panel.Sizer <-
            column {
                row {
                    item searchLabel |> center
                    item search |> grow |> center
                    item orderLabel |> center
                    item order |> center
                }
                |> fill
                item listLabel |> border (Direction.Left ||| Direction.Right ||| Direction.Top)
                item list |> grow
                row {
                    add
                    remove
                    markRead
                }
            }
        this.CreateStatusBar() |> ignore
        showBooks ()
        this.Center()

    /// For the smoke test: searches, selects the first book shown, marks it read, and removes it.
    member _.Exercise() =
        search.Value <- "Austen"
        if shown.Length <> 2 then
            failwith $"The search showed {shown.Length} books, not 2."
        list.Select(model.GetItem 0)
        toggleSelected ()
        removeSelected ()
        if books.Length <> 7 then
            failwith $"The library has {books.Length} books, not 7."
