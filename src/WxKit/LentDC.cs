using WxKit.Interop;

namespace WxKit;

public unsafe partial class DC {
	private DC(WxkObject* native, bool owns) : base(native, owns) {
	}

	// A DC wxWidgets owns, which it lends for the length of a call, such as a printout's page.
	// Disposing it, when the call returns, only stops it being used.
	internal static DC Lend(WxkObject* native) => new(native, owns: false);

	/// <summary>Where the device's origin is, in device units, which is its top left corner to begin with.</summary>
	public Point DeviceOrigin {
		get => WxKitNative.DCGetDeviceOrigin(Native);
		set => WxKitNative.DCSetDeviceOrigin(Native, value.X, value.Y);
	}

	/// <summary>Where the logical origin is, in logical units.</summary>
	public Point LogicalOrigin {
		get => WxKitNative.DCGetLogicalOrigin(Native);
		set => WxKitNative.DCSetLogicalOrigin(Native, value.X, value.Y);
	}

	/// <summary>How much the application scales what it draws, across and down, which is 1 and 1 to begin with.</summary>
	public (double X, double Y) UserScale {
		get {
			double x = 0;
			double y = 0;
			WxKitNative.DCGetUserScale(Native, &x, &y);
			return (x, y);
		}
		set => WxKitNative.DCSetUserScale(Native, value.X, value.Y);
	}

	/// <summary>How much the device scales logical units, across and down, which the mapping mode sets.</summary>
	public (double X, double Y) LogicalScale {
		get {
			double x = 0;
			double y = 0;
			WxKitNative.DCGetLogicalScale(Native, &x, &y);
			return (x, y);
		}
		set => WxKitNative.DCSetLogicalScale(Native, value.X, value.Y);
	}
}
