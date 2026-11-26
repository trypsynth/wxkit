namespace WxKit.Tests;

// Notifications, which the tests make and free without showing, since a notification posted from a
// test run can stay on the desktop, and macOS only posts them from an app bundle.
public class NotificationTests {
	[Fact]
	public Task MakesAndFreesANotification() => Gui.WithFrame(frame => {
		NotificationMessage notification = new("Saved", "Your tasks are saved.", frame, MessageIcon.Warning);
		notification.SetTitle("Backed up");
		notification.SetMessage("Your tasks are backed up.");
		notification.SetFlags(MessageIcon.Error);
		notification.Click += (_, _) => { };
		Assert.False(notification.IsDisposed);
		notification.Dispose();
		Assert.True(notification.IsDisposed);
		notification.Dispose();
	});
}
