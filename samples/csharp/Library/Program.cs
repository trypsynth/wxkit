using Library;
using WxKit;

// A library of books, in a data view whose model gives the cells of the rows on screen as the view
// draws them, filtered as a search is typed, in the order chosen, with a check box column the user
// ticks. Screen readers read it as a list on Windows, macOS, and Linux. The books are records that
// have to be given their title, author, and year. With --smoke, the sample searches, marks a book
// read, and removes it, then closes, so CI can check that a published sample does and ends cleanly.
bool smoke = args is ["--smoke"];
return Application.Run(() => {
	LibraryFrame frame = new();
	frame.Show();
	if (smoke) {
		Application.Post(() => {
			frame.Exercise();
			frame.Close(force: true);
		});
	}
});
