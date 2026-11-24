# Uses the packages in artifacts/packages the way an application would: a new project made with
# their dotnet new template, which references WxKit from them, and its F# version, which
# references WxKit.FSharp. It builds the template's project,
# then gives it the Hello sample's code, which can close itself, runs it as it is, which loads the
# shared library from the native package, and then publishes it with Native AOT, which links the
# static one, and runs that. Run scripts/pack.ps1 first.
param(
	[string]$Runtime = "win-x64",
	[int]$TimeoutSeconds = 30
)
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$packages = Join-Path $root "artifacts/packages"
$test = Join-Path $root "artifacts/package-test"
# The packages restore beside the project rather than inside it, where its default items would
# take in the source files packages carry, such as WxKit's resource merge task.
$project = Join-Path $test "app"
$restored = Join-Path $test "packages"

$package = Get-ChildItem $packages -Filter "WxKit.*.nupkg" | Where-Object { $_.Name -notlike "WxKit.Native.*" -and $_.Name -notlike "WxKit.Templates.*" } | Select-Object -First 1
if (-not $package) { throw "There is no WxKit package in $packages. Run scripts/pack.ps1 first." }
$version = $package.Name.Substring("WxKit.".Length, $package.Name.Length - "WxKit.".Length - ".nupkg".Length)

if ($IsWindows) {
	$installer = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio/Installer"
	if ((Test-Path $installer) -and -not ($env:PATH -split ";" -contains $installer)) {
		$env:PATH = "$installer;$env:PATH"
	}
}

# Empty Directory.Build files shut the project off from the repository's build, so none of its
# props, targets, or native builds reach it, and it restores into a folder of its own, so it uses
# these packages and no others.
Remove-Item -Recurse -Force $test -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $project | Out-Null
Set-Content (Join-Path $project "Directory.Build.props") "<Project />" -Encoding utf8NoBOM
Set-Content (Join-Path $project "Directory.Build.targets") "<Project />" -Encoding utf8NoBOM
Set-Content (Join-Path $project "nuget.config") @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
	<config>
		<add key="globalPackagesFolder" value="$restored" />
	</config>
	<packageSources>
		<clear />
		<add key="local" value="$packages" />
		<add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
	</packageSources>
</configuration>
"@ -Encoding utf8NoBOM
# The template goes into a hive of its own, so the machine's own templates are left as they are.
$templates = Get-ChildItem $packages -Filter "WxKit.Templates.*.nupkg" | Select-Object -First 1
if (-not $templates) { throw "There is no WxKit.Templates package in $packages. Run scripts/pack.ps1 first." }
$hive = Join-Path $test "template-hive"
dotnet new install $templates.FullName --debug:custom-hive $hive
if ($LASTEXITCODE -ne 0) { throw "Installing the templates failed." }
dotnet new wxkit -n PackageTest -o $project --no-restore --debug:custom-hive $hive
if ($LASTEXITCODE -ne 0) { throw "Making a project with the template failed." }
dotnet build $project -c Release -nologo
if ($LASTEXITCODE -ne 0) { throw "Building the template's project failed." }
# The copy keeps the sample's time, older than the build just made, so it is marked as changed now,
# or the next build would keep the template's program, which waits for a person to close it.
Copy-Item (Join-Path $root "samples/csharp/Hello/Program.cs") $project -Force
(Get-Item (Join-Path $project "Program.cs")).LastWriteTime = Get-Date

function Test-Executable([string]$Path, [string]$What) {
	$process = Start-Process $Path -ArgumentList "--smoke" -PassThru
	if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
		Stop-Process -Id $process.Id
		throw "$What did not exit within $TimeoutSeconds seconds."
	}
	if ($process.ExitCode -ne 0) { throw "$What exited with $($process.ExitCode)." }
}

dotnet build $project -c Release -nologo
if ($LASTEXITCODE -ne 0) { throw "Building the package test failed." }
# The template's project builds for the machine's own runtime, so its output has the runtime in its path.
$built = Get-ChildItem (Join-Path $project "bin/Release/net10.0") -Recurse -Filter "PackageTest$($IsWindows ? '.exe' : '')" | Where-Object { $_.Directory.Name -ne "publish" } | Select-Object -First 1
if (-not $built) { throw "The package test's build has no executable." }
Test-Executable $built.FullName "The package test, run as it is,"

$published = Join-Path $project "published"
dotnet publish $project -c Release -r $Runtime -o $published -nologo "-p:PublishAot=true"
if ($LASTEXITCODE -ne 0) { throw "Publishing the package test with Native AOT failed." }
$extras = Get-ChildItem $published -File | Where-Object { $_.Name -like "*wxkitnative*" }
if ($extras) { throw "The published package test still ships $($extras.Name -join ', ') beside it." }
$executable = Join-Path $published "PackageTest$($IsWindows ? '.exe' : '')"
if ($IsMacOS) {
	$executable = Join-Path $published "PackageTest.app/Contents/MacOS/PackageTest"
	if (-not (Test-Path $executable)) { throw "The published package test has no app bundle." }
}
Test-Executable $executable "The package test, published with Native AOT,"
$size = [math]::Round((Get-Item $executable).Length / 1MB, 1)

# The F# template's project, which references WxKit.FSharp, builds from the packages too, shut off
# from the repository's build in the same way.
$fsharp = Join-Path $test "fsharp"
dotnet new wxkit -lang "F#" -n PackageTestFSharp -o $fsharp --no-restore --debug:custom-hive $hive
if ($LASTEXITCODE -ne 0) { throw "Making an F# project with the template failed." }
foreach ($file in "Directory.Build.props", "Directory.Build.targets", "nuget.config") { Copy-Item (Join-Path $project $file) $fsharp }
dotnet build $fsharp -c Release -nologo
if ($LASTEXITCODE -ne 0) { throw "Building the F# template's project failed." }
# Then it publishes the F# sample's code with Native AOT, which can close itself, so WxKit's
# native library and link targets reach an application through WxKit.FSharp.
Copy-Item (Join-Path $root "samples/fsharp/Hello/Program.fs") $fsharp -Force
(Get-Item (Join-Path $fsharp "Program.fs")).LastWriteTime = Get-Date
$fsharpPublished = Join-Path $fsharp "published"
dotnet publish $fsharp -c Release -r $Runtime -o $fsharpPublished -nologo
if ($LASTEXITCODE -ne 0) { throw "Publishing the F# package test with Native AOT failed." }
$fsharpExecutable = Join-Path $fsharpPublished "PackageTestFSharp$($IsWindows ? '.exe' : '')"
if ($IsMacOS) { $fsharpExecutable = Join-Path $fsharpPublished "PackageTestFSharp.app/Contents/MacOS/PackageTestFSharp" }
Test-Executable $fsharpExecutable "The F# package test, published with Native AOT,"
Write-Host "WxKit $version restored from its packages, ran, and published with Native AOT for $Runtime as $size MB, and so did its F# template with WxKit.FSharp."
