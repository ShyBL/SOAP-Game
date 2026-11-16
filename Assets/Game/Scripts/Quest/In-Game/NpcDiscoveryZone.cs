using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Place near an NPC to mark "the player found this location" — used by
    /// the Prologue's single step (find the quest giver via trigger/range,
    /// not interaction). Same shape as EchoTriggerZone: a single, fixed story
    /// beat, so a plain trigger raising one EventId is enough — no per-NPC
    /// string routing needed.
    ///
    /// Inspector setup:
    ///   questChannel → QuestChannel SO
    ///   eventId      → must match the Prologue's single BoolFlagObjectiveSO's
    ///                  EventId exactly (authored data, not enforced in code)
    ///   doOnce       → true (discovering a location only needs to happen once)
    /// </summary>
    public class NpcDiscoveryZone : CollisionHandler
    {
        [SerializeField] private GameChannel questChannel;
        [SerializeField, EventIdField] private string eventId;

        private void OnValidate()
        {
            if (questChannel == null)
                Debug.LogError($"[NpcDiscoveryZone] QuestChannel is not assigned on {name}!", this);
            if (string.IsNullOrEmpty(eventId))
                Debug.LogWarning($"[NpcDiscoveryZone] EventId is empty on {name} — this won't advance anything.", this);
        }

        protected override void ApplyEffect(GameObject other)
        {
            if (!other.CompareTag("Player")) return;
            questChannel.Raise(eventId);
        }
    }
}