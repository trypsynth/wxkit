using WxKit.Interop;

namespace WxKit;

// Properties for what wxWidgets reads with one member and sets with another that the generator
// can't pair: the setter takes two values, or returns one, or there are two to choose between.

public unsafe partial class SplitterWindow {
	/// <summary>Where the sash is, in pixels from the left or the top. Setting it moves the sash and lays out the panes.</summary>
	public int SashPosition {
		get => WxKitNative.SplitterWindowGetSashPosition(Native);
		set => WxKitNative.SplitterWindowSetSashPosition(Native, value, 1);
	}
}

public unsafe partial class BookCtrlBase {
	/// <summary>
	/// The page that shows, or -1 when there is none. Setting it shows the page without raising
	/// the page changing and page changed events, as setting any control's selection does.
	/// </summary>
	public int Selection {
		get => WxKitNative.BookCtrlBaseGetSelection(Native);
		set => WxKitNative.BookCtrlBaseChangeSelection(Native, checked((nuint)value));
	}
}

public unsafe partial class Image {
	/// <summary>
	/// The color that is see-through where the image has no alpha, or null when it has no such
	/// color. Setting it gives the image a mask of that color, and setting null takes the mask away.
	/// </summary>
	public Color? MaskColor {
		get => WxKitNative.ImageHasMask(Native) == 0 ? null : new Color(WxKitNative.ImageGetMaskRed(Native), WxKitNative.ImageGetMaskGreen(Native), WxKitNative.ImageGetMaskBlue(Native));
		set {
			if (value is Color color) {
				WxKitNative.ImageSetMaskColour(Native, color.R, color.G, color.B);
			} else {
				WxKitNative.ImageSetMask(Native, 0);
			}
		}
	}

	/// <summary>Whether the image has a color that is see-through, which <see cref="MaskColor"/> is.</summary>
	public bool HasMask => WxKitNative.ImageHasMask(Native) != 0;
}

public unsafe partial class CalendarCtrl {
	/// <summary>The color of the text of the header, which names the days of the week.</summary>
	public Color? HeaderForegroundColor {
		get => Color.FromNative(WxKitNative.CalendarCtrlGetHeaderColourFg(Native));
		set => WxKitNative.CalendarCtrlSetHeaderColours(Native, Color.ToNative(value), WxKitNative.CalendarCtrlGetHeaderColourBg(Native));
	}

	/// <summary>The color behind the header, which names the days of the week.</summary>
	public Color? HeaderBackgroundColor {
		get => Color.FromNative(WxKitNative.CalendarCtrlGetHeaderColourBg(Native));
		set => WxKitNative.CalendarCtrlSetHeaderColours(Native, WxKitNative.CalendarCtrlGetHeaderColourFg(Native), Color.ToNative(value));
	}

	/// <summary>The color of the text of the selected date.</summary>
	public Color? HighlightForegroundColor {
		get => Color.FromNative(WxKitNative.CalendarCtrlGetHighlightColourFg(Native));
		set => WxKitNative.CalendarCtrlSetHighlightColours(Native, Color.ToNative(value), WxKitNative.CalendarCtrlGetHighlightColourBg(Native));
	}

	/// <summary>The color behind the selected date.</summary>
	public Color? HighlightBackgroundColor {
		get => Color.FromNative(WxKitNative.CalendarCtrlGetHighlightColourBg(Native));
		set => WxKitNative.CalendarCtrlSetHighlightColours(Native, WxKitNative.CalendarCtrlGetHighlightColourFg(Native), Color.ToNative(value));
	}

	/// <summary>The color of the text of holidays.</summary>
	public Color? HolidayForegroundColor {
		get => Color.FromNative(WxKitNative.CalendarCtrlGetHolidayColourFg(Native));
		set => WxKitNative.CalendarCtrlSetHolidayColours(Native, Color.ToNative(value), WxKitNative.CalendarCtrlGetHolidayColourBg(Native));
	}

	/// <summary>The color behind holidays.</summary>
	public Color? HolidayBackgroundColor {
		get => Color.FromNative(WxKitNative.CalendarCtrlGetHolidayColourBg(Native));
		set => WxKitNative.CalendarCtrlSetHolidayColours(Native, WxKitNative.CalendarCtrlGetHolidayColourFg(Native), Color.ToNative(value));
	}
}

public unsafe partial class PrintDialogData {
	/// <summary>Whether the dialog lets the user print to a file.</summary>
	public bool EnablePrintToFile {
		get => WxKitNative.PrintDialogDataGetEnable(Native, 0) != 0;
		set => WxKitNative.PrintDialogDataEnablePrintToFile(Native, value ? (byte)1 : (byte)0);
	}

	/// <summary>Whether the dialog lets the user print only what is selected.</summary>
	public bool EnableSelection {
		get => WxKitNative.PrintDialogDataGetEnable(Native, 1) != 0;
		set => WxKitNative.PrintDialogDataEnableSelection(Native, value ? (byte)1 : (byte)0);
	}

	/// <summary>Whether the dialog lets the user print only the current page.</summary>
	public bool EnableCurrentPage {
		get => WxKitNative.PrintDialogDataGetEnable(Native, 2) != 0;
		set => WxKitNative.PrintDialogDataEnableCurrentPage(Native, value ? (byte)1 : (byte)0);
	}

	/// <summary>Whether the dialog lets the user choose which pages to print.</summary>
	public bool EnablePageNumbers {
		get => WxKitNative.PrintDialogDataGetEnable(Native, 3) != 0;
		set => WxKitNative.PrintDialogDataEnablePageNumbers(Native, value ? (byte)1 : (byte)0);
	}

	/// <summary>Whether the dialog has a Help button.</summary>
	public bool EnableHelp {
		get => WxKitNative.PrintDialogDataGetEnable(Native, 4) != 0;
		set => WxKitNative.PrintDialogDataEnableHelp(Native, value ? (byte)1 : (byte)0);
	}
}
