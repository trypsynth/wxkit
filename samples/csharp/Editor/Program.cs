using Editor;
using WxKit;

// A text editor, as a fuller example of WxKit than the Hello sample: menus with the platform's
// standard labels and shortcuts, files opened and saved without blocking the window, find and
// replace, and a prompt before unsaved changes are lost. A file named on the command line opens.
// With --smoke, the editor makes a print preview, which lays out a page, then closes as soon as the
// event loop runs, so CI can check that a published editor starts and ends cleanly.
bool smoke = args is ["--smoke"];
return Application.Run(() => {
	EditorFrame frame = new();
	frame.Show();
	if (smoke) {
		Application.Post(() => {
			frame.PreviewAndClose();
			frame.Close(force: true);
		});
	} else if (args is [string path]) {
		_ = frame.OpenAsync(path);
	}
});
