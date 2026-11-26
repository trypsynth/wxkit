using WxKit.Interop;

namespace WxKit;

// A dialog is shown modally and then destroyed, which IDisposable lets a using block do.
/// <remarks>
/// Show a dialog with <see cref="ShowModal"/> inside a <c>using</c> block, which destroys it once
/// it closes.
/// </remarks>
public unsafe partial class Dialog : IDisposable {
	/// <summary>
	/// Shows the dialog modal to its parent window alone, which on macOS is a sheet, and returns at
	/// once. The task ends when the dialog closes, with what it returned, such as
	/// <see cref="StandardId.Ok"/>. Wraps ShowWindowModal.
	/// </summary>
	public Task<int> ShowWindowModalAsync() {
		TaskCompletionSource<int> closed = new(TaskCreationOptions.RunContinuationsAsynchronously);
		EventHandler<WindowModalDialogEvent>? handler = null;
		handler = (_, e) => {
			WindowModalClosed -= handler;
			closed.TrySetResult(e.ReturnCode);
		};
		WindowModalClosed += handler;
		WxKitNative.DialogShowWindowModal(Native);
		return closed.Task;
	}

	/// <summary>
	/// Makes a sizer with the dialog's standard buttons, in the platform's order, with the usual
	/// one the default. Add it to the dialog's sizer. The buttons close the dialog as their ids
	/// say, such as <see cref="StandardId.Ok"/>. Wraps CreateStdDialogButtonSizer.
	/// </summary>
	public StdDialogButtonSizer CreateStdDialogButtonSizer(DialogButtons buttons) => new(WxKitNative.DialogCreateStdDialogButtonSizer(Native, (long)buttons));

	/// <summary>Destroys the dialog, unless wxWidgets already has.</summary>
	public void Dispose() {
		if (!IsDestroyed) Destroy();
		GC.SuppressFinalize(this);
	}
}

/// <summary>Shows a message in one call.</summary>
public static class MessageBox {
	/// <summary>Shows a <see cref="MessageDialog"/> and returns the id of the button that closed it, such as <see cref="StandardId.Yes"/>.</summary>
	public static int Show(string message, string caption = "Message", MessageDialogStyle style = MessageDialogStyle.Ok | MessageDialogStyle.Center, Window? parent = null) {
		using MessageDialog dialog = new(parent, message, caption, style);
		return dialog.ShowModal();
	}
}
