using WxKit;

namespace Checksums;

// A folder picker, a button that hashes the folder's files, and the list of their checksums. The
// hashing runs on a thread pool thread, so the window keeps responding, while a progress dialog,
// which can cancel it, follows it.
internal sealed class ChecksumsFrame : Frame {
	private readonly DirPickerCtrl _folder;
	private readonly Button _compute;
	private readonly ListBox _results;

	public ChecksumsFrame() : base(null, "Checksums", size: new Size(640, 440)) {
		Panel panel = new(this);
		// Each label is made just before the control it names, so a screen reader names the control
		// after it.
		StaticText folderLabel = new(panel, "&Folder:");
		_folder = new DirPickerCtrl(panel, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Choose a folder to hash", style: DirPickerCtrlStyle.UseTextCtrl | DirPickerCtrlStyle.DirectoryMustExist);
		_compute = new Button(panel, "&Compute Checksums");
		StaticText resultsLabel = new(panel, "&Checksums:");
		_results = new ListBox(panel);
		_compute.Click += async (_, _) => await ComputeAsync(_folder.Path);
		BoxSizer sizer = new(Orientation.Vertical);
		sizer.Add(folderLabel, SizerFlags.Default.Border(Direction.Left | Direction.Right | Direction.Top));
		sizer.Add(_folder, SizerFlags.Default.Expand().Border());
		sizer.Add(_compute, SizerFlags.Default.Border(Direction.Left | Direction.Right | Direction.Bottom));
		sizer.Add(resultsLabel, SizerFlags.Default.Border(Direction.Left | Direction.Right | Direction.Top));
		sizer.Add(_results, new SizerFlags(1).Expand().Border());
		panel.Sizer = sizer;
		CreateStatusBar();
		Center();
	}

	public int ResultCount => _results.Count;

	// Hashes the files in the folder, showing their checksums when it's done, or that it was canceled.
	public async Task ComputeAsync(string folder) {
		string[] files;
		try {
			files = Directory.GetFiles(folder);
		} catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException) {
			MessageBox.Show($"The folder could not be read. {error.Message}", "Checksums", MessageDialogStyle.Ok | MessageDialogStyle.IconError, this);
			return;
		}
		Array.Sort(files, StringComparer.CurrentCulture);
		_compute.IsEnabled = false;
		_results.Clear();
		using CancellationTokenSource cancel = new();
		using ProgressDialog progress = new("Checksums", "Starting...", Math.Max(files.Length, 1), this, ProgressDialogStyle.AppModal | ProgressDialogStyle.CanAbort | ProgressDialogStyle.ElapsedTime | ProgressDialogStyle.RemainingTime);
		bool done = false;
		// Progress<T> runs its handler on the thread that made it, this one, through WxKit's
		// synchronization context, so the handler can update the dialog. One that comes after the
		// work is done is ignored, since the dialog is gone by then.
		Progress<(int Done, string Name)> reporter = new(report => {
			if (!done && !progress.Update(report.Done, $"Hashing {report.Name}")) cancel.Cancel();
		});
		try {
			List<Checksum> sums = await Task.Run(() => Checksum.Compute(files, reporter, cancel.Token), cancel.Token);
			foreach (Checksum sum in sums) _results.Append(sum.ToString());
			int failed = sums.Count(static sum => sum.Error is not null);
			SetStatusText(failed == 0 ? $"{sums.Count} files hashed" : $"{sums.Count} files, {failed} of which could not be read");
		} catch (OperationCanceledException) {
			SetStatusText("Canceled");
		} finally {
			done = true;
			_compute.IsEnabled = true;
		}
	}
}
