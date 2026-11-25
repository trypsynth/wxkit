using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WxKit.Interop;

namespace WxKit;

/// <summary>
/// Anything that receives events: windows, menus, and timers. Wraps wxEvtHandler. Every member
/// must be used on the GUI thread, and throws <see cref="ObjectDisposedException"/> once wxWidgets
/// has destroyed the object, except <see cref="IsDestroyed"/>.
/// </summary>
public abstract unsafe class EvtHandler {
	private WxkEvtHandler* _native;
	private Dictionary<(int Type, int Id, int LastId), EventSlot>? _slots;

	// Every constructor that creates a native object checks the thread before it does, not here,
	// since by the time this runs the object would already exist on the wrong thread.
	private protected EvtHandler(WxkEvtHandler* native) {
		Application.VerifyAccess();
		if (native is null) throw new InvalidOperationException($"wxWidgets could not create the {GetType().Name}.");
		_native = native;
		ObjectRegistry.Register(this, native);
		// A control is named by the label before it, which on Linux WxKit has to add for some, and
		// an icon button inside a window, such as an editable list box's, by its tooltip.
		if (this is Control control and not StaticText && !OperatingSystem.IsWindows() && control.LabelBefore() is StaticText label) WxKitNative.WindowLinkLabel((WxkWindow*)native, label.Native);
		if (this is Window) WxKitNative.WindowNameIconButtons((WxkWindow*)native);
	}

	/// <summary>Whether wxWidgets has destroyed the object, after which nothing else on it can be used.</summary>
	public bool IsDestroyed => _native is null;

	// The same handle, for code that passes another object's to the shim, which Handle cannot give.
	internal WxkEvtHandler* NativeHandler => Handle;

	private protected WxkEvtHandler* Handle {
		get {
			Application.VerifyAccess();
			return _native is not null ? _native : throw new ObjectDisposedException(GetType().Name, $"wxWidgets has destroyed this {GetType().Name}.");
		}
	}

	/// <summary>Calls <paramref name="handler"/> for every event of <paramref name="type"/> this object receives.</summary>
	public void Bind<TEvent>(EventType<TEvent> type, EventHandler<TEvent> handler) where TEvent : Event => Bind(type, StandardId.Any, StandardId.Any, handler);

	/// <summary>Calls <paramref name="handler"/> for events of <paramref name="type"/> from the window or menu item with <paramref name="id"/>.</summary>
	public void Bind<TEvent>(EventType<TEvent> type, int id, EventHandler<TEvent> handler) where TEvent : Event => Bind(type, id, id, handler);

	/// <summary>Calls <paramref name="handler"/> for events of <paramref name="type"/> from ids <paramref name="id"/> to <paramref name="lastId"/>, inclusive.</summary>
	public void Bind<TEvent>(EventType<TEvent> type, int id, int lastId, EventHandler<TEvent> handler) where TEvent : Event {
		ArgumentNullException.ThrowIfNull(type);
		ArgumentNullException.ThrowIfNull(handler);
		AddHandler(type, id, lastId, handler, this);
	}

	public void Unbind<TEvent>(EventType<TEvent> type, EventHandler<TEvent> handler) where TEvent : Event => Unbind(type, StandardId.Any, StandardId.Any, handler);

	public void Unbind<TEvent>(EventType<TEvent> type, int id, EventHandler<TEvent> handler) where TEvent : Event => Unbind(type, id, id, handler);

	public void Unbind<TEvent>(EventType<TEvent> type, int id, int lastId, EventHandler<TEvent> handler) where TEvent : Event {
		ArgumentNullException.ThrowIfNull(type);
		RemoveHandler(type, id, lastId, handler);
	}

	// Objects the native one uses for as long as it exists, such as the data a find dialog reads and
	// writes, which have to stay alive as long as this does.
	private List<object>? _keptAlive;

	internal void KeepAlive(object value) => (_keptAlive ??= []).Add(value);

	// wxWidgets binds one functor per event type and id range, the first time a handler is added,
	// and never unbinds it: once the last handler is removed, the functor skips the event, which
	// leaves wxWidgets to handle it as if nothing were bound. The functor is released, and the
	// slot with it, when wxWidgets destroys this object.
	internal void AddHandler<TEvent>(EventType<TEvent> type, int id, int lastId, EventHandler<TEvent>? handler, object sender) where TEvent : Event {
		if (handler is null) return;
		WxkEvtHandler* native = Handle;
		(int, int, int) key = (type.Value, id, lastId);
		_slots ??= [];
		if (!_slots.TryGetValue(key, out EventSlot? slot)) {
			slot = new EventSlot<TEvent>(type, sender);
			GCHandle handle = GCHandle.Alloc(slot);
			if (WxKitNative.EvtHandlerBind(native, key.Item1, id, lastId, &Dispatch, (void*)GCHandle.ToIntPtr(handle), &Release) == 0) {
				handle.Free();
				throw new InvalidOperationException("wxWidgets could not bind the event.");
			}
			_slots.Add(key, slot);
		}
		((EventSlot<TEvent>)slot).Handlers += handler;
	}

	internal void RemoveHandler<TEvent>(EventType<TEvent> type, int id, int lastId, EventHandler<TEvent>? handler) where TEvent : Event {
		if (handler is null || _native is null) return;
		Application.VerifyAccess();
		if (_slots is not null && _slots.TryGetValue((type.Value, id, lastId), out EventSlot? slot)) ((EventSlot<TEvent>)slot).Handlers -= handler;
	}

	internal void OnNativeDestroyed() {
		_native = null;
		_slots = null;
		OnDestroyed();
	}

	/// <summary>Runs once wxWidgets has destroyed the object, so derived classes can mark what it owned as gone too.</summary>
	private protected virtual void OnDestroyed() {
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void Dispatch(void* userdata, WxkEvent* native) {
		EventSlot slot = (EventSlot)GCHandle.FromIntPtr((nint)userdata).Target!;
		try {
			slot.Dispatch(native);
		} catch (Exception error) {
			Application.ReportException(error);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void Release(void* userdata) => GCHandle.FromIntPtr((nint)userdata).Free();

	private abstract class EventSlot {
		public abstract void Dispatch(WxkEvent* native);
	}

	private sealed class EventSlot<TEvent>(EventType<TEvent> type, object sender) : EventSlot where TEvent : Event {
		public EventHandler<TEvent>? Handlers { get; set; }

		public override void Dispatch(WxkEvent* native) {
			// With no handler left, the event goes on as if nothing were bound, and no object is made for it.
			EventHandler<TEvent>? handlers = Handlers;
			if (handlers is null) {
				WxKitNative.EventSkip(native, 1);
				return;
			}
			TEvent e = type.Create(native);
			try {
				handlers(sender, e);
				e.OnHandled();
			} finally {
				e.Invalidate();
			}
		}
	}
}
