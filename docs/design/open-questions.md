# Open questions

These are the design questions still open. Each has a recommendation, which the other documents assume until it is answered. Decisions already made are in the document each belongs to.

## Order of the optional libraries

Beyond the core, wxWidgets has AUI docking, the property grid, the web view, rich text, the grid, HTML, the ribbon, OpenGL, the media control, and XRC. The styled text control is bound, and an application that doesn't use it doesn't link it.

Recommendation: the web view next, whose content browser engines make accessible to screen readers, for help viewers and rich text. AUI and the property grid draw their own controls, which screen readers can't read yet, so they wait until they can, or until WxKit describes them itself, as it can a control that draws itself. XRC comes last.

## Native libraries an application can't link

Native AOT links wxWidgets into the executable, but not a native library another package brings, such as ImageMagick's for Magick.NET, which ships beside it. An application can embed such a library and unpack it on first use, as SIC! does, but each would write that code itself.

Recommendation: a `WxKitBundle` item that embeds a native library or a folder of files when publishing, and a small runtime helper that unpacks and loads them, once a second application needs it.

## An F# helper package

F# uses the C# API directly, as the HelloFSharp sample shows. A `WxKit.FSharp` package could make it read more naturally, such as with a computation expression for sizer layouts.

Recommendation: decide once the C# API is stable, from what the sample shows is awkward.

## Disk images and notarization

A macOS publish makes an app bundle, but not a disk image, and doesn't notarize it. The docs give the commands, and Velopack does both.

Recommendation: leave both to the docs and Velopack, unless applications ask for them in the publish.

## What 1.0 needs

Recommendation: 1.0 once a release goes by with no changes that need code changes, the remaining optional libraries an application commonly needs are bound, and an application beyond the samples, such as SIC!, has shipped on WxKit.
