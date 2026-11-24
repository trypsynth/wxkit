using System.Security.Cryptography;

namespace Checksums;

// A file's SHA-256, or why it couldn't be read. A checksum has to be given the file's name, and
// either its hash or the error.
internal sealed record Checksum {
	public required string Name { get; init; }

	public string? Hash { get; init; }

	public string? Error { get; init; }

	public override string ToString() => Hash is null ? $"{Name}: {Error}" : $"{Name}: {Hash}";

	// Hashes the files in order, on whatever thread calls it, telling progress about each before it
	// starts, and stopping between files once the token is canceled.
	public static List<Checksum> Compute(IReadOnlyList<string> files, IProgress<(int Done, string Name)> progress, CancellationToken token) {
		List<Checksum> sums = [];
		for (int index = 0; index < files.Count; index++) {
			token.ThrowIfCancellationRequested();
			string name = Path.GetFileName(files[index]);
			progress.Report((index, name));
			try {
				using FileStream stream = File.OpenRead(files[index]);
				sums.Add(new Checksum { Name = name, Hash = Convert.ToHexStringLower(SHA256.HashData(stream)) });
			} catch (Exception error) when (error is IOException or UnauthorizedAccessException) {
				sums.Add(new Checksum { Name = name, Error = error.Message });
			}
		}
		return sums;
	}
}
