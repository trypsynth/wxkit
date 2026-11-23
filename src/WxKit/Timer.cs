using WxKit.Interop;

namespace WxKit;

/// <summary>
/// Raises <see cref="Tick"/> on the GUI thread after an interval, once or repeatedly. Wraps wxTimer.
/// Dispose it when you are done with it; wxWidgets does not free timers on its own.
/// </summary>
public sealed unsafe class Timer : EvtHandler, IDisposable {
	public Timer() : base(Create()) {
	}

	public bool IsRunning => WxKitNative.TimerIsRunning(Handle) != 0;

	public event EventHandler<TimerEvent>? Tick {
		add => AddHandler(EventType.Timer, StandardId.Any, StandardId.Any, value, this);
		remove => RemoveHandler(EventType.Timer, StandardId.Any, StandardId.Any, value);
	}

	/// <summary>Starts the timer, or restarts it if it is running. Returns false when the system could not start it.</summary>
	public bool Start(TimeSpan interval, bool oneShot = false) {
		ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(interval, TimeSpan.Zero);
		int milliseconds = checked((int)Math.Ceiling(interval.TotalMilliseconds));
		return (oneShot ? WxKitNative.TimerStartOnce(Handle, milliseconds) : WxKitNative.TimerStart(Handle, milliseconds)) != 0;
	}

	public void Stop() => WxKitNative.TimerStop(Handle);

	public void Dispose() {
		if (!IsDestroyed) WxKitNative.ObjectFree((WxkObject*)Handle);
	}

	private static WxkEvtHandler* Create() {
		Application.VerifyAccess();
		WxkEvtHandler* timer = WxKitNative.TimerNewDefault();
		// A timer without an owner has nowhere to send its events, so it sends them to itself, where
		// WxKit binds them like any other handler's.
		if (timer is not null) WxKitNative.TimerSetOwner(timer, timer, -1);
		return timer;
	}
}
