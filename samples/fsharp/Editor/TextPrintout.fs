namespace Editor

open System
open System.Collections.Generic
open WxKit

/// The editor's text, wrapped to the page in the editor's font, half an inch from each edge. It
/// draws in screen units, which MapScreenSizeToPage scales to the page, so it can measure the lines
/// with a screen DC before the pages are counted. The font is its description, which the printout
/// makes a font from each time it needs one, since it prints after the editor's font is gone.
type TextPrintout(title: string, text: string, fontDescription: string option) =
    inherit Printout(title)
    let lines = List<string>()
    let mutable lineHeight = 1
    let mutable linesPerPage = 1
    let mutable margin = 0
    let mutable hasPrinted = false

    let makeFont () =
        fontDescription |> Option.map (fun description -> new Font(description)) |> Option.toObj

    // Breaks a line at spaces so each part fits the width, or within a word too long to fit.
    let rec wrap (line: string) width (measure: DC) =
        let fits (part: string) = measure.GetTextExtent(part).Width <= width
        if line = "" || fits line then
            lines.Add line
        else
            let mutable fit = 1
            while fit < line.Length && fits line[.. fit] do
                fit <- fit + 1
            let cut =
                match line.LastIndexOf(' ', fit - 1, fit) with
                | space when space > 0 -> space
                | _ -> fit
            lines.Add line[.. cut - 1]
            wrap (line[cut..].TrimStart ' ') width measure

    /// Whether a page has been printed or previewed, which the smoke test waits for.
    member _.HasPrinted = hasPrinted

    override this.OnPreparePrinting() =
        let screen = this.PpiScreen
        let printer = this.PpiPrinter
        let page = this.PageSizePixels
        // The page's size in screen pixels, which the page is drawn in.
        let width = int (int64 page.Width * int64 screen.Width / int64 (max printer.Width 1))
        let height = int (int64 page.Height * int64 screen.Height / int64 (max printer.Height 1))
        margin <- screen.Height / 2
        use measure = new ScreenDC()
        use font = makeFont ()
        if not (isNull font) then
            measure.Font <- font
        lineHeight <- max measure.CharHeight 1
        linesPerPage <- max ((height - 2 * margin) / lineHeight) 1
        lines.Clear()
        for line in text.Split '\n' do
            wrap (line.Replace("\t", "    ")) (max (width - 2 * margin) 1) measure

    override _.GetPageInfo() =
        PageRange.All(max ((lines.Count + linesPerPage - 1) / linesPerPage) 1)

    override this.OnPrintPage(page, dc) =
        this.MapScreenSizeToPage()
        use font = makeFont ()
        if not (isNull font) then
            dc.Font <- font
        dc.TextForeground <- Color(0uy, 0uy, 0uy)
        lines
        |> Seq.skip ((page - 1) * linesPerPage)
        |> Seq.truncate linesPerPage
        |> Seq.iteri (fun index line -> dc.DrawText(line, Point(margin, margin + index * lineHeight)))
        hasPrinted <- true
        true
