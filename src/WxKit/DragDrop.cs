using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WxKit.Interop;

namespace WxKit;

/// <summary>
/// A kind of data a drag can carry: text, HTML, a bitmap, a list of files, or a format the
/// application names, such as "MyApp.Shape", which only applications that know it take.
/// </summary>
public readonly record struct DataFormat {
	private DataFormat(int kind, string? name) {
		Kind = kind;
		Name = name;
	}

	/// <summary>Plain text.</summary>
	public static DataFormat Text { get; } = new(0, null);

	/// <summary>A bitmap.</summary>
	public static DataFormat Bitmap { get; } = new(1, null);

	/// <summary>A list of files, such as ones dragged from a file manager.</summary>
	public static DataFormat Files { get; } = new(2, null);

	/// <summary>An HTML fragment.</summary>
	public static DataFormat Html { get; } = new(3, null);

	internal int Kind { get; }

	/// <summary>The name of a format the application names, or null for a standard one.</summary>
	public string? Name { get; }

	/// <summary>A format the application names, the same name as <see cref="ClipboardContent.Add"/> takes.</summary>
	public static DataFormat Named(string name) {
		ArgumentException.ThrowIfNullOrEmpty(name);
		return new(4, name);
	}

	internal static unsafe DataFormat FromNative(int kind, char* name, nuint nameLength) => kind == 4 ? Named(new string(name, 0, checked((int)nameLength))) : new(kind, null);

	/// <summary>The format's name, or Text, Bitmap, Files, or Html.</summary>
	public override string ToString() => Name ?? Kind switch {
		0 => "Text",
		1 => "Bitmap",
		2 => "Files",
		_ => "Html",
	};
}

/// <summary>Starts dragging content out of a window. Wraps wxDropSource.</summary>
public static unsafe class DragDrop {
	/// <summary>
	/// Drags <paramref name="content"/> from <paramref name="source"/> until it is dropped or the
	/// drag is cancelled, which the user does with Escape. Call it from a handler of the mouse
	/// moving with a button held, such as a list's BeginDrag. It returns what the target did: when
	/// it is <see cref="DragResult.Move"/>, remove what was dragged from the source.
	/// </summary>
	/// <param name="source">The window the drag starts in.</param>
	/// <param name="content">What the drag carries, in each format the application can give it, so each target takes one it understands.</param>
	/// <param name="flags">Whether a target may move the content, rather than only copy it.</param>
	public static DragResult DoDragDrop(Window source, ClipboardContent content, DragFlags flags = DragFlags.CopyOnly) {
		ArgumentNullException.ThrowIfNull(source);
		ArgumentNullException.ThrowIfNull(content);
		WxkWindow* window = source.Native;
		if (content.IsEmpty) throw new ArgumentException("The content has no formats.", nameof(content));
		return (DragResult)WxKitNative.DragDropDoDragDrop(window, content.CreateNative(), (int)flags);
	}
}

/// <summary>
/// Takes content dropped on a window, in the formats given. Wraps wxDropTarget. Set a window's
/// <see cref="Window.DropTarget"/> to it, and the window keeps it until it is destroyed, or given
/// another, or null. To take only files, <see cref="Window.DragAcceptFiles"/> and its DropFiles
/// event are enough.
/// </summary>
/// <example>
/// <code>
/// DropTarget target = new(DataFormat.Text, DataFormat.Files);
/// target.Drop += (_, e) => list.Append(e.Text ?? string.Join(", ", e.Files!));
/// list.DropTarget = target;
/// </code>
/// </example>
public sealed unsafe class DropTarget {
	private static bool _callbacksSet;

	/// <summary>Makes a target that takes the formats given.</summary>
	/// <param name="formats">The formats it takes. A drag that has several arrives in the first of text, HTML, a bitmap, files, and then the named formats in order.</param>
	public DropTarget(params DataFormat[] formats) {
		ArgumentNullException.ThrowIfNull(formats);
		if (formats.Length == 0) throw new ArgumentException("A drop target takes at least one format.", nameof(formats));
		Formats = [.. formats.Distinct()];
	}

	/// <summary>The window content is dropped on, or null while no window has the target.</summary>
	public Window? Window { get; private set; }

	// Makes a window's drop target this one, in place of any it had.
	internal void Give(Window window) {
		if (Window is not null) throw new InvalidOperationException("The drop target is another window's already.");
		WxkWindow* native = window.Native;
		uint kinds = 0;
		foreach (DataFormat format in Formats.Where(static format => format.Name is null)) kinds |= 1u << format.Kind;
		string[] named = [.. Formats.Select(static format => format.Name).OfType<string>()];
		SetCallbacks();
		GCHandle handle = GCHandle.Alloc(this);
		using NativeStringArray names = new(named);
		// The window deletes the target it had, which OnFree hears of, and forgets.
		if (WxKitNative.DropTargetSet(native, kinds, names.Handle, (void*)GCHandle.ToIntPtr(handle)) == 0) {
			handle.Free();
			throw new InvalidOperationException("The window could not be made a drop target.");
		}
		Window = window;
		window.KeepDropTarget(this);
	}

	/// <summary>The formats the target takes.</summary>
	public IReadOnlyList<DataFormat> Formats { get; }

	/// <summary>
	/// Raised when a drag the target can take comes into the window. Set the event's
	/// <see cref="DragEventArgs.Effect"/> to <see cref="DragResult.None"/> to refuse it.
	/// </summary>
	public event EventHandler<DragEventArgs>? DragEnter;

	/// <summary>Raised as the drag moves over the window, such as to refuse a drop on part of it, or to show where it would go.</summary>
	public event EventHandler<DragEventArgs>? DragOver;

	/// <summary>Raised when the drag leaves the window, or is cancelled, without a drop.</summary>
	public event EventHandler? DragLeave;

	/// <summary>Raised when content is dropped, with the content. Set the event's Effect to say what was done with it, such as <see cref="DragResult.None"/> when nothing was.</summary>
	public event EventHandler<DropEventArgs>? Drop;

	// For tests: drags content over the target and drops it, as the system does, without its drag loop.
	internal DragResult TestDrop(ClipboardContent content, Point position, DragResult suggested = DragResult.Copy) {
		WxkWindow* window = Window is not null ? Window.Native : throw new InvalidOperationException("No window has the drop target.");
		return (DragResult)WxKitNative.DropTargetTestDrop(window, content.CreateNative(), position.X, position.Y, (int)suggested);
	}

	private static void SetCallbacks() {
		if (_callbacksSet) return;
		_callbacksSet = true;
		WxkDropTargetCallbacks callbacks = new() {
			OnDrag = &OnDrag,
			OnLeave = &OnLeave,
			OnDrop = &OnDrop,
			OnFree = &OnFree,
		};
		WxKitNative.DropTargetSetCallbacks(&callbacks);
	}

	private static DropTarget? Of(void* userdata) => (DropTarget?)GCHandle.FromIntPtr((nint)userdata).Target;

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static int OnDrag(void* userdata, int stage, int x, int y, int suggested, int kind, char* name, nuint nameLength) {
		try {
			if (Of(userdata) is not DropTarget target) return suggested;
			DragEventArgs args = new(new Point(x, y), DataFormat.FromNative(kind, name, nameLength), (DragResult)suggested);
			(stage == 0 ? target.DragEnter : target.DragOver)?.Invoke(target, args);
			return (int)args.Effect;
		} catch (Exception error) {
			Application.ReportException(error);
			return (int)DragResult.None;
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void OnLeave(void* userdata) {
		try {
			if (Of(userdata) is DropTarget target) target.DragLeave?.Invoke(target, EventArgs.Empty);
		} catch (Exception error) {
			Application.ReportException(error);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static int OnDrop(void* userdata, int x, int y, int suggested, int kind, char* name, nuint nameLength, WxkDropData* data) {
		try {
			if (Of(userdata) is not DropTarget target) return (int)DragResult.None;
			DropEventArgs args = new(new Point(x, y), DataFormat.FromNative(kind, name, nameLength), (DragResult)suggested, data);
			target.Drop?.Invoke(target, args);
			return (int)args.Effect;
		} catch (Exception error) {
			Application.ReportException(error);
			return (int)DragResult.None;
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void OnFree(void* userdata) {
		GCHandle handle = GCHandle.FromIntPtr((nint)userdata);
		if (handle.Target is DropTarget target) {
			// The window deleted it, as it does when it is destroyed, or given another, or none.
			if (ReferenceEquals(target.Window?.DropTarget, target)) target.Window.KeepDropTarget(null);
			target.Window = null;
		}
		handle.Free();
	}
}

public unsafe partial class Window {
	private DropTarget? _dropTarget;

	/// <summary>
	/// What takes content dropped on the window, or null when nothing does. A target is one
	/// window's at a time, and the window lets it go when it is destroyed.
	/// </summary>
	public DropTarget? DropTarget {
		get => _dropTarget;
		set {
			if (ReferenceEquals(value, _dropTarget)) return;
			if (value is not null) value.Give(this);
			else WxKitNative.DropTargetRemove(Native);
		}
	}

	internal void KeepDropTarget(DropTarget? target) => _dropTarget = target;
}

/// <summary>A drag over a drop target: where it is, the format its content would arrive in, and what dropping it would do.</summary>
public class DragEventArgs : EventArgs {
	internal DragEventArgs(Point position, DataFormat format, DragResult effect) {
		Position = position;
		Format = format;
		Effect = effect;
	}

	/// <summary>Where the pointer is, in the window's client coordinates.</summary>
	public Point Position { get; }

	/// <summary>The format the content would arrive in, the first the target takes that the drag has.</summary>
	public DataFormat Format { get; }

	/// <summary>
	/// What dropping does, which the pointer shows: <see cref="DragResult.Copy"/>,
	/// <see cref="DragResult.Move"/>, or <see cref="DragResult.Link"/>, or
	/// <see cref="DragResult.None"/> to refuse. It starts as what the system suggests, from the
	/// keys held and what the source allows.
	/// </summary>
	public DragResult Effect { get; set; }
}

/// <summary>Content dropped on a drop target, in the one format it arrived in.</summary>
public sealed class DropEventArgs : DragEventArgs {
	internal unsafe DropEventArgs(Point position, DataFormat format, DragResult effect, WxkDropData* data) : base(position, format, effect) {
		if (format == DataFormat.Text) Text = NativeString.Take(WxKitNative.DropDataGetText(data));
		else if (format == DataFormat.Html) Html = NativeString.Take(WxKitNative.DropDataGetText(data));
		else if (format == DataFormat.Bitmap) Bitmap = Bitmap.Take(WxKitNative.DropDataGetBitmap(data));
		else if (format == DataFormat.Files) Files = NativeStringArray.Take(WxKitNative.DropDataGetFiles(data));
		else Data = TakeBytes(data);
	}

	/// <summary>The text dropped, when the format is <see cref="DataFormat.Text"/>.</summary>
	public string? Text { get; }

	/// <summary>The HTML fragment dropped, when the format is <see cref="DataFormat.Html"/>.</summary>
	public string? Html { get; }

	/// <summary>The bitmap dropped, when the format is <see cref="DataFormat.Bitmap"/>.</summary>
	public Bitmap? Bitmap { get; }

	/// <summary>The full paths of the files dropped, when the format is <see cref="DataFormat.Files"/>.</summary>
	public string[]? Files { get; }

	/// <summary>The data dropped, when the format is one the application names.</summary>
	public byte[]? Data { get; }

	private static unsafe byte[]? TakeBytes(WxkDropData* data) {
		nuint size;
		byte* bytes = WxKitNative.DropDataGetBytes(data, &size);
		if (bytes is null) return null;
		try {
			return new ReadOnlySpan<byte>(bytes, checked((int)size)).ToArray();
		} finally {
			WxKitNative.BytesFree(bytes);
		}
	}
}
