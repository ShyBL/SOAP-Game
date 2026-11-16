using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Place one instance at each Echo location.
    ///
    /// Inspector setup:
    ///   questChannel → QuestChannel SO
    ///   eventId      → e.g. "echo_discovered" — must match a
    ///                  QuestObjectiveSO's EventId to advance anything
    ///   doOnce       → true  (each echo can only be discovered once)
    ///   onEnter      → wire to the vision/cutscene trigger for this Echo
    /// </summary>
    public class EchoTriggerZone : CollisionHandler
    {
        [SerializeField] private GameChannel questChannel;
        [SerializeField, EventIdField] private string eventId;

        private void OnValidate()
        {
            if (questChannel == null)
                Debug.LogError($"[EchoTriggerZone] QuestChannel is not assigned on {name}!", this);
            if (string.IsNullOrEmpty(eventId))
                Debug.LogWarning($"[EchoTriggerZone] EventId is empty on {name} — this won't advance anything.", this);
        }

        protected override void ApplyEffect(GameObject other)
        {
            if (!other.CompareTag("Player")) return;

            questChannel.Raise(eventId);
            // Vision/cutscene fires via the base-class onEnter UnityEvent wired in the inspector.
        }
    }
}