using UnityEngine;
using UnityEngine.InputSystem;

namespace SOAP
{
    /// <summary>
    /// Lives on the Player. Tracks the current nearby IInteractable —
    /// registered/unregistered by whatever range-detection component finds it
    /// (e.g. QuestGiverZone via CollisionHandler.OnRangeEnter/OnRangeExit) —
    /// and performs the interaction when the player presses the Interact
    /// action, via the New Input System.
    ///
    /// Inspector setup:
    ///   interactAction → an InputActionReference pointing at your project's
    ///                    "Interact" action (e.g. from a PlayerControls asset).
    /// </summary>
    public class PlayerInteractionController : MonoBehaviour
    {
        [SerializeField] private InputActionReference interactAction;

        private IInteractable currentInteractable;

        /// <summary>
        /// The prompt text for whatever is currently in range, or null if
        /// nothing is. Read by prompt UI (e.g. "Press E to talk").
        /// </summary>
        public string CurrentPrompt => HasValidInteractable() ? currentInteractable.InteractionPrompt : null;

        private void OnValidate()
        {
            if (interactAction == null)
                Debug.LogError($"[PlayerInteractionController] InteractAction is not assigned on {name}!", this);
        }

        private void OnEnable()
        {
            if (interactAction != null)
            {
                interactAction.action.Enable();
                interactAction.action.performed += OnInteractPerformed;
            }
        }

        private void OnDisable()
        {
            if (interactAction != null)
                interactAction.action.performed -= OnInteractPerformed;
        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            if (HasValidInteractable() && currentInteractable.CanInteract(gameObject))
                currentInteractable.Interact(gameObject);
        }

        /// <summary>
        /// A zone that deactivates or destroys itself while the player is
        /// still inside its trigger (a collected pickup, a rescued survivor)
        /// never gets an exit callback, so it would stay registered and could
        /// be interacted with again. Drop it here instead.
        /// </summary>
        private bool HasValidInteractable()
        {
            if (currentInteractable == null) return false;

            if (currentInteractable is Component component
                && (component == null || !component.gameObject.activeInHierarchy))
            {
                currentInteractable = null;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Called by a range-detection component (e.g. QuestGiverZone) when
        /// the player enters its trigger. Overwrites whatever was previously
        /// registered — only one interactable is tracked at a time.
        /// </summary>
        public void RegisterInteractable(IInteractable interactable)
        {
            currentInteractable = interactable;
        }

        /// <summary>
        /// Called by a range-detection component when the player leaves its
        /// trigger. Only clears the tracked interactable if it matches —
        /// guards against a stale exit call clearing a different, newer
        /// registration (e.g. overlapping trigger volumes).
        /// </summary>
        public void UnregisterInteractable(IInteractable interactable)
        {
            if (currentInteractable == interactable)
                currentInteractable = null;
        }
    }
}