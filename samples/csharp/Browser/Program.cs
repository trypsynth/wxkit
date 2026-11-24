using WxKit;

// A small browser: an address, Back, Forward, and Reload, and a web view, whose pages screen
// readers read as they do in any browser. It starts on a page of its own, so it runs offline.
// With --smoke, the frame closes as soon as the event loop is running, so CI can check that a
// published executable starts and ends cleanly.
bool smoke = args is ["--smoke"];
const string StartPage = """
	<!doctype html>
	<html lang="en">
	<head><title>WxKit browser</title></head>
	<body>
	<h1>A WxKit web view</h1>
	<p>This page is shown by the system's browser engine: Edge on Windows, WebKit on macOS, and WebKitGTK on Linux.</p>
	<p>Type an address above and press Enter, or follow <a href="https://www.wxwidgets.org/">the wxWidgets site</a>.</p>
	</body>
	</html>
	""";
return Application.Run(() => {
	Frame frame = new(null, "WxKit browser", size: new Size(900, 700));
	Panel panel = new(frame);
	// Each label is created just before the control it names, so a screen reader names the control
	// after it, and the tab order follows.
	StaticText addressLabel = new(panel, "&Address:");
	TextCtrl address = new(panel, style: TextCtrlStyle.ProcessEnter);
	Button back = new(panel, "&Back") { IsEnabled = false };
	Button forward = new(panel, "&Forward") { IsEnabled = false };
	Button reload = new(panel, "&Reload");
	WebView view = new(panel);
	address.TextEnter += (_, _) => {
		string text = address.Value.Trim();
		if (text.Length == 0) return;
		view.Navigate(text.Contains("://", StringComparison.Ordinal) ? text : "https://" + text);
		view.SetFocus();
	};
	back.Click += (_, _) => view.GoBack();
	forward.Click += (_, _) => view.GoForward();
	reload.Click += (_, _) => view.Reload();
	// The address and the buttons follow the page, and the window is named after it.
	view.Navigated += (_, e) => {
		address.Value = e.Url.StartsWith("data:", StringComparison.Ordinal) || e.Url == "about:blank" ? string.Empty : e.Url;
		back.IsEnabled = view.CanGoBack;
		forward.IsEnabled = view.CanGoForward;
	};
	view.TitleChanged += (_, _) => frame.Title = view.Title.Length > 0 ? $"{view.Title} - WxKit browser" : "WxKit browser";
	view.NavigationFailed += (_, e) => frame.Title = $"Could not open {e.Url} - WxKit browser";
	BoxSizer bar = new(Orientation.Horizontal);
	bar.Add(addressLabel, SizerFlags.Default.CenterVertical().Border(Direction.Right));
	bar.Add(address, SizerFlags.Default.Proportion(1).CenterVertical().Border(Direction.Right));
	bar.Add(back, SizerFlags.Default.Border(Direction.Right));
	bar.Add(forward, SizerFlags.Default.Border(Direction.Right));
	bar.Add(reload);
	BoxSizer sizer = new(Orientation.Vertical);
	sizer.Add(bar, SizerFlags.Default.Expand().Border());
	sizer.Add(view, SizerFlags.Default.Proportion(1).Expand());
	panel.Sizer = sizer;
	view.NavigateToString(StartPage);
	frame.Center();
	frame.Show();
	view.SetFocus();
	if (smoke) Application.Post(() => frame.Close(force: true));
});
