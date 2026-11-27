using System.Text;

namespace WxKit.Tests;

public class SoundTests {
	// A twentieth of a second of silence, as 8-bit mono PCM at 8 kHz, so a test that plays it can't
	// be heard over a screen reader.
	private static byte[] Silence() {
		const int rate = 8000;
		byte[] samples = new byte[rate / 20];
		Array.Fill(samples, (byte)128);
		using MemoryStream wave = new();
		using (BinaryWriter writer = new(wave, Encoding.ASCII, leaveOpen: true)) {
			writer.Write("RIFF"u8);
			writer.Write(36 + samples.Length);
			writer.Write("WAVE"u8);
			writer.Write("fmt "u8);
			writer.Write(16);
			writer.Write((short)1);
			writer.Write((short)1);
			writer.Write(rate);
			writer.Write(rate);
			writer.Write((short)1);
			writer.Write((short)8);
			writer.Write("data"u8);
			writer.Write(samples.Length);
			writer.Write(samples);
		}
		return wave.ToArray();
	}

	// Windows fails to play a sound when there is nothing to play it on, as on CI's runners.
	private static bool CanPlay => !OperatingSystem.IsWindows() || Win32.HasAudioOutput;

	[Fact]
	public Task PlaysASoundFromAStream() => Gui.Run(() => {
		using Sound? sound = Sound.FromStream(new MemoryStream(Silence()));
		Assert.NotNull(sound);
		Assert.True(sound.IsOk);
		bool played = sound.Play();
		if (CanPlay) Assert.True(played);
		Sound.Stop();
	});

	[Fact]
	public Task PlaysASoundFromAFile() => Gui.Run(() => {
		string path = Path.Combine(Path.GetTempPath(), $"wxkit-{Guid.NewGuid():N}.wav");
		File.WriteAllBytes(path, Silence());
		try {
			using Sound? sound = Sound.FromFile(path);
			Assert.NotNull(sound);
			Assert.True(sound.IsOk);
			bool played = sound.Play(SoundFlags.Sync);
			if (CanPlay) Assert.True(played);
		} finally {
			File.Delete(path);
		}
	});

	// wxMSW plays a sound from memory out of the sound's own bytes, which a sound disposed of as it
	// plays, as the garbage collector may do, would take with it.
	[Fact]
	public Task GoesOnPlayingASoundFromAStreamOnceItIsDisposed() => Gui.Run(() => {
		Sound? sound = Sound.FromStream(new MemoryStream(Silence()));
		Assert.NotNull(sound);
		sound.Play(SoundFlags.Async | SoundFlags.Loop);
		sound.Dispose();
		Assert.True(sound.IsDisposed);
		using Sound? next = Sound.FromStream(new MemoryStream(Silence()));
		Assert.NotNull(next);
		next.Play();
		Sound.Stop();
	});

	[Fact]
	public Task GivesNullForASoundThatIsNotThere() => Gui.Run(() => {
		Assert.Null(Sound.FromFile(Path.Combine(Path.GetTempPath(), $"wxkit-{Guid.NewGuid():N}.wav")));
		Assert.Null(Sound.FromStream(new MemoryStream()));
	});

	// Only Windows reads a sound as it plays it, rather than as it's loaded.
	[Fact]
	public Task FailsToPlayWhatIsNotASound() => Gui.Run(() => {
		using Sound? sound = Sound.FromStream(new MemoryStream("not a sound"u8.ToArray()));
		if (sound is not null) Assert.False(sound.Play(SoundFlags.Sync));
	});
}
