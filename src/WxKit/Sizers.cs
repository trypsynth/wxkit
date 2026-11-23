using WxKit.Interop;

namespace WxKit;

/// <summary>
/// Lays out a window's children, so they move and resize with it. Wraps wxSizer. A sizer belongs
/// to the window it is set on, or the sizer it is added to, and is destroyed with it.
/// </summary>
public abstract unsafe class Sizer {
	private WxkSizer* _native;
	private object? _owner;
	private List<Sizer>? _children;

	private protected Sizer(WxkSizer* native) {
		Application.VerifyAccess();
		_native = native is not null ? native : throw new InvalidOperationException($"wxWidgets could not create the {GetType().Name}.");
	}

	// A sizer nothing owns is freed with its managed object. Its finalizer runs on the finalizer
	// thread, and wxWidgets objects must be freed on the GUI thread, so the free is posted there.
	~Sizer() {
		if (_native is null || _owner is not null) return;
		nint native = (nint)_native;
		Application.TryPost(() => WxKitNative.ObjectFree((WxkObject*)native));
	}

	/// <summary>Whether wxWidgets has destroyed the sizer, along with the window or sizer that owned it.</summary>
	public bool IsDestroyed => _native is null;

	internal WxkSizer* Native {
		get {
			Application.VerifyAccess();
			return _native is not null ? _native : throw new ObjectDisposedException(GetType().Name, "wxWidgets has destroyed this sizer, along with the window it belonged to.");
		}
	}

	public void Add(Window window, SizerFlags flags = default) {
		ArgumentNullException.ThrowIfNull(window);
		WxKitNative.SizerAddWindowIntIntInt(Native, window.Native, flags.ProportionValue, flags.FlagsValue, Border(flags));
	}

	/// <summary>Adds a sizer inside this one, which then owns it.</summary>
	public void Add(Sizer sizer, SizerFlags flags = default) => Adopt(sizer, child => {
		WxKitNative.SizerAddSizerIntIntInt(Native, (WxkSizer*)child, flags.ProportionValue, flags.FlagsValue, Border(flags));
		return true;
	});

	/// <summary>Adds empty space of a fixed size.</summary>
	public void AddSpacer(int size) => WxKitNative.SizerAddSpacer(Native, size);

	/// <summary>Adds empty space that grows to take up room, in proportion to the other items.</summary>
	public void AddStretchSpacer(int proportion = 1) => WxKitNative.SizerAddStretchSpacer(Native, proportion);

	internal void Attach(Window window) => Claim(window);

	private protected static int Border(SizerFlags flags) => flags.BorderValue < 0 ? WxKitNative.SizerFlagsGetDefaultBorder() : flags.BorderValue;

	// Makes a sizer this one's, if add, given the child's handle, adds it. A child it could not add
	// is left to its own managed object, as before.
	private protected bool Adopt(Sizer sizer, Func<nint, bool> add) {
		ArgumentNullException.ThrowIfNull(sizer);
		_ = Native;
		if (ReferenceEquals(sizer, this)) throw new ArgumentException("A sizer cannot contain itself.", nameof(sizer));
		sizer.Claim(this);
		if (!add((nint)sizer.Native)) {
			sizer._owner = null;
			return false;
		}
		(_children ??= []).Add(sizer);
		return true;
	}

	internal void MarkDestroyed() {
		_native = null;
		if (_children is not null) {
			foreach (Sizer child in _children) child.MarkDestroyed();
		}
		_children = null;
		GC.SuppressFinalize(this);
	}

	private void Claim(object owner) {
		_ = Native;
		if (_owner is not null) throw new InvalidOperationException("The sizer already belongs to a window or another sizer.");
		_owner = owner;
	}
}

/// <summary>
/// How a sizer lays out one item: how much it grows, how it aligns, and its border. Wraps
/// wxSizerFlags. Each method returns a copy with the change, so calls chain:
/// <c>new SizerFlags(1).Expand().Border()</c>.
/// </summary>
public readonly struct SizerFlags {
	private const int ExpandFlag = 0x2000;
	private const int ShapedFlag = 0x4000;
	private const int AlignCenterHorizontal = 0x0100;
	private const int AlignRight = 0x0200;
	private const int AlignBottom = 0x0400;
	private const int AlignCenterVertical = 0x0800;
	private const int HorizontalAlignment = AlignCenterHorizontal | AlignRight;
	private const int VerticalAlignment = AlignCenterVertical | AlignBottom;

	private readonly int _proportion;
	private readonly int _flags;
	private readonly int _border;
	private readonly bool _defaultBorder;

	/// <summary>Flags for an item that grows by <paramref name="proportion"/> shares of the spare room, or not at all for 0.</summary>
	public SizerFlags(int proportion) : this(proportion, 0, 0, false) {
	}

	private SizerFlags(int proportion, int flags, int border, bool defaultBorder) {
		_proportion = proportion;
		_flags = flags;
		_border = border;
		_defaultBorder = defaultBorder;
	}

	/// <summary>An item that does not grow, with no border.</summary>
	public static SizerFlags Default => default;

	internal int ProportionValue => _proportion;

	internal int FlagsValue => _flags;

	// A negative border is the platform's default, which a sizer asks wxWidgets for as it adds the item.
	internal int BorderValue => _defaultBorder ? -1 : _border;

	public SizerFlags Proportion(int proportion) => new(proportion, _flags, _border, _defaultBorder);

	/// <summary>Fills the space the sizer gives the item across its direction.</summary>
	public SizerFlags Expand() => WithFlags(_flags | ExpandFlag);

	/// <summary>Grows the item while keeping its shape.</summary>
	public SizerFlags Shaped() => WithFlags(_flags | ShapedFlag);

	public SizerFlags Center() => WithFlags((_flags & ~(HorizontalAlignment | VerticalAlignment)) | AlignCenterHorizontal | AlignCenterVertical);

	public SizerFlags CenterHorizontal() => WithFlags((_flags & ~HorizontalAlignment) | AlignCenterHorizontal);

	public SizerFlags CenterVertical() => WithFlags((_flags & ~VerticalAlignment) | AlignCenterVertical);

	public SizerFlags Left() => WithFlags(_flags & ~HorizontalAlignment);

	public SizerFlags Right() => WithFlags((_flags & ~HorizontalAlignment) | AlignRight);

	public SizerFlags Top() => WithFlags(_flags & ~VerticalAlignment);

	public SizerFlags Bottom() => WithFlags((_flags & ~VerticalAlignment) | AlignBottom);

	/// <summary>Adds the platform's standard border on the given sides, scaled for the display.</summary>
	public SizerFlags Border(Direction direction = Direction.All) => new(_proportion, (_flags & ~(int)Direction.All) | (int)direction, 0, true);

	/// <summary>Adds a border of <paramref name="pixels"/> on the given sides.</summary>
	public SizerFlags Border(Direction direction, int pixels) {
		ArgumentOutOfRangeException.ThrowIfNegative(pixels);
		return new(_proportion, (_flags & ~(int)Direction.All) | (int)direction, pixels, false);
	}

	private SizerFlags WithFlags(int flags) => new(_proportion, flags, _border, _defaultBorder);
}
