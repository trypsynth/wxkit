namespace WxKit.Tests;

// Runs test code on the GUI thread, and keeps the application alive between tests.
internal static class Gui {
	private static readonly List<Exception> Unhandled = [];
	private static readonly List<AssertFailedEventArgs> Assertions = [];
	private static Frame? _keeper;

	public static TimeSpan Timeout { get; } = TimeSpan.FromSeconds(10);

	// The event loop ends when the last top-level window goes, so a hidden frame outlives every test.
	public static void Start() {
		_keeper = new Frame(null, "WxKit tests");
		Application.UnhandledException += (_, e) => {
			lock (Unhandled) Unhandled.Add(e.Exception);
			e.Handled = true;
		};
		Application.AssertFailed += (_, e) => {
			lock (Assertions) Assertions.Add(e);
			e.Handled = true;
		};
	}

	// Exiting directly, rather than waiting for the last window to go, means a test that leaks a
	// window cannot keep the run from ending.
	public static void Stop() => Application.Post(() => {
		_keeper?.Destroy();
		Application.Exit();
	});

	public static Task Run(Action body) => Run(() => {
		body();
		return Task.CompletedTask;
	});

	public static Task<T> Run<T>(Func<T> body) => Run(() => Task.FromResult(body()));

	public static Task Run(Func<Task> body) => Run(async () => {
		await body();
		return true;
	});

	// The body runs on the GUI thread, and so do its continuations. An exception that escapes an
	// event handler while it runs fails the test, since nothing else would see it, and so does a
	// failed wxWidgets assertion, which means the test, or WxKit, called wxWidgets wrongly.
	public static Task<T> Run<T>(Func<Task<T>> body) {
		TaskCompletionSource<T> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
		Application.Post(async () => {
			TakeUnhandled();
			TakeAssertions();
			try {
				T result = await body().WaitAsync(Timeout);
				Exception[] unhandled = TakeUnhandled();
				if (unhandled.Length > 0) throw new AggregateException("An event handler threw.", unhandled);
				AssertFailedEventArgs[] assertions = TakeAssertions();
				if (assertions.Length > 0) throw new InvalidOperationException(string.Join("\n", assertions.Select(assertion => assertion.ToString())));
				completion.SetResult(result);
			} catch (Exception error) {
				completion.SetException(error);
			}
		});
		return completion.Task;
	}

	/// <summary>Runs <paramref name="body"/> with a new frame, which is destroyed afterwards if the body did not destroy it.</summary>
	public static Task WithFrame(Func<Frame, Task> body) => Run(async () => {
		Frame frame = new(null, "Test frame");
		try {
			await body(frame);
		} finally {
			if (!frame.IsDestroyed) frame.Destroy();
		}
	});

	public static Task WithFrame(Action<Frame> body) => WithFrame(frame => {
		body(frame);
		return Task.CompletedTask;
	});

	/// <summary>Lets the event loop run until <paramref name="condition"/> holds, such as until a window wxWidgets destroys at idle time is gone.</summary>
	public static Task WaitUntil(Func<bool> condition) => WaitUntil(condition, Timeout);

	/// <summary>Lets the event loop run until <paramref name="condition"/> holds, for at most <paramref name="timeout"/>, which a test that waits more than once keeps inside its own.</summary>
	public static async Task WaitUntil(Func<bool> condition, TimeSpan timeout) {
		DateTime deadline = DateTime.UtcNow + timeout;
		while (!condition()) {
			if (DateTime.UtcNow > deadline) throw new TimeoutException("The condition did not hold in time.");
			await Task.Delay(10);
		}
	}

	/// <summary>The wxWidgets assertions that failed since the last call, for a test that expects one.</summary>
	public static AssertFailedEventArgs[] TakeAssertions() {
		lock (Assertions) {
			AssertFailedEventArgs[] taken = [.. Assertions];
			Assertions.Clear();
			return taken;
		}
	}

	/// <summary>The exceptions that escaped event handlers since the last call.</summary>
	public static Exception[] TakeUnhandled() {
		lock (Unhandled) {
			Exception[] taken = [.. Unhandled];
			Unhandled.Clear();
			return taken;
		}
	}
}
