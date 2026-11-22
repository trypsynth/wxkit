using System.Reflection;
using WxKit.Interop;

namespace WxKit;

/// <summary>
/// Shows web pages, with the system's browser engine: Edge's WebView2 on Windows, WebKit on macOS,
/// and WebKitGTK on Linux. Screen readers read its pages as they do in a browser. An application
/// that uses it sets <c>WxKitWebView</c> to <c>true</c> in its project file, which on Linux links
/// WebKitGTK, so applications that don't need none of it.
/// </summary>
/// <remarks>
/// On Windows, the engine starts after the control is created, and <see cref="Created"/> says when
/// it is ready. A page or a script given before then waits for it, but other members, such as
/// <see cref="AddScriptMessageHandler"/>, fail until then.
/// </remarks>
public unsafe partial class WebView {
	private const string Switch = "WxKit.WebView";
	private readonly Dictionary<long, TaskCompletionSource<string>> _scripts = [];
	// The scripts given before the engine was ready, which run once it is.
	private readonly List<(long Request, string Script)> _waiting = [];
	private long _lastRequest;
	private bool _isCreated;

	/// <summary>Creates a web view that shows <paramref name="url"/>.</summary>
	/// <exception cref="InvalidOperationException">The project does not set <c>WxKitWebView</c>.</exception>
	/// <exception cref="PlatformNotSupportedException">The system has no browser engine for it, as <see cref="IsAvailable"/> says.</exception>
	public WebView(Window parent, string url = "about:blank", int id = StandardId.Any, Point? position = null, Size? size = null) : this(CreateNative(parent, url, id, position, size)) {
		ScriptResult += OnScriptResult;
		Created += OnCreated;
	}

	/// <summary>
	/// Whether a web view can be created: the project sets <c>WxKitWebView</c>, and the system has
	/// the browser engine, which on Windows is Edge's WebView2 runtime, part of Windows 11 and of
	/// current Windows 10.
	/// </summary>
	public static bool IsAvailable => IsSwitchedOn && WxKitNative.WebViewIsAvailable() != 0;

	/// <summary>
	/// The folder the browser engine keeps its data in, such as cookies and its cache, for the web
	/// views created after it is set. On Windows, Edge keeps it beside the executable unless told
	/// otherwise, which an installed application can't write to, so it is
	/// <c>%LOCALAPPDATA%\&lt;application&gt;\WebView2</c> by default. Elsewhere, the engine chooses
	/// unless this is set. WebKit on macOS keeps its data where the system does, whatever this says.
	/// </summary>
	public static string? DataFolder { get; set; }

	/// <summary>The address of the page shown, or null before one is. Setting it navigates there.</summary>
	public Uri? Source {
		get => Uri.TryCreate(NativeString.Take(WxKitNative.WebViewGetCurrentURL(Native)), UriKind.Absolute, out Uri? address) ? address : null;
		set {
			ArgumentNullException.ThrowIfNull(value);
			Navigate(value);
		}
	}

	// Set by WxKitWebView, which RuntimeHostConfigurationOption bakes into the application.
	private static bool IsSwitchedOn => AppContext.TryGetSwitch(Switch, out bool enabled) && enabled;

	/// <summary>Shows the page at <paramref name="url"/>. <see cref="Navigating"/> can cancel it, and <see cref="Loaded"/> or <see cref="NavigationFailed"/> says how it went.</summary>
	public void Navigate(string url) {
		ArgumentNullException.ThrowIfNull(url);
		fixed (char* text = url) {
			WxKitNative.WebViewLoadURL(Native, text, NativeString.Length(url));
		}
	}

	/// <summary>Shows the page at <paramref name="address"/>.</summary>
	public void Navigate(Uri address) {
		ArgumentNullException.ThrowIfNull(address);
		Navigate(address.IsAbsoluteUri ? address.AbsoluteUri : address.OriginalString);
	}

	/// <summary>Shows <paramref name="html"/>, whose relative links are resolved against <paramref name="baseUrl"/>.</summary>
	public void NavigateToString(string html, string baseUrl = "") {
		ArgumentNullException.ThrowIfNull(html);
		ArgumentNullException.ThrowIfNull(baseUrl);
		fixed (char* htmlText = html)
		fixed (char* baseText = baseUrl) {
			WxKitNative.WebViewSetPage(Native, htmlText, NativeString.Length(html), baseText, NativeString.Length(baseUrl));
		}
	}

	/// <summary>
	/// Runs <paramref name="script"/> in the page, and gives its result as text, such as a number
	/// or a string as JavaScript writes it. A script given before the engine is ready, as
	/// <see cref="Created"/> says, runs once it is.
	/// </summary>
	/// <exception cref="InvalidOperationException">The script threw, or the page could not run it. The message is the engine's.</exception>
	/// <exception cref="TaskCanceledException">The web view was destroyed before the script finished.</exception>
	public Task<string> ExecuteScriptAsync(string script) {
		ArgumentNullException.ThrowIfNull(script);
		WxkWindow* native = Native;
		long request = ++_lastRequest;
		TaskCompletionSource<string> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
		_scripts.Add(request, completion);
		if (_isCreated) Run(native, request, script);
		else _waiting.Add((request, script));
		return completion.Task;
	}

	private static void Run(WxkWindow* native, long request, string script) {
		fixed (char* text = script) {
			WxKitNative.WebViewRunScriptAsync(native, text, NativeString.Length(script), request);
		}
	}

	// Every engine sends it after the constructor returns, even one that is ready at once.
	private void OnCreated(object? sender, WebViewEvent e) {
		e.Skip();
		_isCreated = true;
		foreach ((long request, string script) in _waiting) Run(Native, request, script);
		_waiting.Clear();
	}

	private protected override void OnDestroyed() {
		base.OnDestroyed();
		foreach (TaskCompletionSource<string> completion in _scripts.Values) completion.TrySetCanceled();
		_scripts.Clear();
		_waiting.Clear();
	}

	private static WxkWindow* CreateNative(Window parent, string url, int id, Point? position, Size? size) {
		Application.VerifyAccess();
		ArgumentNullException.ThrowIfNull(parent);
		ArgumentNullException.ThrowIfNull(url);
		if (!IsSwitchedOn) throw new InvalidOperationException("A web view needs <WxKitWebView>true</WxKitWebView> in the project file, which on Linux links WebKitGTK.");
		if (WxKitNative.WebViewIsAvailable() == 0) throw new PlatformNotSupportedException(OperatingSystem.IsWindows() ? "A web view needs Edge's WebView2 runtime, which this system does not have." : "A web view needs the system's browser engine, which this system does not have.");
		string dataFolder = DataFolder ?? DefaultDataFolder();
		WxkWindow* native;
		fixed (char* urlText = url)
		fixed (char* dataText = dataFolder) {
			native = WxKitNative.WebViewNew((WxkWindow*)parent.Native, id, urlText, NativeString.Length(url), position ?? Point.Default, size ?? Size.Default, 0, dataText, NativeString.Length(dataFolder));
		}
		if (native is null) throw new InvalidOperationException("The browser engine could not start.");
		return native;
	}

	// Edge's own default, beside the executable, is not writable once the application is installed.
	private static string DefaultDataFolder() {
		if (!OperatingSystem.IsWindows()) return string.Empty;
		string application = Assembly.GetEntryAssembly()?.GetName().Name ?? Path.GetFileNameWithoutExtension(Environment.ProcessPath) ?? "WxKit";
		return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), application, "WebView2");
	}

	private void OnScriptResult(object? sender, WebViewEvent e) {
		if (!_scripts.Remove(e.Request, out TaskCompletionSource<string>? completion)) return;
		if (e.IsError) completion.SetException(new InvalidOperationException(e.String.Length > 0 ? e.String : "The script could not run."));
		else completion.SetResult(e.String);
	}
}

public unsafe partial class WebViewEvent {
	// The number ExecuteScriptAsync gave the script whose result this is.
	internal long Request => WxKitNative.WebViewEventGetRequest(Handle);
}
