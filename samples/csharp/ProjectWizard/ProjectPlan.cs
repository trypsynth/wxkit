namespace ProjectWizard;

internal enum ProjectKind {
	Console,
	Desktop,
	Library,
}

// What the wizard asks for. A plan has to be given its name, folder, and kind, and the desktop
// options only matter to a desktop application.
internal sealed record ProjectPlan {
	public static readonly string[] KindNames = ["Console application", "Desktop application", "Library"];

	public required string Name { get; init; }

	public required string Folder { get; init; }

	public required ProjectKind Kind { get; init; }

	public bool HasMenuBar { get; init; }

	public bool HasStatusBar { get; init; }

	public bool IsTranslated { get; init; }

	// The plan as lines of text, which a screen reader reads one at a time.
	public string Describe() {
		List<string> lines = [$"Name: {Name}", $"Folder: {Path.Combine(Folder, Name)}", $"Kind: {KindNames[(int)Kind]}"];
		if (Kind == ProjectKind.Desktop) {
			lines.Add($"Menu bar: {(HasMenuBar ? "yes" : "no")}");
			lines.Add($"Status bar: {(HasStatusBar ? "yes" : "no")}");
			lines.Add($"Translated: {(IsTranslated ? "yes" : "no")}");
		}
		return string.Join('\n', lines);
	}
}
