# Publishes each sample with Native AOT and runs it in smoke test mode, which shows its window,
# waits for the event loop to start, and exits. This catches what a normal build cannot: a native
# library missing from the link, or one the published executable still needs beside it. Each
# sample's executable, and on macOS its app bundle, is named after the sample.
param(
	[string]$Runtime = "win-x64",
	[string[]]$Samples = @("Hello", "HelloFSharp", "Editor", "Settings", "Checklist", "Browser"),
	[int]$TimeoutSeconds = 30
)
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent

# Native AOT finds the linker through vcvarsall.bat, which in the Visual Studio 2026 Build Tools
# runs vswhere.exe by name, so the installer's folder has to be on PATH.
if ($IsWindows) {
	$installer = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio/Installer"
	if ((Test-Path $installer) -and -not ($env:PATH -split ";" -contains $installer)) {
		$env:PATH = "$installer;$env:PATH"
	}
}

# A list given as one string, such as from bash, is "Hello,Editor".
foreach ($sample in ($Samples -split "," | ForEach-Object { $_.Trim() } | Where-Object { $_ })) {
	$output = Join-Path $root "artifacts/aot/$Runtime/$sample"
	Remove-Item -Recurse -Force $output -ErrorAction SilentlyContinue
	# Native AOT analyzes the whole application as it compiles it, so a trim or AOT warning, which
	# says the published application may not work as it does under the JIT, fails the test.
	$log = dotnet publish (Join-Path $root "samples/$sample") -c Release -r $Runtime -o $output -nologo 2>&1
	$log | Write-Host
	if ($LASTEXITCODE -ne 0) { throw "Publishing the $sample sample failed." }
	$warnings = @($log | Select-String "warning IL\d+" | Select-Object -ExpandProperty Line -Unique)
	if ($warnings.Count -gt 0) { throw "Publishing the $sample sample gave trim or AOT warnings:`n$($warnings -join "`n")" }
	# On macOS, the publish makes an app bundle too, which is what an application ships, so the
	# test checks its Info.plist and signature, and runs the executable inside it.
	if ($IsMacOS) {
		$bundle = Join-Path $output "$sample.app"
		plutil -lint (Join-Path $bundle "Contents/Info.plist") | Out-Null
		if ($LASTEXITCODE -ne 0) { throw "The $sample sample's Info.plist is not valid." }
		codesign --verify --strict $bundle
		if ($LASTEXITCODE -ne 0) { throw "The $sample sample's bundle is not validly signed." }
		$executable = Join-Path $bundle "Contents/MacOS/$sample"
	} else {
		$executable = Join-Path $output ($IsWindows ? "$sample.exe" : $sample)
	}
	$extras = Get-ChildItem $output -File | Where-Object { $_.Name -like "*wxkitnative*" }
	if ($extras) { throw "The published $sample sample still ships $($extras.Name -join ', ') beside it." }
	$process = Start-Process $executable -ArgumentList "--smoke" -PassThru
	if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
		Stop-Process -Id $process.Id
		throw "The published $sample sample did not exit within $TimeoutSeconds seconds."
	}
	if ($process.ExitCode -ne 0) { throw "The published $sample sample exited with $($process.ExitCode)." }
	$size = [math]::Round((Get-Item $executable).Length / 1MB, 1)
	Write-Host "The Native AOT $sample sample for $Runtime started and exited cleanly. It is $size MB."
}
