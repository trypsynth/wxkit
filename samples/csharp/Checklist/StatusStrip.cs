using WxKit;

namespace Checklist;

// A strip of fields that draws itself, as a status bar does. Screen readers read each field as
// text, through SetAccessibleParts, as they read a status bar's fields. It is a plain control,
// not a panel, since a panel takes the focus from the keyboard when it has no children whatever
// it is told, and a status strip is read, not typed in, so it is no Tab stop.
internal sealed class StatusStrip : Control {
	private string[] _fields = [];

	public StatusStrip(Window parent) : base(parent) {
		BackgroundStyle = BackgroundStyle.Paint;
		DisableFocusFromKeyboard();
		MinSize = new Size(-1, CharHeight + 10);
		Paint += (_, _) => Draw();
		SizeChanged += (_, e) => {
			Refresh();
			Describe();
			e.Skip();
		};
	}

	public void SetFields(params string[] fields) {
		_fields = fields;
		Refresh();
		Describe();
	}

	// Each field takes an equal share of the width.
	private Rect FieldBounds(int index) {
		Size size = ClientSize;
		int width = _fields.Length == 0 ? 0 : size.Width / _fields.Length;
		return new Rect(index * width, 0, width, size.Height);
	}

	private void Describe() => SetAccessibleParts(_fields.Select((field, index) => new AccessiblePart(field, FieldBounds(index))));

	private void Draw() {
		using PaintDC dc = new(this);
		using Brush background = new(SystemSettings.GetColor(SystemColor.ButtonFace) ?? new Color(240, 240, 240));
		dc.Background = background;
		dc.Clear();
		using Font? font = Font;
		dc.Font = font;
		dc.TextForeground = SystemSettings.GetColor(SystemColor.ButtonText) ?? new Color(0, 0, 0);
		using Pen divider = new(SystemSettings.GetColor(SystemColor.ButtonShadow) ?? new Color(160, 160, 160));
		dc.Pen = divider;
		for (int index = 0; index < _fields.Length; index++) {
			Rect bounds = FieldBounds(index);
			dc.DrawText(_fields[index], bounds.X + 6, bounds.Y + 5);
			if (index > 0) dc.DrawLine(bounds.X, bounds.Y + 2, bounds.X, bounds.Y + bounds.Height - 2);
		}
	}
}
