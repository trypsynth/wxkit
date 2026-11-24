using Checklist;
using WxKit;

// Controls that draw themselves, described to screen readers on Windows, macOS, and Linux: a
// checklist, which they see as a list whose current item they announce, and a status strip,
// whose fields they read as text. With --smoke, the window closes as soon as the event loop runs,
// so CI can check that a published sample starts and ends cleanly.
bool smoke = args is ["--smoke"];
return Application.Run(() => {
	Frame frame = new(null, "Checklist", size: new Size(360, 340));
	Panel panel = new(frame);
	// The label comes just before the control it names, so screen readers name the control after it.
	StaticText label = new(panel, "&Shopping list:");
	ChecklistControl list = new(panel, ["Milk", "Bread", "Eggs", "Apples", "Coffee", "Rice"]);
	StatusStrip status = new(panel);
	void ShowStatus() {
		int done = list.Items.Count(static item => item.IsDone);
		status.SetFields($"{done} of {list.Items.Count} done", $"Current: {list.Items[list.Current].Text}");
	}
	list.Changed += (_, _) => ShowStatus();
	ShowStatus();
	BoxSizer sizer = new(Orientation.Vertical);
	sizer.Add(label, SizerFlags.Default.Border(Direction.Left | Direction.Right | Direction.Top));
	sizer.Add(list, new SizerFlags(1).Expand().Border());
	sizer.Add(status, SizerFlags.Default.Expand());
	panel.Sizer = sizer;
	frame.Center();
	frame.Show();
	list.SetFocus();
	if (smoke) Application.Post(() => frame.Close(force: true));
});
