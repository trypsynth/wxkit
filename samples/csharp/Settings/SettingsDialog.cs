using WxKit;
using static WxKit.Gettext;

namespace Settings;

// The settings, a page each in a notebook, with OK and Cancel. OK runs the validators on every
// page first, and the dialog stays open while one fails.
internal sealed class SettingsDialog : Dialog {
	private readonly TextCtrl _name;
	private readonly Choice _theme;
	private readonly ColorPickerCtrl _accent;
	private readonly FontPickerCtrl _font;
	private readonly SpinCtrl _recentFiles;
	private readonly Choice _language;
	private readonly List<string> _languageCodes = [string.Empty];
	private readonly CheckBox _startWithSystem;
	private readonly DirPickerCtrl _documents;
	private readonly List<CheckBox> _notifications = [];

	public SettingsDialog(Window parent, AppSettings settings) : base(parent, _("Settings"), style: DialogStyle.Default | DialogStyle.ResizeBorder) {
		if (Languages.IsRightToLeft) LayoutDirection = LayoutDirection.RightToLeft;
		// The notebook comes before the buttons, so the Tab key reaches the pages first.
		Notebook pages = new(this);
		// The general page is a form: a label in the first column and its field in the second,
		// each label made just before its field, so a screen reader names the field after it.
		Panel general = new(pages);
		GridBagSizer form = new(5, 10);
		AddField(form, 0, new StaticText(general, _("&Name:")), _name = new TextCtrl(general, settings.Name));
		_name.Validator = new NameRequired();
		AddField(form, 1, new StaticText(general, _("&Theme:")), _theme = new Choice(general, AppSettings.Themes));
		_theme.Selection = settings.Theme;
		AddField(form, 2, new StaticText(general, _("&Accent color:")), _accent = new ColorPickerCtrl(general, settings.Accent));
		using Font? font = settings.Font.Length == 0 ? null : new Font(settings.Font);
		AddField(form, 3, new StaticText(general, _("&Font:")), _font = new FontPickerCtrl(general, font));
		AddField(form, 4, new StaticText(general, _("&Recent files to remember:")), _recentFiles = new SpinCtrl(general, min: 0, max: 50, initial: settings.RecentFiles));
		// The user's preferred language comes first, and then each one there is a catalog for,
		// named in itself.
		List<(string Code, string Name)> languages = Languages.Available();
		_languageCodes.AddRange(languages.Select(static language => language.Code));
		AddField(form, 5, new StaticText(general, _("&Language:")), _language = new Choice(general, [_("The system's language"), .. languages.Select(static language => language.Name)]));
		_language.Selection = Math.Max(_languageCodes.IndexOf(settings.Language), 0);
		_startWithSystem = new CheckBox(general, _("&Start when I sign in")) {
			IsChecked = settings.StartWithSystem,
		};
		form.Add(_startWithSystem, new GridPosition(6, 0), new GridSpan(1, 2));
		form.AddGrowableColumn(1);
		general.Sizer = Bordered(form);
		pages.AddPage(general, _("General"));
		// The folders page has a picker with a text box, which can be typed in as well as browsed.
		Panel folders = new(pages);
		BoxSizer folderSizer = new(Orientation.Vertical);
		folderSizer.Add(new StaticText(folders, _("&Documents folder:")));
		_documents = new DirPickerCtrl(folders, settings.DocumentsFolder, _("Choose the documents folder"), style: DirPickerCtrlStyle.UseTextCtrl | DirPickerCtrlStyle.DirectoryMustExist);
		folderSizer.Add(_documents, SizerFlags.Default.Expand().Border(Direction.Top));
		folders.Sizer = Bordered(folderSizer);
		pages.AddPage(folders, _("Folders"));
		// The notifications page has more check boxes than fit, so it scrolls, by rows of 10 pixels.
		ScrolledWindow notifications = new(pages);
		notifications.SetScrollRate(0, 10);
		StaticBoxSizer group = new(Orientation.Vertical, notifications, _("Notify me when"));
		string[] events = AppSettings.Events;
		for (int index = 0; index < events.Length; index++) {
			CheckBox box = new(group.StaticBox!, events[index]) {
				IsChecked = settings.Notifications.Contains(index),
			};
			group.Add(box, SizerFlags.Default.Border(Direction.Top | Direction.Bottom));
			_notifications.Add(box);
		}
		notifications.Sizer = Bordered(group);
		pages.AddPage(notifications, _("Notifications"));
		// OK and Cancel, in the order and with the labels the platform uses, which wxWidgets
		// translates itself.
		StdDialogButtonSizer buttons = new();
		buttons.AddButton(new Button(this, id: StandardId.Ok));
		buttons.AddButton(new Button(this, id: StandardId.Cancel));
		buttons.Realize();
		BoxSizer sizer = new(Orientation.Vertical);
		sizer.Add(pages, new SizerFlags(1).Expand().Border());
		sizer.Add(buttons, SizerFlags.Default.Expand().Border(Direction.Left | Direction.Right | Direction.Bottom));
		Sizer = sizer;
		SetSize(560, 480);
		_name.SetFocus();
	}

	public void ApplyTo(AppSettings settings) {
		settings.Name = _name.Value.Trim();
		settings.Theme = Math.Max(_theme.Selection, 0);
		settings.Accent = _accent.Color ?? settings.Accent;
		using Font? font = _font.SelectedFont;
		settings.Font = font?.NativeFontInfoDescription ?? string.Empty;
		settings.RecentFiles = _recentFiles.Value;
		settings.Language = _languageCodes[Math.Max(_language.Selection, 0)];
		settings.StartWithSystem = _startWithSystem.IsChecked;
		settings.DocumentsFolder = _documents.Path;
		settings.Notifications.Clear();
		settings.Notifications.UnionWith(Enumerable.Range(0, _notifications.Count).Where(index => _notifications[index].IsChecked));
	}

	private static void AddField(GridBagSizer form, int row, StaticText label, Window field) {
		form.Add(label, new GridPosition(row, 0), flags: SizerFlags.Default.CenterVertical());
		form.Add(field, new GridPosition(row, 1), flags: SizerFlags.Default.Expand());
	}

	// A page's contents, with the platform's border around them.
	private static BoxSizer Bordered(Sizer contents) {
		BoxSizer border = new(Orientation.Vertical);
		border.Add(contents, new SizerFlags(1).Expand().Border());
		return border;
	}

	// Keeps the dialog open, and says why, until the name is filled in.
	private sealed class NameRequired : CustomValidator {
		protected override bool OnValidate(Window window, Window? parent) {
			if (window is TextCtrl text && text.Value.Trim().Length > 0) return true;
			MessageBox.Show(_("Enter a name, which the application shows as yours."), _("Settings"), MessageDialogStyle.Ok | MessageDialogStyle.IconWarning, parent);
			window.SetFocus();
			return false;
		}
	}
}
