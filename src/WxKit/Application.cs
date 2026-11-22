using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using WxKit.Interop;

namespace WxKit;

/// <summary>
/// The application: starts wxWidgets, runs the event loop, and ends when the last top-level window
/// closes. Derive from it to override its startup, or call <see cref="Run(Action)"/>.
/// </summary>
public unsafe partial class Application {
	private static int _guiThreadId;
	private static LayoutDirection _layoutDirection = LayoutDirection.Default;
	private ExceptionDispatchInfo? _failure;

	// wxWidgets reports its failed assertions here from the first use of the application on.
	static Application() => WxKitNative.AppSetAssertCallback(&OnAssertFailed);

	/// <summary>
	/// Raised when one of wxWidgets' assertions fails, which means it was called in a way it does
	/// not support, such as with an index out of range or a style a control does not take. wxWidgets
	/// then goes on as best it can, often by doing nothing. Unless a handler sets
	/// <see cref="AssertFailedEventArgs.Handled"/>, the failure is written to
	/// <see cref="Trace"/>, and a debugger that is attached breaks. It is raised on the thread the
	/// assertion failed on, which is usually the GUI thread.
	/// </summary>
	public static event EventHandler<AssertFailedEventArgs>? AssertFailed;

	/// <summary>
	/// Raised on the GUI thread when an event handler, a posted action, or an <c>await</c>
	/// continuation throws. Set <see cref="ApplicationExceptionEventArgs.Handled"/> to keep the
	/// application running; otherwise the event loop ends and <see cref="Run()"/> rethrows the exception.
	/// </summary>
	public static event EventHandler<ApplicationExceptionEventArgs>? UnhandledException;

	/// <summary>The running application, or null when none is running.</summary>
	public static Application? Current { get; private set; }

	/// <summary>Whether the calling thread is the one running the event loop.</summary>
	public static bool IsGuiThread => _guiThreadId != 0 && _guiThreadId == Environment.CurrentManagedThreadId;

	/// <summary>The version of wxWidgets that WxKit is built with, such as "wxWidgets 3.3.4".</summary>
	public static string WxWidgetsVersion => NativeString.Take(WxKitNative.VersionGetString());

	/// <summary>
	/// The direction that windows with nothing to take one from are laid out in: new frames and
	/// dialogs with no parent, and message boxes with no window under them. A window with a parent
	/// takes its parent's. <see cref="LayoutDirection.Default"/>, the default, follows the UI locale,
	/// so an application whose language is its own setting, such as Hebrew on an English system,
	/// sets it to that language's <see cref="LanguageInfo.LayoutDirection"/>. Windows already open
	/// keep theirs. This decides the direction on Windows and Linux. Set it on the GUI thread, or
	/// before the application runs.
	/// </summary>
	public static LayoutDirection LayoutDirection {
		get => _layoutDirection;
		set {
			if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
			VerifyAccessOrNotRunning();
			WxKitNative.AppSetLayoutDirection((int)value);
			_layoutDirection = value;
		}
	}

	/// <summary>Runs an application whose startup is <paramref name="onInit"/>, and returns its exit code.</summary>
	public static int Run(Action onInit) {
		ArgumentNullException.ThrowIfNull(onInit);
		return new ActionApplication(onInit).Run();
	}

	/// <summary>
	/// Runs <paramref name="action"/> on the GUI thread once the event loop gets to it. Safe to call
	/// from any thread while the application is running.
	/// </summary>
	public static void Post(Action action) {
		ArgumentNullException.ThrowIfNull(action);
		if (!TryPost(action)) throw new InvalidOperationException("No application is running.");
	}

	/// <summary>
	/// Ends the event loop once it has finished the event it is handling, so <see cref="Run()"/>
	/// returns, even if top-level windows are still open. Must be called on the GUI thread.
	/// </summary>
	public static void Exit() {
		VerifyAccess();
		WxKitNative.AppExitMainLoop();
	}

	/// <summary>
	/// Starts wxWidgets, calls <see cref="OnInit"/>, and runs the event loop. Returns the exit code.
	/// On Windows, the GUI needs a single-threaded apartment. Mark Main with <see cref="STAThreadAttribute"/>
	/// to run it on the calling thread; otherwise it runs on a thread of its own, and this call waits for it.
	/// On macOS, Cocoa only runs a user interface on the process's main thread, so call this from Main.
	/// </summary>
	/// <exception cref="InvalidOperationException">On macOS, the calling thread is not the main thread.</exception>
	public int Run() {
		if (OperatingSystem.IsWindows() && Thread.CurrentThread.GetApartmentState() != ApartmentState.STA) return RunOnStaThread();
		if (WxKitNative.AppCanRunOnThisThread() == 0) throw new InvalidOperationException("On macOS, Cocoa only runs a user interface on the process's main thread, so Application.Run has to be called from Main, on the thread it starts on.");
		return RunHere();
	}

	/// <summary>Creates the first windows. Returning false ends the application without running the event loop.</summary>
	protected virtual bool OnInit() => true;

	internal static bool TryPost(Action action) {
		GCHandle handle = GCHandle.Alloc(action);
		if (WxKitNative.AppCallAfter(&RunPosted, (void*)GCHandle.ToIntPtr(handle)) != 0) return true;
		handle.Free();
		return false;
	}

	// For what wxWidgets can do before its application starts, such as translations, which a
	// command-line mode or Main sets up: on the GUI thread while an application runs, and on any
	// thread while none does.
	internal static void VerifyAccessOrNotRunning() {
		if (_guiThreadId == 0) return;
		VerifyAccess();
	}

	internal static void VerifyAccess() {
		if (_guiThreadId == Environment.CurrentManagedThreadId) return;
		throw new InvalidOperationException(_guiThreadId == 0
			? "WxKit objects can only be used while Application.Run is running."
			: "WxKit objects can only be used on the GUI thread. Use Application.Post, or await, to get back to it.");
	}

	// An exception cannot unwind back through wxWidgets, so every callback from it catches what it
	// can and reports it here.
	internal static void ReportException(Exception error) {
		Application? application = Current;
		try {
			ApplicationExceptionEventArgs args = new(error);
			UnhandledException?.Invoke(application, args);
			if (args.Handled) return;
		} catch (Exception handlerError) {
			error = handlerError;
		}
		application?.Fail(error);
	}

	// OLE, which the clipboard, drag and drop and accessibility use, only works in a single-threaded
	// apartment. .NET starts Main in the multithreaded one unless it is marked [STAThread], which top-level
	// statements cannot be, and a thread's apartment cannot change once it is set. wxWidgets would then fail
	// to start OLE without saying so, so the GUI gets a thread that is set up right instead.
	[System.Runtime.Versioning.SupportedOSPlatform("windows")]
	private int RunOnStaThread() {
		int exitCode = 0;
		ExceptionDispatchInfo? failure = null;
		Thread thread = new(() => {
			try {
				exitCode = RunHere();
			} catch (Exception error) {
				failure = ExceptionDispatchInfo.Capture(error);
			}
		}) {
			Name = "WxKit GUI",
			// The calling thread waits for this one, so this one must not also keep the process alive.
			IsBackground = true
		};
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		thread.Join();
		failure?.Throw();
		return exitCode;
	}

	private int RunHere() {
		if (Current is not null) throw new InvalidOperationException("An application is already running.");
		Current = this;
		_guiThreadId = Environment.CurrentManagedThreadId;
		SynchronizationContext? previousContext = SynchronizationContext.Current;
		SynchronizationContext.SetSynchronizationContext(new GuiSynchronizationContext());
		GCHandle self = GCHandle.Alloc(this);
		try {
			int exitCode = WxKitNative.AppRun(&OnInitCore, (void*)GCHandle.ToIntPtr(self));
			_failure?.Throw();
			return exitCode;
		} finally {
			ObjectRegistry.Clear();
			self.Free();
			SynchronizationContext.SetSynchronizationContext(previousContext);
			_guiThreadId = 0;
			Current = null;
		}
	}

	// The first failure ends the event loop, and is rethrown from Run on the thread that called it.
	private void Fail(Exception error) {
		if (_failure is not null) return;
		_failure = ExceptionDispatchInfo.Capture(error);
		WxKitNative.AppExitMainLoop();
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static byte OnInitCore(void* userdata) {
		Application application = (Application)GCHandle.FromIntPtr((nint)userdata).Target!;
		try {
			return application.OnInit() ? (byte)1 : (byte)0;
		} catch (Exception error) {
			application._failure = ExceptionDispatchInfo.Capture(error);
			return 0;
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void OnAssertFailed(WxkString* file, int line, WxkString* function, WxkString* condition, WxkString* message) {
		try {
			AssertFailedEventArgs args = new(NativeString.Take(file), line, NativeString.Take(function), NativeString.Take(condition), NativeString.Take(message));
			AssertFailed?.Invoke(Current, args);
			if (args.Handled) return;
			Trace.WriteLine(args.ToString());
			if (Debugger.IsAttached) Debugger.Break();
		} catch (Exception error) {
			// An exception cannot unwind into wxWidgets, and an assertion can fail on any thread.
			Trace.WriteLine($"An AssertFailed handler threw: {error}");
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void RunPosted(void* userdata) {
		GCHandle handle = GCHandle.FromIntPtr((nint)userdata);
		Action action = (Action)handle.Target!;
		handle.Free();
		try {
			action();
		} catch (Exception error) {
			ReportException(error);
		}
	}

	private sealed class ActionApplication(Action onInit) : Application {
		protected override bool OnInit() {
			onInit();
			return true;
		}
	}

	// Makes await resume on the GUI thread, as it does in Windows Forms and WPF.
	private sealed class GuiSynchronizationContext : SynchronizationContext {
		public override void Post(SendOrPostCallback callback, object? state) {
			// Once the event loop has ended there is nothing left to run the continuation on.
			TryPost(() => callback(state));
		}

		public override void Send(SendOrPostCallback callback, object? state) {
			if (IsGuiThread) {
				callback(state);
				return;
			}
			using ManualResetEventSlim done = new();
			ExceptionDispatchInfo? failure = null;
			bool posted = TryPost(() => {
				try {
					callback(state);
				} catch (Exception error) {
					failure = ExceptionDispatchInfo.Capture(error);
				} finally {
					done.Set();
				}
			});
			if (!posted) throw new InvalidOperationException("No application is running.");
			done.Wait();
			failure?.Throw();
		}

		public override SynchronizationContext CreateCopy() => this;
	}
}

/// <summary>A failed wxWidgets assertion. See <see cref="Application.AssertFailed"/>.</summary>
public sealed class AssertFailedEventArgs(string file, int line, string function, string condition, string message) : EventArgs {
	/// <summary>The wxWidgets source file the assertion is in.</summary>
	public string File { get; } = file;
	/// <summary>Its line in the file.</summary>
	public int Line { get; } = line;
	/// <summary>The wxWidgets function it is in.</summary>
	public string Function { get; } = function;
	/// <summary>The condition that was false, as the source writes it.</summary>
	public string Condition { get; } = condition;
	/// <summary>What wxWidgets says went wrong, which may be empty.</summary>
	public string Message { get; } = message;
	/// <summary>Set to true to leave out the trace and the break into an attached debugger.</summary>
	public bool Handled { get; set; }
	public override string ToString() => $"wxWidgets assertion \"{Condition}\" failed in {Function} at {File}:{Line}{(Message.Length > 0 ? ": " + Message : string.Empty)}";
}

/// <summary>An exception that escaped an event handler. See <see cref="Application.UnhandledException"/>.</summary>
public sealed class ApplicationExceptionEventArgs(Exception exception) : EventArgs {
	public Exception Exception { get; } = exception;

	/// <summary>Set to true to keep the application running.</summary>
	public bool Handled { get; set; }
}
