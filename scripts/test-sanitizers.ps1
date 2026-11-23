# Runs WxKit's tests on Linux against a shared library built, wxWidgets and the shim alike, with
# AddressSanitizer and UndefinedBehaviorSanitizer, which report memory errors, such as a read after
# a free, and undefined behavior, such as a cast to a class an object isn't, as the tests run. It
# fails on any report, even when every test passes. What they find in wxWidgets, WxKit fixes with a
# patch in native/patches. Run it under a display, such as with xvfb-run.
param(
	[string]$Runtime = "linux-x64"
)
$ErrorActionPreference = "Stop"
if (-not $IsLinux) { throw "The sanitizers run on Linux." }
$root = Split-Path $PSScriptRoot -Parent
$build = Join-Path $root "artifacts/native-build/$Runtime-sanitize"
$native = Join-Path $root "artifacts/native-sanitize"
$log = Join-Path $root "artifacts/sanitizers.log"

& (Join-Path $PSScriptRoot "patch-wxwidgets.ps1")
# The build's compile commands are also what scripts/check-tidy.ps1 reads.
cmake -S (Join-Path $root "native/shim") -B $build -G Ninja "-DCMAKE_BUILD_TYPE=Release" "-DWXKIT_SANITIZE=ON" "-DCMAKE_EXPORT_COMPILE_COMMANDS=ON"
if ($LASTEXITCODE -ne 0) { throw "Configuring the sanitized build failed." }
# The tests use the shared library built with the web view, as on Linux they always do.
cmake --build $build --target wxkitnative_webview
if ($LASTEXITCODE -ne 0) { throw "Building the sanitized library failed." }
$shared = Join-Path $native "$Runtime/shared/webview"
Remove-Item -Recurse -Force $native -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $shared | Out-Null
Copy-Item (Join-Path $build "shared/webview/libwxkitnative.so") $shared
# wxWidgets' own catalogs, which the translation tests embed, compiled as scripts/build-native.ps1
# compiles them. Without msgfmt, those tests skip, but CI has it.
$catalogs = Get-ChildItem (Join-Path $root "native/wxWidgets/locale") -Filter *.po
if (-not (Get-Command msgfmt -ErrorAction SilentlyContinue)) {
	if ($env:CI) { throw "msgfmt, from GNU gettext, is not installed, so wxWidgets' catalogs cannot be compiled." }
	Write-Warning "msgfmt, from GNU gettext, is not installed, so the translation tests skip."
	$catalogs = @()
}
foreach ($catalog in $catalogs) {
	$directory = Join-Path $native "$Runtime/locale/$($catalog.BaseName)"
	New-Item -ItemType Directory -Force $directory | Out-Null
	msgfmt --output-file (Join-Path $directory "wxstd.mo") $catalog.FullName
	if ($LASTEXITCODE -ne 0) { throw "Compiling $($catalog.Name) failed." }
}

# The tests are built first, without the sanitizers' runtime, which .NET's own build tools would
# otherwise run under too.
dotnet build (Join-Path $root "tests/WxKit.Tests") -c Release "-p:WxKitNativeRoot=$native"
if ($LASTEXITCODE -ne 0) { throw "Building the tests failed." }

# .NET isn't built with the sanitizers, so their runtime is loaded first. CoreCLR handles SIGSEGV
# itself, for null references, and allocates with malloc and frees with delete in places, which
# AddressSanitizer would otherwise report as WxKit's.
$env:LD_PRELOAD = (gcc -print-file-name=libasan.so)
$env:ASAN_OPTIONS = "detect_leaks=0:alloc_dealloc_mismatch=0:handle_segv=0:handle_sigbus=0:handle_sigfpe=0:allow_user_segv_handler=1:verify_asan_link_order=0"
$env:UBSAN_OPTIONS = "print_stacktrace=1"
try {
	dotnet test --project (Join-Path $root "tests/WxKit.Tests") -c Release --no-build 2>&1 | Tee-Object -FilePath $log
	$tests = $LASTEXITCODE
} finally {
	Remove-Item Env:LD_PRELOAD, Env:ASAN_OPTIONS, Env:UBSAN_OPTIONS -ErrorAction SilentlyContinue
}
$reports = Select-String -Path $log -Pattern "runtime error:|ERROR: AddressSanitizer" | ForEach-Object { $_.Line.Trim() } | Select-Object -Unique
if ($reports) {
	$reports | ForEach-Object { Write-Host $_ }
	throw "The sanitizers reported $($reports.Count) problems, listed above and in $log."
}
if ($tests -ne 0) { throw "The tests failed under the sanitizers." }
Write-Host "The tests passed with the sanitizers, which reported nothing."
