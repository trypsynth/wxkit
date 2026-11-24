# Building and packaging

This document covers how WxKit builds wxWidgets and the shim, how Native AOT links them into an application, how the NuGet packages are made and released, and what CI runs.

## Versions and names

WxKit tracks the wxWidgets 3.3 series: the `native/wxWidgets` submodule is pinned to the `v3.3.4` tag, and WxKit moves to 3.4.0 when it comes out. WxKit's own version is independent of the wxWidgets version it bundles. Everything targets `net10.0`, since .NET 8 leaves support in November 2026.

WxKit follows semantic versioning, as it applies before 1.0:

- `0.1.0-preview.N` previews come first, and any of them can need code changes. They add what 0.1.0 should have, and make the changes to the API that it needs, such as naming, before anyone depends on it.
- 0.1.0 is the first release that isn't a preview. From it on, a patch release, such as 0.1.1, only fixes bugs and adds what needs no code changes, such as a new control or utility.
- A change that needs code changes goes in the next minor release, such as 0.2.0, with its own previews, and its notes say what to change. [Planned changes](planned-changes.md) collects them until then.
- `master` is heading for 0.2.0 now. A fix that can't wait for it goes on a `release/0.1` branch, made from the last 0.1 tag, and is released from there as 0.1.x.
- 1.0 promises that the API stays as it is, which [Open questions](open-questions.md#what-10-needs) says when.

0.1.0 came once its planned work was in, the API had a pass for names and types that aren't .NET's, an application beyond the samples, SIC!, ran on it, and screen readers had read the samples' basics on each platform: NVDA on Windows, VoiceOver on macOS, and Orca on Linux. The planned work was checks of what screen readers read in the tests, a `dotnet new` template, the dialogs and controls screen readers can read that were still unbound, a `WxKit.FSharp` package that makes F# read naturally, and dragging data other than files. A grid that screen readers can read comes in 0.2.0.

The packages are `WxKit`, the managed library and its targets, `WxKit.Native.<runtime>`, such as `WxKit.Native.win-x64`, for each runtime's native files, `WxKit.FSharp`, and `WxKit.Templates`. The GitHub repository is `trypsynth/wxkit`. The native library is `wxkitnative`, not `wxkit`, because Windows file names ignore case, and a native `wxkit.dll` would have the same name as the managed `WxKit.dll`.

## Native builds

`scripts/build-native.ps1` builds wxWidgets and the shim for each runtime in `-Runtimes`, which defaults to the machine's own platform: `win-x64`, `linux-x64`, or `osx-arm64`. A Windows runtime builds on Windows for any architecture, a Linux runtime on Linux for the machine's own architecture, and a macOS runtime on a Mac for either.

```powershell
./scripts/build-native.ps1 -Runtimes win-x64, win-arm64
```

Each runtime has one CMake build in `artifacts/native-build/<runtime>`, which takes 10 to 20 minutes cold on a desktop machine. The shim's `CMakeLists.txt` fixes the wxWidgets options, as [Native layer](native-layer.md) describes, including the built-in copies of zlib, libpng, and the other image libraries on every platform. The build's output goes to `artifacts/native/<runtime>`:

- `static`, for Native AOT: the shim and the wxWidgets libraries as static libraries, and a `WxKit.Native.targets` file that lists everything the final link needs, so platform details stay out of the package's own targets.
- `shared`, for everything else: `wxkitnative.dll`, `libwxkitnative.dylib`, or `libwxkitnative.so`, with wxWidgets linked in, exporting only the shim's `wxk_` functions. On Linux, it leaves the web view out, and `shared/webview` has one built with it, which an application that sets `WxKitWebView` gets in its place, so applications without a web view never need WebKitGTK.
- `licenses`: the licenses of what the build brings in itself, such as the WebView2 SDK's loader on Windows, which the native package carries.
- `locale`: wxWidgets' own translations, compiled with GNU gettext's `msgfmt`. Without `msgfmt` the script warns and leaves them out, but in CI it fails, since the `WxKit` package takes them from this build. See [Translating](../translating.md).

On Windows:

- Everything uses the static C runtime, `/MT`, because Native AOT links it. The script finds the newest Visual Studio with the C++ tools, and its CMake when none is on `PATH`. CI builds with Visual Studio 2022, since MSVC links libraries from an older toolset but not a newer one. `win-arm64` builds on x64 too, but CI tests it on ARM64.
- WxKit's targets set `ApplicationManifest` to `wxkit.manifest`, which declares version 6 of the common controls and per-monitor DPI awareness, unless the application has its own manifest.
- wxMSW's resources from `wx.rc` are compiled to `wxkit.res`. The linker takes one set of Win32 resources, so a static link sets `IlcGenerateWin32Resources` to false, and a build task merges the application's resources with WxKit's, the application's winning where both have one.
- The web view is Edge's WebView2, whose SDK wxWidgets downloads from nuget.org while configuring, checked against its SHA-256 hash. Its static loader is linked in, so nothing ships beside the application, and the link targets put its folder on the linker's path, since wxWidgets names it with a `#pragma`.
- In the Visual Studio 2026 Build Tools, `vcvarsall.bat` runs `vswhere.exe` by name, so a Native AOT link fails unless `%ProgramFiles(x86)%\Microsoft Visual Studio\Installer` is on `PATH`, as `scripts/test-aot.ps1` arranges.

On macOS:

- wxOSX is built for Cocoa with CMake, Ninja, and Xcode's clang, with macOS 12 as the deployment target. Building needs Xcode, CMake, Ninja, PowerShell 7, and the .NET 10 SDK. An Apple Silicon Mac builds both runtimes, and runs `osx-x64` under Rosetta.
- The link targets list the frameworks and libraries that `otool -L` reads from the shared library, each as one clang argument, such as `-Wl,-framework,AppKit`.
- Cocoa only runs a user interface on the main thread, so `Application.Run` throws on any other.

On Linux:

- wxGTK is built against GTK 3 with CMake and Ninja, and needs the development files of GTK 3 and WebKitGTK 4.1, such as Ubuntu's `libgtk-3-dev` and `libwebkit2gtk-4.1-dev`, and PowerShell 7. The link targets list WebKitGTK's libraries, and on macOS WebKit, only for an application that sets `WxKitWebView`. CI builds on Ubuntu 22.04, so the packages work with its glibc and anything newer.
- Features that need a system library beyond GTK's are left out: web requests, the secret store, sound, libnotify notifications, and spell checking. Everything is position independent, since Native AOT links position-independent executables, and the shared library hides wxWidgets's symbols with `--exclude-libs`.
- The link targets name the system libraries that `readelf` finds the shared library needs by soname, such as `-l:libgtk-3.so.0`, so an application's build machine needs GTK but not its development files. GNU ld reads static libraries once, in order, so each is listed before the ones it uses. A Native AOT publish also needs `clang` and zlib's development files.
- `wxAccessible` is Windows only, so `Accessible` does nothing on Linux, where GTK describes its controls through ATK.

## Patches to wxWidgets

WxKit fixes bugs in the wxWidgets it pins with the patches in `native/patches/wxWidgets`, until a wxWidgets release it moves to has them. Each is also a pull request to wxWidgets, named after it, as the folder's README lists. `scripts/patch-wxwidgets.ps1` applies them to the submodule, and `scripts/build-native.ps1`, the sanitizer script, and CI run it before building. It leaves a patch already applied as it is, and fails on one that wxWidgets already has, or that no longer applies, which is to be deleted. CI applies them before dating the sources, and its build caches' keys name the patches, so a cached build is reused only when it has the same ones.

## Native AOT and static linking

With `PublishAot` on, `WxKitStaticLink` defaults to `true`. The package's `buildTransitive/WxKit.targets` then adds `DirectPInvoke` for `wxkitnative`, imports the runtime's `WxKit.Native.targets`, and drops the shared library from the publish, so nothing ships beside the executable. The targets fail with a clear message when the runtime has no static build. With `WxKitStaticLink` set to `false`, or without Native AOT, the shared library loads from the native package's `runtimes` folder. The managed library is `IsAotCompatible` and treats warnings as errors, so the trimming and AOT analyzers fail its build. `SYSLIB1054` is an error in `.editorconfig`, so every native call uses `LibraryImport`.

## macOS app bundles

WxKit's targets make an app bundle, such as `Hello.app`, beside the published files when `WxKitAppBundle` is on. It is on by default when an executable is published for an `osx` runtime, with or without Native AOT. Set it to `false` to make none. The bundle is built with MSBuild, so a publish that is not Native AOT can make one on Windows or Linux, but only a Mac can sign it, and Apple Silicon only runs signed code.

`Contents/MacOS` holds everything published except symbols and documentation files. `Info.plist` takes the properties .NET MAUI uses: `ApplicationTitle`, which defaults to `Product` and then the assembly name, `ApplicationId`, the bundle identifier, which the publish warns about when it is not set, and `ApplicationDisplayVersion` and `ApplicationVersion`, which default to `Version` without its prerelease label. `WxKitMacIcon` names an `.icns` icon, and `WxKitInfoPlist` an `Info.plist` to use instead of WxKit's.

On a Mac, the publish signs the bundle innermost first: each other file in `Contents/MacOS`, then the executable, then the bundle. Without `WxKitCodesignIdentity` it signs ad hoc, which runs only on the Mac that built it. With one, such as `-p:WxKitCodesignIdentity="Developer ID Application: Jane Doe (TEAMID)"`, it signs with the hardened runtime and a secure timestamp, which notarization needs, and adds the JIT entitlement when the application is not Native AOT, and the library validation entitlement when it is framework dependent. `codesign` cannot unlock the keychain over SSH and fails there with `errSecInternalComponent`, so sign from the Mac's own session, or in CI from a temporary keychain. To notarize a signed bundle and staple the ticket:

```sh
ditto -c -k --keepParent Hello.app Hello.zip
xcrun notarytool submit Hello.zip --keychain-profile notary --wait
xcrun stapler staple Hello.app
```

For an installer, a disk image, or updates, [Velopack](https://docs.velopack.io/packaging/operating-systems/macos) takes the bundle as its `--packDir`.

## Packages

On `win-x64` the static libraries come to about 220 megabytes, and the package to about 50, under nuget.org's 250 megabyte limit, so each runtime has a package of its own. An application that links them is much smaller: the Hello sample publishes as an executable of about 5 megabytes.

- `WxKit` holds the managed library, its targets in `buildTransitive`, and one copy of wxWidgets' compiled catalogs in `locale`. It depends on every native package, so an application references only `WxKit`.
- `WxKit.FSharp` holds the F# layer, and depends on `WxKit` of the same version, with all of its assets, so its build targets reach an F# application through it.
- `WxKit.Templates` holds the `dotnet new wxkit` templates, in C# and F#, in `content/templates`. It has the same version as `WxKit`, which its projects reference: packing writes the version into them, in place of `WXKIT_VERSION`. A template's project builds for the machine's own runtime, so `dotnet publish` makes a native executable without `-r`.
- `WxKit.Native.<runtime>` holds the shared library in `runtimes/<runtime>/native`, and the static libraries, link targets, and resources in `native/<runtime>`. Its targets set `WxKitNativeDirectory` to that folder when the application's runtime matches. Both its targets and WxKit's import the link targets, since NuGet does not say which comes first, and the second finds them imported already.
- A project that builds WxKit from source sets `WxKitNativeDirectory` itself. The samples do that when a native build exists, and otherwise use the newest `WxKit` package from nuget.org.

`scripts/pack.ps1 -Runtimes <runtimes> -Version <version>` packs `src/WxKit.Native` once per runtime, then `WxKit`, `WxKit.FSharp`, and `WxKit.Templates`, into `artifacts/packages`. `-Version` defaults to the one in `Directory.Build.props`. `WxKit` depends on the native packages only while it is packed, so building it needs none. `scripts/test-package.ps1 -Runtime <runtime>` installs the templates into a hive of its own, makes a project with them outside the repository's build, which restores `WxKit` from `artifacts/packages`, and builds it. It then gives the project the Hello sample's code, which can close itself, runs it with the shared library, then publishes it with Native AOT, checks that nothing native ships beside the executable, and runs it. [Testing](testing.md) covers the other tests.

## Releasing

To release, write the notes in `docs/releases/<version>.md`, then push a tag that names the version, such as `git tag v0.1.0-preview.6` followed by `git push origin v0.1.0-preview.6`. CI packs every package with the tag's version and tests them. The publish job then pushes them to nuget.org, native packages first, then `WxKit` and `WxKit.FSharp`, and the templates last, and creates a GitHub release with the `.nupkg` files attached, whose notes are `docs/releases/<version>.md` followed by notes generated from the commits. A version with a suffix, such as `-preview.6`, is a prerelease in both places. A version nuget.org has already is skipped, so a failed run can run again. nuget.org can unlist a version but not delete it, so each number is used once. The packages carry Source Link, and the managed library's symbols go to nuget.org's symbol server. CI builds are deterministic, which Source Link needs.

The publish job signs in with Trusted Publishing, through the `NuGet/login` action, so no key is kept in the repository. Setting it up takes three steps, once:

1. On nuget.org, under Trusted Publishing, add a policy for the owner `trypsynth`, the repository `wxkit`, the workflow file `ci.yml`, and the environment `nuget`.
2. On GitHub, add an environment called `nuget`. A required reviewer there makes each release wait for approval, and limiting it to tags that start with `v` keeps other refs from using it.
3. Add the Actions variable `NUGET_USER`, set to the nuget.org user name the policy belongs to.

## Licensing

WxKit is MIT licensed. wxWidgets uses the wxWindows Library Licence, the LGPL with an exception that allows distributing applications linked with it, statically or not, under their own terms, which is what makes a single executable possible. The native packages also contain the libraries wxWidgets builds in: zlib, libpng, libjpeg, libtiff, PCRE2, libwebp, and NanoSVG, which `THIRD-PARTY-NOTICES.md` lists, and the styled text control's Scintilla and Lexilla, which it does not list yet. The notices are the native packages' license file, and each license text is in their `licenses` folder.

## Continuous integration

`.github/workflows/ci.yml` runs on pushes to `master`, on pull requests, and on `v*` tags. A newer run for a pull request cancels an older one, and runs on `master` wait for each other. Its jobs are:

- `build`, once per runtime on its own system and architecture: `win-x64` on `windows-2022`, `win-arm64` on `windows-11-arm`, `linux-x64` on `ubuntu-22.04`, `linux-arm64` on `ubuntu-22.04-arm`, `osx-arm64` on `macos-15`, and `osx-x64` on `macos-15-intel`. It tests the build tasks, builds and caches the native build, runs the tests, under `xvfb-run` on Linux, runs `scripts/test-aot.ps1`, and uploads the native build. On `win-x64` it also tests the generator, checks that the generated bindings are current, and checks formatting.
- `pack` gathers every native build and packs the packages, which it keeps as a run artifact for a week.
- `test-packages` runs `scripts/test-package.ps1` on each runtime's machine. On Linux it installs GTK and Native AOT's tools, but not GTK's development files.
- `publish`, only on a tag, releases as described above.
