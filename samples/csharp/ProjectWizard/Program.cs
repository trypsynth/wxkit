using ProjectWizard;
using WxKit;

// A wizard that plans a new project, a page at a time, which skips a page that doesn't apply and
// keeps the user on a page until it's filled in. The window shows the last plan made. With --smoke,
// the sample fills the wizard's pages in, checks which follow which, opens it, and cancels it, then
// closes, so CI can check that a published sample does and ends cleanly.
bool smoke = args is ["--smoke"];
return Application.Run(() => {
	Frame frame = new(null, "Project Wizard", size: new Size(480, 360));
	Panel panel = new(frame);
	Button start = new(panel, "&New Project...");
	StaticText planLabel = new(panel, "&Last plan:");
	TextCtrl plan = new(panel, "None yet", style: TextCtrlStyle.MultiLine | TextCtrlStyle.ReadOnly);
	start.Click += (_, _) => {
		using NewProjectWizard wizard = new(frame);
		if (wizard.Run() is ProjectPlan made) plan.ChangeValue(made.Describe());
	};
	BoxSizer sizer = new(Orientation.Vertical);
	sizer.Add(start, SizerFlags.Default.Border());
	sizer.Add(planLabel, SizerFlags.Default.Border(Direction.Left | Direction.Right | Direction.Top));
	sizer.Add(plan, new SizerFlags(1).Expand().Border());
	panel.Sizer = sizer;
	frame.Center();
	frame.Show();
	start.SetFocus();
	if (smoke) {
		Application.Post(() => {
			using (NewProjectWizard wizard = new(frame)) {
				wizard.Exercise();
				Application.Post(() => wizard.EndModal(StandardId.Cancel));
				if (wizard.Run() is not null) throw new InvalidOperationException("The canceled wizard gave a plan.");
			}
			frame.Close(force: true);
		});
	}
});
