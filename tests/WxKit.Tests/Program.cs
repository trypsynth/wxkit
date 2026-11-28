using Xunit.MicrosoftTestingPlatform;
using Xunit.Runner.InProc.SystemConsole;

// Every test touches the GUI, and on macOS the GUI has to run on the main thread, which the test
// runner would otherwise take. So wxWidgets gets the main thread, the runner gets a thread of its
// own, and each test hands its body to the GUI thread through Gui.Run. The runner is chosen the
// same way xUnit's generated entry point chooses it. Tests do not run in parallel, as
// xunit.runner.json says, because they share the one GUI thread.

namespace WxKit.Tests;

internal static class Program {
	public static int Main(string[] args) {
		// SingleInstanceTests runs this as another copy of an application, before any GUI, as an
		// application's Main would.
		if (args is [SingleInstanceTests.AnotherCopy, ..]) {
			int copy = SingleInstanceTests.RunAsAnotherCopy(args[1..]);
			Console.Error.WriteLine($"Main returns {copy}");
			return copy;
		}
		int result = 1;
		int exitCode = Application.Run(() => {
			Gui.Start();
			Thread runner = new(() => {
				try {
					result = RunTests(args);
				} finally {
					Gui.Stop();
				}
			}) {
				Name = "xUnit runner"
			};
			runner.Start();
		});
		return result != 0 ? result : exitCode;
	}

	private static int RunTests(string[] args) {
		if (args.Any(static arg => arg is "--server" or "--internal-msbuild-node")) return TestPlatformTestFramework.RunAsync(args, SelfRegisteredExtensions.AddSelfRegisteredExtensions).GetAwaiter().GetResult();
		return ConsoleRunner.Run(args).GetAwaiter().GetResult();
	}
}
