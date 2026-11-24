namespace Checksums

open System
open System.IO
open System.Threading
open System.Threading.Tasks
open WxKit
open WxKit.FSharp

/// A folder picker, a button that hashes the folder's files, and the list of their checksums. The
/// hashing runs on a thread pool thread, so the window keeps responding, while a progress dialog,
/// which can cancel it, follows it.
type ChecksumsFrame() as this =
    inherit Frame(null, "Checksums", size = Size(640, 440))
    let panel = new Panel(this)
    // Each label is made just before the control it names, so a screen reader names the control
    // after it.
    let folderLabel = new StaticText(panel, "&Folder:")
    let folder =
        let style = DirPickerCtrlStyle.UseTextCtrl ||| DirPickerCtrlStyle.DirectoryMustExist
        new DirPickerCtrl(panel, Environment.GetFolderPath Environment.SpecialFolder.MyDocuments, "Choose a folder to hash", style = style)
    let compute = new Button(panel, "&Compute Checksums")
    let resultsLabel = new StaticText(panel, "&Checksums:")
    let results = new ListBox(panel)

    let listFiles path =
        try
            Ok(Directory.GetFiles path |> Array.sortWith (fun a b -> String.Compare(a, b, StringComparison.CurrentCulture)) |> List.ofArray)
        with
        | :? IOException
        | :? UnauthorizedAccessException
        | :? ArgumentException as error -> Error error.Message

    /// Hashes the files in the folder, showing their checksums when it's done, or that it was canceled.
    let computeIn path : Task =
        task {
            match listFiles path with
            | Error error ->
                let icon = MessageDialogStyle.Ok ||| MessageDialogStyle.IconError
                MessageBox.Show($"The folder could not be read. {error}", "Checksums", icon, this) |> ignore
            | Ok files ->
                compute.IsEnabled <- false
                results.Clear()
                use cancel = new CancellationTokenSource()
                let style =
                    ProgressDialogStyle.AppModal ||| ProgressDialogStyle.CanAbort ||| ProgressDialogStyle.ElapsedTime ||| ProgressDialogStyle.RemainingTime
                use progress = new ProgressDialog("Checksums", "Starting...", max files.Length 1, this, style)
                let finished = ref false
                // Progress runs its handler on the thread that made it, this one, through WxKit's
                // synchronization context, so the handler can update the dialog. One that comes
                // after the work is done is ignored, since the dialog is gone by then.
                let reporter =
                    Progress<int * string>(fun (count, name) ->
                        if not finished.Value && not (progress.Update(count, $"Hashing {name}")) then
                            cancel.Cancel())
                try
                    try
                        let! sums = Task.Run((fun () -> Checksum.compute files reporter cancel.Token), cancel.Token)
                        for sum in sums do
                            results.Append(Checksum.describe sum) |> ignore
                        match sums |> List.filter (fun sum -> Result.isError sum.Hash) |> List.length with
                        | 0 -> this.SetStatusText $"{sums.Length} files hashed"
                        | failed -> this.SetStatusText $"{sums.Length} files, {failed} of which could not be read"
                    with :? OperationCanceledException ->
                        this.SetStatusText "Canceled"
                finally
                    finished.Value <- true
                    compute.IsEnabled <- true
        }

    do
        compute.Click.Add(fun _ -> computeIn folder.Path |> ignore)
        panel.Sizer <-
            column {
                item folderLabel |> border (Direction.Left ||| Direction.Right ||| Direction.Top)
                item folder |> fill
                compute
                item resultsLabel |> border (Direction.Left ||| Direction.Right ||| Direction.Top)
                item results |> grow
            }
        this.CreateStatusBar() |> ignore
        this.Center()

    member _.ResultCount = results.Count

    member _.ComputeAsync(path: string) = computeIn path
