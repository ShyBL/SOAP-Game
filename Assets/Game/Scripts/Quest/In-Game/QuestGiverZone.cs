using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Abstract mid-level base for all NPC quest interaction zones.
    /// Extends InteractableZone for range detection + interaction gating — see
    /// InteractableZone for why OnRangeEnter/OnRangeExit replace ApplyEffect.
    ///
    /// Owns the fields every quest giver interaction needs:
    ///   questChannel → QuestChannel SO (shared across all quest events)
    ///   npcId        → matches the NpcId on the target NpcQuestGiverSO
    ///   registry     → QuestGiverRegistry SO (project-wide NPC lookup)
    ///
    /// Concrete subclasses (QuestOfferZone, QuestTurnInZone) implement
    /// Interact and call QuestGiverFactory to resolve which quest is
    /// currently actionable. They never hold a direct QuestDefinitionSO
    /// reference — the factory queries live SOAP variable state at the
    /// moment of interaction.
    ///
    /// Inheritance tree:
    ///   CollisionHandler (abstract — existing)
    ///     └── InteractableZone (abstract) — range tracking + IInteractable
    ///           └── QuestGiverZone (abstract — this file)
    ///                 ├── QuestOfferZone  (concrete)
    ///                 └── QuestTurnInZone (concrete)
    ///
    /// Inspector setup (on the NPC GameObject, set once):
    ///   questChannel → QuestChannel SO
    ///   npcId        → e.g. "captain_aldric"  (must match NpcQuestGiverSO.NpcId)
    ///   registry     → QuestGiverRegistry SO
    ///   doOnce       → false  (player can approach and interact repeatedly —
    ///                  each subclass guards against redundant state changes
    ///                  internally, e.g. GetNextOfferable/GetPendingTurnIn
    ///                  returning null when nothing is actionable)
    /// </summary>
    public abstract class QuestGiverZone : InteractableZone
    {
        [Header("Quest Giver")]
        [SerializeField] protected GameChannel questChannel;

        [Tooltip("Must match NpcQuestGiverSO.NpcId exactly. Case-sensitive.")]
        [SerializeField] protected string npcId;

        [SerializeField] protected QuestGiverRegistry registry;

        // Resolved once on first access, cached for the lifetime of the component.
        private NpcQuestGiver _resolvedNpc;

        /// <summary>
        /// Lazily resolves and caches the NpcQuestGiverSO for this zone's npcId.
        /// Subclasses read this property inside Interact.
        /// Returns null if the registry lookup fails — Interact should guard on null.
        /// </summary>
        protected NpcQuestGiver Npc
        {
            get
            {
                if (_resolvedNpc == null)
                    _resolvedNpc = registry?.FindByNpcId(npcId);
                return _resolvedNpc;
            }
        }

        private void OnValidate()
        {
            if (questChannel == null)
                Debug.LogError($"[{GetType().Name}] QuestChannel is not assigned on {name}!", this);
            if (string.IsNullOrEmpty(npcId))
                Debug.LogWarning($"[{GetType().Name}] NpcId is empty on {name}. " +
                                 "Set it to match NpcQuestGiverSO.NpcId.", this);
            if (registry == null)
                Debug.LogError($"[{GetType().Name}] QuestGiverRegistry is not assigned on {name}!", this);
        }

        // ── IInteractable ─────────────────────────────────────────────────────

        public override bool CanInteract(GameObject interactor) => Npc != null;

        public override string InteractionPrompt => Npc != null ? $"Talk to {Npc.DisplayName}" : "Talk";

        // Interact stays abstract — inherited from InteractableZone, implemented
        // per concrete subclass (QuestOfferZone, QuestTurnInZone).
    }
}