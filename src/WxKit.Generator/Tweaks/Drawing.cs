namespace WxKit.Generator.Tweaks;

// Fonts, pens, brushes, bitmaps, icons, cursors, and images, drawing contexts, dragging, and the
// system's colors, fonts, and metrics.
internal static class Drawing {
	public static void Apply(Binding.Tweaks tweaks) {
		// Copied like values.
		foreach (string value in (string[])["wxFont", "wxPen", "wxBrush", "wxBitmap", "wxIcon", "wxCursor", "wxImage"]) {
			tweaks.Class(value);
		}
		// Underlined and Strikethrough return changed copies, beside the properties of the same names.
		tweaks.Class("wxFont", font => font
			.Rename("GetNativeFontInfoDesc", "NativeFontInfoDescription")
			.Property("NativeFontInfoUserDescription", "GetNativeFontInfoUserDesc", "SetNativeFontInfoUserDesc")
			.Rename("Underlined", "WithUnderline")
			.Rename("Strikethrough", "WithStrikethrough")
			.Guard("wxUSE_PRIVATE_FONTS", "AddPrivateFont"));
		// Size returns a resized copy, beside the Size property, as Resize resizes the image itself.
		// SetAlpha with no array gives the image an alpha channel it leaves as it was in memory, rather
		// than opaque, as InitAlpha does.
		tweaks.Class("wxImage").Rename("GetImageExtWildcard", "ImageExtensionWildcard").Rename("Size", "Resized").IgnoreOverload("SetAlpha", "alpha", "unsigned char *");
		// wxMSW's wxIcon has no ConvertToDisabled, though the documentation gives it one.
		tweaks.Class("wxIcon").Ignore("ConvertToDisabled");
		// Drawing contexts. wxWidgets 3.3 moved a DC's font, text measurement, and other parts that
		// do not draw to wxReadOnlyDC, which wxDC derives from and which is not bound itself. A
		// getter that can fail returns whether it could, as .NET's Try methods do.
		foreach (string dc in (string[])["wxDC", "wxWindowDC", "wxClientDC", "wxPaintDC", "wxMemoryDC", "wxScreenDC"]) {
			tweaks.Class(dc);
		}
		tweaks.Class("wxDC", dc => dc
			.Property("MappingMode", "GetMapMode", "SetMapMode")
			.Rename("GetSizeMM", "SizeInMillimeters")
			// The scales are written by hand, as properties that give both numbers as one value.
			.Internal("GetUserScale", "SetUserScale", "GetLogicalScale", "SetLogicalScale")
			.InheritUnboundBases()
			.ParameterEnum("SetBackgroundMode", "mode", "BrushStyle")
			.ParameterEnum("GetBackgroundMode", "return", "BrushStyle")
			.Property("AreAutomaticBoundingBoxUpdatesEnabled", "AreAutomaticBoundingBoxUpdatesEnabled")
			.Rename("GetClippingBox", "TryGetClippingBox"));
		// What a drag may do, and what it did, such as DragResult.Copy, without the type's name again.
		tweaks.ConstantEnumOf("DragFlags", isFlags: true, "wxDrag_CopyOnly", "wxDrag_AllowMove", "wxDrag_DefaultMove");
		tweaks.Enum("wxDragResult", "DragResult", memberPrefix: "wxDrag");
		// The system's colors, fonts, and metrics, and the standard icons.
		tweaks.Class("wxSystemSettings").StaticOnly();
		// Pop takes the newest provider off the stack, which without Push is one of wxWidgets' own, so
		// the standard icons go.
		tweaks.Class("wxArtProvider", art => art
			.StaticOnly()
			.Ignore("Pop")
			.ParameterEnum("GetMessageBoxIcon", "flags", "MessageDialogStyle")
			.ParameterEnum("GetMessageBoxIconId", "flags", "MessageDialogStyle"));
	}
}
