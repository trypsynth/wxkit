namespace Library;

// A book in the library, which has to be given its title, author, and year, and starts unread. It
// doesn't change: marking it read makes a changed copy, with with.
internal sealed record Book {
	public required string Title { get; init; }

	public required string Author { get; init; }

	public required int Year { get; init; }

	public bool IsRead { get; init; }

	// Whether the title or the author has the text in it, whatever its case.
	public bool Matches(string text) => Title.Contains(text, StringComparison.CurrentCultureIgnoreCase) || Author.Contains(text, StringComparison.CurrentCultureIgnoreCase);
}

// The columns of the list, which are the model's column numbers.
internal enum BookColumn {
	Title,
	Author,
	Year,
	IsRead,
}
