namespace ProjectWizard

open System
open WxKit
open WxKit.FSharp

/// A wizard that plans a new project, a page at a time: its name and folder, its kind, the options a
/// desktop application has, and a summary. The options page is skipped for any other kind, by
/// chaining the kind page to the summary instead, before the wizard moves on. The name has to be
/// filled in before the first page can be left.
type NewProjectWizard(parent: Window) as this =
    inherit Wizard(parent, "New Project")

    // Keeps the wizard on the first page, and says why, until the name is filled in.
    let nameRequired =
        { new CustomValidator() with
            override _.OnValidate(window, parent) =
                match window with
                | :? TextCtrl as text when text.Value.Trim() <> "" -> true
                | _ ->
                    let icon = MessageDialogStyle.Ok ||| MessageDialogStyle.IconWarning
                    MessageBox.Show("Enter a name for the project.", "New Project", icon, parent) |> ignore
                    window.SetFocus()
                    false }

    // Each label is made just before its field, so a screen reader names the field after it.
    let details = new WizardPageSimple(this)
    let nameLabel = new StaticText(details, "Project &name:")
    let name = new TextCtrl(details, Validator = nameRequired)
    let folderLabel = new StaticText(details, "&Folder to make it in:")
    let folder =
        new DirPickerCtrl(details, Environment.GetFolderPath Environment.SpecialFolder.MyDocuments, "Choose the folder", style = DirPickerCtrlStyle.UseTextCtrl)
    let kindPage = new WizardPageSimple(this)
    let kind = new RadioBox(kindPage, "&Kind of project", ProjectPlan.kindNames, majorDimension = 1)
    let options = new WizardPageSimple(this)
    let optionsLabel = new StaticText(options, "A desktop application starts with:")
    let menuBar = new CheckBox(options, "A &menu bar", IsChecked = true)
    let statusBar = new CheckBox(options, "A &status bar")
    let translated = new CheckBox(options, "&Translations")
    let summary = new WizardPageSimple(this)
    let summaryLabel = new StaticText(summary, "&Check the project, then choose Finish:")
    let summaryText = new TextCtrl(summary, style = (TextCtrlStyle.MultiLine ||| TextCtrlStyle.ReadOnly), size = Size(-1, 120))

    let plan () =
        { Name = name.Value.Trim()
          Folder = folder.Path
          Kind =
            match kind.Selection with
            | 1 ->
                DesktopApp
                    { HasMenuBar = menuBar.IsChecked
                      HasStatusBar = statusBar.IsChecked
                      IsTranslated = translated.IsChecked }
            | 2 -> Library
            | _ -> ConsoleApp }

    // The options page comes after the kind page only for a desktop application.
    let rechain () =
        let before, after =
            match (plan ()).Kind with
            | DesktopApp _ -> options :> WizardPage, options :> WizardPage
            | ConsoleApp
            | Library -> summary, kindPage
        kindPage.SetNext before
        summary.SetPrevious after

    let stack (windows: Window list) =
        column {
            for window in windows do
                item window |> fill |> border Direction.Bottom
        }

    do
        details.Sizer <- stack [ nameLabel; name; folderLabel; folder ]
        kindPage.Sizer <- stack [ kind ]
        options.Sizer <- stack [ optionsLabel; menuBar; statusBar; translated ]
        summary.Sizer <- stack [ summaryLabel; summaryText ]
        WizardPageSimple.Chain(details, kindPage)
        WizardPageSimple.Chain(kindPage, options)
        WizardPageSimple.Chain(options, summary)
        // After the page's validators pass, and before the wizard asks the page which comes next.
        this.BeforePageChanged.Add(fun e ->
            if e.IsForward && e.Page = kindPage then
                rechain ())
        this.PageChanged.Add(fun e ->
            if e.Page = summary then
                summaryText.ChangeValue(ProjectPlan.describe (plan ())))
        this.FitToPage details

    /// Runs the wizard, and gives the plan when it's finished.
    member _.Run() =
        if this.RunWizard details then Some(plan ()) else None

    /// For the smoke test: fills the pages in as the user would, and checks which pages follow which.
    member _.Exercise() =
        name.Value <- "Smoke"
        for index, kindName in List.indexed ProjectPlan.kindNames do
            kind.Selection <- index
            rechain ()
            let desktop = index = 1
            let expectedNext = if desktop then options :> WizardPage else summary
            let expectedPrevious = if desktop then options :> WizardPage else kindPage
            if kindPage.Next <> expectedNext || summary.Previous <> expectedPrevious then
                failwith $"The pages after {kindName} are chained wrong."
