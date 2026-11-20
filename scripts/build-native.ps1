# Builds wxWidgets and WxKit's shim for each runtime. One CMake build per runtime makes both kinds
# of output, sharing one static build of wxWidgets:
#   static: the shim's static library, every wxWidgets library it needs, and on Windows wxkit.res,
#           for Native AOT to link into the application, with a WxKit.Native.targets naming each of them.
#   shared: wxkitnative.dll or libwxkitnative.so, with wxWidgets linked into it, for everything else.
# A Windows runtime builds on Windows, for any architecture, a Linux runtime on Linux, for the
# machine's own, and a macOS runtime on macOS, for either, since Apple's clang builds for both.
param(
	[string[]]$Runtimes = @($IsLinux ? "linux-x64" : $IsMacOS ? "osx-arm64" : "win-x64"),
	[ValidateSet("Release")]
	[string]$Configuration = "Release"
)
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$source = Join-Path $root "native/shim"
$wxWidgets = Join-Path $root "native/wxWidgets"
$windowsArchitectures = @{ "win-x64" = "x64"; "win-arm64" = "ARM64"; "win-x86" = "Win32" }
$linuxArchitectures = @{ "linux-x64" = "x86_64"; "linux-arm64" = "aarch64" }
$macArchitectures = @{ "osx-arm64" = "arm64"; "osx-x64" = "x86_64" }
# The oldest macOS the libraries run on, which is no newer than any .NET application needs.
$macDeploymentTarget = "12.0"
# GNU ld reads each static library once, in order, so each comes before the ones it uses: the
# shim, wxWidgets's core, its base, and then the libraries they build in.
$linuxLinkOrder = @("wxkitnative", "core", "base", "tiff", "jpeg", "png", "webpdemux", "webp", "sharpyuv", "regex", "expat", "zlib")
$windowsLibraries = @("kernel32.lib", "user32.lib", "gdi32.lib", "gdiplus.lib", "msimg32.lib", "comdlg32.lib", "winspool.lib", "winmm.lib", "shell32.lib", "shlwapi.lib", "comctl32.lib", "ole32.lib", "oleaut32.lib", "uuid.lib", "rpcrt4.lib", "advapi32.lib", "version.lib", "ws2_32.lib", "wininet.lib", "oleacc.lib", "uxtheme.lib", "imm32.lib", "dwmapi.lib", "usp10.lib")

function Invoke-Checked([string]$description) {
	if ($LASTEXITCODE -ne 0) { throw "$description failed." }
}

function Get-VisualStudio {
	$vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio/Installer/vswhere.exe"
	$studio = & $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -format json | ConvertFrom-Json | Select-Object -First 1
	if (-not $studio) { throw "No Visual Studio with the C++ tools was found." }
	return $studio
}

function Get-WindowsCMake($studio) {
	# Visual Studio's C++ tools install a CMake of their own, which is only on PATH in a developer
	# shell, so it is used when no other CMake is.
	$command = Get-Command cmake -ErrorAction SilentlyContinue
	if ($command) { return $command.Source }
	$bundled = Join-Path $studio.installationPath "Common7/IDE/CommonExtensions/Microsoft/CMake/CMake/bin/cmake.exe"
	if (-not (Test-Path $bundled)) { throw "CMake is not on PATH, and Visual Studio has none of its own. Install CMake, or Visual Studio's C++ CMake tools." }
	return $bundled
}

function Get-WindowsGenerator($studio) {
	# CMake falls back to Ninja on some setups, which cannot take -A, so the newest Visual Studio
	# with the C++ tools is named explicitly.
	$years = @{ "16" = "2019"; "17" = "2022"; "18" = "2026" }
	$major = $studio.installationVersion.Split(".")[0]
	if (-not $years.ContainsKey($major)) { throw "Visual Studio $major is not one this script knows the CMake generator for." }
	return "Visual Studio $major $($years[$major])"
}

function Write-Targets([string]$directory, [string[]]$items) {
	# The flag tells WxKit's targets and the native package's, which can each import this, that one has.
	$lines = @("<Project>", "", "`t<PropertyGroup>", "`t`t<_WxKitLinkTargetsImported>true</_WxKitLinkTargetsImported>", "`t</PropertyGroup>", "", "`t<ItemGroup>") + ($items | ForEach-Object { "`t`t$_" }) + @("`t</ItemGroup>", "", "</Project>")
	Set-Content -Path (Join-Path $directory "WxKit.Native.targets") -Value $lines -Encoding utf8NoBOM
}

# wxWidgets keeps its bundled libraries, such as zlib and libpng, in submodules of its own.
if (-not (Test-Path (Join-Path $wxWidgets "src/zlib/zlib.h"))) {
	git -C $wxWidgets submodule update --init --recursive --depth 1
	Invoke-Checked "Fetching the wxWidgets submodules"
}

function Get-LinkRank([string]$name) {
	for ($index = 0; $index -lt $linuxLinkOrder.Count; $index++) {
		if ($name -like "*$($linuxLinkOrder[$index])*") { return $index }
	}
	throw "$name is a library the link order does not place."
}

function Build-Linux([string]$runtime) {
	if (-not $IsLinux) { throw "$runtime builds on Linux." }
	$machine = (uname -m)
	if ($machine -ne $linuxArchitectures[$runtime]) { throw "$runtime builds on a $($linuxArchitectures[$runtime]) machine, and this one is $machine." }
	$build = Join-Path $root "artifacts/native-build/$runtime"
	$output = Join-Path $root "artifacts/native/$runtime"
	cmake -S $source -B $build -G Ninja "-DCMAKE_BUILD_TYPE=$Configuration"
	Invoke-Checked "Configuring $runtime"
	cmake --build $build --target wxkitnative_static wxkitnative
	Invoke-Checked "Building $runtime"
	$static = Join-Path $output "static"
	$shared = Join-Path $output "shared"
	Remove-Item -Recurse -Force $static, $shared -ErrorAction SilentlyContinue
	New-Item -ItemType Directory -Force $static, $shared | Out-Null
	$libraries = @(Get-ChildItem (Join-Path $build "wx/lib") -Filter *.a)
	if ($libraries.Count -eq 0) { throw "The build did not produce any wxWidgets libraries." }
	$libraries += Get-Item (Join-Path $build "static/libwxkitnative.a")
	foreach ($library in $libraries) { Copy-Item $library.FullName $static }
	$library = Join-Path $build "shared/libwxkitnative.so"
	Copy-Item $library $shared
	# The system libraries are the ones the shared library needs, named by their sonames, such as
	# libgtk-3.so.0, so an application's build machine needs GTK, but not its development packages.
	# The dynamic loader is one of them, which the linker brings in itself.
	$needed = @(readelf -d $library | Select-String "\(NEEDED\)\s+Shared library: \[(.+)\]" | ForEach-Object { $_.Matches[0].Groups[1].Value } | Where-Object { $_ -notlike "ld-linux*" })
	if ($needed.Count -eq 0) { throw "readelf found no libraries the shared library needs." }
	$items = @($libraries | Sort-Object { Get-LinkRank $_.Name } | ForEach-Object { "<NativeLibrary Include=`"`$(MSBuildThisFileDirectory)$($_.Name)`" />" })
	$items += $needed | ForEach-Object { "<NativeLibrary Include=`"-l:$_`" />" }
	Write-Targets $static $items
	Write-Host "Built $runtime into $output"
}

function Build-Mac([string]$runtime) {
	if (-not $IsMacOS) { throw "$runtime builds on macOS." }
	$build = Join-Path $root "artifacts/native-build/$runtime"
	$output = Join-Path $root "artifacts/native/$runtime"
	cmake -S $source -B $build -G Ninja "-DCMAKE_BUILD_TYPE=$Configuration" "-DCMAKE_OSX_ARCHITECTURES=$($macArchitectures[$runtime])" "-DCMAKE_OSX_DEPLOYMENT_TARGET=$macDeploymentTarget"
	Invoke-Checked "Configuring $runtime"
	cmake --build $build --target wxkitnative_static wxkitnative
	Invoke-Checked "Building $runtime"
	$static = Join-Path $output "static"
	$shared = Join-Path $output "shared"
	Remove-Item -Recurse -Force $static, $shared -ErrorAction SilentlyContinue
	New-Item -ItemType Directory -Force $static, $shared | Out-Null
	$libraries = @(Get-ChildItem (Join-Path $build "wx/lib") -Filter *.a)
	if ($libraries.Count -eq 0) { throw "The build did not produce any wxWidgets libraries." }
	$libraries += Get-Item (Join-Path $build "static/libwxkitnative.a")
	foreach ($library in $libraries) { Copy-Item $library.FullName $static }
	$library = Join-Path $build "shared/libwxkitnative.dylib"
	Copy-Item $library $shared
	# The system libraries are the frameworks and libraries the shared library loads, such as AppKit
	# and libc++. A framework is one argument for clang to pass to the linker, as -Wl,-framework,AppKit.
	$loads = @(otool -L $library | Select-Object -Skip 1 | ForEach-Object { ($_.Trim() -split " ")[0] })
	$system = foreach ($load in $loads) {
		if ($load -match "^/System/Library/Frameworks/([^/]+)\.framework/") { "-Wl,-framework,$($Matches[1])" }
		# libSystem is always linked, and .NET links libobjc itself.
		elseif ($load -match "^/usr/lib/lib(.+?)(\.[0-9A-Z]+)*\.dylib$" -and $Matches[1] -notin @("System", "objc")) { "-l$($Matches[1])" }
	}
	if (-not ($system -contains "-Wl,-framework,Cocoa" -or $system -contains "-Wl,-framework,AppKit")) { throw "otool found no Cocoa or AppKit among the libraries the shared library loads." }
	$items = @($libraries | Sort-Object { Get-LinkRank $_.Name } | ForEach-Object { "<NativeLibrary Include=`"`$(MSBuildThisFileDirectory)$($_.Name)`" />" })
	$items += $system | Select-Object -Unique | ForEach-Object { "<NativeLibrary Include=`"$_`" />" }
	Write-Targets $static $items
	Write-Host "Built $runtime into $output"
}

$studio = $null
foreach ($runtime in $Runtimes) {
	if ($linuxArchitectures.ContainsKey($runtime)) {
		Build-Linux $runtime
		continue
	}
	if ($macArchitectures.ContainsKey($runtime)) {
		Build-Mac $runtime
		continue
	}
	if (-not $windowsArchitectures.ContainsKey($runtime)) { throw "$runtime is not a runtime this script can build yet." }
	if (-not $studio) {
		$studio = Get-VisualStudio
		$generator = Get-WindowsGenerator $studio
		$cmake = Get-WindowsCMake $studio
	}
	$build = Join-Path $root "artifacts/native-build/$runtime"
	$output = Join-Path $root "artifacts/native/$runtime"
	& $cmake -S $source -B $build -G $generator -A $windowsArchitectures[$runtime] "-DCMAKE_MSVC_RUNTIME_LIBRARY=MultiThreaded"
	Invoke-Checked "Configuring $runtime"
	& $cmake --build $build --config $Configuration --parallel --target wxkitnative_static wxkitnative wxkit_resources
	Invoke-Checked "Building $runtime"
	$static = Join-Path $output "static"
	$shared = Join-Path $output "shared"
	Remove-Item -Recurse -Force $static, $shared -ErrorAction SilentlyContinue
	New-Item -ItemType Directory -Force $static, $shared | Out-Null
	# wxWidgets puts its libraries in a directory named after the compiler and architecture, such as vc_x64_lib.
	$libraries = @(Get-ChildItem (Join-Path $build "wx/lib") -Recurse -Filter *.lib | Where-Object { $_.Directory.Name -like "vc*_lib" })
	if ($libraries.Count -eq 0) { throw "The build did not produce any wxWidgets libraries." }
	$libraries += Get-Item (Join-Path $build "static/wxkitnative.lib")
	$resources = Get-ChildItem (Join-Path $build "wxkit_resources.dir") -Recurse -Filter wxkit.res | Select-Object -First 1
	if (-not $resources) { throw "The build did not produce wxkit.res." }
	foreach ($library in $libraries) { Copy-Item $library.FullName $static }
	Copy-Item $resources.FullName $static
	# A CMake build of wxWidgets does not name the Windows libraries it uses in its objects, so they
	# are listed here. They are all part of Windows, and a library nothing uses costs nothing.
	$items = @($libraries | Sort-Object Name | ForEach-Object { "<NativeLibrary Include=`"`$(MSBuildThisFileDirectory)$($_.Name)`" />" })
	$items += '<NativeLibrary Include="' + ($windowsLibraries -join ";") + '" />'
	$items += '<WxKitResource Include="$(MSBuildThisFileDirectory)wxkit.res" />'
	Write-Targets $static $items
	Copy-Item (Join-Path $build "shared/wxkitnative.dll") $shared
	$symbols = Join-Path $build "shared/wxkitnative.pdb"
	if (Test-Path $symbols) { Copy-Item $symbols $shared }
	Write-Host "Built $runtime into $output"
}
