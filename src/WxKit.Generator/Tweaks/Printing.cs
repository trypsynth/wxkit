namespace WxKit.Generator.Tweaks;

// Printing. The data and dialogs are objects the application owns, and so is a printout, whose DC
// is only lent to it for each page. A preview, which takes its printouts, is too.
internal static class Printing {
	public static void Apply(Binding.Tweaks tweaks) {
		foreach (string data in (string[])["wxPrintData", "wxPrintDialogData", "wxPageSetupDialogData"]) tweaks.Class(data, value => value.ValueObject());
		// A print quality is a resolution in dots per inch, or one of these, which are negative.
		tweaks.ConstantEnum("PrintQuality", "wxPRINT_QUALITY_");
		// A paper size is a PaperKind, as System.Drawing.Printing names it, so the enum doesn't share a
		// name with the page setup's PaperSize, a size. Whether to print in color is IsColor, a bool.
		tweaks.Enum("wxPaperSize", "PaperKind");
		foreach (string data in (string[])["wxPrintData", "wxPrintDialogData"]) tweaks.Class(data, value => value.Property("Copies", "GetNoCopies", "SetNoCopies"));
		tweaks.Class("wxPrintData", data => data
			.ParameterEnum("GetQuality", "return", "PrintQuality")
			.ParameterEnum("SetQuality", "quality", "PrintQuality")
			.Property("PaperKind", "GetPaperId", "SetPaperId")
			.Property("IsColor", "GetColour", "SetColour"));
		tweaks.Class("wxPageSetupDialogData").Property("PaperKind", "GetPaperId", "SetPaperId");
		// The page setup dialog's Enable setters pair with GetEnable getters, as properties of the same name.
		foreach (string part in (string[])["Help", "Margins", "Orientation", "Paper", "Printer"]) tweaks.Class("wxPageSetupDialogData").Property("Enable" + part, "GetEnable" + part, "Enable" + part);
		// The print dialog's have getters too, which the interface headers leave out, so its
		// properties are written by hand, in PairedProperties.cs.
		tweaks.Class("wxPrintDialogData").Internal("EnablePrintToFile", "EnableSelection", "EnableCurrentPage", "EnablePageNumbers", "EnableHelp");
		foreach (string owned in (string[])["wxPrinter", "wxPrintDialog", "wxPageSetupDialog"]) tweaks.Class(owned, value => value.Owned());
		// The hand-written Printout makes a director, whose callbacks it gives, and draws on the DC it
		// reads while a page prints. wxWidgets starts each copy's document before .NET's OnBeginDocument
		// and ends it after its OnEndDocument.
		tweaks.Class("wxPrintout", printout => printout
			.Property("PageSizeInMillimeters", "GetPageSizeMM")
			.Owned()
			.Internal("wxPrintout", "GetDC")
			.InternalDirector("OnPrintPage", "HasPage", "GetPageInfo", "OnPreparePrinting", "OnBeginPrinting", "OnEndPrinting", "OnBeginDocument", "OnEndDocument")
			.DirectorAfterBase("OnBeginDocument")
			.DirectorBeforeBase("OnEndDocument")
			.Ignore("GetPageInfo", "GetPagesInfo", "HasPage", "OnBeginDocument", "OnEndDocument", "OnBeginPrinting", "OnEndPrinting", "OnPreparePrinting", "OnPrintPage", "GetPreview"));
	}
}
