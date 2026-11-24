namespace Settings

open System
open System.Globalization
open System.IO
open WxKit
open WxKit.FSharp

/// How the application looks.
type Theme =
    | MatchSystem
    | Light
    | Dark

module Theme =
    /// Every theme, in the order the dialog lists them and the file stores the position of.
    let all = [ MatchSystem; Light; Dark ]

    /// A theme's name, in the current language.
    let name theme =
        match theme with
        | MatchSystem -> tr "Match the system"
        | Light -> tr "Light"
        | Dark -> tr "Dark"

/// What the sample remembers. The dialog gives a changed copy, rather than changing it.
type AppSettings =
    { Name: string
      Theme: Theme
      Accent: Color
      /// The font's description, as Font.NativeFontInfoDescription gives it, or None for the default font.
      Font: string option
      RecentFiles: int
      DocumentsFolder: string
      StartWithSystem: bool
      /// The language, such as de, or None for the user's preferred language that there is a catalog for.
      Language: string option
      /// The positions in AppSettings.events of the events to be notified of.
      Notifications: Set<int> }

/// Reading and writing the settings, as lines of key=value in a file of the sample's own.
module AppSettings =
    /// The events' names, in the current language, which the file stores the positions of.
    let events () =
        [ tr "A file is saved"
          tr "A file is changed elsewhere"
          tr "An update is ready"
          tr "A backup finishes"
          tr "A backup fails"
          tr "A download finishes"
          tr "Someone shares a file"
          tr "A comment is added"
          tr "A reminder is due"
          tr "The disk is nearly full"
          tr "The battery is low"
          tr "A sync conflict is found" ]

    let defaultPath =
        Path.Combine(Environment.GetFolderPath Environment.SpecialFolder.ApplicationData, "WxKit Settings sample", "settings.txt")

    let defaults () =
        { Name = Environment.UserName
          Theme = MatchSystem
          Accent = Color(0uy, 120uy, 215uy)
          Font = None
          RecentFiles = 10
          DocumentsFolder = Environment.GetFolderPath Environment.SpecialFolder.MyDocuments
          StartWithSystem = false
          Language = None
          Notifications = set [ 0; 2; 4 ] }

    let private (|Int|_|) (text: string) =
        match Int32.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture) with
        | true, value -> Some value
        | _ -> None

    // A color as #rrggbb.
    let private (|Rgb|_|) (text: string) =
        match text.Length, Int32.TryParse(text.Substring(min 1 text.Length), NumberStyles.HexNumber, CultureInfo.InvariantCulture) with
        | 7, (true, rgb) when text[0] = '#' -> Some(Color(byte (rgb >>> 16), byte (rgb >>> 8), byte rgb))
        | _ -> None

    let private optional text = if text = "" then None else Some text

    // A line's setting, over the defaults. A value that can't be read leaves the default.
    let private read settings (key, value) =
        match key, value with
        | "name", name -> { settings with Name = name }
        | "theme", Int index when index >= 0 && index < Theme.all.Length -> { settings with Theme = Theme.all[index] }
        | "accent", Rgb color -> { settings with Accent = color }
        | "font", font -> { settings with Font = optional font }
        | "recentFiles", Int count -> { settings with RecentFiles = count }
        | "documents", folder -> { settings with DocumentsFolder = folder }
        | "startWithSystem", start -> { settings with StartWithSystem = (start = "true") }
        | "language", language -> { settings with Language = optional language }
        | "notifications", list ->
            let count = (events ()).Length
            let notifications =
                list.Split('|', StringSplitOptions.RemoveEmptyEntries)
                |> Seq.choose (function
                    | Int index when index >= 0 && index < count -> Some index
                    | _ -> None)
            { settings with Notifications = Set.ofSeq notifications }
        | _ -> settings

    /// Reads the settings, or gives the defaults when there are none yet or the file can't be read.
    let load path =
        let lines =
            try
                File.ReadAllLines path
            with
            | :? IOException
            | :? UnauthorizedAccessException -> [||]
        lines
        |> Seq.choose (fun line ->
            match line.IndexOf '=' with
            | equals when equals > 0 -> Some(line[.. equals - 1], line[equals + 1 ..])
            | _ -> None)
        |> Seq.fold read (defaults ())

    let save path settings =
        match Path.GetDirectoryName(path: string) with
        | null -> ()
        | folder -> Directory.CreateDirectory folder |> ignore
        let number (value: int) = value.ToString(CultureInfo.InvariantCulture)
        let hex (value: byte) = value.ToString("X2", CultureInfo.InvariantCulture)
        File.WriteAllLines(
            path,
            [ "name=" + settings.Name
              "theme=" + number (List.findIndex ((=) settings.Theme) Theme.all)
              "accent=#" + hex settings.Accent.R + hex settings.Accent.G + hex settings.Accent.B
              "font=" + Option.defaultValue "" settings.Font
              "recentFiles=" + number settings.RecentFiles
              "documents=" + settings.DocumentsFolder
              "startWithSystem=" + (if settings.StartWithSystem then "true" else "false")
              "language=" + Option.defaultValue "" settings.Language
              "notifications=" + String.Join('|', settings.Notifications |> Seq.map number) ]
        )
