using WxKit;

namespace Scrapbook;

// Something kept in the scrapbook: text, or a list of files, as the clipboard and a drag carry them.
internal abstract record Snippet {
	// What the list shows for it.
	public abstract string Describe();

	// It, as the clipboard and a drag take it.
	public abstract ClipboardContent ToContent();

	// What's on the clipboard, preferring files, as a file manager copies them, or nothing when it
	// has neither files nor text.
	public static Snippet? FromClipboard() {
		if (Clipboard.HasFiles && Clipboard.GetFiles() is [_, ..] files) return new FilesSnippet { Files = files };
		if (Clipboard.HasText && Clipboard.GetText() is { Length: > 0 } text) return new TextSnippet { Text = text };
		return null;
	}
}

internal sealed record TextSnippet : Snippet {
	public required string Text { get; init; }

	// The first line, cut short when it's long.
	public override string Describe() {
		string line = Text.Split('\n')[0].Trim();
		return line.Length > 60 ? line[..60] + "..." : line;
	}

	public override ClipboardContent ToContent() => new() { Text = Text };
}

internal sealed record FilesSnippet : Snippet {
	public required IReadOnlyList<string> Files { get; init; }

	public override string Describe() => Files.Count == 1 ? $"File: {Path.GetFileName(Files[0])}" : $"{Files.Count} files: {string.Join(", ", Files.Select(Path.GetFileName))}";

	// Files, and their paths as text, for an application that only takes text.
	public override ClipboardContent ToContent() => new() { Files = Files, Text = string.Join(Environment.NewLine, Files) };
}
