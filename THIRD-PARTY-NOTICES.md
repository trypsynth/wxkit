# Third-party notices

WxKit itself, including its native shim, is licensed under the MIT license, in `LICENSE`.

The native packages, such as `WxKit.Native.win-x64`, also contain wxWidgets and the libraries it builds in. An application that uses WxKit links them, statically when it publishes with Native AOT. Each one is listed here with its license, and the packages carry each license text in their `licenses` folder.

## wxWidgets

- Version: 3.3.4.
- Website: https://www.wxwidgets.org
- License: the wxWindows Library Licence, version 3.1, in `licenses/wxWidgets/licence.txt`. It is the GNU Library General Public License, version 2 or later, in `licenses/wxWidgets/lgpl.txt`, with an exception that allows distributing applications linked with wxWidgets, statically or not, under their own terms.

## zlib

- Version: 1.3.2.
- Website: https://zlib.net
- License: the zlib license, in `licenses/zlib/LICENSE`.

## libpng

- Version: 1.6.59.
- Website: http://www.libpng.org/pub/png/libpng.html
- License: the PNG Reference Library License, version 2, in `licenses/libpng/LICENSE`.

## libjpeg

- Version: release 10, of the Independent JPEG Group's software.
- Website: https://www.ijg.org
- License: the Independent JPEG Group's license, under "Legal issues" in `licenses/libjpeg/README`. This software is based in part on the work of the Independent JPEG Group.

## libtiff

- Version: 4.7.2.
- Website: https://libtiff.gitlab.io/libtiff/
- License: the libtiff license, in `licenses/libtiff/LICENSE.md`.

## PCRE2

- Version: 10.49.
- Website: https://github.com/PCRE2Project/pcre2
- License: the BSD 3-clause license, with the PCRE2 exception for binary distributions, in `licenses/pcre2/LICENCE.md`.

## libwebp

- Version: 1.6.0.
- Website: https://chromium.googlesource.com/webm/libwebp
- License: the BSD 3-clause license, in `licenses/libwebp/COPYING`.

## NanoSVG

- Website: https://github.com/memononen/nanosvg
- License: the zlib license, in `licenses/nanosvg/LICENSE.txt`.

## WebView2 loader

- Version: the WebView2 SDK 1.0.3485.44, in the Windows packages only, whose static loader the web view links. An application links it only when it uses `WebView`.
- Website: https://learn.microsoft.com/microsoft-edge/webview2/
- License: Microsoft's BSD 3-clause license, in `licenses/webview2/LICENSE.txt`, with the third-party notices in `licenses/webview2/NOTICE.txt`.

## Scintilla and Lexilla

- Versions: Scintilla 5.0.0 and Lexilla 5.5.4, as wxWidgets' styled text control bundles them. An application links them only when it uses `StyledTextCtrl`.
- Website: https://www.scintilla.org
- License: Neil Hodgson's permission notice, in `licenses/scintilla/License.txt` and `licenses/lexilla/License.txt`.
