using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Place on the portal seal interaction zone at the end of Ashen Depths.
    ///
    /// Inspector setup:
    ///   questChannel   → QuestChannel SO
    ///   heartCollected → HeartOfGateCollected_Bool SO
    ///   doOnce         → true  (sealing is irreversible)
    ///
    /// Guard check: if the player enters the zone WITHOUT the Heart of the Gate
    /// the event is silently ignored — no feedback, no state change. Add a UI
    /// prompt to the base-class onEnter UnityEvent if you want a "you need the
    /// Heart" message to display regardless of item state.
    ///
    /// When the player enters WITH the Heart:
    ///   1. PortalSealed is raised → QuestManager sets PortalEntered = true.
    ///   2. QuestManager runs CheckAllObjectives — if all pass, QuestCompleted fires.
    ///   3. Wire the collapse cinematic to a TopicResponder listening for
    ///      PortalSealed or QuestCompleted on the QuestChannel.
    /// </summary>
    public class PortalSealTrigger : CollisionHandler
    {
        [SerializeField] private GameChannel questChannel;
        [SerializeField] private BoolVariable heartCollected;
 
        private void OnValidate()
        {
            if (questChannel == null)
                Debug.LogError($"[PortalSealTrigger] QuestChannel is not assigned on {name}!", this);
            if (heartCollected == null)
                Debug.LogError($"[PortalSealTrigger] HeartCollected BoolVariable is not assigned on {name}!", this);
        }
 
        protected override void ApplyEffect(GameObject other)
        {
            if (!other.CompareTag("Player")) return;
            if (!heartCollected.currentValue) return;   // player doesn't have the Heart yet
 
            questChannel.Raise(GameTopic.PortalSealed);
        }
    }
}