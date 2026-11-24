using WxKit;

// With --smoke, the frame closes as soon as the event loop is running, so CI can check that a
// published executable starts and ends cleanly.
bool smoke = args is ["--smoke"];
return Application.Run(() => {
	Frame frame = new(null, "Hello from WxKit", size: new Size(420, 260));
	Menu fileMenu = new();
	fileMenu.Append(StandardId.About, "&About Hello\tF1").Click += (_, _) => MessageBox.Show($"A WxKit sample, built with {Application.WxWidgetsVersion}.", "About Hello", MessageDialogStyle.Ok | MessageDialogStyle.IconInformation, frame);
	fileMenu.AppendSeparator();
	fileMenu.Append(StandardId.Exit).Click += (_, _) => frame.Close();
	MenuBar menuBar = new();
	menuBar.Append(fileMenu, "&File");
	frame.MenuBar = menuBar;
	// Each label is created just before the control it names, so a screen reader names the control
	// after it, and the tab order follows.
	Panel panel = new(frame);
	StaticText label = new(panel, "&Name:");
	TextCtrl name = new(panel);
	Button greet = new(panel, "&Greet") {
		IsEnabled = false
	};
	StaticText greeting = new(panel, "");
	StaticText uptime = new(panel, "Running for 0 seconds");
	name.TextChanged += (_, _) => greet.IsEnabled = name.Value.Trim().Length > 0;
	greet.Click += async (_, _) => {
		greet.IsEnabled = false;
		greeting.Label = "Thinking...";
		// The continuation runs back on the GUI thread, so it can use the controls.
		await Task.Delay(500);
		greeting.Label = $"Hello, {name.Value.Trim()}!";
		greet.IsEnabled = true;
	};
	int seconds = 0;
	Timer timer = new();
	timer.Tick += (_, _) => uptime.Label = $"Running for {++seconds} seconds";
	timer.Start(TimeSpan.FromSeconds(1));
	frame.Closing += (_, e) => {
		if (e.CanVeto && name.Value.Length > 0 && MessageBox.Show("Quit Hello?", "Hello", MessageDialogStyle.YesNo | MessageDialogStyle.IconQuestion, frame) != StandardId.Yes) {
			e.Veto();
			return;
		}
		timer.Dispose();
	};
	BoxSizer sizer = new(Orientation.Vertical);
	sizer.Add(label, SizerFlags.Default.Border(Direction.Left | Direction.Right | Direction.Top));
	sizer.Add(name, SizerFlags.Default.Expand().Border());
	sizer.Add(greet, SizerFlags.Default.Border());
	sizer.Add(greeting, SizerFlags.Default.Expand().Border());
	sizer.AddStretchSpacer();
	sizer.Add(uptime, SizerFlags.Default.Border());
	panel.Sizer = sizer;
	frame.Center();
	frame.Show();
	name.SetFocus();
	if (smoke) Application.Post(() => frame.Close(force: true));
});
