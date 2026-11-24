using WxKit;

namespace Editor;

// The editor's text, wrapped to the page in the editor's font, half an inch from each edge. It
// draws in screen units, which MapScreenSizeToPage scales to the page, so it can measure the
// lines with a screen DC before the pages are counted.
internal sealed class TextPrintout(string title, string text, Font? font) : Printout(title) {
	private readonly List<string> _lines = [];
	private int _lineHeight = 1;
	private int _linesPerPage = 1;
	private int _margin;

	protected override void OnPreparePrinting() {
		Size screen = PpiScreen;
		Size printer = PpiPrinter;
		Size page = PageSizePixels;
		// The page's size in screen pixels, which the page is drawn in.
		int width = (int)((long)page.Width * screen.Width / Math.Max(printer.Width, 1));
		int height = (int)((long)page.Height * screen.Height / Math.Max(printer.Height, 1));
		_margin = screen.Height / 2;
		using ScreenDC measure = new();
		if (font is not null) measure.Font = font;
		_lineHeight = Math.Max(measure.CharHeight, 1);
		_linesPerPage = Math.Max((height - 2 * _margin) / _lineHeight, 1);
		int usable = Math.Max(width - 2 * _margin, 1);
		_lines.Clear();
		foreach (string line in text.Split('\n')) Wrap(line.Replace("\t", "    ", StringComparison.Ordinal), usable, measure);
	}

	protected override PageRange GetPageInfo() => PageRange.All(Math.Max((_lines.Count + _linesPerPage - 1) / _linesPerPage, 1));

	protected override bool OnPrintPage(int page, DC dc) {
		MapScreenSizeToPage();
		if (font is not null) dc.Font = font;
		dc.TextForeground = new Color(0, 0, 0);
		int y = _margin;
		foreach (string line in _lines.Skip((page - 1) * _linesPerPage).Take(_linesPerPage)) {
			dc.DrawText(line, _margin, y);
			y += _lineHeight;
		}
		return true;
	}

	// Breaks a line at spaces so each part fits the width, or within a word too long to fit.
	private void Wrap(string line, int width, DC measure) {
		while (line.Length > 0 && measure.GetTextExtent(line).Width > width) {
			int fit = 1;
			while (fit < line.Length && measure.GetTextExtent(line[..(fit + 1)]).Width <= width) fit++;
			int space = line.LastIndexOf(' ', fit - 1, fit);
			int cut = space > 0 ? space : fit;
			_lines.Add(line[..cut]);
			line = line[cut..].TrimStart(' ');
		}
		_lines.Add(line);
	}
}
