namespace ProjectWizard

open System
open System.IO

type ProjectKind =
    | ConsoleApp
    | DesktopApp of DesktopOptions
    | Library

/// What a desktop application starts with, which only it has.
and DesktopOptions =
    { HasMenuBar: bool
      HasStatusBar: bool
      IsTranslated: bool }

/// What the wizard asks for.
type ProjectPlan =
    { Name: string
      Folder: string
      Kind: ProjectKind }

module ProjectPlan =
    /// The kinds' names, in the order the wizard lists them.
    let kindNames = [ "Console application"; "Desktop application"; "Library" ]

    let private yesNo value = if value then "yes" else "no"

    /// The plan as lines of text, which a screen reader reads one at a time.
    let describe plan =
        let kind =
            match plan.Kind with
            | ConsoleApp -> [ "Kind: Console application" ]
            | Library -> [ "Kind: Library" ]
            | DesktopApp options ->
                [ "Kind: Desktop application"
                  "Menu bar: " + yesNo options.HasMenuBar
                  "Status bar: " + yesNo options.HasStatusBar
                  "Translated: " + yesNo options.IsTranslated ]
        String.Join('\n', [ "Name: " + plan.Name; "Folder: " + Path.Combine(plan.Folder, plan.Name) ] @ kind)
