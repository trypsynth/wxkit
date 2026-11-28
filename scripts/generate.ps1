# Regenerates WxKit's bindings from the wxWidgets interface headers. Doxygen reads the headers
# and writes XML, which the generator reads. Both run only here: the generated files are checked
# in, so a normal build needs neither.
param(
	# Only write the Doxygen XML, without running the generator.
	[switch]$XmlOnly
)
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$wxWidgets = Join-Path $root "native/wxWidgets"
$tools = Join-Path $root "artifacts/tools"
$output = Join-Path $root "artifacts/doxygen"

# wxWidgets's own documentation is built with this release, so its XML is what the headers are written for.
$doxygenVersion = "1.15.0"
$doxygenArchives = @{
	"windows" = @{ Name = "doxygen-$doxygenVersion.windows.x64.bin.zip"; Sha256 = "44658b9cc5c91749e6e3cc426ba63e2550b4a4a7619065acd77029aa234719c6" }
	# An x64 build, which Apple silicon runs with Rosetta.
	"macos" = @{ Name = "Doxygen-$doxygenVersion.dmg"; Sha256 = "b7630eaa0d97bb50b0333929ef5dc1c18f9e38faf1e22dca3166189a9718faf0" }
}

function Get-Doxygen {
	if (-not ($IsWindows -or $IsMacOS)) { throw "This script only fetches Doxygen for Windows and macOS so far." }
	$archive = $doxygenArchives[$IsWindows ? "windows" : "macos"]
	$directory = Join-Path $tools "doxygen-$doxygenVersion"
	$executable = Join-Path $directory ($IsWindows ? "doxygen.exe" : "doxygen")
	if (Test-Path $executable) { return $executable }
	New-Item -ItemType Directory -Force $tools | Out-Null
	$download = Join-Path $tools $archive.Name
	$tag = "Release_" + $doxygenVersion.Replace(".", "_")
	Invoke-WebRequest "https://github.com/doxygen/doxygen/releases/download/$tag/$($archive.Name)" -OutFile $download
	$hash = (Get-FileHash $download -Algorithm SHA256).Hash.ToLowerInvariant()
	if ($hash -ne $archive.Sha256) { throw "$($archive.Name) has SHA-256 $hash, not the expected $($archive.Sha256)." }
	if ($IsWindows) {
		Expand-Archive $download -DestinationPath $directory -Force
	} else {
		# The disk image holds an app, whose executable needs nothing else in it.
		$mount = Join-Path $tools "doxygen-mount"
		hdiutil attach -nobrowse -readonly -mountpoint $mount $download | Out-Null
		if ($LASTEXITCODE -ne 0) { throw "hdiutil couldn't open $($archive.Name)." }
		try {
			New-Item -ItemType Directory -Force $directory | Out-Null
			Copy-Item (Join-Path $mount "Doxygen.app/Contents/Resources/doxygen") $executable
		} finally {
			hdiutil detach $mount | Out-Null
		}
	}
	Remove-Item $download
	return $executable
}

function Invoke-Doxygen([string]$doxygen) {
	# wxWidgets's Doxyfile takes its settings from the environment, and writes into its own source
	# tree. A configuration of WxKit's own includes it, and changes only what WxKit needs: XML of
	# the interface headers alone, written under artifacts.
	$doxyfileDirectory = Join-Path $wxWidgets "docs/doxygen"
	Remove-Item -Recurse -Force $output -ErrorAction SilentlyContinue
	New-Item -ItemType Directory -Force $output | Out-Null
	$configuration = Join-Path $output "Doxyfile"
	$interface = (Join-Path $wxWidgets "interface").Replace("\", "/")
	@(
		"@INCLUDE = `"$((Join-Path $doxyfileDirectory "Doxyfile").Replace("\", "/"))`"",
		"OUTPUT_DIRECTORY = `"$($output.Replace("\", "/"))`"",
		"INPUT = `"$interface`"",
		"WARN_LOGFILE = `"$((Join-Path $output "warnings.log").Replace("\", "/"))`"",
		"QUIET = YES"
	) | Set-Content $configuration -Encoding utf8NoBOM
	$settings = @{
		WXWIDGETS = $wxWidgets; WX_HTML_OUTPUT_DIR = (Join-Path $output "html")
		GENERATE_XML = "YES"; GENERATE_HTML = "NO"; GENERATE_HTMLHELP = "NO"; GENERATE_LATEX = "NO"
		GENERATE_QHP = "NO"; GENERATE_DOCSET = "NO"; GENERATE_TAGFILE = ""; SEARCHENGINE = "NO"
		SERVER_BASED_SEARCH = "NO"; HTML_CODE_FOLDING = "NO"; DOT_IMAGE_FORMAT = "svg"; HTML_HEADER = ""
		CUSTOM_THEME_CSS = ""; CUSTOM_THEME_JS1 = ""; CUSTOM_THEME_JS2 = ""
	}
	foreach ($setting in $settings.GetEnumerator()) { [Environment]::SetEnvironmentVariable($setting.Key, $setting.Value) }
	Push-Location $doxyfileDirectory
	try {
		& $doxygen $configuration
		if ($LASTEXITCODE -ne 0) { throw "Doxygen failed." }
	} finally {
		Pop-Location
	}
	$classes = (Get-ChildItem (Join-Path $output "xml") -Filter "class*.xml").Count
	Write-Host "Doxygen wrote XML for $classes classes to $(Join-Path $output "xml")."
}

Invoke-Doxygen (Get-Doxygen)
if ($XmlOnly) { return }
dotnet run --project (Join-Path $root "src/WxKit.Generator") -c Release -- (Join-Path $output "xml") $root
if ($LASTEXITCODE -ne 0) { throw "The generator failed." }
