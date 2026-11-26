using WxKit.Interop;

namespace WxKit;

// wxWidgets does not free notifications on its own, so the application does, with Dispose, as it
// does timers. One that is never disposed lasts as long as the application.
public sealed unsafe partial class NotificationMessage : IDisposable {
	/// <summary>
	/// Shows the notification, and hides it after <paramref name="timeout"/>, which the system
	/// rounds to seconds, and may ignore. Without one, the system hides it when it would any other,
	/// and with <see cref="Timeout.InfiniteTimeSpan"/> it stays until the user closes it.
	/// </summary>
	public bool Show(TimeSpan? timeout = null) {
		Application.VerifyAccess();
		// wxWidgets takes seconds, or -1 for the system's own timeout, or 0 for none.
		int seconds = -1;
		if (timeout == Timeout.InfiniteTimeSpan) {
			seconds = 0;
		} else if (timeout is TimeSpan time) {
			ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(time, TimeSpan.Zero, nameof(timeout));
			// A timeout under a second is a second, since none is wxWidgets's way of saying never.
			seconds = Math.Max(1, checked((int)Math.Round(time.TotalSeconds)));
		}
		return WxKitNative.NotificationMessageShow(NativeHandler, seconds) != 0;
	}

	/// <summary>Deletes the notification, which hides it if it is showing.</summary>
	public void Dispose() {
		if (!IsDestroyed) WxKitNative.ObjectFree((WxkObject*)NativeHandler);
	}
}
