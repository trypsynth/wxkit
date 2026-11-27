namespace WxKit.Tests;

// The system's real browser engine, which a runner may not have, so each test needs one.
public class WebViewTests {
	private static readonly TimeSpan PageWait = TimeSpan.FromSeconds(30);

	// A test waits for the engine more than once, and the first to make a web view on a runner also
	// waits for the engine to start, which can take longer than Gui.Timeout.
	private static readonly TimeSpan TestWait = TimeSpan.FromMinutes(1);

	[Fact]
	public Task ShowsAPageAndRunsItsScripts() => Gui.WithFrame(async frame => {
		Assert.SkipUnless(WebView.IsAvailable, "This system has no browser engine for a web view.");
		WebView view = new(frame);
		TaskCompletionSource loaded = new(TaskCreationOptions.RunContinuationsAsynchronously);
		view.Loaded += (_, _) => loaded.TrySetResult();
		frame.Show();
		view.NavigateToString("<html><head><title>Greeting</title></head><body><h1>Hello</h1></body></html>");
		await loaded.Task.WaitAsync(PageWait, TestContext.Current.CancellationToken);
		await Gui.WaitUntil(() => view.Title == "Greeting", PageWait);
		Assert.Equal("3", await view.ExecuteScriptAsync("1 + 2").WaitAsync(PageWait, TestContext.Current.CancellationToken));
		Assert.Equal("Hello", (await view.ExecuteScriptAsync("document.querySelector('h1').textContent").WaitAsync(PageWait, TestContext.Current.CancellationToken)).Trim('"'));
		await Assert.ThrowsAsync<InvalidOperationException>(() => view.ExecuteScriptAsync("throw new Error('no')").WaitAsync(PageWait, TestContext.Current.CancellationToken));
	}, TestWait);

	[Fact]
	public Task HearsTheScriptsOfThePage() => Gui.WithFrame(async frame => {
		Assert.SkipUnless(WebView.IsAvailable, "This system has no browser engine for a web view.");
		WebView view = new(frame);
		TaskCompletionSource<string> message = new(TaskCreationOptions.RunContinuationsAsynchronously);
		view.ScriptMessageReceived += (_, e) => message.TrySetResult(e.String);
		TaskCompletionSource loaded = new(TaskCreationOptions.RunContinuationsAsynchronously);
		view.Loaded += (_, _) => loaded.TrySetResult();
		frame.Show();
		await Gui.WaitUntil(() => view.AddScriptMessageHandler("wxkit"), PageWait);
		view.NavigateToString("<html><body>Page</body></html>");
		await loaded.Task.WaitAsync(PageWait, TestContext.Current.CancellationToken);
		await view.ExecuteScriptAsync("window.wxkit.postMessage('from the page')").WaitAsync(PageWait, TestContext.Current.CancellationToken);
		Assert.Equal("from the page", await message.Task.WaitAsync(PageWait, TestContext.Current.CancellationToken));
	}, TestWait);

	[Fact]
	public Task LetsANavigationBeCancelled() => Gui.WithFrame(async frame => {
		Assert.SkipUnless(WebView.IsAvailable, "This system has no browser engine for a web view.");
		WebView view = new(frame);
		TaskCompletionSource loaded = new(TaskCreationOptions.RunContinuationsAsynchronously);
		view.Loaded += (_, _) => loaded.TrySetResult();
		TaskCompletionSource<string> refused = new(TaskCreationOptions.RunContinuationsAsynchronously);
		view.Navigating += (_, e) => {
			if (!e.Url.Contains("refused", StringComparison.Ordinal)) return;
			e.Veto();
			refused.TrySetResult(e.Url);
		};
		frame.Show();
		view.NavigateToString("<html><body>Start</body></html>");
		await loaded.Task.WaitAsync(PageWait, TestContext.Current.CancellationToken);
		Uri? before = view.Source;
		view.Navigate("https://example.invalid/refused");
		Assert.Contains("refused", await refused.Task.WaitAsync(PageWait, TestContext.Current.CancellationToken));
		await Task.Delay(200, TestContext.Current.CancellationToken);
		Assert.Equal(before, view.Source);
	}, TestWait);

	[Fact]
	public Task NeedsTheProjectToAskForIt() => Gui.WithFrame(frame => {
		AppContext.SetSwitch("WxKit.WebView", false);
		try {
			Assert.False(WebView.IsAvailable);
			InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => new WebView(frame));
			Assert.Contains("WxKitWebView", error.Message);
		} finally {
			AppContext.SetSwitch("WxKit.WebView", true);
		}
	});

	// On Windows the engine starts after the control is created, and a script waits for it.
	[Fact]
	public Task RunsAScriptGivenBeforeTheEngineIsReady() => Gui.WithFrame(async frame => {
		Assert.SkipUnless(WebView.IsAvailable, "This system has no browser engine for a web view.");
		WebView view = new(frame);
		Task<string> early = view.ExecuteScriptAsync("1 + 1");
		frame.Show();
		Assert.Equal("2", await early.WaitAsync(PageWait, TestContext.Current.CancellationToken));
	}, TestWait);

	[Fact]
	public Task CancelsItsScriptsWhenDestroyed() => Gui.WithFrame(async frame => {
		Assert.SkipUnless(WebView.IsAvailable, "This system has no browser engine for a web view.");
		WebView view = new(frame);
		Task<string> pending = view.ExecuteScriptAsync("new Promise(() => {})");
		view.Destroy();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.WaitAsync(PageWait, TestContext.Current.CancellationToken));
	}, TestWait);
}
