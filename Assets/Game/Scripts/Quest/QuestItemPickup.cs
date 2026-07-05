using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Place on the Heart of the Gate item that drops from Malgor (100% drop).
    ///
    /// Inspector setup:
    ///   questChannel → QuestChannel SO
    ///   doOnce       → true  (prevents double-pickup if player re-enters collider)
    ///
    /// When the Player collides with the item:
    ///   1. QuestItemPickedUp is raised → QuestManager sets HeartCollected = true.
    ///   2. The item GameObject is deactivated (acts as visual pickup).
    ///
    /// For a proper inventory integration, replace or supplement gameObject.SetActive(false)
    /// with your inventory add call and optionally a pickup VFX trigger on the
    /// base-class onEnter UnityEvent.
    /// </summary>
    public class QuestItemPickup : CollisionHandler
    {
        [SerializeField] private GameChannel questChannel;
 
        private void OnValidate()
        {
            if (questChannel == null)
                Debug.LogError($"[QuestItemPickup] QuestChannel is not assigned on {name}!", this);
        }
 
        protected override void ApplyEffect(GameObject other)
        {
            if (!other.CompareTag("Player")) return;
 
            questChannel.Raise(GameTopic.QuestItemPickedUp);
            gameObject.SetActive(false);    // hide the item after pickup
        }
    }
}