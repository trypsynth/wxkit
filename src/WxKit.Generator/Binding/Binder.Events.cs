using System.Globalization;
using System.Text.RegularExpressions;
using WxKit.Generator.Doxygen;

namespace WxKit.Generator.Binding;

// The events a class sends, and the types of event they are.
internal sealed partial class Binder {
	private void BindEvents(BoundClass bound) {
		ClassTweak tweak = bound.Tweak;
		List<DoxygenEvent> events = [.. bound.Source.Events, .. tweak.AddedEvents.Select(static added => new DoxygenEvent(added.Macro, added.Constant, added.EventClass, added.Description))];
		HashSet<string> macros = [.. events.Select(static found => found.Macro)];
		foreach (string named in tweak.EventNames.Keys.Concat(tweak.IgnoredEvents)) {
			if (!macros.Contains(named)) throw new InvalidOperationException($"A tweak names the event {named} of {bound.CppName}, which its event table does not list.");
		}
		foreach (DoxygenEvent found in events.Where(found => !tweak.IgnoredEvents.Contains(found.Macro))) {
			// A macro for a range of ids, or a scroll event's macro that takes an id, binds the event
			// type another macro already does, so it would be the same event under a second name.
			if (found.Macro.EndsWith("_RANGE", StringComparison.Ordinal) || found.Macro.StartsWith("EVT_COMMAND_SCROLL", StringComparison.Ordinal)) {
				_skipped.Add($"{bound.CppName} event {found.Macro}: another macro binds the same event type");
				continue;
			}
			// With a list of members to bind, events are bound only when a tweak names them too.
			if (!tweak.EventNames.TryGetValue(found.Macro, out string? name)) {
				if (tweak.OnlyMembers is not null) continue;
				name = Naming.Event(found.Macro);
			}
			// An entry that leaves out its event type follows the convention that EVT_X sends wxEVT_X.
			// Entries for groups of events, such as EVT_MOUSE_EVENTS or EVT_JOY_*, send no one type, so
			// only an event type the headers declare is bound.
			string constant = found.EventType ?? "wx" + found.Macro;
			if (!MacroPattern().IsMatch(found.Macro) || !api.EventTypes.ContainsKey(constant)) {
				_skipped.Add($"{bound.CppName} event {found.Macro}: it is not one event type the headers declare");
				continue;
			}
			BoundEventType type = EventTypeFor(constant, found.EventClass ?? "wxEvent");
			// An event class's own table only says which event types it carries, and a class
			// re-lists the events it inherits, so neither gets a .NET event.
			if (!bound.Handle.IsEvtHandler || HasEvent(bound.Base, name)) continue;
			// C# allows no member named after its class, and none named after another member.
			if (HasMember(bound, name) || name == bound.Name) {
				_skipped.Add($"{bound.CppName} event {found.Macro}: named {name}Event, since {name} is taken");
				name += "Event";
			}
			if (bound.Events.Any(existing => existing.Name == name)) continue;
			bound.Events.Add(new BoundEvent(name, type, found.Description));
		}
	}

	private static bool HasEvent(BoundClass? bound, string name) {
		for (BoundClass? current = bound; current is not null; current = current.Base) {
			if (current.Events.Any(boundEvent => boundEvent.Name == name)) return true;
		}
		return false;
	}

	private static bool HasMember(BoundClass bound, string name) {
		for (BoundClass? current = bound; current is not null; current = current.Base) {
			if (current.Properties.Any(property => property.Name == name) || current.Methods.Any(method => method.Name == name)) return true;
		}
		return false;
	}

	private BoundEventType EventTypeFor(string constant, string eventClass) {
		if (_eventTypes.TryGetValue(constant, out BoundEventType? existing)) return existing;
		if (tweaks.ExtraEventTypes.TryGetValue(constant, out string? given)) eventClass = given;
		// An event class WxKit does not bind is delivered as its nearest bound ancestor.
		BoundClass wrapper = Ancestors(eventClass).Select(name => _classes.GetValueOrDefault(name)).FirstOrDefault(static candidate => candidate?.Handle == HandleKind.Event)
			?? throw new InvalidOperationException($"No bound event class can carry {constant}, a {eventClass}.");
		BoundEventType type = new(constant, Naming.EventType(constant), wrapper, api.EventTypes.GetValueOrDefault(constant), tweaks.EventTypeGuards.GetValueOrDefault(constant));
		_eventTypes[constant] = type;
		return type;
	}

	[GeneratedRegex(@"^EVT_[A-Z0-9_]+$")]
	private static partial Regex MacroPattern();
}
