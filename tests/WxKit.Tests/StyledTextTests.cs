namespace WxKit.Tests;

// The styled text control, Scintilla, with its constants in Stc.
public class StyledTextTests {
	[Fact]
	public Task EditsText() => Gui.WithFrame(frame => {
		StyledTextCtrl editor = new(frame);
		editor.Text = "first line\nsecond line";
		Assert.Equal("first line\nsecond line", editor.Text);
		Assert.Equal(2, editor.LineCount);
		editor.AppendText("\nthird line");
		Assert.Equal(3, editor.LineCount);
		editor.GotoLine(2);
		Assert.Equal(2, editor.CurrentLine);
		Assert.Equal(editor.Text.Length, editor.TextLength);
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
		editor.Text = "def f():\n    return 1\n";
		Assert.Equal(Stc.MarginNumber, editor.GetMarginType(0));
	});

	[Fact]
	public Task RaisesModifiedAndTracksTheSavePoint() => Gui.WithFrame(frame => {
		StyledTextCtrl editor = new(frame);
		int modified = 0;
		editor.Modified += (_, e) => {
			if ((e.ModificationType & Stc.ModInserttext) != 0) modified++;
		};
		editor.Text = "hello";
		Assert.True(modified > 0);
		editor.SetSavePoint();
		Assert.False(editor.Modify);
		editor.AppendText(" world");
		Assert.True(editor.Modify);
	});
}
