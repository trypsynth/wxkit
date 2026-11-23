# wxWidgets patches

Fixes WxKit applies to the wxWidgets it pins, `v3.3.4`, until a wxWidgets release it moves to has them. `scripts/patch-wxwidgets.ps1` applies them, which `scripts/build-native.ps1`, `scripts/test-sanitizers.ps1`, and CI run first. Each is also a pull request to wxWidgets, and is named after it.

- `27399-init-encoding-info.patch`: [wxWidgets#27399](https://github.com/wxWidgets/wxWidgets/pull/27399) initializes `wxNativeEncodingInfo::encoding`, which copying a default `wxFontData` read uninitialized.
- `27400-msgdlg-parent-before-base.patch`: [wxWidgets#27400](https://github.com/wxWidgets/wxWidgets/pull/27400) stops `wxGenericMessageDialog` calling a member of its base class before the base exists.
- `27401-print-preview-downcast.patch`: [wxWidgets#27401](https://github.com/wxWidgets/wxWidgets/pull/27401) stops `wxPrintPreviewBase` casting itself to `wxPrintPreview` in its constructor, and sets a printout's preview to the `wxPrintPreview` rather than its implementation object.

To add one, make the fix on a branch of a wxWidgets fork from its `master`, open the pull request, and export the commit with `git format-patch -1 <branch> --stdout --no-signature` to `<pull request number>-<branch>.patch` here, without the author's email. Check that it applies to the pinned wxWidgets with `git -C native/wxWidgets apply --check`, and list it above.

When WxKit moves to a wxWidgets release, the script fails on each patch that release has, since it no longer applies. Delete each of those, and its line above.
