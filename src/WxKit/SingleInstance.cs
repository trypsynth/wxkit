using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using WxKit.Interop;

namespace WxKit;

/// <summary>
/// Tells whether another copy of an application is running. Wraps wxSingleInstanceChecker, which
/// holds a named mutex on Windows, and a lock file with its process's ID on Linux and macOS, so a
/// copy that crashed never blocks the next one. Keep it for as long as the application runs, and
/// dispose of it to let the next copy be the first.
/// </summary>
/// <remarks>
/// It needs no running application, so it can be made in Main, before <see cref="Application.Run(Action)"/>.
/// To hand what a second copy was started with to the first, such as files to open, use
/// <see cref="SingleInstance"/>, which uses one.
/// </remarks>
public sealed unsafe class SingleInstanceChecker : IDisposable {
	private WxkSingleInstanceChecker* _native;

	/// <summary>Checks for copies that use the same name, which identifies the application, and its user.</summary>
	/// <param name="name">The name, which cannot have slashes or backslashes.</param>
	/// <param name="folder">On Linux and macOS, the folder of the lock file, or null for the home folder.</param>
	/// <exception cref="IOException">The checker could not be made, as when the lock file cannot be written.</exception>
	public SingleInstanceChecker(string name, string? folder = null) {
		ArgumentException.ThrowIfNullOrEmpty(name);
		if (name.AsSpan().IndexOfAny('/', '\\') >= 0) throw new ArgumentException("The name cannot have slashes or backslashes.", nameof(name));
		fixed (char* nameText = name)
		fixed (char* folderText = folder) {
			_native = WxKitNative.SingleInstanceCheckerNew(nameText, NativeString.Length(name), folderText, folder is null ? 0 : NativeString.Length(folder));
		}
		if (_native is null) throw new IOException($"wxWidgets could not check for another copy of {name}.");
	}

	/// <summary>Whether another copy, which made a checker with the same name first, is still running.</summary>
	public bool IsAnotherRunning => WxKitNative.SingleInstanceCheckerIsAnotherRunning(_native is not null ? _native : throw new ObjectDisposedException(nameof(SingleInstanceChecker))) != 0;

	/// <summary>Stops holding the name, so the next copy to start is the first.</summary>
	public void Dispose() {
		if (_native is null) return;
		WxKitNative.SingleInstanceCheckerDelete(_native);
		_native = null;
	}
}

/// <summary>
/// Keeps an application to one running copy, and hands what a second copy was started with, such as
/// files to open, to the first. Open a document while the application is running, and it opens in
/// the window that is already there.
/// </summary>
/// <remarks>
/// <para>
/// Call <see cref="Claim"/> at the start of Main, before <see cref="Application.Run(Action)"/>, with
/// what this copy was started with. When another copy is running, it gets the message, and Claim
/// returns null, so this copy can exit. Otherwise this copy is the first, and Claim returns the
/// <see cref="SingleInstance"/> that hears the copies started after it. Subscribe to
/// <see cref="MessageReceived"/> in Run's start-up action. Messages that come before then wait for it.
/// </para>
/// <para>
/// The copies talk over a named pipe on Windows, and a Unix socket on Linux and macOS, in a folder
/// only the user can open, so only copies the same user runs reach each other. On Windows, the
/// second copy lets the first bring its window to the front, which Windows otherwise refuses to a
/// process the user is not using.
/// </para>
/// </remarks>
public sealed partial class SingleInstance : IDisposable {
	// A message is its length, then a count of strings, then each string's length and UTF-8 bytes,
	// all as little-endian 32-bit numbers, which is doorbell's format, with the length before it.
	internal const int MaxMessageBytes = 1 << 20;
	private static readonly TimeSpan AnswerTimeout = TimeSpan.FromSeconds(5);
	private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(3);
	private readonly SingleInstanceChecker _checker;
	private readonly CancellationTokenSource _closing = new();
	private readonly ConcurrentQueue<IReadOnlyList<string>> _pending = new();
	private readonly Lock _gate = new();
	private readonly NamedPipeServerStream? _pipe;
	private readonly Socket? _socket;
	private readonly string? _socketPath;
	private EventHandler<SingleInstanceMessageEventArgs>? _messageReceived;

	private SingleInstance(SingleInstanceChecker checker, NamedPipeServerStream? pipe, Socket? socket, string? socketPath) {
		_checker = checker;
		_pipe = pipe;
		_socket = socket;
		_socketPath = socketPath;
		_ = Task.Run(ListenAsync);
	}

	/// <summary>
	/// A message from a copy started later, on the GUI thread. An empty one usually means it was
	/// started with nothing to open, so bring the window to the front.
	/// </summary>
	public event EventHandler<SingleInstanceMessageEventArgs>? MessageReceived {
		add {
			lock (_gate) _messageReceived += value;
			RaisePendingLater();
		}
		remove {
			lock (_gate) _messageReceived -= value;
		}
	}

	/// <summary>
	/// Finds out whether another copy of the application is running, and if one is, hands it a message.
	/// </summary>
	/// <param name="name">
	/// The application's name, such as myapp, of letters, digits, periods, hyphens, and underscores.
	/// Copies that different users run do not see each other.
	/// </param>
	/// <param name="message">
	/// What this copy was started with, such as its arguments. Turn relative paths into full ones,
	/// since the first copy can be running in another folder.
	/// </param>
	/// <returns>This copy's <see cref="SingleInstance"/> when it is the first, or null when another copy has the message.</returns>
	/// <exception cref="ArgumentException">The name has other characters, or the message is over 1 MB.</exception>
	/// <exception cref="IOException">Another copy is running, but did not answer in a few seconds.</exception>
	public static SingleInstance? Claim(string name, IEnumerable<string> message) {
		ArgumentException.ThrowIfNullOrEmpty(name);
		if (!name.All(static character => char.IsAsciiLetterOrDigit(character) || character is '.' or '-' or '_')) throw new ArgumentException("The name can only have letters, digits, periods, hyphens, and underscores.", nameof(name));
		byte[] frame = Encode(message);
		string user = string.Concat(Environment.UserName.Select(static character => char.IsAsciiLetterOrDigit(character) || character is '.' or '-' or '_' ? character : '_'));
		string id = $"{name}-{user}";
		string? folder = OperatingSystem.IsWindows() ? null : PrivateFolder();
		// Twice more when the other copy exits between being found and being reached.
		for (int attempt = 0; attempt < 3; attempt++) {
			SingleInstanceChecker checker = new(id, folder);
			if (!checker.IsAnotherRunning) {
				try {
					return OperatingSystem.IsWindows() ? new SingleInstance(checker, CreatePipe(id), null, null) : CreateSocket(checker, SocketPath(folder!, name));
				} catch {
					checker.Dispose();
					throw;
				}
			}
			checker.Dispose();
			bool delivered = OperatingSystem.IsWindows() ? DeliverToPipe(id, frame) : DeliverToSocket(SocketPath(folder!, name), frame);
			if (delivered) return null;
		}
		throw new IOException($"Another copy of {name} is running, but could not be reached.");
	}

	/// <summary>Stops hearing other copies, and lets the next copy to start be the first.</summary>
	public void Dispose() {
		if (_closing.IsCancellationRequested) return;
		_closing.Cancel();
		_pipe?.Dispose();
		_socket?.Dispose();
		if (_socketPath is not null) {
			try {
				File.Delete(_socketPath);
			} catch (IOException) {
			} catch (UnauthorizedAccessException) {
			}
		}
		_checker.Dispose();
	}

	internal static byte[] Encode(IEnumerable<string> message) {
		ArgumentNullException.ThrowIfNull(message);
		List<byte[]> parts = [.. message.Select(static part => Encoding.UTF8.GetBytes(part ?? throw new ArgumentException("The message cannot have null strings.", nameof(message))))];
		long length = 4 + parts.Sum(static part => 4L + part.Length);
		if (length > MaxMessageBytes) throw new ArgumentException("The message is over 1 MB.", nameof(message));
		byte[] frame = new byte[4 + length];
		BinaryPrimitives.WriteInt32LittleEndian(frame, (int)length);
		BinaryPrimitives.WriteInt32LittleEndian(frame.AsSpan(4), parts.Count);
		int offset = 8;
		foreach (byte[] part in parts) {
			BinaryPrimitives.WriteInt32LittleEndian(frame.AsSpan(offset), part.Length);
			part.CopyTo(frame, offset + 4);
			offset += 4 + part.Length;
		}
		return frame;
	}

	// Null for anything that is not a whole, well-formed message, so a stray connection cannot reach
	// the application.
	internal static List<string>? Decode(ReadOnlySpan<byte> bytes) {
		if (bytes.Length < 4) return null;
		int count = BinaryPrimitives.ReadInt32LittleEndian(bytes);
		bytes = bytes[4..];
		if (count < 0 || count > bytes.Length / 4) return null;
		List<string> parts = new(count);
		UTF8Encoding strict = new(false, true);
		for (int index = 0; index < count; index++) {
			if (bytes.Length < 4) return null;
			int length = BinaryPrimitives.ReadInt32LittleEndian(bytes);
			bytes = bytes[4..];
			if (length < 0 || length > bytes.Length) return null;
			try {
				parts.Add(strict.GetString(bytes[..length]));
			} catch (DecoderFallbackException) {
				return null;
			}
			bytes = bytes[length..];
		}
		return bytes.IsEmpty ? parts : null;
	}

	// The pipe's name includes the user's, and only the same user can open it. Only one pipe of the
	// name can be made, so a copy that is still exiting can hold it a moment longer.
	private static NamedPipeServerStream CreatePipe(string id) {
		for (int attempt = 0; ; attempt++) {
			try {
				return new NamedPipeServerStream(id, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
			} catch (Exception error) when (error is IOException or UnauthorizedAccessException && attempt < 20) {
				Thread.Sleep(100);
			}
		}
	}

	private static SingleInstance CreateSocket(SingleInstanceChecker checker, string path) {
		// The checker is free, so no copy is running, and a socket one that crashed left can go.
		File.Delete(path);
		Socket socket = new(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
		try {
			socket.Bind(new UnixDomainSocketEndPoint(path));
			socket.Listen();
			return new SingleInstance(checker, null, socket, path);
		} catch {
			socket.Dispose();
			throw;
		}
	}

	// A socket's path can be at most 104 bytes on macOS and 108 on Linux, and macOS's temporary
	// folder is long, so a name that would make it longer is shortened to a hash of it.
	internal static string SocketPath(string folder, string name) {
		const int MaxBytes = 100;
		string path = Path.Combine(folder, $"{name}.sock");
		if (Encoding.UTF8.GetByteCount(path) <= MaxBytes) return path;
		string hashed = Path.Combine(folder, $"{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(name)))[..16]}.sock");
		if (Encoding.UTF8.GetByteCount(hashed) <= MaxBytes) return hashed;
		throw new IOException($"{folder} is too long a path for the socket the copies talk over.");
	}

	// $XDG_RUNTIME_DIR where there is one, which only the user can open. Elsewhere, such as on macOS,
	// a folder of WxKit's own in the temporary folder, which on macOS is the user's own already.
	[UnsupportedOSPlatform("windows")]
	private static string PrivateFolder() {
		string? runtime = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
		if (!string.IsNullOrEmpty(runtime)) return runtime;
		string folder = Path.Combine(Path.GetTempPath(), $"wxkit-{Environment.UserName}");
		Directory.CreateDirectory(folder, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
		// Someone else could make it first in a shared temporary folder. If they did, either it is
		// not private, or it is theirs, and making the lock file in it fails.
		if ((File.GetUnixFileMode(folder) & (UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute)) != 0) throw new IOException($"{folder} can be opened by other users.");
		return folder;
	}

	// False when no copy is listening, as when the one found has just exited.
	private static bool DeliverToPipe(string id, byte[] frame) {
		// Windows only lets the process the user is using bring a window to the front, so this one
		// passes that on to the first copy.
		if (OperatingSystem.IsWindows()) _ = AllowSetForegroundWindow(-1);
		using NamedPipeClientStream pipe = new(".", id, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
		try {
			pipe.Connect(ConnectTimeout);
		} catch (TimeoutException) {
			return false;
		}
		Send(pipe, frame);
		return true;
	}

	private static bool DeliverToSocket(string path, byte[] frame) {
		Socket socket = new(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
		using NetworkStream? stream = Connect(socket, path);
		if (stream is null) return false;
		Send(stream, frame);
		return true;
	}

	// The first copy listens just after it claims its place, so this waits a moment for it.
	private static NetworkStream? Connect(Socket socket, string path) {
		DateTime deadline = DateTime.UtcNow + ConnectTimeout;
		while (true) {
			try {
				socket.Connect(new UnixDomainSocketEndPoint(path));
				return new NetworkStream(socket, true);
			} catch (SocketException) when (DateTime.UtcNow < deadline) {
				Thread.Sleep(100);
			} catch (SocketException) {
				socket.Dispose();
				return null;
			}
		}
	}

	// Writes the message, and waits for the first copy to answer that it has it, which one that is
	// stuck exiting never does.
	private static void Send(Stream stream, byte[] frame) {
		using CancellationTokenSource timeout = new(AnswerTimeout);
		try {
			Task<int> answer = SendAsync(stream, frame, timeout.Token);
			if (answer.GetAwaiter().GetResult() != 1) throw new IOException("Another copy is running, but did not take the message.");
		} catch (OperationCanceledException) {
			throw new IOException("Another copy is running, but did not answer in time.");
		}
	}

	private static async Task<int> SendAsync(Stream stream, byte[] frame, CancellationToken token) {
		await stream.WriteAsync(frame, token).ConfigureAwait(false);
		await stream.FlushAsync(token).ConfigureAwait(false);
		byte[] answer = new byte[1];
		return await stream.ReadAsync(answer, token).ConfigureAwait(false) == 1 ? answer[0] : 0;
	}

	private async Task ListenAsync() {
		CancellationToken closing = _closing.Token;
		while (!closing.IsCancellationRequested) {
			try {
				if (_pipe is not null) {
					await _pipe.WaitForConnectionAsync(closing).ConfigureAwait(false);
					try {
						await ReceiveAsync(_pipe, closing).ConfigureAwait(false);
					} finally {
						if (_pipe.IsConnected) _pipe.Disconnect();
					}
				} else {
					Socket connection = await _socket!.AcceptAsync(closing).ConfigureAwait(false);
					await using NetworkStream stream = new(connection, true);
					await ReceiveAsync(stream, closing).ConfigureAwait(false);
				}
			} catch (OperationCanceledException) when (closing.IsCancellationRequested) {
				return;
			} catch (ObjectDisposedException) when (closing.IsCancellationRequested) {
				return;
			} catch (Exception error) when (error is IOException or SocketException or OperationCanceledException or InvalidOperationException) {
				// A copy that sent something broken, or went away partway, is not this one's problem.
			}
		}
	}

	// Reads one message, so a copy that connects and never writes cannot hold up the ones after it
	// for longer than a few seconds, answers that it has it, and passes it on.
	private async Task ReceiveAsync(Stream stream, CancellationToken closing) {
		using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(closing);
		timeout.CancelAfter(AnswerTimeout);
		byte[] header = new byte[4];
		await stream.ReadExactlyAsync(header, timeout.Token).ConfigureAwait(false);
		int length = BinaryPrimitives.ReadInt32LittleEndian(header);
		if (length < 4 || length > MaxMessageBytes) return;
		byte[] body = new byte[length];
		await stream.ReadExactlyAsync(body, timeout.Token).ConfigureAwait(false);
		if (Decode(body) is not List<string> message) return;
		await stream.WriteAsync(new byte[] { 1 }, timeout.Token).ConfigureAwait(false);
		await stream.FlushAsync(timeout.Token).ConfigureAwait(false);
		_pending.Enqueue(message);
		RaisePendingLater();
	}

	private void RaisePendingLater() {
		lock (_gate) {
			if (_messageReceived is null || _pending.IsEmpty) return;
		}
		// Before the application runs, the messages wait for a handler added while it does.
		Application.TryPost(RaisePending);
	}

	private void RaisePending() {
		while (true) {
			EventHandler<SingleInstanceMessageEventArgs>? handler;
			lock (_gate) handler = _messageReceived;
			if (handler is null || !_pending.TryDequeue(out IReadOnlyList<string>? message)) return;
			handler(this, new SingleInstanceMessageEventArgs(message));
		}
	}

	[LibraryImport("user32.dll")]
	[SupportedOSPlatform("windows")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool AllowSetForegroundWindow(int processId);
}

/// <summary>What a copy of the application started later was started with.</summary>
public sealed class SingleInstanceMessageEventArgs(IReadOnlyList<string> message) : EventArgs {
	/// <summary>The strings the copy passed to <see cref="SingleInstance.Claim"/>, such as files to open.</summary>
	public IReadOnlyList<string> Message { get; } = message;
}
