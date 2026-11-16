using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Apply to any string field that holds a content-specific event ID —
    /// QuestObjectiveSO.EventId, or a trigger script's own eventId field.
    ///
    /// Renders as a normal text field (typing a brand-new ID always works)
    /// plus a small dropdown button listing every EventId currently in use
    /// across the project's QuestObjectiveSO assets — picking one overwrites
    /// the field, making reuse a one-click action instead of copy-paste.
    ///
    /// This exists because string fields don't get compiler-checked the way
    /// an enum does — a typo fails silently instead of failing to build.
    /// The dropdown closes most of that gap without giving up the
    /// flexibility of free-form content authoring.
    /// </summary>
    public class EventIdFieldAttribute : PropertyAttribute { }
}
