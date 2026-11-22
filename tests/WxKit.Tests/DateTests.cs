namespace WxKit.Tests;

// Date and time controls, whose values are DateTimes in local time.
public class DateTests {
	[Fact]
	public Task PicksADate() => Gui.WithFrame(frame => {
		DateTime birthday = new(1990, 7, 14);
		DatePickerCtrl picker = new(frame, birthday);
		Assert.Equal(birthday, picker.Value?.Date);
		Assert.Equal(DateTimeKind.Local, picker.Value?.Kind);
		DateTime later = new(2026, 2, 28);
		picker.Value = later;
		Assert.Equal(later, picker.Value?.Date);
		picker.SetRange(new DateTime(2000, 1, 1), new DateTime(2030, 12, 31));
	});

	[Fact]
	public Task PicksATime() => Gui.WithFrame(frame => {
		TimePickerCtrl picker = new(frame);
		Assert.True(picker.SetTime(13, 45, 30));
		Assert.Equal(new TimeSpan(13, 45, 30), picker.Value?.TimeOfDay);
	});

	[Fact]
	public Task ShowsACalendar() => Gui.WithFrame(frame => {
		DateTime day = new(2026, 11, 15);
		CalendarCtrl calendar = new(frame, day);
		Assert.Equal(day, calendar.Date?.Date);
		// A UTC time goes in as the local time it is.
		DateTime noon = new(2026, 3, 9, 12, 0, 0, DateTimeKind.Utc);
		calendar.Date = noon;
		Assert.Equal(noon.ToLocalTime().Date, calendar.Date?.Date);
		Assert.True(calendar.SetDateRange(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31)));
	});
}
