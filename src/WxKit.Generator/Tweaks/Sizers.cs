namespace WxKit.Generator.Tweaks;

// Sizers, which lay out a window's children.
internal static class Sizers {
	public static void Apply(Binding.Tweaks tweaks) {
		// Written by hand, since WxKit tracks which window or sizer owns each sizer.
		tweaks.Class("wxSizer").Handwritten().Internal("Add", "AddSpacer", "AddStretchSpacer").DiscardUnboundReturns();
		// The border SizerFlags.Border uses when it is not given one.
		tweaks.Class("wxSizerFlags", flags => flags.StaticOnly().InteropOnly().Only("GetDefaultBorder"));
		tweaks.Class("wxBoxSizer", sizer => sizer
			.ParameterEnum("wxBoxSizer", "orient", "Orientation")
			.ParameterEnum("GetOrientation", "return", "Orientation")
			.ParameterEnum("SetOrientation", "orient", "Orientation"));
		// A static box sizer always has its box, which it makes or is given.
		tweaks.Class("wxStaticBoxSizer", sizer => sizer.ParameterEnum("wxStaticBoxSizer", "orient", "Orientation").NonNullReturn("GetStaticBox"));
		tweaks.Class("wxGridSizer");
		tweaks.Class("wxFlexGridSizer", sizer => sizer
			.Rename("IsColGrowable", "IsColumnGrowable")
			.Rename("AddGrowableCol", "AddGrowableColumn")
			.Rename("RemoveGrowableCol", "RemoveGrowableColumn")
			.Rename("GetColWidths", "ColumnWidths"));
		tweaks.Class("wxStdDialogButtonSizer");
		// A sizer that wraps its items onto more lines, as words wrap, and one that places them in
		// cells. Adding to a grid bag sizer is written by hand, since it takes SizerFlags, as Add does,
		// and must not let wxWidgets delete a sizer it cannot add.
		tweaks.ConstantEnumOf("WrapSizerFlags", isFlags: true, "wxEXTEND_LAST_ON_EACH_LINE", "wxREMOVE_LEADING_SPACES", "wxWRAPSIZER_DEFAULT_FLAGS");
		tweaks.Class("wxWrapSizer", sizer => sizer.ParameterEnum("wxWrapSizer", "flags", "WrapSizerFlags"));
		tweaks.Class("wxGridBagSizer", grid => grid.Internal("Add").DiscardUnboundReturns().Ignore("FindItem", "FindItemAtPoint", "FindItemAtPosition", "FindItemWithData"));
	}
}
