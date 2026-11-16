using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Place on the portal seal interaction zone.
    ///
    /// Inspector setup:
    ///   questChannel   → QuestChannel SO
    ///   eventId        → e.g. "portal_sealed" — must match a
    ///                    QuestObjectiveSO's EventId to advance anything
    ///   heartCollected → the item-collected BoolVariable gating this trigger
    ///   doOnce         → true  (sealing is irreversible)
    ///
    /// Guard check: if the player enters the zone WITHOUT the required item
    /// the event is silently ignored — no feedback, no state change.
    /// </summary>
    public class PortalSealTrigger : CollisionHandler
    {
        [SerializeField] private GameChannel questChannel;
        [SerializeField, EventIdField] private string eventId;
        [SerializeField, SOCreate] private BoolVariable heartCollected;

        private void OnValidate()
        {
            if (questChannel == null)
                Debug.LogError($"[PortalSealTrigger] QuestChannel is not assigned on {name}!", this);
            if (string.IsNullOrEmpty(eventId))
                Debug.LogWarning($"[PortalSealTrigger] EventId is empty on {name} — this won't advance anything.", this);
            if (heartCollected == null)
                Debug.LogError($"[PortalSealTrigger] HeartCollected BoolVariable is not assigned on {name}!", this);
        }

        protected override void ApplyEffect(GameObject other)
        {
            if (!other.CompareTag("Player")) return;
            if (!heartCollected.currentValue) return;   // player doesn't have the item yet

            questChannel.Raise(eventId);
        }
    }
}