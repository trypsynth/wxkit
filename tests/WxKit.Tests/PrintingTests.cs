namespace WxKit.Tests;

// Printing, which a test machine has no printer for, so the tests check the settings and a
// printout's life, and draw pages through a preview, which only needs the screen.
public class PrintingTests {
	[Fact]
	public Task KeepsPrintSettings() => Gui.Run(() => {
		using PrintData print = new() { Orientation = PrintOrientation.Landscape, Copies = 2, PaperKind = PaperKind.A4, Quality = PrintQuality.Draft };
		Assert.Equal(PrintOrientation.Landscape, print.Orientation);
		Assert.Equal(2, print.Copies);
		Assert.Equal(PaperKind.A4, print.PaperKind);
		Assert.Equal(PrintQuality.Draft, print.Quality);
		using PrintDialogData dialog = new() { MinPage = 1, MaxPage = 9, FromPage = 2, ToPage = 4 };
		dialog.PrintData = print;
		Assert.Equal((2, 4), (dialog.FromPage, dialog.ToPage));
		Assert.Equal(PrintOrientation.Landscape, dialog.PrintData?.Orientation);
		using PageSetupDialogData setup = new() { EnableHelp = false };
		Assert.False(setup.EnableHelp);
		using Printer printer = new(dialog);
		Assert.Equal(9, printer.PrintDialogData?.MaxPage);
	});

	[Fact]
	public Task FreesAPrintoutItsOwnerDisposes() => Gui.Run(() => {
		Pages printout = new(3);
		Assert.Equal("Three pages", printout.Title);
		Assert.False(printout.IsPreview);
		printout.Dispose();
		Assert.True(printout.IsDisposed);
	});

	[Fact]
	public Task DrawsPagesInAPreview() => Gui.WithFrame(async frame => {
		Pages shown = new(3);
		Pages printing = new(3);
		Assert.Throws<ArgumentException>(() => PrintPreview.Show(shown, shown));
		Frame? preview = PrintPreview.Show(shown, printing, frame, "Preview", size: new Size(500, 600));
		// Without a printer, some platforms cannot make a preview, which then frees the printouts at once.
		if (preview is null) {
			Assert.True(shown.IsDisposed);
			Assert.True(printing.IsDisposed);
			return;
		}
		Assert.False(shown.IsDisposed);
		await Gui.WaitUntil(() => shown.Drawn.Count > 0);
		Assert.Contains(1, shown.Drawn);
		preview.Close(force: true);
		await Gui.WaitUntil(() => shown.IsDisposed && printing.IsDisposed);
	});

	private sealed class Pages(int count) : Printout("Three pages") {
		public List<int> Drawn { get; } = [];

		protected override PageRange GetPageInfo() => PageRange.All(count);

		protected override bool OnPrintPage(int page, DC dc) {
			Drawn.Add(page);
			dc.DrawText($"Page {page} of {count}", new Point(10, 10));
			return true;
		}
	}
}
