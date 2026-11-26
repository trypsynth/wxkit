using WxKit.Interop;

namespace WxKit;

// A place is a Point and an extent is a Size, where wxWidgets takes each as two numbers and has
// no overload that takes one value.

public unsafe partial class DC {
	/// <summary>
	/// The rectangle that holds everything drawn so far, in logical coordinates, which
	/// <see cref="ResetBoundingBox"/> empties and <see cref="CalcBoundingBox(Point)"/> adds a
	/// point to.
	/// </summary>
	public Rect BoundingBox {
		get {
			int left = WxKitNative.DCMinX(Native);
			int top = WxKitNative.DCMinY(Native);
			return new Rect(left, top, WxKitNative.DCMaxX(Native) - left, WxKitNative.DCMaxY(Native) - top);
		}
	}

	/// <summary>Copies part of another DC onto this one. Returns whether it could.</summary>
	/// <param name="destination">Where the copy's top left corner goes.</param>
	/// <param name="size">How much to copy.</param>
	/// <param name="source">The DC to copy from.</param>
	/// <param name="sourcePoint">The top left corner of what is copied.</param>
	/// <param name="logicalFunction">How the copy combines with what is there.</param>
	/// <param name="useMask">Whether to leave out what the source's bitmap masks.</param>
	/// <param name="sourceMask">The top left corner of the mask, when it isn't <paramref name="sourcePoint"/>.</param>
	public bool Blit(Point destination, Size size, DC source, Point sourcePoint, RasterOperationMode logicalFunction = RasterOperationMode.Copy, bool useMask = false, Point? sourceMask = null) {
		ArgumentNullException.ThrowIfNull(source);
		Point mask = sourceMask ?? Point.Default;
		return WxKitNative.DCBlit(Native, destination.X, destination.Y, size.Width, size.Height, source.Native, sourcePoint.X, sourcePoint.Y, (int)logicalFunction, useMask ? (byte)1 : (byte)0, mask.X, mask.Y) != 0;
	}

	/// <summary>Copies part of another DC onto this one, stretched or shrunk to fit. Returns whether it could.</summary>
	/// <param name="destination">The rectangle the copy fills.</param>
	/// <param name="source">The DC to copy from.</param>
	/// <param name="sourceRect">The rectangle that is copied.</param>
	/// <param name="logicalFunction">How the copy combines with what is there.</param>
	/// <param name="useMask">Whether to leave out what the source's bitmap masks.</param>
	/// <param name="sourceMask">The top left corner of the mask, when it isn't <paramref name="sourceRect"/>'s.</param>
	public bool StretchBlit(Rect destination, DC source, Rect sourceRect, RasterOperationMode logicalFunction = RasterOperationMode.Copy, bool useMask = false, Point? sourceMask = null) {
		ArgumentNullException.ThrowIfNull(source);
		Point mask = sourceMask ?? Point.Default;
		return WxKitNative.DCStretchBlit(Native, destination.X, destination.Y, destination.Width, destination.Height, source.Native, sourceRect.X, sourceRect.Y, sourceRect.Width, sourceRect.Height, (int)logicalFunction, useMask ? (byte)1 : (byte)0, mask.X, mask.Y) != 0;
	}

	/// <summary>Grows the bounding box, which holds everything drawn, to hold a point.</summary>
	public void CalcBoundingBox(Point point) => WxKitNative.DCCalcBoundingBox(Native, point.X, point.Y);

	/// <summary>Draws a curve from the first point to the third, bent toward the second.</summary>
	public void DrawSpline(Point start, Point control, Point end) => WxKitNative.DCDrawSpline(Native, start.X, start.Y, control.X, control.Y, end.X, end.Y);
}

public unsafe partial class Image {
	/// <summary>Copies another image onto this one, with its top left corner at a pixel.</summary>
	public void Paste(Image image, Point position, ImageAlphaBlendMode alphaBlend = ImageAlphaBlendMode.Over) {
		ArgumentNullException.ThrowIfNull(image);
		WxKitNative.ImagePaste(Native, image.Native, position.X, position.Y, (int)alphaBlend);
	}
}

public unsafe partial class Printout {
	/// <summary>Puts the logical origin at a point, in the logical units in use, such as to draw from a page's margin.</summary>
	public void SetLogicalOrigin(Point origin) => WxKitNative.PrintoutSetLogicalOrigin(Native, origin.X, origin.Y);

	/// <summary>Moves the logical origin by an offset, in the logical units in use.</summary>
	public void OffsetLogicalOrigin(Point offset) => WxKitNative.PrintoutOffsetLogicalOrigin(Native, offset.X, offset.Y);
}

public unsafe partial class StyledTextCtrl {
	/// <summary>The position of the character nearest a point, in the control's client coordinates.</summary>
	public int CharPositionFromPoint(Point point) => WxKitNative.StyledTextCtrlCharPositionFromPoint(Native, point.X, point.Y);

	/// <summary>The position of the character at a point, or -1 when the point isn't on a character.</summary>
	public int CharPositionFromPointClose(Point point) => WxKitNative.StyledTextCtrlCharPositionFromPointClose(Native, point.X, point.Y);

	/// <summary>The position at a point, or -1 when the point isn't close to any text.</summary>
	public int PositionFromPointClose(Point point) => WxKitNative.StyledTextCtrlPositionFromPointClose(Native, point.X, point.Y);
}

public unsafe partial class ToolBar {
	/// <summary>The tool at a point, in the toolbar's client coordinates, or null when none is there.</summary>
	public ToolBarTool? FindToolForPosition(Point point) => ToolBarTool.Wrap(WxKitNative.ToolBarFindToolForPosition(Native, point.X, point.Y));
}

public unsafe partial class Window {
	/// <summary>Moves the pointer to a point, in the window's client coordinates.</summary>
	public void WarpPointer(Point point) => WxKitNative.WindowWarpPointer(Native, point.X, point.Y);
}
