using Checksums;
using WxKit;

// Work on another thread, with a progress dialog: the sample hashes a folder's files on a thread
// pool thread, so the window keeps responding, and the dialog, updated through Progress<T> on the
// GUI thread, shows how far it is and can cancel it. With --smoke, the sample hashes its own folder
// and closes, so CI can check that a published sample does and ends cleanly.
bool smoke = args is ["--smoke"];
return Application.Run(() => {
	ChecksumsFrame frame = new();
	frame.Show();
	if (smoke) {
		Application.Post(async () => {
			await frame.ComputeAsync(AppContext.BaseDirectory);
			if (frame.ResultCount == 0) throw new InvalidOperationException("No checksums were computed.");
			frame.Close(force: true);
		});
	}
});
