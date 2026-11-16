using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Contract for anything the player can interact with via an input press
    /// while in range — quest givers, doors, levers, pickups that need
    /// confirmation, etc.
    ///
    /// Decoupled from CollisionHandler and physics callbacks entirely — range
    /// detection and interaction execution are separate concerns. Something
    /// implementing this interface is registered with PlayerInteractionController
    /// on range enter and unregistered on range exit; the interaction itself
    /// only fires when the player presses the interact input while registered.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Whether this interactable can currently be interacted with.
        /// Checked by PlayerInteractionController before calling Interact —
        /// lets an interactable temporarily disable itself (e.g. an NPC mid-dialogue)
        /// without needing to unregister/re-register.
        /// </summary>
        bool CanInteract(GameObject interactor);

        /// <summary>
        /// Performs the interaction. Called by PlayerInteractionController
        /// when the interact input fires while this is the registered interactable.
        /// </summary>
        void Interact(GameObject interactor);

        /// <summary>
        /// Short prompt text for UI (e.g. "Talk", "Open"). Read by
        /// PlayerInteractionController.CurrentPrompt for an on-screen hint.
        /// </summary>
        string InteractionPrompt { get; }
    }
}
