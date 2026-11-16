using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Place on each survivor. Normally placed and wired by the World
    /// Authoring window from a quest objective — it fills in questChannel,
    /// eventId, and a unique persistentId, so nothing is typed by hand.
    ///
    /// Inspector setup (only if placing manually):
    ///   questChannel → QuestChannel SO
    ///   eventId      → e.g. "survivor_rescued" — must match a
    ///                  QuestObjectiveSO's EventId to advance anything
    ///   persistentId → unique per survivor; remembers the rescue across
    ///                  save/load so the survivor does not return and cannot
    ///                  be counted twice. Empty = not remembered.
    ///
    /// Interaction (not trigger enter) rescues the survivor — see
    /// InteractableZone for why. Player must be in range and press interact.
    /// </summary>
    public class SurvivorRescueZone : InteractableZone
    {
        [SerializeField] private GameChannel questChannel;
        [SerializeField, EventIdField] private string eventId;
        [SerializeField] private string persistentId;

        private bool rescued;

        private string SaveKey => $"rescue_{persistentId}";

        private void OnValidate()
        {
            if (questChannel == null)
                Debug.LogError($"[SurvivorRescueZone] QuestChannel is not assigned on {name}!", this);
            if (string.IsNullOrEmpty(eventId))
                Debug.LogWarning($"[SurvivorRescueZone] EventId is empty on {name} — this won't advance anything.", this);
        }

        private void Start()
        {
            if (!string.IsNullOrEmpty(persistentId) && SaveSystem.RequestLoad(SaveKey) == "1")
            {
                rescued = true;
                gameObject.SetActive(false);
            }
        }

        public override bool CanInteract(GameObject interactor) => !rescued;

        public override void Interact(GameObject interactor)
        {
            if (rescued) return;
            if (!interactor.CompareTag("Player")) return;

            rescued = true;

            if (!string.IsNullOrEmpty(persistentId))
            {
                SaveSystem.Set(SaveKey, "1");
                SaveSystem.Flush();
            }

            questChannel.Raise(eventId);
            gameObject.SetActive(false);    // hide the survivor after rescue
        }

        public override string InteractionPrompt => "Rescue";
    }
}