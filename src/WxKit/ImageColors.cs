using WxKit.Interop;

namespace WxKit;

// An image's colors are Colors, and its pixels are at Points, where wxWidgets takes a color as
// three bytes and a pixel as x and y. A pixel's alpha is apart from its color, as in wxWidgets.

public unsafe partial class Image {
	/// <summary>Gives the color of a pixel, without its alpha, which <see cref="GetAlpha"/> gives. Wraps GetRed, GetGreen, and GetBlue.</summary>
	public Color GetRgb(Point pixel) => new(WxKitNative.ImageGetRed(Native, pixel.X, pixel.Y), WxKitNative.ImageGetGreen(Native, pixel.X, pixel.Y), WxKitNative.ImageGetBlue(Native, pixel.X, pixel.Y));

	/// <summary>Sets the color of a pixel. The color's alpha isn't used: <see cref="SetAlpha"/> sets a pixel's. Wraps SetRGB.</summary>
	public void SetRgb(Point pixel, Color color) => WxKitNative.ImageSetRGBIntIntByteByteByte(Native, pixel.X, pixel.Y, color.R, color.G, color.B);

	/// <summary>Sets the color of every pixel in a rectangle. Wraps SetRGB.</summary>
	public void SetRgb(Rect rect, Color color) => WxKitNative.ImageSetRGBRectByteByteByte(Native, rect, color.R, color.G, color.B);

	/// <summary>Gives how opaque a pixel is, from 0, which is see-through, to 255, in an image that has alpha.</summary>
	public byte GetAlpha(Point pixel) => WxKitNative.ImageGetAlpha(Native, pixel.X, pixel.Y);

	/// <summary>Sets how opaque a pixel is, from 0, which is see-through, to 255, in an image that has alpha.</summary>
	public void SetAlpha(Point pixel, byte alpha) => WxKitNative.ImageSetAlpha(Native, pixel.X, pixel.Y, alpha);

	/// <summary>Whether a pixel is see-through, by the image's mask or by its alpha.</summary>
	public bool IsTransparent(Point pixel) => WxKitNative.ImageIsTransparent(Native, pixel.X, pixel.Y) != 0;

	/// <summary>Changes every pixel of one color to another.</summary>
	public void Replace(Color from, Color to) => WxKitNative.ImageReplace(Native, from.R, from.G, from.B, to.R, to.G, to.B);

	/// <summary>Returns a copy in black and white, in which the pixels of this color are white and the rest are black.</summary>
	public Image? ConvertToMono(Color white) => Take(WxKitNative.ImageConvertToMono(Native, white.R, white.G, white.B));

	/// <summary>Makes a mask of the image's alpha, with this color as the mask's, and returns whether it could.</summary>
	public bool ConvertAlphaToMask(Color mask) => WxKitNative.ImageConvertAlphaToMaskByteByteByte(Native, mask.R, mask.G, mask.B) != 0;

	/// <summary>Makes a mask from another image of the same size, in which the pixels of this color are see-through, and returns whether it could.</summary>
	public bool SetMaskFromImage(Image mask, Color color) {
		ArgumentNullException.ThrowIfNull(mask);
		return WxKitNative.ImageSetMaskFromImage(Native, mask.Native, color.R, color.G, color.B) != 0;
	}

	/// <summary>
	/// Changes the image's size without scaling it, by cutting it or by adding a border.
	/// <see cref="Resized"/> returns a copy.
	/// </summary>
	/// <param name="size">The new size.</param>
	/// <param name="position">Where the image goes in the new size.</param>
	/// <param name="border">The border's color. Left out, the border is the mask's color, or see-through in an image with alpha, or black.</param>
	public void Resize(Size size, Point position, Color? border = null) => WxKitNative.ImageResize(Native, size, position, border?.R ?? -1, border?.G ?? -1, border?.B ?? -1);

	/// <summary>Returns a copy of another size, without scaling it, by cutting it or by adding a border, as <see cref="Resize"/> changes this image.</summary>
	public Image? Resized(Size size, Point position, Color? border = null) => Take(WxKitNative.ImageSize(Native, size, position, border?.R ?? -1, border?.G ?? -1, border?.B ?? -1));
}
