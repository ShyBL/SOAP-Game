using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Base for any CollisionHandler zone where interaction is gated by
    /// player input, not trigger enter — range only registers/unregisters
    /// with PlayerInteractionController; the actual effect runs from Interact,
    /// called when the player presses the interact input while registered.
    ///
    /// Extracted from QuestGiverZone and SurvivorRescueZone, which had
    /// identical OnRangeEnter/OnRangeExit/ApplyEffect boilerplate.
    ///
    /// See CollisionHandler for why OnRangeEnter/OnRangeExit are used instead
    /// of ApplyEffect/onEnter/onExit (the double-fire footgun).
    /// </summary>
    public abstract class InteractableZone : CollisionHandler, IInteractable
    {
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

        /// <summary>
        /// Intentionally unused. All business logic lives in Interact, gated by
        /// player input via PlayerInteractionController rather than by trigger
        /// enter/exit. Required override to satisfy CollisionHandler's contract.
        /// </summary>
        protected override void ApplyEffect(GameObject other) { }

        // ── IInteractable ─────────────────────────────────────────────────────

        public virtual bool CanInteract(GameObject interactor) => true;
        public abstract void Interact(GameObject interactor);
        public abstract string InteractionPrompt { get; }
    }
}