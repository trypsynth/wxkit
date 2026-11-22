# Packs WxKit into artifacts/packages: a native package for each runtime, such as
# WxKit.Native.win-x64, from its build in artifacts/native, then WxKit, which depends on them, and
# WxKit.Templates, the dotnet new templates, whose projects reference that WxKit.
# Run scripts/build-native.ps1 for the same runtimes first.
param(
	[string[]]$Runtimes = @("win-x64"),
	[string]$Configuration = "Release",
	# The version every package gets, such as a release's from its tag. By default, the one in
	# Directory.Build.props.
	[string]$Version
)
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$output = Join-Path $root "artifacts/packages"
# WxKit restores the native packages it was just given, so it restores into a folder of its own,
# where an older package with the same version cannot stand in for them.
$cache = Join-Path $root "artifacts/package-cache"

$versionArguments = $Version ? @("-p:Version=$Version") : @()

Remove-Item -Recurse -Force $output, $cache -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $output | Out-Null

foreach ($runtime in $Runtimes) {
	dotnet pack (Join-Path $root "src/WxKit.Native") -c $Configuration -o $output -nologo "-p:WxKitRuntime=$runtime" @versionArguments
	if ($LASTEXITCODE -ne 0) { throw "Packing WxKit.Native.$runtime failed." }
}

# A semicolon or a comma would separate properties on the command line, so the runtimes are joined with plus signs.
$runtimeList = $Runtimes -join "+"
dotnet pack (Join-Path $root "src/WxKit") -c $Configuration -o $output -nologo "-p:WxKitPacking=true" "-p:WxKitRuntimes=$runtimeList" "-p:RestoreAdditionalProjectSources=$output" "-p:RestorePackagesPath=$cache" @versionArguments
if ($LASTEXITCODE -ne 0) { throw "Packing WxKit failed." }

# The templates reference the WxKit just packed, by its version.
$package = Get-ChildItem $output -Filter "WxKit.*.nupkg" | Where-Object { $_.Name -notlike "WxKit.Native.*" } | Select-Object -First 1
$packed = $package.Name.Substring("WxKit.".Length, $package.Name.Length - "WxKit.".Length - ".nupkg".Length)
dotnet pack (Join-Path $root "src/WxKit.Templates") -c $Configuration -o $output -nologo "-p:Version=$packed"
if ($LASTEXITCODE -ne 0) { throw "Packing WxKit.Templates failed." }

foreach ($package in Get-ChildItem $output -Include *.nupkg, *.snupkg -Recurse) {
	$size = [math]::Round($package.Length / 1MB, 1)
	Write-Host "$($package.Name): $size MB"
}
