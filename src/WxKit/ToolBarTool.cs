using WxKit.Interop;

namespace WxKit;

// A tool belongs to its toolbar, which deletes it when the tool is deleted, the tools are cleared,
// or the toolbar is destroyed. wxWidgets says nothing when it does, so the toolbar keeps the one
// managed object of each of its tools, and marks it destroyed then. A tool's own setters only
// change what the tool records, so its state changes through the toolbar, which shows it.
public sealed unsafe partial class ToolBarTool {
	private readonly ToolBar _toolBar;
	private WxkToolBarTool* _native;

	internal ToolBarTool(ToolBar toolBar, WxkToolBarTool* native) {
		_toolBar = toolBar;
		_native = native;
	}

	/// <summary>Whether wxWidgets has deleted the tool, along with the toolbar or when the toolbar deleted it.</summary>
	public bool IsDisposed => _native is null;

	internal WxkToolBarTool* Native {
		get {
			Application.VerifyAccess();
			return _native is not null ? _native : throw new ObjectDisposedException(nameof(ToolBarTool), "wxWidgets has deleted this tool, along with the toolbar or when the toolbar deleted it.");
		}
	}

	/// <summary>The toolbar the tool is on.</summary>
	public ToolBar ToolBar {
		get {
			_ = Native;
			return _toolBar;
		}
	}

	/// <summary>Whether the tool can be used. The toolbar enables or disables the tool with its id.</summary>
	public bool IsEnabled {
		get => _toolBar.GetToolEnabled(Id);
		set => _toolBar.EnableTool(Id, value);
	}

	/// <summary>Whether a check or radio tool is down. The toolbar toggles the tool with its id.</summary>
	public bool IsToggled {
		get => _toolBar.GetToolState(Id);
		set => _toolBar.ToggleTool(Id, value);
	}

	/// <summary>The tool's tooltip. The toolbar sets it on the tool with its id.</summary>
	public string ShortHelp {
		get => _toolBar.GetToolShortHelp(Id);
		set => _toolBar.SetToolShortHelp(Id, value);
	}

	/// <summary>The tool's longer help, which a frame shows in its status bar. The toolbar sets it on the tool with its id.</summary>
	public string LongHelp {
		get => _toolBar.GetToolLongHelp(Id);
		set => _toolBar.SetToolLongHelp(Id, value);
	}

	// The managed object of a tool wxWidgets hands back, which its toolbar keeps.
	internal static ToolBarTool? Wrap(WxkToolBarTool* native) {
		if (native is null) return null;
		ToolBar toolBar = ObjectRegistry.Wrap<ToolBar>(WxKitNative.ToolBarToolGetToolBar(native)) ?? throw new InvalidOperationException("The tool is not on a toolbar WxKit knows.");
		return toolBar.ToolFor(native);
	}

	internal void MarkDestroyed() => _native = null;
}

public unsafe partial class ToolBar {
	private Dictionary<nint, ToolBarTool>? _tools;

	/// <summary>Deletes the first tool with the id, which is then destroyed. Returns false when there is none.</summary>
	public bool DeleteTool(int toolId) {
		WxkToolBarTool* tool = WxKitNative.ToolBarFindById(Native, toolId);
		if (WxKitNative.ToolBarDeleteTool(Native, toolId) == 0) return false;
		Forget(tool);
		return true;
	}

	/// <summary>Deletes the tool at the position, which is then destroyed. Returns false when there is none.</summary>
	public bool DeleteToolByPosition(int position) {
		ArgumentOutOfRangeException.ThrowIfNegative(position);
		WxkToolBarTool* tool = WxKitNative.ToolBarGetToolByPos(Native, position);
		if (WxKitNative.ToolBarDeleteToolByPos(Native, (nuint)position) == 0) return false;
		Forget(tool);
		return true;
	}

	/// <summary>Deletes every tool, which are then destroyed.</summary>
	public void ClearTools() {
		WxKitNative.ToolBarClearTools(Native);
		MarkToolsDestroyed();
	}

	internal ToolBarTool ToolFor(WxkToolBarTool* native) {
		_tools ??= [];
		if (!_tools.TryGetValue((nint)native, out ToolBarTool? tool)) _tools[(nint)native] = tool = new ToolBarTool(this, native);
		return tool;
	}

	private protected override void OnDestroyed() {
		MarkToolsDestroyed();
		base.OnDestroyed();
	}

	private void Forget(WxkToolBarTool* native) {
		if (_tools is not null && _tools.Remove((nint)native, out ToolBarTool? tool)) tool.MarkDestroyed();
	}

	private void MarkToolsDestroyed() {
		if (_tools is null) return;
		foreach (ToolBarTool tool in _tools.Values) tool.MarkDestroyed();
		_tools = null;
	}
}
