using System.Diagnostics;

namespace WxKit.Tests;

// The other copies are this test application run again, as a process of its own, since
// wxWidgets counts a lock its own process holds as no other copy on Linux and macOS. Other tests
// run it the same way, for what an application does before Application.Run.
public class SingleInstanceTests {
	internal const string AnotherCopy = "--wxkit-another-copy";
	private const int Delivered = 10;
	private const int First = 11;
	private const int Running = 12;
	private const int NotRunning = 13;
	private const int Failed = 14;

	[Fact]
	public Task TellsWhetherAnotherCopyIsRunning() => Gui.Run(async () => {
		string name = UniqueName();
		string? folder = OperatingSystem.IsWindows() ? null : Directory.CreateTempSubdirectory("wxkit-checker-").FullName;
		try {
			using (SingleInstanceChecker first = new(name, folder)) {
				Assert.False(first.IsAnotherRunning);
				Assert.Equal(Running, await RunAnotherCopy("check", name, folder ?? string.Empty));
			}
			Assert.Equal(NotRunning, await RunAnotherCopy("check", name, folder ?? string.Empty));
		} finally {
			if (folder is not null) Directory.Delete(folder, true);
		}
	});

	[Fact]
	public Task HandsAMessageToTheFirstCopy() => Gui.Run(async () => {
		string name = UniqueName();
		using SingleInstance? first = SingleInstance.Claim(name, []);
		Assert.NotNull(first);
		TaskCompletionSource<(IReadOnlyList<string> Message, bool OnGuiThread)> received = new();
		first.MessageReceived += (_, e) => received.TrySetResult((e.Message, Application.IsGuiThread));
		Assert.Equal(Delivered, await RunAnotherCopy("claim", name, "one", "two three", "café", ""));
		(IReadOnlyList<string> message, bool onGuiThread) = await received.Task.WaitAsync(Gui.Timeout);
		Assert.Equal(["one", "two three", "café", ""], message);
		Assert.True(onGuiThread);
	});

	[Fact]
	public Task KeepsMessagesUntilThereIsAHandler() => Gui.Run(async () => {
		string name = UniqueName();
		using SingleInstance? first = SingleInstance.Claim(name, []);
		Assert.NotNull(first);
		Assert.Equal(Delivered, await RunAnotherCopy("claim", name, "early"));
		Assert.Equal(Delivered, await RunAnotherCopy("claim", name));
		List<IReadOnlyList<string>> messages = [];
		TaskCompletionSource both = new();
		first.MessageReceived += (_, e) => {
			messages.Add(e.Message);
			if (messages.Count == 2) both.TrySetResult();
		};
		await both.Task.WaitAsync(Gui.Timeout);
		Assert.Equal([["early"], []], messages);
	});

	[Fact]
	public Task LetsTheNextCopyBeFirstOnceDisposed() => Gui.Run(async () => {
		string name = UniqueName();
		SingleInstance? first = SingleInstance.Claim(name, []);
		Assert.NotNull(first);
		first.Dispose();
		Assert.Equal(First, await RunAnotherCopy("claim", name));
	});

	// As macOS's temporary folder is, which made the whole path longer than a socket's can be.
	[Fact]
	public void ShortensASocketPathThatWouldBeTooLong() {
		string folder = "/var/folders/20/jp1_0n3n7kndh6rnbqb5344m0000gn/T/wxkit-runner";
		Assert.Equal($"{folder}/short.sock", SingleInstance.SocketPath(folder, "short").Replace(Path.DirectorySeparatorChar, '/'));
		string hashed = SingleInstance.SocketPath(folder, "wxkit-test-50ed0038d6ba4deb85a6bdac46c41f0b").Replace(Path.DirectorySeparatorChar, '/');
		Assert.StartsWith(folder + "/", hashed);
		Assert.True(hashed.Length <= 100, hashed);
		Assert.Equal(hashed, SingleInstance.SocketPath(folder, "wxkit-test-50ed0038d6ba4deb85a6bdac46c41f0b").Replace(Path.DirectorySeparatorChar, '/'));
		Assert.Throws<IOException>(() => SingleInstance.SocketPath(new string('x', 95), "name"));
	}

	[Fact]
	public void RejectsBadNamesAndMessages() {
		Assert.Throws<ArgumentException>(() => SingleInstance.Claim("my app", []));
		Assert.Throws<ArgumentException>(() => SingleInstance.Claim("../escape", []));
		Assert.Throws<ArgumentException>(() => SingleInstance.Claim(UniqueName(), [new string('x', SingleInstance.MaxMessageBytes)]));
	}

	[Fact]
	public void EncodesMessagesAndRejectsBrokenOnes() {
		string[] parts = [@"C:\My Books\one.epub", "", "https://example.org/two.pdf", "café"];
		byte[] frame = SingleInstance.Encode(parts);
		Assert.Equal(frame.Length - 4, BitConverter.ToInt32(frame, 0));
		byte[] body = frame[4..];
		Assert.Equal(parts, SingleInstance.Decode(body));
		Assert.Equal([], SingleInstance.Decode(SingleInstance.Encode([])[4..]));
		Assert.Null(SingleInstance.Decode(body[..^1]));
		Assert.Null(SingleInstance.Decode([.. body, (byte)'x']));
		Assert.Null(SingleInstance.Decode("GET / HTTP/1.1\r\n"u8));
		Assert.Null(SingleInstance.Decode([1, 0, 0, 0, 2, 0, 0, 0, 0xFF, 0xFE]));
		Assert.Null(SingleInstance.Decode([0xFF, 0xFF, 0xFF, 0x7F]));
	}

	// The other copy: check name folder, or claim name message..., which exits with what it found,
	// or translate language text, which writes the translation, or best text, which writes the
	// language the user's preferred languages pick and the translation in it, on two lines.
	internal static int RunAsAnotherCopy(string[] args) {
		try {
			if (args is ["translate", string language, string text]) {
				using Translations translations = Translations.Use("WxKit.Tests", language);
				Console.Out.Write(Translations.Translate(text));
				return Delivered;
			}
			if (args is ["best", string original]) {
				using Translations translations = Translations.Use("WxKit.Tests");
				Console.Out.Write($"{translations.GetBestTranslation("WxKit.Tests")}\n{Translations.Translate(original)}");
				return Delivered;
			}
			if (args is ["check", string name, string folder]) {
				using SingleInstanceChecker checker = new(name, folder.Length == 0 ? null : folder);
				return checker.IsAnotherRunning ? Running : NotRunning;
			}
			if (args is ["claim", string claimed, .. string[] message]) {
				using SingleInstance? instance = SingleInstance.Claim(claimed, message);
				return instance is null ? Delivered : First;
			}
		} catch (Exception error) {
			Console.Error.WriteLine(error);
		}
		return Failed;
	}

	private static async Task<int> RunAnotherCopy(params string[] args) => (await RunAnotherProcess(args)).ExitCode;

	internal static Task<(int ExitCode, string Output)> RunAnotherProcess(params string[] args) => RunAnotherProcess(new Dictionary<string, string?>(), args);

	// The environment variables are set in the other process only, since on Linux and macOS
	// Environment.SetEnvironmentVariable doesn't change the variables native code reads.
	internal static async Task<(int ExitCode, string Output)> RunAnotherProcess(IReadOnlyDictionary<string, string?> environment, params string[] args) {
		string path = Environment.ProcessPath!;
		ProcessStartInfo start = new(path) { RedirectStandardError = true, RedirectStandardOutput = true, StandardOutputEncoding = System.Text.Encoding.UTF8 };
		foreach ((string name, string? value) in environment) start.Environment[name] = value;
		// Run as dotnet WxKit.Tests.dll, the other copy needs the assembly too.
		if (Path.GetFileNameWithoutExtension(path) == "dotnet") start.ArgumentList.Add(typeof(SingleInstanceTests).Assembly.Location);
		start.ArgumentList.Add(AnotherCopy);
		foreach (string arg in args) start.ArgumentList.Add(arg);
		using Process process = Process.Start(start)!;
		Task<string> errors = process.StandardError.ReadToEndAsync();
		Task<string> output = process.StandardOutput.ReadToEndAsync();
		await process.WaitForExitAsync().WaitAsync(Gui.Timeout);
		Assert.True(process.ExitCode != Failed, await errors);
		return (process.ExitCode, await output);
	}

	private static string UniqueName() => $"wxkit-test-{Guid.NewGuid():N}";
}
