using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Place one instance at each of the three Echo locations:
    ///   Echo 1 — Market:      portal opens, sky tears
    ///   Echo 2 — Noble House: deal with shadow figure
    ///   Echo 3 — Cathedral:   last defense before collapse
    ///
    /// Inspector setup:
    ///   questChannel → QuestChannel SO
    ///   doOnce       → true  (each echo can only be discovered once)
    ///   onEnter      → wire to the vision/cutscene trigger for this Echo
    ///                  (base class fires onEnter automatically after ApplyEffect)
    ///
    /// Flow per Echo:
    ///   1. Player enters trigger zone.
    ///   2. ApplyEffect raises EchoDiscovered → QuestManager increments EchoesFound.
    ///   3. Base class fires onEnter UnityEvent → vision sequence plays.
    /// </summary>
    public class EchoTriggerZone : CollisionHandler
    {
        [SerializeField] private GameChannel questChannel;
 
        private void OnValidate()
        {
            if (questChannel == null)
                Debug.LogError($"[EchoTriggerZone] QuestChannel is not assigned on {name}!", this);
        }
 
        protected override void ApplyEffect(GameObject other)
        {
            if (!other.CompareTag("Player")) return;
 
            questChannel.Raise(GameTopic.EchoDiscovered);
            // Vision/cutscene fires via the base-class onEnter UnityEvent wired in the inspector.
        }
    }
}