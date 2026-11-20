using Settings;
using WxKit;

// A settings dialog, as most applications have: a notebook of pages, a form laid out with a grid
// bag sizer, pickers for a color, a font, a folder, and a language, a page that scrolls, and OK and
// Cancel, with a validator that keeps the dialog open until the name is filled in. The settings are
// saved to a file in the user's application data. The sample is translated into German, from
// po/de.po, and follows the user's language, or the one they pick. With --smoke, the sample opens
// the dialog and cancels it as soon as the event loop runs, then closes, so CI can check that a
// published sample makes every page and ends cleanly.
bool smoke = args is ["--smoke"];
return Application.Run(() => {
	AppSettings settings = AppSettings.Load(AppSettings.DefaultPath);
	// Numbers and dates in wxWidgets' controls follow the user's locale, and the text the language,
	// both before any window is made.
	UILocale.UseDefault();
	Languages.Use(settings.Language);
	MainFrame frame = new(settings);
	frame.Show();
	if (smoke) {
		Application.Post(() => {
			frame.OpenAndCancelSettings();
			frame.Close(force: true);
		});
	}
});
