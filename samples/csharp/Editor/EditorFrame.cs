using System.Globalization;
using WxKit;

namespace Editor;

// The editor's window: one text control, its menus, and the file it shows.
internal sealed class EditorFrame : Frame {
	private const string AppName = "Editor";
	private const string Wildcard = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
	// Ids of the commands without a standard one, above the ids wxWidgets keeps for itself.
	private const int FindNextId = 6001;
	private const int GoToLineId = 6002;
	private const int WordWrapId = 6003;
	private readonly FindReplaceData _findData = new(FindReplaceFlags.Down);
	private TextCtrl _text;
	private FindReplaceDialog? _findDialog;
	private bool _findDialogReplaces;
	private string? _path;
	// The line ending the file had, which saving keeps. The control itself only knows "\n".
	private string _newLine = Environment.NewLine;
	private bool _wordWrap = true;
	private string _status = string.Empty;
	// The printer and page settings, which the page setup dialog and printing keep between uses.
	private PrintData _printData = new();
	private PageSetupDialogData _pageSetup = new();

	public EditorFrame() : base(null, AppName, size: new Size(800, 600)) {
		_text = CreateText(string.Empty);
		MenuBar = CreateMenus();
		CreateStatusBar();
		Closing += OnClosing;
		// The menu items, and the text control, ask whether they apply as the window goes idle.
		Bind(EventType.UpdateUi, OnUpdateUi);
		UpdateTitle();
	}

	private string DocumentName => _path is null ? "Untitled" : Path.GetFileName(_path);

	// Opens a file into the editor, after asking about unsaved changes.
	public async Task OpenAsync(string path) {
		string contents;
		try {
			contents = await File.ReadAllTextAsync(path);
		} catch (Exception error) when (error is IOException or UnauthorizedAccessException) {
			MessageBox.Show($"{AppName} could not open {path}. {error.Message}", AppName, MessageDialogStyle.Ok | MessageDialogStyle.IconError, this);
			return;
		}
		_newLine = contents.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
		_text.ChangeValue(contents.Replace("\r\n", "\n", StringComparison.Ordinal));
		_text.DiscardEdits();
		_text.InsertionPoint = 0;
		_path = path;
		UpdateTitle();
	}

	private TextCtrl CreateText(string value) {
		TextCtrlStyle wrap = _wordWrap ? TextCtrlStyle.WordWrap : TextCtrlStyle.DontWrap | TextCtrlStyle.HorizontalScrollBar;
		// A rich text control on Windows counts a line break as one character, as the text does,
		// so positions in one are positions in the other. Tab types a tab, as in any editor.
		TextCtrl text = new(this, value, style: TextCtrlStyle.MultiLine | TextCtrlStyle.Rich2 | TextCtrlStyle.ProcessTab | wrap);
		// Windows and macOS check spelling as you type. Linux, without gspell, does not.
		text.EnableProofCheck();
		// A file dropped on the text opens, as File > Open does, rather than being inserted.
		text.DragAcceptFiles(true);
		text.DropFiles += async (_, e) => {
			string[] files = e.Files;
			if (files.Length > 0) await DropAndOpenAsync(files[0]);
		};
		return text;
	}

	private MenuBar CreateMenus() {
		// With a standard id and no text, an item gets the platform's label and shortcut.
		Menu file = new();
		file.Append(StandardId.New).Click += async (_, _) => await NewAsync();
		file.Append(StandardId.Open).Click += async (_, _) => await ChooseAndOpenAsync();
		file.Append(StandardId.Save).Click += async (_, _) => await SaveAsync(saveAs: false);
		file.Append(StandardId.SaveAs).Click += async (_, _) => await SaveAsync(saveAs: true);
		file.AppendSeparator();
		file.Append(StandardId.PageSetup).Click += (_, _) => SetUpPage();
		file.Append(StandardId.Preview).Click += (_, _) => PreviewPrint();
		file.Append(StandardId.Print).Click += (_, _) => Print();
		file.AppendSeparator();
		file.Append(StandardId.Exit).Click += (_, _) => Close();
		Menu edit = new();
		edit.Append(StandardId.Undo).Click += (_, _) => _text.Undo();
		edit.Append(StandardId.Redo).Click += (_, _) => _text.Redo();
		edit.AppendSeparator();
		edit.Append(StandardId.Cut).Click += (_, _) => _text.Cut();
		edit.Append(StandardId.Copy).Click += (_, _) => _text.Copy();
		edit.Append(StandardId.Paste).Click += (_, _) => _text.Paste();
		edit.Append(StandardId.Delete).Click += (_, _) => DeleteSelection();
		edit.AppendSeparator();
		edit.Append(StandardId.Find).Click += (_, _) => ShowFindDialog(replace: false);
		edit.Append(FindNextId, "Find &Next\tF3").Click += (_, _) => FindNext(_findData.FindString, _findData.Flags);
		edit.Append(StandardId.Replace).Click += (_, _) => ShowFindDialog(replace: true);
		edit.Append(GoToLineId, "&Go To Line...\tCtrl+G").Click += (_, _) => GoToLine();
		edit.AppendSeparator();
		edit.Append(StandardId.SelectAll).Click += (_, _) => _text.SelectAll();
		Menu format = new();
		format.Append(WordWrapId, "&Word Wrap", kind: MenuItemKind.Check).Click += (_, _) => ToggleWordWrap();
		format.Append(StandardId.SelectFont).Click += (_, _) => ChooseFont();
		Menu help = new();
		help.Append(StandardId.About).Click += (_, _) => AboutBox.Show(new AboutDialogInfo {
			Name = AppName,
			Description = $"A text editor, as a WxKit sample, built with {Application.WxWidgetsVersion}.",
			WebSite = "https://github.com/trypsynth/wxkit",
		}, this);
		MenuBar bar = new();
		bar.Append(file, "&File");
		bar.Append(edit, "&Edit");
		bar.Append(format, "F&ormat");
		bar.Append(help, "&Help");
		return bar;
	}

	private void OnUpdateUi(object? sender, UpdateUIEvent e) {
		switch (e.Id) {
			case StandardId.Undo:
				e.Enable(_text.CanUndo);
				break;
			case StandardId.Redo:
				e.Enable(_text.CanRedo);
				break;
			case StandardId.Cut or StandardId.Delete:
				e.Enable(_text.CanCut);
				break;
			case StandardId.Copy:
				e.Enable(_text.CanCopy);
				break;
			case StandardId.Paste:
				e.Enable(_text.CanPaste);
				break;
			case WordWrapId:
				e.Check(_wordWrap);
				break;
			default:
				if (e.Id == _text.Id) UpdateStatus();
				break;
		}
	}

	private void UpdateStatus() {
		_text.PositionToXY(_text.InsertionPoint, out long column, out long line);
		string status = $"Line {line + 1}, column {column + 1}";
		if (status != _status) {
			_status = status;
			SetStatusText(status);
		}
		UpdateTitle();
	}

	private void UpdateTitle() {
		string title = $"{(_text.IsModified ? "*" : string.Empty)}{DocumentName} - {AppName}";
		if (Title != title) Title = title;
	}

	// Asks what to do with unsaved changes. Returns false when the user cancels, or saving fails.
	private async Task<bool> ConfirmDiscardAsync() {
		if (!_text.IsModified) return true;
		int answer = MessageBox.Show($"Do you want to save the changes to {DocumentName}?", AppName, MessageDialogStyle.YesNo | MessageDialogStyle.Cancel | MessageDialogStyle.IconWarning, this);
		return answer switch {
			StandardId.Yes => await SaveAsync(saveAs: false),
			StandardId.No => true,
			_ => false,
		};
	}

	// A close the user asks for waits for an answer about unsaved changes, which may mean saving,
	// so it is vetoed, and the window closes itself once the answer allows it.
	private void OnClosing(object? sender, CloseEvent e) {
		if (!e.CanVeto || !_text.IsModified) return;
		e.Veto();
		_ = CloseWhenConfirmedAsync();
	}

	private async Task CloseWhenConfirmedAsync() {
		if (await ConfirmDiscardAsync()) Close(force: true);
	}

	private async Task NewAsync() {
		if (!await ConfirmDiscardAsync()) return;
		_text.ChangeValue(string.Empty);
		_text.DiscardEdits();
		_path = null;
		_newLine = Environment.NewLine;
		UpdateTitle();
	}

	private async Task ChooseAndOpenAsync() {
		if (!await ConfirmDiscardAsync()) return;
		using FileDialog dialog = new(this, "Open", wildcard: Wildcard, style: FileDialogStyle.Open | FileDialogStyle.FileMustExist);
		if (dialog.ShowModal() != StandardId.Ok) return;
		await OpenAsync(dialog.Path);
	}

	private async Task DropAndOpenAsync(string path) {
		if (!await ConfirmDiscardAsync()) return;
		await OpenAsync(path);
	}

	// Saves to the file the editor shows, or asks where first. Returns false when the user cancels, or saving fails.
	private async Task<bool> SaveAsync(bool saveAs) {
		string? path = _path;
		if (saveAs || path is null) {
			using FileDialog dialog = new(this, "Save As", defaultFile: path is null ? "Untitled.txt" : Path.GetFileName(path), wildcard: Wildcard, style: FileDialogStyle.Save | FileDialogStyle.OverwritePrompt);
			if (dialog.ShowModal() != StandardId.Ok) return false;
			path = dialog.Path;
		}
		try {
			await File.WriteAllTextAsync(path, _text.Value.Replace("\n", _newLine, StringComparison.Ordinal));
		} catch (Exception error) when (error is IOException or UnauthorizedAccessException) {
			MessageBox.Show($"{AppName} could not save {path}. {error.Message}", AppName, MessageDialogStyle.Ok | MessageDialogStyle.IconError, this);
			return false;
		}
		_path = path;
		_text.DiscardEdits();
		UpdateTitle();
		return true;
	}

	private void DeleteSelection() {
		_text.GetSelection(out long from, out long to);
		if (to > from) _text.Remove(from, to);
	}

	// Find and replace share one modeless dialog, which starts with the selected text, if any.
	private void ShowFindDialog(bool replace) {
		string selected = _text.StringSelection;
		if (selected.Length > 0 && !selected.Contains('\n', StringComparison.Ordinal)) _findData.FindString = selected;
		if (_findDialog is not null) {
			if (_findDialogReplaces == replace) {
				_findDialog.Raise();
				return;
			}
			_findDialog.Destroy();
		}
		FindReplaceDialog dialog = new(this, _findData, replace ? "Replace" : "Find", replace ? FindReplaceDialogStyle.ReplaceDialog : 0);
		dialog.Find += (_, e) => FindNext(e.FindString, e.Flags);
		dialog.FindNext += (_, e) => FindNext(e.FindString, e.Flags);
		dialog.Replace += (_, e) => Replace(e.FindString, e.ReplaceString, e.Flags);
		dialog.ReplaceAll += (_, e) => ReplaceAll(e.FindString, e.ReplaceString, e.Flags);
		dialog.FindClose += (_, _) => {
			_findDialog?.Destroy();
			_findDialog = null;
		};
		_findDialog = dialog;
		_findDialogReplaces = replace;
		dialog.Show();
	}

	// Selects the next match after the selection, or the one before it, going up.
	private bool FindNext(string needle, FindReplaceFlags flags) {
		if (needle.Length == 0) {
			ShowFindDialog(replace: false);
			return false;
		}
		_text.GetSelection(out long from, out long to);
		bool down = flags.HasFlag(FindReplaceFlags.Down);
		int found = Search(_text.Value, needle, down ? (int)to : (int)from - 1, down, flags);
		if (found < 0) {
			MessageBox.Show($"{AppName} cannot find \"{needle}\".", AppName, MessageDialogStyle.Ok | MessageDialogStyle.IconInformation, (Window?)_findDialog ?? this);
			return false;
		}
		_text.SetSelection(found, found + needle.Length);
		_text.ShowPosition(found);
		return true;
	}

	private void Replace(string needle, string replacement, FindReplaceFlags flags) {
		_text.GetSelection(out long from, out long to);
		if (to > from && string.Equals(_text.GetRange(from, to), needle, Comparison(flags))) {
			_text.Replace(from, to, replacement);
			_text.SetSelection(from, from + replacement.Length);
		}
		FindNext(needle, flags);
	}

	private void ReplaceAll(string needle, string replacement, FindReplaceFlags flags) {
		if (needle.Length == 0) return;
		string text = _text.Value;
		System.Text.StringBuilder result = new();
		int count = 0;
		int start = 0;
		for (int found = Search(text, needle, 0, down: true, flags); found >= 0; found = Search(text, needle, start, down: true, flags)) {
			result.Append(text, start, found - start).Append(replacement);
			start = found + needle.Length;
			count++;
		}
		if (count > 0) {
			result.Append(text, start, text.Length - start);
			_text.Value = result.ToString();
			_text.MarkDirty();
		}
		string occurrences = count == 1 ? "occurrence" : "occurrences";
		MessageBox.Show($"{AppName} replaced {count} {occurrences} of \"{needle}\".", AppName, MessageDialogStyle.Ok | MessageDialogStyle.IconInformation, (Window?)_findDialog ?? this);
	}

	private static StringComparison Comparison(FindReplaceFlags flags) => flags.HasFlag(FindReplaceFlags.MatchCase) ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;

	// The index of the first match at or after start, or, going up, the last one that ends at or
	// before it, which is a whole word when the flags say so. -1 when there is none.
	private static int Search(string text, string needle, int start, bool down, FindReplaceFlags flags) {
		StringComparison comparison = Comparison(flags);
		bool wholeWord = flags.HasFlag(FindReplaceFlags.WholeWord);
		while (true) {
			int found;
			if (down) {
				found = start > text.Length ? -1 : text.IndexOf(needle, Math.Max(start, 0), comparison);
			} else {
				found = start < 0 || text.Length == 0 ? -1 : text.LastIndexOf(needle, Math.Min(start + needle.Length - 1, text.Length - 1), comparison);
			}
			if (found < 0 || !wholeWord || IsWholeWord(text, found, needle.Length)) return found;
			start = down ? found + 1 : found - 1;
		}
	}

	private static bool IsWholeWord(string text, int start, int length) {
		bool before = start == 0 || !IsWordCharacter(text[start - 1]);
		bool after = start + length == text.Length || !IsWordCharacter(text[start + length]);
		return before && after;
	}

	private static bool IsWordCharacter(char character) => char.IsLetterOrDigit(character) || character == '_';

	private void GoToLine() {
		_text.PositionToXY(_text.InsertionPoint, out _, out long current);
		int lines = _text.NumberOfLines;
		using TextEntryDialog dialog = new(this, $"Line number, from 1 to {lines}:", "Go To Line", (current + 1).ToString(CultureInfo.CurrentCulture));
		if (dialog.ShowModal() != StandardId.Ok) return;
		if (!int.TryParse(dialog.Value, NumberStyles.Integer, CultureInfo.CurrentCulture, out int line) || line < 1 || line > lines) {
			MessageBox.Show($"The line number has to be from 1 to {lines}.", "Go To Line", MessageDialogStyle.Ok | MessageDialogStyle.IconWarning, this);
			return;
		}
		long position = _text.XYToPosition(0, line - 1);
		_text.InsertionPoint = position;
		_text.ShowPosition(position);
	}

	// A text control wraps lines or not from when it is created, so turning wrapping on or off
	// replaces it with one that does, keeping the text, the selection, the font, and whether it changed.
	private void ToggleWordWrap() {
		_wordWrap = !_wordWrap;
		TextCtrl old = _text;
		old.GetSelection(out long from, out long to);
		bool modified = old.IsModified;
		using Font? font = old.Font;
		_text = CreateText(old.Value);
		if (font is not null) _text.Font = font;
		_text.SetSelection(from, to);
		if (modified) _text.MarkDirty();
		old.Destroy();
		SendSizeEvent();
		_text.SetFocus();
	}

	private void SetUpPage() {
		_pageSetup.PrintData = _printData;
		using PageSetupDialog dialog = new(this, _pageSetup);
		if (dialog.ShowModal() != StandardId.Ok) return;
		_pageSetup = dialog.PageSetupData ?? _pageSetup;
		_printData = _pageSetup.PrintData ?? _printData;
	}

	private TextPrintout CreatePrintout() {
		using Font? font = _text.Font;
		return new TextPrintout(DocumentName, _text.Value, font);
	}

	// The preview takes its printouts, one to show and one for its Print button.
	private void PreviewPrint() {
		using PrintDialogData data = new() { PrintData = _printData };
		Frame? preview = PrintPreview.Show(CreatePrintout(), CreatePrintout(), this, $"Print Preview of {DocumentName}", data, new Size(700, 800));
		if (preview is null) MessageBox.Show("There is no printer to preview for.", AppName, MessageDialogStyle.Ok | MessageDialogStyle.IconWarning, this);
	}

	// For the smoke test: makes a preview, which counts and draws the first page, and closes it.
	internal void PreviewAndClose() {
		using PrintDialogData data = new() { PrintData = _printData };
		PrintPreview.Show(CreatePrintout(), null, this, "Smoke test", data)?.Close(force: true);
	}

	private void Print() {
		using PrintDialogData data = new() { PrintData = _printData };
		using Printer printer = new(data);
		using TextPrintout printout = CreatePrintout();
		if (printer.Print(this, printout)) {
			_printData = printer.PrintDialogData?.PrintData ?? _printData;
		} else if (Printer.LastError == PrinterError.Error) {
			MessageBox.Show($"{AppName} could not print {DocumentName}.", AppName, MessageDialogStyle.Ok | MessageDialogStyle.IconError, this);
		}
	}

	private void ChooseFont() {
		using FontData data = new();
		using Font? current = _text.Font;
		if (current is not null) data.InitialFont = current;
		using FontDialog dialog = new(this, data);
		if (dialog.ShowModal() != StandardId.Ok) return;
		using FontData? chosen = dialog.FontData;
		using Font? font = chosen?.ChosenFont;
		if (font is not null) _text.Font = font;
	}
}
