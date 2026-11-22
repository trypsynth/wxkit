using WxKit;
using static WxKit.Gettext;

// Text a user reads goes through _, so it can be translated. Translating an application:
// https://github.com/trypsynth/wxkit/blob/master/docs/translating.md
Translations.Use("WxKitApp");
return Application.Run(() => {
	Frame frame = new(null, _("WxKitApp"), size: new Size(480, 320));
	Panel panel = new(frame);
	// Each label comes just before the control it names, so screen readers read the control by
	// it on every platform. The & marks the letter that Alt and that letter go to.
	StaticText nameLabel = new(panel, _("&Name:"));
	TextCtrl name = new(panel, style: TextCtrlStyle.ProcessEnter);
	Button greet = new(panel, _("&Greet"));
	StaticText greeting = new(panel, string.Empty);
	// Speaks news that no control has the focus for, such as the greeting, to screen readers.
	LiveRegion announcer = new(panel);
	void Greet() {
		string text = name.Value.Trim();
		string message = text.Length == 0 ? _("Type your name first.") : _("Hello, {0}!", text);
		greeting.Label = message;
		announcer.Announce(message);
	}
	greet.Click += (_, _) => Greet();
	name.TextEnter += (_, _) => Greet();
	BoxSizer sizer = new(Orientation.Vertical);
	sizer.Add(nameLabel, SizerFlags.Default.Border(Direction.Left | Direction.Right | Direction.Top));
	sizer.Add(name, SizerFlags.Default.Expand().Border());
	sizer.Add(greet, SizerFlags.Default.Border());
	sizer.Add(greeting, SizerFlags.Default.Expand().Border());
	panel.Sizer = sizer;
	frame.Center();
	frame.Show();
	name.SetFocus();
});
