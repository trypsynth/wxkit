namespace WxKit.Generator.Tweaks;

// The core classes: windows, the basic controls, sizers, menus, and the events they send. Every
// member the generator can convert is bound, except the ones the tweaks list, which are written by
// hand. Each area's tweaks are in a file of their own.
internal static class Core {
	public static void Apply(Binding.Tweaks tweaks) {
		Windows.Apply(tweaks);
		Events.Apply(tweaks);
		Sizers.Apply(tweaks);
		Controls.Apply(tweaks);
		Lists.Apply(tweaks);
		Dialogs.Apply(tweaks);
		Accessibility.Apply(tweaks);
		Drawing.Apply(tweaks);
		Printing.Apply(tweaks);
		Desktop.Apply(tweaks);
		Libraries.Apply(tweaks);
		Internals.Apply(tweaks);
	}
}
