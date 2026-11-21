using System.Text;
using WxKit.Interop;

namespace WxKit;

/// <summary>How insistently a screen reader delivers an announcement.</summary>
/// <remarks>
/// <para>
/// On Windows, it is the UI Automation notification's processing: <see cref="Low"/> queues behind
/// everything pending, <see cref="Medium"/> lets what is being said finish and then drops anything
/// staler, and <see cref="High"/> cuts off what is being said. NVDA cancels speech for
/// <see cref="High"/>, except during a say all, which it speaks over instead of cutting short.
/// </para>
/// <para>
/// On Linux, <see cref="Low"/> and <see cref="Medium"/> are ATK's polite, and <see cref="High"/>
/// its assertive. On macOS, they are VoiceOver's low, medium, and high, which it queues
/// unreliably: an announcement of the same priority as the one being spoken, or a high one,
/// interrupts it, and only two high ones in a row clear what is waiting.
/// </para>
/// </remarks>
public enum AnnouncementPriority {
	/// <summary>The least urgent.</summary>
	Low,

	/// <summary>More urgent than low.</summary>
	Medium,

	/// <summary>The most urgent, which interrupts. The default.</summary>
	High,
}

/// <summary>
/// An empty label that a window has screen readers speak through, for news that has no control of
/// its own to land on, such as a status that changed. It shows nothing. Add it to the window's
/// sizer or not, as it has no size to speak of.
/// </summary>
/// <remarks>
/// <para>
/// It follows accessware/live-region, a crate for wxDragon tested against NVDA, VoiceOver, and
/// Orca. On Windows it raises a UI Automation notification, which a screen reader speaks without
/// moving the focus, and gives the label the UI Automation provider of its own that raising one
/// needs, since the window's default provider raises it to no one. On macOS it posts VoiceOver an
/// announcement, on the next pass of the run loop, and on Linux it emits ATK's notification
/// signal, or before ATK 2.50 its announcement signal.
/// </para>
/// <para>
/// Each message is cleaned first: line breaks and tabs become spaces, other control characters
/// are left out, runs of spaces become one, and it is cut to 512 characters. An empty message is
/// not announced.
/// </para>
/// </remarks>
public sealed unsafe class LiveRegion : StaticText {
	internal const int MaxLength = 512;

	/// <summary>Makes a live region in a window, usually the window's panel or the window itself.</summary>
	public LiveRegion(Window parent) : base(parent, string.Empty) {
	}

	/// <summary>
	/// Gets the live region ready, and returns whether announcements can work. Announcing does it
	/// too, so call this only to find out early.
	/// </summary>
	public bool Prepare() {
		Application.VerifyAccess();
		return WxKitNative.LiveRegionPrepare((WxkWindow*)Native) != 0;
	}

	/// <summary>
	/// Has screen readers speak a message, and returns whether one could be told. On Windows, that
	/// is false while no UI Automation client, such as a screen reader, is listening.
	/// </summary>
	public bool Announce(string message, AnnouncementPriority priority = AnnouncementPriority.High) {
		ArgumentNullException.ThrowIfNull(message);
		Application.VerifyAccess();
		string cleaned = Clean(message);
		if (cleaned.Length == 0) return false;
		fixed (char* text = cleaned) return WxKitNative.LiveRegionAnnounce((WxkWindow*)Native, text, NativeString.Length(cleaned), (int)priority) != 0;
	}

	// One line of speech: line breaks and tabs as spaces, no other control characters, single
	// spaces, and at most MaxLength characters, without half a surrogate pair at the end.
	internal static string Clean(string message) {
		StringBuilder text = new(message.Length);
		bool space = false;
		foreach (char character in message) {
			if (char.IsWhiteSpace(character)) {
				space = text.Length > 0;
				continue;
			}
			if (char.IsControl(character)) continue;
			if (space) text.Append(' ');
			space = false;
			text.Append(character);
		}
		if (text.Length <= MaxLength) return text.ToString();
		int length = char.IsHighSurrogate(text[MaxLength - 1]) ? MaxLength - 1 : MaxLength;
		return text.ToString(0, length);
	}
}
