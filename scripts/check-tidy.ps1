# Runs clang-tidy over the shim's hand-written C++, with the checks native/shim/.clang-tidy lists,
# and fails on any warning. It reads how each file compiles from a Linux build's
# compile_commands.json, which scripts/build-native.ps1 and scripts/test-sanitizers.ps1 write.
param(
	[string]$Build = "artifacts/native-build/linux-x64"
)
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$build = Join-Path $root $Build
if (-not (Test-Path (Join-Path $build "compile_commands.json"))) { throw "$build has no compile_commands.json. Build the native library on Linux first." }
$files = Get-ChildItem (Join-Path $root "native/shim/src") -Filter *.cpp -Recurse | ForEach-Object FullName
$reports = $files | ForEach-Object -ThrottleLimit ([Environment]::ProcessorCount) -Parallel {
	clang-tidy -p $using:build --quiet $_ 2>$null
}
# Only what is in the shim's own files counts. The static analyzer also reports along a path from
# the shim into wxWidgets' headers, such as into its reference-counted buffers, which it can't
# follow, and clang-tidy's header filter doesn't drop those.
$warnings = $reports | Where-Object { $_ -match "native/shim/(src|include)/[^:]+:\d+:\d+: (warning|error): " } | Sort-Object -Unique
if ($warnings) {
	$warnings | Write-Host
	throw "clang-tidy found $($warnings.Count) problems in the shim, listed above."
}
Write-Host "clang-tidy found nothing in the shim's $($files.Count) hand-written files."
