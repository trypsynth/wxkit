# Applies the patches in native/patches/wxWidgets to the wxWidgets submodule: fixes WxKit needs
# before the wxWidgets release it pins has them, each also a pull request to wxWidgets, as
# native/patches/wxWidgets/README.md lists. A patch already applied to the working tree is left as
# it is, so this can run before every build. One that the wxWidgets commit already has, such as
# after moving to a release with it, or that no longer applies, fails, and is to be deleted.
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$wxWidgets = Join-Path $root "native/wxWidgets"
$patches = Get-ChildItem (Join-Path $root "native/patches/wxWidgets") -Filter *.patch | Sort-Object Name
foreach ($patch in $patches) {
	# Against the commit's own files, which a patch WxKit applied doesn't change.
	git -C $wxWidgets apply --cached --reverse --check $patch.FullName 2>$null
	if ($LASTEXITCODE -eq 0) { throw "The wxWidgets WxKit pins has $($patch.Name) already. Delete the patch, and its line in native/patches/wxWidgets/README.md." }
	git -C $wxWidgets apply --reverse --check $patch.FullName 2>$null
	if ($LASTEXITCODE -eq 0) { continue }
	git -C $wxWidgets apply --check $patch.FullName
	if ($LASTEXITCODE -ne 0) { throw "$($patch.Name) no longer applies to wxWidgets. If the wxWidgets WxKit pins has it now, delete the patch, and its line in native/patches/wxWidgets/README.md." }
	git -C $wxWidgets apply $patch.FullName
	if ($LASTEXITCODE -ne 0) { throw "Applying $($patch.Name) failed." }
	Write-Host "Applied $($patch.Name) to wxWidgets."
}
