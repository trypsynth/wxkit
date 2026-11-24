// The Browser sample in F#: an address, Back, Forward, and Reload, and a web view, whose pages
// screen readers read as they do in any browser. It starts on a page of its own, so it runs offline.
open System
open WxKit
open WxKit.FSharp

let name = "WxKit browser"

let startPage =
    """<!doctype html>
<html lang="en">
<head><title>WxKit browser</title></head>
<body>
<h1>A WxKit web view</h1>
<p>This page is shown by the system's browser engine: Edge on Windows, WebKit on macOS, and WebKitGTK on Linux.</p>
<p>Type an address above and press Enter, or follow <a href="https://www.wxwidgets.org/">the wxWidgets site</a>.</p>
</body>
</html>"""

/// The address typed, if any, with https:// when it names no scheme.
let addressFrom (typed: string) =
    match typed.Trim() with
    | "" -> None
    | text when text.Contains("://", StringComparison.Ordinal) -> Some text
    | text -> Some("https://" + text)

/// What the address box shows for a page. The start page has no address of its own.
let shownAddress (url: string) =
    if url.StartsWith("data:", StringComparison.Ordinal) || url = "about:blank" then "" else url

[<EntryPoint>]
let main args =
    // With --smoke, the frame closes as soon as the event loop is running, so CI can check that a
    // published executable starts and ends cleanly.
    let smoke = args = [| "--smoke" |]
    Application.Run(fun () ->
        let frame = new Frame(null, name, size = Size(900, 700))
        let panel = new Panel(frame)
        // Each label is created just before the control it names, so a screen reader names the
        // control after it, and the tab order follows.
        let addressLabel = new StaticText(panel, "&Address:")
        let address = new TextCtrl(panel, style = TextCtrlStyle.ProcessEnter)
        let back = new Button(panel, "&Back", IsEnabled = false)
        let forward = new Button(panel, "&Forward", IsEnabled = false)
        let reload = new Button(panel, "&Reload")
        let view = new WebView(panel)
        address.TextEnter.Add(fun _ ->
            addressFrom address.Value
            |> Option.iter (fun url ->
                view.Navigate url
                view.SetFocus()))
        back.Click.Add(fun _ -> view.GoBack())
        forward.Click.Add(fun _ -> view.GoForward())
        reload.Click.Add(fun _ -> view.Reload())
        // The address and the buttons follow the page, and the window is named after it.
        view.Navigated.Add(fun e ->
            address.Value <- shownAddress e.Url
            back.IsEnabled <- view.CanGoBack
            forward.IsEnabled <- view.CanGoForward)
        view.TitleChanged.Add(fun _ ->
            frame.Title <- if view.Title = "" then name else $"{view.Title} - {name}")
        view.NavigationFailed.Add(fun e -> frame.Title <- $"Could not open {e.Url} - {name}")
        panel.Sizer <-
            column {
                section (
                    row {
                        item addressLabel |> center
                        item address |> grow |> center
                        back
                        forward
                        reload
                    }
                )
                |> fill
                item view |> grow |> noBorder
            }
        view.NavigateToString startPage
        frame.Center()
        frame.Show() |> ignore
        view.SetFocus()
        if smoke then Application.Post(fun () -> frame.Close(force = true) |> ignore))
