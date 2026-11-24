using WxKit;

namespace ProjectWizard;

// A wizard that plans a new project, a page at a time: its name and folder, its kind, the options a
// desktop application has, and a summary. The options page is skipped for any other kind, by
// chaining the kind page to the summary instead, before the wizard moves on. The name has to be
// filled in before the first page can be left.
internal sealed class NewProjectWizard : Wizard {
	private readonly WizardPageSimple _details;
	private readonly WizardPageSimple _kindPage;
	private readonly WizardPageSimple _options;
	private readonly WizardPageSimple _summary;
	private readonly TextCtrl _name;
	private readonly DirPickerCtrl _folder;
	private readonly RadioBox _kind;
	private readonly CheckBox _menuBar;
	private readonly CheckBox _statusBar;
	private readonly CheckBox _translated;
	private readonly TextCtrl _summaryText;

	public NewProjectWizard(Window parent) : base(parent, "New Project") {
		// Each label is made just before its field, so a screen reader names the field after it.
		_details = new WizardPageSimple(this);
		StaticText nameLabel = new(_details, "Project &name:");
		_name = new TextCtrl(_details) { Validator = new NameRequired() };
		StaticText folderLabel = new(_details, "&Folder to make it in:");
		_folder = new DirPickerCtrl(_details, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Choose the folder", style: DirPickerCtrlStyle.UseTextCtrl);
		_details.Sizer = Column(nameLabel, _name, folderLabel, _folder);
		_kindPage = new WizardPageSimple(this);
		_kind = new RadioBox(_kindPage, "&Kind of project", ProjectPlan.KindNames, majorDimension: 1);
		_kindPage.Sizer = Column(_kind);
		_options = new WizardPageSimple(this);
		_menuBar = new CheckBox(_options, "A &menu bar") { IsChecked = true };
		_statusBar = new CheckBox(_options, "A &status bar");
		_translated = new CheckBox(_options, "&Translations");
		_options.Sizer = Column(new StaticText(_options, "A desktop application starts with:"), _menuBar, _statusBar, _translated);
		_summary = new WizardPageSimple(this);
		StaticText summaryLabel = new(_summary, "&Check the project, then choose Finish:");
		_summaryText = new TextCtrl(_summary, style: TextCtrlStyle.MultiLine | TextCtrlStyle.ReadOnly, size: new Size(-1, 120));
		_summary.Sizer = Column(summaryLabel, _summaryText);
		WizardPageSimple.Chain(_details, _kindPage);
		WizardPageSimple.Chain(_kindPage, _options);
		WizardPageSimple.Chain(_options, _summary);
		// After the page's validators pass, and before the wizard asks the page which comes next.
		BeforePageChanged += (_, e) => {
			if (e.IsForward && e.Page == _kindPage) Rechain();
		};
		PageChanged += (_, e) => {
			if (e.Page == _summary) _summaryText.ChangeValue(Plan.Describe());
		};
		FitToPage(_details);
	}

	public ProjectPlan Plan => new() {
		Name = _name.Value.Trim(),
		Folder = _folder.Path,
		Kind = (ProjectKind)Math.Max(_kind.Selection, 0),
		HasMenuBar = _menuBar.IsChecked,
		HasStatusBar = _statusBar.IsChecked,
		IsTranslated = _translated.IsChecked,
	};

	// Runs the wizard, and gives the plan when it's finished, or null when it's canceled.
	public ProjectPlan? Run() => RunWizard(_details) ? Plan : null;

	// For the smoke test: fills the pages in as the user would, and checks which pages follow which.
	internal void Exercise() {
		_name.Value = "Smoke";
		foreach (ProjectKind kind in Enum.GetValues<ProjectKind>()) {
			_kind.Selection = (int)kind;
			Rechain();
			WizardPage expected = kind == ProjectKind.Desktop ? _options : _summary;
			if (_kindPage.Next != expected || _summary.Previous != (kind == ProjectKind.Desktop ? _options : _kindPage)) throw new InvalidOperationException($"The pages after {kind} are chained wrong.");
		}
	}

	// The options page comes after the kind page only for a desktop application.
	private void Rechain() {
		bool desktop = (ProjectKind)_kind.Selection == ProjectKind.Desktop;
		_kindPage.SetNext(desktop ? _options : _summary);
		_summary.SetPrevious(desktop ? _options : _kindPage);
	}

	private static BoxSizer Column(params Window[] windows) {
		BoxSizer sizer = new(Orientation.Vertical);
		foreach (Window window in windows) sizer.Add(window, SizerFlags.Default.Expand().Border(Direction.Bottom));
		return sizer;
	}

	// Keeps the wizard on the first page, and says why, until the name is filled in.
	private sealed class NameRequired : CustomValidator {
		protected override bool OnValidate(Window window, Window? parent) {
			if (window is TextCtrl text && text.Value.Trim().Length > 0) return true;
			MessageBox.Show("Enter a name for the project.", "New Project", MessageDialogStyle.Ok | MessageDialogStyle.IconWarning, parent);
			window.SetFocus();
			return false;
		}
	}
}
