using System.Runtime.CompilerServices;

namespace WxKit.Tests;

// Validators: wxTextValidator through its generated binding, and validators written in C#.
public class ValidatorTests {
	[Fact]
	public Task ChecksTextAgainstAStyle() => Gui.Run(() => {
		using TextValidator validator = new(TextValidatorStyle.Digits);
		Assert.Equal(TextValidatorStyle.Digits, validator.Style);
		Assert.True(validator.Style.HasFlag(TextValidatorStyle.Digits));
		Assert.Empty(validator.GetError("123"));
		Assert.NotEmpty(validator.GetError("12a"));
	});

	[Fact]
	public Task ChecksTextAgainstAList() => Gui.Run(() => {
		using TextValidator validator = new(TextValidatorStyle.IncludeList);
		validator.Includes = ["red", "green"];
		validator.AddInclude("blue");
		Assert.Equal(["red", "green", "blue"], validator.Includes);
		Assert.Empty(validator.GetError("blue"));
		Assert.NotEmpty(validator.GetError("cyan"));
	});

	[Fact]
	public Task RunsACustomValidator() => Gui.WithFrame(frame => {
		Panel panel = new(frame);
		TextCtrl text = new(panel);
		NotEmpty validator = new();
		text.Validator = validator;
		Assert.Same(validator, text.Validator);
		Assert.False(panel.Validate());
		Assert.Same(text, validator.Checked);
		Assert.Same(text, validator.Parent);
		text.Value = "Something";
		Assert.True(panel.Validate());
		text.Validator = null;
		Assert.Null(text.Validator);
		text.Value = string.Empty;
		Assert.True(panel.Validate());
	});

	[Fact]
	public Task TransfersData() => Gui.WithFrame(frame => {
		Panel panel = new(frame);
		TextCtrl text = new(panel);
		Bound validator = new() { Data = "From the app" };
		text.Validator = validator;
		Assert.True(panel.TransferDataToWindow());
		Assert.Equal("From the app", text.Value);
		text.Value = "From the user";
		Assert.True(panel.TransferDataFromWindow());
		Assert.Equal("From the user", validator.Data);
	});

	[Fact]
	public Task ServesSeveralControls() => Gui.WithFrame(frame => {
		Panel panel = new(frame);
		TextCtrl first = new(panel, "Filled");
		TextCtrl second = new(panel);
		NotEmpty validator = new();
		first.Validator = validator;
		second.Validator = validator;
		Assert.False(panel.Validate());
		second.Value = "Filled too";
		Assert.True(panel.Validate());
	});

	[Fact]
	public Task LetsGoOfAValidatorNoControlHolds() => Gui.Run(async () => {
		Frame frame = new(null, "Validator");
		try {
			TextCtrl text = new(new Panel(frame));
			WeakReference validator = SetAndRemove(text);
			for (int i = 0; i < 3 && validator.IsAlive; i++) {
				GC.Collect();
				GC.WaitForPendingFinalizers();
				await Task.Yield();
			}
			Assert.False(validator.IsAlive);
		} finally {
			frame.Close(force: true);
		}
	});

	[Fact]
	public Task ReportsAnExceptionAndFails() => Gui.WithFrame(frame => {
		Panel panel = new(frame);
		TextCtrl text = new(panel);
		text.Validator = new Throwing();
		Assert.False(panel.Validate());
		Exception error = Assert.Single(Gui.TakeUnhandled());
		Assert.IsType<InvalidOperationException>(error);
	});

	// Gives the control a validator and takes it away again, keeping only a weak reference.
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference SetAndRemove(TextCtrl text) {
		NotEmpty validator = new();
		text.Validator = validator;
		text.Validator = null;
		return new WeakReference(validator);
	}

	private sealed class NotEmpty : CustomValidator {
		public Window? Checked { get; private set; }

		public Window? Parent { get; private set; }

		protected override bool OnValidate(Window window, Window? parent) {
			Checked = window;
			Parent = parent;
			return window is TextCtrl text && text.Value.Length > 0;
		}
	}

	private sealed class Bound : CustomValidator {
		public string Data { get; set; } = string.Empty;

		protected override bool OnTransferToWindow(Window window) {
			((TextCtrl)window).Value = Data;
			return true;
		}

		protected override bool OnTransferFromWindow(Window window) {
			Data = ((TextCtrl)window).Value;
			return true;
		}
	}

	private sealed class Throwing : CustomValidator {
		protected override bool OnValidate(Window window, Window? parent) => throw new InvalidOperationException("Validation failed.");
	}
}
