using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WxKit.Interop;

namespace WxKit;

/// <summary>
/// Describes a window to screen readers and other assistive technology, through Microsoft Active
/// Accessibility. Wraps wxAccessible. Derive from it to describe a control that draws itself, such
/// as a grid or a game board, whose parts Windows cannot see.
/// </summary>
/// <remarks>
/// <para>
/// Each member answers a question about the window itself, when the id is 0, or about one of its
/// parts, numbered from 1. A member returns null when it has no answer, and Windows then answers
/// from the window's standard accessible object, so a class only overrides what the control needs.
/// Standard controls are accessible already.
/// </para>
/// <para>
/// A member that throws <see cref="ArgumentOutOfRangeException"/> tells the screen reader that it
/// has no such part. Any other exception goes to <see cref="Application.UnhandledException"/>, and
/// the screen reader is told that the question failed.
/// </para>
/// <para>Microsoft Active Accessibility is Windows's, so on other platforms, nothing asks.</para>
/// </remarks>
public abstract unsafe class Accessible : IDirected {
	private const int Failed = 0;
	private const int False = 1;
	private const int Ok = 2;
	private const int NotImplemented = 3;
	private const int NotSupported = 4;
	private const int InvalidArgument = 5;
	private static bool _directorsSet;
	private WxkAccessible* _native;
	private Window? _window;

	/// <summary>Makes an accessible object, which describes the window whose <see cref="Window.Accessible"/> is set to it.</summary>
	protected Accessible() {
	}

	/// <summary>The window this describes. Throws while no window has it, which is never while a member is asked something.</summary>
	public Window Window => _window ?? throw new InvalidOperationException("No window has the accessible object. Set a window's Accessible to it.");

	// Makes a window's accessible object this one, in place of the one it had.
	internal void Give(Window window) {
		Application.VerifyAccess();
		if (_window is not null) throw new InvalidOperationException("The accessible object is another window's already.");
		WxkWindow* owner = window.Native;
		// Only Windows asks, through Microsoft Active Accessibility. Elsewhere the window keeps the
		// accessibility its port gives it.
		if (!OperatingSystem.IsWindows()) {
			window.Accessible?.Forget();
			_window = window;
			window.KeepAccessible(this);
			return;
		}
		SetDirectors();
		GCHandle handle = GCHandle.Alloc(this);
		WxkAccessible* native = WxKitNative.AccessibleNew((void*)GCHandle.ToIntPtr(handle), owner);
		if (native is null) {
			handle.Free();
			throw new InvalidOperationException("The window could not be given an accessible object.");
		}
		_native = native;
		_window = window;
		// The window deletes the one it had, which OnNativeFreed hears of, and forgets.
		if (WxKitNative.WindowGiveAccessible(owner, native) == 0) {
			// Deleting it calls OnNativeFreed, which forgets it, and frees the handle.
			WxKitNative.ObjectFree((WxkObject*)native);
			throw new InvalidOperationException("The window could not be given an accessible object.");
		}
		window.KeepAccessible(this);
	}

	// Gives the window back its standard accessible object.
	internal void TakeFrom(Window window) {
		Application.VerifyAccess();
		if (_native is not null) WxKitNative.WindowGiveAccessible(window.Native, null);
		else Forget();
	}

	private void Forget() {
		Window? window = _window;
		_window = null;
		if (ReferenceEquals(window?.Accessible, this)) window.KeepAccessible(null);
	}

	internal WxkAccessible* Native => _native is not null ? _native : throw new InvalidOperationException("The window this accessible object described has been destroyed, or given another one.");

	/// <summary>
	/// Tells screen readers that something changed, such as the focus moving to another part, or a
	/// part's name or state changing. Wraps wxAccessible::NotifyEvent.
	/// </summary>
	/// <param name="eventType">What changed, such as <see cref="AccessibleEvent.ObjectFocus"/>.</param>
	/// <param name="window">The window it changed in.</param>
	/// <param name="objectType">Which of the window's objects, usually <see cref="AccessibleObjectId.Client"/>.</param>
	/// <param name="objectId">The part that changed, or 0 for the window itself.</param>
	public static void NotifyEvent(AccessibleEvent eventType, Window window, AccessibleObjectId objectType, int objectId) {
		ArgumentNullException.ThrowIfNull(window);
		Application.VerifyAccess();
		WxKitNative.AccessibleNotifyEvent((int)eventType, window.Native, (int)objectType, objectId);
	}

	/// <summary>
	/// Tells screen readers that something changed in the window's client area, where a control's parts
	/// are, as <see cref="NotifyEvent(AccessibleEvent, Window, AccessibleObjectId, int)"/> does with
	/// <see cref="AccessibleObjectId.Client"/>.
	/// </summary>
	/// <param name="eventType">What changed, such as <see cref="AccessibleEvent.ObjectFocus"/>.</param>
	/// <param name="window">The window it changed in.</param>
	/// <param name="objectId">The part that changed, or 0 for the window itself.</param>
	public static void NotifyEvent(AccessibleEvent eventType, Window window, int objectId) => NotifyEvent(eventType, window, AccessibleObjectId.Client, objectId);

	/// <summary>The name of the window or a part, which a screen reader says first, such as a cell's text.</summary>
	protected virtual string? GetName(int childId) => null;

	/// <summary>A longer description of the window or a part.</summary>
	protected virtual string? GetDescription(int childId) => null;

	/// <summary>Help for the window or a part, like a tooltip.</summary>
	protected virtual string? GetHelpText(int childId) => null;

	/// <summary>The keyboard shortcut of the window or a part, such as "Alt+K".</summary>
	protected virtual string? GetKeyboardShortcut(int childId) => null;

	/// <summary>
	/// What the window's or a part's default action is, such as "Press" or "Check". It describes the
	/// action, not its result: a button that prints has the action "Press", not "Print". An empty
	/// string means that it has none.
	/// </summary>
	protected virtual string? GetDefaultAction(int childId) => null;

	/// <summary>The value of the window or a part, such as a slider's position or a cell's contents.</summary>
	protected virtual string? GetValue(int childId) => null;

	/// <summary>What kind of element the window or a part is, such as a cell or a button.</summary>
	protected virtual AccessibleRole? GetRole(int childId) => null;

	/// <summary>The state of the window or a part, such as focused or selected.</summary>
	protected virtual AccessibleStates? GetState(int childId) => null;

	/// <summary>How many parts the window has.</summary>
	protected virtual int? GetChildCount() => null;

	/// <summary>Where the window or a part is, in screen coordinates.</summary>
	/// <param name="elementId">0 for the window, or the part.</param>
	protected virtual Rect? GetLocation(int elementId) => null;

	/// <summary>
	/// A part, numbered from 1: <see cref="AccessibleElement.Child"/> for a simple part, which this
	/// object describes itself, or <see cref="AccessibleElement.Of"/> for one that has its own
	/// accessible object.
	/// </summary>
	protected virtual AccessibleElement? GetChild(int childId) => null;

	/// <summary>
	/// What has the keyboard focus: <see cref="AccessibleElement.Self"/>, a part, or
	/// <see cref="AccessibleElement.None"/> when nothing in the window does.
	/// </summary>
	protected virtual AccessibleElement? GetFocus() => null;

	/// <summary>What is at a point on the screen, or <see cref="AccessibleElement.None"/> when the point is outside the window.</summary>
	/// <param name="point">The point, in screen coordinates.</param>
	protected virtual AccessibleElement? HitTest(Point point) => null;

	/// <summary>The element in a direction from the window or a part, or <see cref="AccessibleElement.None"/> when there is none.</summary>
	protected virtual AccessibleElement? Navigate(NavigationDirection direction, int fromId) => null;

	/// <summary>Does the default action of the window or a part. Returns false when it has none.</summary>
	protected virtual bool? DoDefaultAction(int childId) => null;

	/// <summary>Selects the window or a part, or moves the focus to it, as the flags say. Returns false when it cannot.</summary>
	protected virtual bool? Select(int childId, AccessibleSelection flags) => null;

	// The callbacks every accessible object's director calls, with the userdata it was made with,
	// which Directors frees when the window deletes the object.
	private static void SetDirectors() {
		if (_directorsSet) return;
		_directorsSet = true;
		Directors.Install();
		WxKitNative.AccessibleSetDirectors(&AnswerName, &AnswerDescription, &AnswerHelpText, &AnswerKeyboardShortcut, &AnswerDefaultAction, &AnswerValue, &AnswerRole, &AnswerState, &AnswerChildCount, &AnswerLocation, &AnswerChild, &AnswerFocus, &AnswerHitTest, &AnswerNavigate, &AnswerDoDefaultAction, &AnswerSelect);
	}

	// The window deleted its accessible object, as it does when it is destroyed or given another.
	void IDirected.OnNativeFreed() {
		_native = null;
		Forget();
	}

	private static Accessible? Of(void* userdata) => (Accessible?)GCHandle.FromIntPtr((nint)userdata).Target;

	// A part that does not exist is an invalid argument, which is the screen reader's to handle.
	// Anything else is the application's.
	private static int Fail(Exception error) {
		if (error is ArgumentOutOfRangeException) return InvalidArgument;
		Application.ReportException(error);
		return Failed;
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static int AnswerName(WxkAccessible* self, void* userdata, int childId, WxkString** name) => Text(userdata, name, accessible => accessible.GetName(childId));

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static int AnswerDescription(WxkAccessible* self, void* userdata, int childId, WxkString** description) => Text(userdata, description, accessible => accessible.GetDescription(childId));

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static int AnswerHelpText(WxkAccessible* self, void* userdata, int childId, WxkString** helpText) => Text(userdata, helpText, accessible => accessible.GetHelpText(childId));

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static int AnswerKeyboardShortcut(WxkAccessible* self, void* userdata, int childId, WxkString** shortcut) => Text(userdata, shortcut, accessible => accessible.GetKeyboardShortcut(childId));

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static int AnswerDefaultAction(WxkAccessible* self, void* userdata, int childId, WxkString** action) => Text(userdata, action, accessible => accessible.GetDefaultAction(childId));

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static int AnswerValue(WxkAccessible* self, void* userdata, int childId, WxkString** value) => Text(userdata, value, accessible => accessible.GetValue(childId));

	private static int Text(void* userdata, WxkString** text, Func<Accessible, string?> ask) {
		try {
			if (Of(userdata) is not Accessible accessible || ask(accessible) is not string value) return NotImplemented;
			*text = NativeString.Create(value);
			return Ok;
		} catch (Exception error) {
			return Fail(error);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static int AnswerRole(WxkAccessible* self, void* userdata, int childId, int* role) {
		try {
			if (Of(userdata)?.GetRole(childId) is not AccessibleRole value) return NotImplemented;
			*role = (int)value;
			return Ok;
		} catch (Exception error) {
			return Fail(error);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static int AnswerState(WxkAccessible* self, void* userdata, int childId, long* state) {
		try {
			if (Of(userdata)?.GetState(childId) is not AccessibleStates value) return NotImplemented;
			*state = (long)value;
			return Ok;
		} catch (Exception error) {
			return Fail(error);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static int AnswerChildCount(WxkAccessible* self, void* userdata, int* count) {
		try {
			if (Of(userdata)?.GetChildCount() is not int value) return NotImplemented;
			*count = value;
			return Ok;
		} catch (Exception error) {
			return Fail(error);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static int AnswerLocation(WxkAccessible* self, void* userdata, Rect* rect, int elementId) {
		try {
			if (Of(userdata)?.GetLocation(elementId) is not Rect value) return NotImplemented;
			*rect = value;
			return Ok;
		} catch (Exception error) {
			return Fail(error);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static int AnswerChild(WxkAccessible* self, void* userdata, int childId, WxkAccessible** child) => Element(userdata, ElementKind.Child, null, child, accessible => accessible.GetChild(childId));

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static int AnswerFocus(WxkAccessible* self, void* userdata, int* childId, WxkAccessible** child) => Element(userdata, ElementKind.Focus, childId, child, static accessible => accessible.GetFocus());

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static int AnswerHitTest(WxkAccessible* self, void* userdata, Point point, int* childId, WxkAccessible** child) => Element(userdata, ElementKind.HitTest, childId, child, accessible => accessible.HitTest(point));

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static int AnswerNavigate(WxkAccessible* self, void* userdata, int direction, int fromId, int* toId, WxkAccessible** toObject) => Element(userdata, ElementKind.Navigate, toId, toObject, accessible => accessible.Navigate((NavigationDirection)direction, fromId));

	private enum ElementKind {
		Child,
		Focus,
		HitTest,
		Navigate,
	}

	// A part comes back as its id, and an element with its own accessible object as that object.
	// The window itself, as the focus, the element at a point, or where navigation leads, is the
	// window's own object, since wxWidgets reads an id of 0 without one as nothing there. An element
	// that is not there is not a failure: a point outside the window is false, as is a direction
	// with nothing in it, and a window without the focus is fine. A child is only asked for as an
	// object, so it has no id to give.
	private static int Element(void* userdata, ElementKind kind, int* resultId, WxkAccessible** resultObject, Func<Accessible, AccessibleElement?> ask) {
		if (resultId is not null) *resultId = 0;
		try {
			if (Of(userdata) is not Accessible accessible || ask(accessible) is not AccessibleElement element) return NotImplemented;
			if (element.IsNone) {
				return kind switch {
					ElementKind.Focus => Ok,
					ElementKind.Child => InvalidArgument,
					_ => False,
				};
			}
			if (element.Object is Accessible other) {
				*resultObject = other.Native;
			} else {
				if (resultId is not null) *resultId = element.ChildId;
				if (kind != ElementKind.Child && element.ChildId == 0) *resultObject = accessible.Native;
			}
			return Ok;
		} catch (Exception error) {
			return Fail(error);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static int AnswerDoDefaultAction(WxkAccessible* self, void* userdata, int childId) => Act(userdata, accessible => accessible.DoDefaultAction(childId));

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static int AnswerSelect(WxkAccessible* self, void* userdata, int childId, int flags) => Act(userdata, accessible => accessible.Select(childId, (AccessibleSelection)flags));

	private static int Act(void* userdata, Func<Accessible, bool?> act) {
		try {
			if (Of(userdata) is not Accessible accessible) return NotImplemented;
			return act(accessible) switch {
				null => NotImplemented,
				true => Ok,
				false => NotSupported,
			};
		} catch (Exception error) {
			return Fail(error);
		}
	}
}

/// <summary>
/// An element an <see cref="Accessible"/> points a screen reader to: the window itself, one of its
/// parts, an element with an accessible object of its own, or nothing.
/// </summary>
public readonly struct AccessibleElement {
	private AccessibleElement(int childId, Accessible? accessible, bool isNone) {
		ChildId = childId;
		Object = accessible;
		IsNone = isNone;
	}

	/// <summary>The window itself.</summary>
	public static AccessibleElement Self => default;

	/// <summary>No element, such as for a point outside the window.</summary>
	public static AccessibleElement None { get; } = new(0, null, isNone: true);

	/// <summary>The part, numbered from 1, or 0 for the window itself.</summary>
	public int ChildId { get; }

	/// <summary>The element's own accessible object, when it has one.</summary>
	public Accessible? Object { get; }

	public bool IsNone { get; }

	/// <summary>A part the accessible object describes itself, numbered from 1.</summary>
	public static AccessibleElement Child(int childId) {
		ArgumentOutOfRangeException.ThrowIfNegative(childId);
		return new(childId, null, isNone: false);
	}

	/// <summary>An element with an accessible object of its own, such as a child window's.</summary>
	public static AccessibleElement Of(Accessible accessible) {
		ArgumentNullException.ThrowIfNull(accessible);
		return new(0, accessible, isNone: false);
	}
}

// The accessible object WxKit gave a window, if any. It's named after its type, which C# allows, so
// Accessible.NotifyEvent still names the class inside a control.
public partial class Window {
	private Accessible? _accessible;

	/// <summary>
	/// The object that describes the window to screen readers, of a class derived from
	/// <see cref="WxKit.Accessible"/>, or null when it has the standard one. An accessible object is
	/// one window's at a time, and the window lets it go when it is destroyed.
	/// </summary>
	public Accessible? Accessible {
		get => _accessible;
		set {
			if (ReferenceEquals(value, _accessible)) return;
			if (value is not null) value.Give(this);
			else _accessible?.TakeFrom(this);
		}
	}

	internal void KeepAccessible(Accessible? accessible) => _accessible = accessible;
}
