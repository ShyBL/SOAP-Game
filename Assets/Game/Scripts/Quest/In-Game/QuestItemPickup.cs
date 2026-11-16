using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Place on any quest item drop.
    ///
    /// Inspector setup:
    ///   questChannel → QuestChannel SO
    ///   eventId      → e.g. "heart_collected" — must match a
    ///                  QuestObjectiveSO's EventId to advance anything
    ///   doOnce       → true  (prevents double-pickup if player re-enters collider)
    ///
    /// When the Player collides with the item: eventId is raised and the
    /// item GameObject is deactivated (acts as visual pickup).
    /// </summary>
    public class QuestItemPickup : CollisionHandler, IInteractable
    {
        [SerializeField] private GameChannel questChannel;
        [SerializeField, EventIdField] private string eventId;

        private void OnValidate()
        {
            if (questChannel == null)
                Debug.LogError($"[QuestItemPickup] QuestChannel is not assigned on {name}!", this);
            if (string.IsNullOrEmpty(eventId))
                Debug.LogWarning($"[QuestItemPickup] EventId is empty on {name} — this won't advance anything.", this);
        }

        protected override void ApplyEffect(GameObject other)
        {
            // if (!other.CompareTag("Player")) return;
            //
            // questChannel.Raise(eventId);
            // gameObject.SetActive(false);    // hide the item after pickup
        }
        
        protected override void OnRangeEnter(GameObject other)
        {
            if (!other.CompareTag("Player")) return;
            if (other.TryGetComponent(out PlayerInteractionController controller))
                controller.RegisterInteractable(this);
        }

        protected override void OnRangeExit(GameObject other)
        {
            if (!other.CompareTag("Player")) return;
            if (other.TryGetComponent(out PlayerInteractionController controller))
                controller.UnregisterInteractable(this);
        }
        
        public bool CanInteract(GameObject interactor)
        {
            throw new System.NotImplementedException();
        }

        public void Interact(GameObject interactor)
        {
            if (!interactor.CompareTag("Player")) return;

            questChannel.Raise(eventId);
            gameObject.SetActive(false);    // hide the item after pickup
        }

        public string InteractionPrompt { get; }
    }
}