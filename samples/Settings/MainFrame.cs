using WxKit;
using static WxKit.Gettext;

namespace Settings;

// The sample's window, which lists the settings and opens the dialog that changes them.
internal sealed class MainFrame : Frame {
	private readonly AppSettings _settings;
	private readonly TextCtrl _summary;

	public MainFrame(AppSettings settings) : base(null, _("Settings sample"), size: new Size(520, 420)) {
		_settings = settings;
		if (Languages.IsRightToLeft) LayoutDirection = LayoutDirection.RightToLeft;
		// On macOS, Preferences goes in the application menu, where Mac applications have it, and
		// Exit becomes Quit there. Exit's label is wxWidgets', which it translates itself.
		Menu file = new();
		file.Append(StandardId.Exit).Click += (_, _) => Close();
		Menu edit = new();
		// TRANSLATORS: Keep the tab and the shortcut after it, which the menu shows and the keyboard uses.
		edit.Append(StandardId.Preferences, _("&Settings...\tCtrl+,")).Click += (_, _) => EditSettings();
		MenuBar menuBar = new();
		menuBar.Append(file, _("&File"));
		menuBar.Append(edit, _("&Edit"));
		MenuBar = menuBar;
		// The settings are read-only text, which a screen reader can read line by line.
		Panel panel = new(this);
		StaticText label = new(panel, _("&Current settings:"));
		_summary = new TextCtrl(panel, style: TextCtrlStyle.MultiLine | TextCtrlStyle.ReadOnly);
		Button change = new(panel, _("&Change settings..."));
		change.Click += (_, _) => EditSettings();
		BoxSizer sizer = new(Orientation.Vertical);
		sizer.Add(label, SizerFlags.Default.Border(Direction.Left | Direction.Right | Direction.Top));
		sizer.Add(_summary, new SizerFlags(1).Expand().Border());
		sizer.Add(change, SizerFlags.Default.Border(Direction.Left | Direction.Right | Direction.Bottom));
		panel.Sizer = sizer;
		ShowSummary();
		Center();
	}

	private void EditSettings() {
		string language;
		using (SettingsDialog dialog = new(this, _settings)) {
			if (dialog.ShowModal() != StandardId.Ok) return;
			language = _settings.Language;
			dialog.ApplyTo(_settings);
		}
		try {
			_settings.Save(AppSettings.DefaultPath);
		} catch (Exception error) when (error is IOException or UnauthorizedAccessException) {
			MessageBox.Show(_("The settings could not be saved. {0}", error.Message), _("Settings sample"), MessageDialogStyle.Ok | MessageDialogStyle.IconError, this);
		}
		if (_settings.Language == language) {
			ShowSummary();
			return;
		}
		// A window's text is translated when it is made, so a new language takes a new window.
		Languages.Use(_settings.Language);
		MainFrame frame = new(_settings);
		frame.Position = Position;
		frame.Show();
		Close();
	}

	// For the smoke test: opens the dialog, and cancels it once its modal loop runs.
	internal void OpenAndCancelSettings() {
		using SettingsDialog dialog = new(this, _settings);
		Application.Post(() => dialog.EndModal(StandardId.Cancel));
		dialog.ShowModal();
	}

	private void ShowSummary() {
		string font = _settings.Font.Length == 0 ? _("the default") : DescribeFont(_settings.Font);
		string notifications = _settings.Notifications.Count == 0 ? _("none") : string.Join(", ", AppSettings.Events.Where((_, index) => _settings.Notifications.Contains(index)));
		string language = _settings.Language.Length == 0 ? _("the system's") : LanguageInfo.Find(_settings.Language)?.NativeName ?? _settings.Language;
		_summary.ChangeValue(string.Join('\n', [
			_("Name: {0}", _settings.Name),
			_("Theme: {0}", AppSettings.Themes[_settings.Theme]),
			_("Accent color: red {0}, green {1}, blue {2}", _settings.Accent.R, _settings.Accent.G, _settings.Accent.B),
			_("Font: {0}", font),
			_("Recent files remembered: {0}", _settings.RecentFiles),
			_("Documents folder: {0}", _settings.DocumentsFolder),
			_("Language: {0}", language),
			_("Start when you sign in: {0}", _settings.StartWithSystem ? _("yes") : _("no")),
			_("Notify when: {0}", notifications),
		]));
	}

	private static string DescribeFont(string description) {
		using Font font = new(description);
		return _n("{0}, {1} point", "{0}, {1} points", font.PointSize, font.FaceName, font.PointSize);
	}
}
