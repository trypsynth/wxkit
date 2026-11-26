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
		editor.Lexer = StyledTextLexer.Python;
		Assert.Equal(StyledTextLexer.Python, editor.Lexer);
		editor.SetKeyWords(0, "def class return");
		editor.StyleSetForeground(StyledText.PWord, new Color(0, 0, 255));
		editor.SetMarginType(0, StyledTextMarginType.Number);
		editor.SetMarginWidth(0, 40);
		editor.Value = "def f():\n    return 1\n";
		Assert.Equal(StyledTextMarginType.Number, editor.GetMarginType(0));
	});

	[Fact]
	public Task RaisesModifiedAndTracksTheSavePoint() => Gui.WithFrame(frame => {
		StyledTextCtrl editor = new(frame);
		int modified = 0;
		editor.Modified += (_, e) => {
			if ((e.ModificationType & StyledTextModificationFlags.InsertText) != 0) modified++;
		};
		editor.Value = "hello";
		Assert.True(modified > 0);
		editor.SetSavePoint();
		Assert.False(editor.IsModified);
		editor.AppendText(" world");
		Assert.True(editor.IsModified);
	});

	// The control's choices and flags are enums, where Scintilla has ints.
	[Fact]
	public Task TakesItsConstantsAsEnums() => Gui.WithFrame(frame => {
		StyledTextCtrl editor = new(frame) { Value = "one two one" };
		editor.WrapMode = StyledTextWrapMode.Word;
		Assert.Equal(StyledTextWrapMode.Word, editor.WrapMode);
		editor.SearchFlags = StyledTextFindFlags.WholeWord | StyledTextFindFlags.MatchCase;
		Assert.Equal(StyledTextFindFlags.WholeWord | StyledTextFindFlags.MatchCase, editor.SearchFlags);
		Assert.Equal(8, editor.FindText(1, editor.LastPosition, "one", StyledTextFindFlags.WholeWord));
		editor.MarkerDefine(1, StyledTextMarkerSymbol.Arrow);
		Assert.Equal(StyledTextMarkerSymbol.Arrow, editor.GetMarkerSymbolDefined(1));
		editor.CmdKeyAssign('Q', StyledTextKeyModifiers.Control, StyledTextCommand.LineDelete);
		editor.CmdKeyExecute(StyledTextCommand.DocumentEnd);
		Assert.Equal(editor.LastPosition, editor.InsertionPoint);
		Assert.True(editor.IsSelectionEmpty);
		Assert.Equal(1, editor.SelectionCount);
		editor.AnnotationVisible = StyledTextAnnotationVisible.Boxed;
		Assert.Equal(StyledTextAnnotationVisible.Boxed, editor.AnnotationVisible);
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
