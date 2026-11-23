using WxKit.Interop;

namespace WxKit;

// wxWidgets does not free notifications on its own, so the application does, with Dispose, as it
// does timers. One that is never disposed lasts as long as the application.
public sealed unsafe partial class NotificationMessage : IDisposable {
	/// <summary>A timeout for <see cref="Show"/> that hides the notification when the system would. Wraps Timeout_Auto.</summary>
	public const int TimeoutAuto = -1;

	/// <summary>A timeout for <see cref="Show"/> that keeps the notification until it is closed. Wraps Timeout_Never.</summary>
	public const int TimeoutNever = 0;

	/// <summary>Deletes the notification, which hides it if it is showing.</summary>
	public void Dispose() {
		if (!IsDestroyed) WxKitNative.ObjectFree((WxkObject*)NativeHandler);
	}
}
