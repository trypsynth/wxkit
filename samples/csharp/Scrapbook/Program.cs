using Scrapbook;
using WxKit;

// The clipboard and drag and drop: snippets of text and lists of files, pasted or dropped into a
// list, and copied or dragged back out. With --smoke, the sample adds a snippet, copies it to the
// clipboard, and pastes it back, then closes, so CI can check that a published sample does and ends
// cleanly.
bool smoke = args is ["--smoke"];
return Application.Run(() => {
	ScrapbookFrame frame = new();
	frame.Show();
	if (smoke) {
		Application.Post(() => {
			frame.Add(new TextSnippet { Text = "Copied by the smoke test" });
			frame.Copy();
			frame.Paste();
			if (frame.Snippets is not [TextSnippet first, TextSnippet second] || first != second) throw new InvalidOperationException("The snippet didn't come back from the clipboard.");
			frame.Close(force: true);
		});
	}
});
