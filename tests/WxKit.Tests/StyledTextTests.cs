namespace WxKit.Tests;

// The styled text control, Scintilla, with its constants in Stc.
public class StyledTextTests {
	[Fact]
	public Task EditsText() => Gui.WithFrame(frame => {
		StyledTextCtrl editor = new(frame);
		editor.Value = "first line\nsecond line";
		Assert.Equal("first line\nsecond line", editor.Value);
		Assert.Equal(2, editor.LineCount);
		editor.AppendText("\nthird line");
		Assert.Equal(3, editor.LineCount);
		editor.GotoLine(2);
		Assert.Equal(2, editor.CurrentLine);
		Assert.Equal(editor.Value.Length, editor.LastPosition);
	});

	[Fact]
	public Task HighlightsWithALexer() => Gui.WithFrame(frame => {
		StyledTextCtrl editor = new(frame);
		editor.Lexer = Stc.LexPython;
		Assert.Equal(Stc.LexPython, editor.Lexer);
		editor.SetKeyWords(0, "def class return");
		editor.StyleSetForeground(Stc.PWord, new Color(0, 0, 255));
		editor.SetMarginType(0, Stc.MarginNumber);
		editor.SetMarginWidth(0, 40);
		editor.Value = "def f():\n    return 1\n";
		Assert.Equal(Stc.MarginNumber, editor.GetMarginType(0));
	});

	[Fact]
	public Task RaisesModifiedAndTracksTheSavePoint() => Gui.WithFrame(frame => {
		StyledTextCtrl editor = new(frame);
		int modified = 0;
		editor.Modified += (_, e) => {
			if ((e.ModificationType & Stc.ModInserttext) != 0) modified++;
		};
		editor.Value = "hello";
		Assert.True(modified > 0);
		editor.SetSavePoint();
		Assert.False(editor.IsModified);
		editor.AppendText(" world");
		Assert.True(editor.IsModified);
	});

	// Scintilla numbers the modifier keys its own way, which an event gives as KeyModifiers.
	[Fact]
	public void ConvertsScintillasModifiers() {
		Assert.Equal(KeyModifiers.None, StyledTextEvent.FromScintilla(0));
		Assert.Equal(KeyModifiers.Shift, StyledTextEvent.FromScintilla(1));
		Assert.Equal(KeyModifiers.Control | KeyModifiers.Alt, StyledTextEvent.FromScintilla(2 | 4));
		Assert.Equal(KeyModifiers.Meta, StyledTextEvent.FromScintilla(8));
		Assert.Equal(KeyModifiers.RawControl | KeyModifiers.Shift, StyledTextEvent.FromScintilla(16 | 1));
	}
}
