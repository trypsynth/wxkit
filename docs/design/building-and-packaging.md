# Building and packaging

This document describes how WxKit builds wxWidgets and its shim, how Native AOT links them into an application, and how they ship on NuGet.

## Native builds

`scripts/build-native.ps1` builds wxWidgets and the shim for each runtime it is given, and defaults to `win-x64`:

```powershell
./scripts/build-native.ps1 -Runtimes win-x64, win-arm64
```

For each runtime it makes two builds:

- A static build, for Native AOT. wxWidgets and the shim are built as static libraries, and the script writes a `WxKit.Native.targets` file beside them that lists everything the final link needs. This keeps platform details out of the package's own targets file.
- A shared build, for everything else. wxWidgets is linked statically into one shared library, `wxkitnative.dll`, `libwxkitnative.dylib`, or `libwxkitnative.so`, which exports only the shim's functions. An application that does not use Native AOT loads this library from the package's `runtimes` folder.

Output goes to `artifacts/native/<runtime>/static` and `artifacts/native/<runtime>/shared`.

### Windows

- wxWidgets and the shim are built with the static C runtime, `/MT`, because that is what Native AOT links. The script finds the newest Visual Studio with the C++ tools and names its CMake generator explicitly. Without a CMake on `PATH`, it uses the one that Visual Studio installs, which is only on `PATH` in a developer shell.
- `win-arm64` builds on an x64 machine as well as an ARM64 one, with Visual Studio's ARM64 build tools, and Native AOT publishes for it from either. Only an ARM64 machine can run what it makes, so CI tests it on one.
- The static link lists the Windows libraries that wxMSW uses, such as `comctl32`, `uxtheme`, `oleacc`, `gdiplus`, `shlwapi`, and `version`.
- wxMSW needs version 6 of the common controls for themed controls, and wants per-monitor DPI awareness. Both come from the application manifest. The package's targets set `ApplicationManifest` to WxKit's own manifest, which declares both, unless the application sets its own. The compiler embeds it in the managed assembly, as it would any manifest, and Native AOT copies it into the executable from there.
- wxMSW's resources, such as its cursors, its default icons, and the menu that MDI frames use, live in `wx.rc`. With a static link, the application has to contain them. The script compiles them, without a manifest, to `wxkit.res`. The linker only accepts one set of Win32 resources, though, and Native AOT already gives it the application's own, which hold its icon, version, and manifest. So the package's targets turn off Native AOT's copy with `IlcGenerateWin32Resources`, and a small build task reads the application's resources out of its managed assembly and writes them, with WxKit's, into one `.res` file for the linker. The application's resources win wherever both have one with the same type, name, and language.
- Native AOT finds the linker through `vcvarsall.bat`. In the Visual Studio 2026 Build Tools, that script runs `vswhere.exe` by name, so if the Visual Studio Installer folder is not on `PATH`, its error message ends up inside the linker's path and the link fails. Until that is fixed upstream, add `%ProgramFiles(x86)%\Microsoft Visual Studio\Installer` to `PATH`.

### macOS

- wxOSX is built for Cocoa with CMake and Ninja, which Homebrew has, and Xcode's clang, with macOS 12 as the deployment target, so the libraries never ask for a newer macOS than a .NET application does. Apple Silicon builds both `osx-arm64` and `osx-x64`, and Rosetta runs the latter, so one Mac tests both.
- The shared library exports only the shim's functions, through `-exported_symbol`.
- The link targets list the frameworks and libraries the shared library loads, which `otool -L` reads from it, such as `AppKit`, `Carbon`, `IOKit`, `QuartzCore`, and `libc++`, each as one argument for clang, such as `-Wl,-framework,AppKit`. .NET links `libobjc` itself.
- Cocoa only runs a user interface on the process's main thread, so `Application.Run` throws on any other.
- A GUI application on macOS behaves best inside an app bundle, as the next section describes.
- Building needs Xcode, CMake, Ninja, PowerShell 7, and the .NET 10 SDK.

### Linux

- wxGTK is built against GTK 3, with CMake and Ninja, on a machine of the runtime's own architecture. wxWidgets is linked statically, but GTK and its dependencies come from the system, because they are part of the desktop. Building needs GTK 3's development files, such as Ubuntu's `libgtk-3-dev`, and PowerShell 7 for the script.
- wxWidgets would use the system's zlib, libpng, libjpeg, and libtiff where it finds their headers, which GTK's development files bring in. The shim's CMake forces wxWidgets's own copies, as on Windows, so an application does not need them, and leaves out the features that need a system library beyond GTK's: web requests, the secret store, sound, notifications, and spell checking.
- Everything is built position independent, since Native AOT links position-independent executables. The shared library exports the shim's functions and hides wxWidgets's, with `--exclude-libs`, as Windows only exports what is marked for it.
- The system libraries an application links are the ones the shared library needs, which `readelf` reads from it, named by their sonames, such as `-l:libgtk-3.so.0`. So an application's build machine needs GTK, but not its development files.
- GNU ld reads each static library once, in order, so the link targets list them with each before the ones it uses: the shim, wxWidgets's core, its base, and then the libraries it builds in.
- The packages are built on the oldest supported Ubuntu, 22.04, so they work with its glibc and anything newer.
- Native AOT links with clang, so a Linux publish needs `clang` and zlib's development files, as any Native AOT publish on Linux does, and the GTK 3 libraries.
- `wxAccessible` is Microsoft Active Accessibility's, so wxGTK does not have it. An `Accessible` does nothing there, and GTK's own accessibility, through ATK, describes its controls.

## macOS app bundles

A GUI application on macOS runs from an app bundle, a folder such as `Hello.app` whose `Contents/Info.plist` names it. Without one, its menu bar and focus can misbehave, and Finder cannot open it as an application. Publishing an application for a macOS runtime makes one beside the published files, with Native AOT or without:

```powershell
dotnet publish -r osx-arm64
```

WxKit's targets build it with MSBuild, so a publish on Windows or Linux can make one too, for an application that is not Native AOT, which only publishes for macOS on a Mac. Only a Mac can sign it, though, and Apple Silicon only runs signed code, so such a bundle has to be signed on a Mac before it runs there. The bundle's `Contents/MacOS` holds everything published except symbols and documentation files, so the executable finds beside it what it would without the bundle. Its `Info.plist` takes the properties .NET MAUI uses for the same:

- `ApplicationTitle` names the bundle and the application in Finder and the menu bar. It defaults to `Product`, which defaults to the assembly's name.
- `ApplicationId` is the bundle identifier, in reverse DNS, such as `com.example.hello`. The publish warns when it is not set.
- `ApplicationDisplayVersion` and `ApplicationVersion` are the version the user sees and the build's version. Both default to `Version` without its prerelease label, since a bundle's versions are only numbers.

`WxKitMacIcon` names an `.icns` file to give it an icon, `WxKitInfoPlist` an `Info.plist` to use instead of WxKit's, and `WxKitAppBundle` set to `false` turns the bundle off. WxKit's `Info.plist` also names wxWidgets's own application class, `wxNSApplication`, as wxWidgets's bundles do, and macOS 12 as the oldest the application runs on.

### Signing and notarizing

On a Mac, the publish signs the bundle, innermost first, as Apple recommends over deep signing: each file in `Contents/MacOS` other than the executable, then the executable, then the bundle. Without `WxKitCodesignIdentity`, it signs ad hoc, which runs on the Mac that built it. With it, such as `-p:WxKitCodesignIdentity="Developer ID Application: Jane Doe (TEAMID)"`, it signs with the hardened runtime and a secure timestamp, which notarization needs. An application that is not Native AOT also gets the entitlement to compile code at run time, which .NET's JIT needs under the hardened runtime. One that is framework dependent also gets the entitlement to load libraries another team signed, since the hardened runtime otherwise refuses .NET's own, which it loads from the shared install and Microsoft signed. A self-contained application carries .NET in its bundle, signed with the application, so it does not need that.

`codesign` signs with a key in the keychain, which an SSH session cannot unlock without the keychain's password, so it fails there with `errSecInternalComponent`. Sign from the Mac's own session, or, in CI, from a temporary keychain that holds the certificate.

Apple only lets a downloaded application open once it is notarized. After a signed publish, notarize the bundle with Apple's notary service, here with a profile that `xcrun notarytool store-credentials` saved, and staple the ticket to it, so Gatekeeper can check it offline:

```sh
ditto -c -k --keepParent Hello.app Hello.zip
xcrun notarytool submit Hello.zip --keychain-profile notary --wait
xcrun stapler staple Hello.app
```

For an installer, a disk image, or updates, [Velopack](https://docs.velopack.io/packaging/operating-systems/macos) packs a published application, bundles and signs it, notarizes it, and makes an installer, on macOS as on Windows and Linux. Its `--packDir` takes a folder ending in `.app`, such as WxKit's bundle, as it is.

## Native AOT and static linking

The package's `buildTransitive/WxKit.targets` works like this:

- With `PublishAot` on, `WxKitStaticLink` defaults to `true`. The targets add `DirectPInvoke` for `wxkitnative` and import the runtime's `WxKit.Native.targets`, so the shim's functions are linked into the executable and called directly, without loading anything at run time.
- With `WxKitStaticLink` set to `false`, the shared library ships beside the executable instead.
- Without Native AOT, the shared library loads from the package's `runtimes` folder.
- The targets fail with a clear message when there is no static build for the runtime being published.

The managed library is marked `IsAotCompatible`, so the trimming and AOT analyzers run on every build. The library uses no reflection, no runtime code generation, and no marshalled delegates. The `SYSLIB1054` diagnostic is an error, so every native call goes through `LibraryImport`.

CI publishes the C# sample with Native AOT on each platform and runs it in a smoke test mode, which shows a frame, waits for the event loop to start, and exits. This catches link errors that ordinary builds cannot.

## Packages

wxWidgets is large. On `win-x64`, the static libraries for the base and core libraries and the shim come to about 175 megabytes, and nuget.org limits a package to 250 megabytes. They compress to about 37 megabytes. An application that links them is much smaller: the Hello sample publishes as a single executable of about 5.2 megabytes.

Every runtime's static libraries in one package would not fit, so there is a package per runtime:

- `WxKit`, the managed library and its targets, with no native files.
- A native package per runtime, such as `WxKit.Native.win-x64`, which holds that runtime's shared library in `runtimes/<runtime>/native`, where .NET finds it, and its static libraries, link targets, and resources in `native/<runtime>`. It has no managed code.
- `WxKit` depends on every native package, so an application only references `WxKit`.

Each native package's targets, in `buildTransitive`, set `WxKitNativeDirectory` to its static build when the application's runtime is the package's, and both they and WxKit's targets import that build's `WxKit.Native.targets` when the application links statically. NuGet does not say which package's targets it imports first, so whichever comes second finds the link targets imported already, through a property they set. A project that builds WxKit from source, such as the Hello sample, sets `WxKitNativeDirectory` itself.

`src/WxKit.Native/WxKit.Native.csproj` packs a native package from a runtime's build, given its runtime as `WxKitRuntime`, and fails when there is no build for it. `scripts/pack.ps1` packs one for each runtime given, then `WxKit`, which depends on those runtimes' packages only while it is packed, so building WxKit itself needs none. The native packages carry `THIRD-PARTY-NOTICES.md` as their license file, and each license text in their `licenses` folder.

`scripts/test-package.ps1` uses the packages as an application would. It makes a project that empty `Directory.Build` files shut off from the repository's build, which restores `WxKit` from `artifacts/packages` into a folder of its own, and holds the Hello sample's code. It runs the project as it is, with the shared library, and then publishes it with Native AOT, checks that nothing native ships beside the executable, and runs that.

## Releasing

A tag that names a version, such as `v0.1.0-preview.1`, releases it. CI runs on the tag as it does on a push, packs every package with the tag's version, tests them, and then a publish job pushes them to nuget.org and makes a GitHub release with notes generated from the commits and the packages attached. A version with a suffix, such as `-preview.1`, is a prerelease in both places. `Directory.Build.props` only gives the version of builds that are not releases.

```powershell
git tag v0.1.0-preview.1
git push origin v0.1.0-preview.1
```

The publish job signs in to nuget.org with Trusted Publishing: nuget.org trusts the workflow's GitHub identity, and the `NuGet/login` action trades it for a key that lasts an hour. No key is kept in the repository. The native packages go before `WxKit`, which depends on them, and a version nuget.org has already is skipped, so a run that failed partway can run again.

Setting this up takes three steps, once:

1. On nuget.org, under Trusted Publishing in the account's settings, add a policy for the repository owner `trypsynth`, the repository `wxkit`, the workflow file `ci.yml`, and the environment `nuget`.
2. In the repository's settings on GitHub, under Environments, add one called `nuget`. Adding yourself as a required reviewer there makes every release wait for your approval before it publishes, and limiting it to tags that start with `v` keeps any other ref from using it.
3. Under Actions variables, add `NUGET_USER`, set to the nuget.org user name the policy belongs to.

nuget.org lets a version be unlisted, so a search no longer finds it, but not deleted, so a version number is only ever used once.

The packages carry Source Link, and the managed library's symbols go to nuget.org's symbol server, so a debugger can step into WxKit's sources at the release's commit. A CI build is deterministic, which Source Link needs.

## Target framework

The managed library targets `net10.0`. .NET 8 reaches the end of its support in November 2026, so supporting it would mean shipping for a version that is already out of support. The tests and samples target `net10.0` as well.

## Licensing

WxKit is MIT licensed. wxWidgets uses the wxWindows Library Licence, which is the LGPL with an exception that allows distributing applications linked with it, statically or not, under their own terms. That exception is what makes the single-executable model possible.

The native packages also contain the third-party libraries that wxWidgets builds into its base and core libraries: zlib, libpng, libjpeg, libtiff, PCRE2, libwebp, and NanoSVG. `THIRD-PARTY-NOTICES.md` lists each one with its version and license, and the packages carry their license texts. Optional libraries bring more, such as expat for XRC, and Scintilla and Lexilla for the styled text control, which the notices will list when WxKit binds them.

## Continuous integration

CI runs on GitHub Actions:

- A job per runtime, on a machine of that runtime's system and architecture, builds wxWidgets and the shim, caches the build by the runtime, the wxWidgets commit, and a hash of the build script, runs the tests, publishes the Native AOT smoke test, and uploads the native build. `win-x64` runs on `windows-2022`, `win-arm64` on `windows-11-arm`, `linux-x64` on `ubuntu-22.04`, `linux-arm64` on `ubuntu-22.04-arm`, `osx-arm64` on `macos-15`, and `osx-x64` on `macos-15-intel`. Linux runs its tests under `xvfb-run`. The x64 job also tests the generator, checks that the generated bindings are current, and checks formatting.
- A pack job gathers every runtime's native build and packs the packages, which CI keeps for a week, as an artifact of the run.
- A job per runtime, on that runtime's system and architecture again, tests the packages with `test-package.ps1`. On Linux it installs GTK and Native AOT's tools, but not GTK's development files, as an application's build machine would.
- On a tag, a publish job pushes the packages to nuget.org, as Releasing describes.
- A newer run for the same pull request cancels an older one.

wxWidgets takes a long time to build, so the cache matters. A cold build of one runtime takes about 10 to 20 minutes on a desktop machine, and CI runners are slower.
