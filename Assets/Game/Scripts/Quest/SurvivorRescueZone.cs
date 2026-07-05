using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Place on each of the 6 survivor GameObjects.
    ///
    /// Inspector setup:
    ///   questChannel → QuestChannel SO
    ///   doOnce       → true  (base class prevents double-trigger)
    ///
    /// When the Player enters the survivor's trigger collider the survivor is
    /// deactivated and SurvivorRescued is raised on the QuestChannel.
    /// QuestManager hears SurvivorRescued and increments SurvivorsRescued_Int.
    /// </summary>
    public class SurvivorRescueZone : CollisionHandler
    {
        [SerializeField] private GameChannel questChannel;

        private void OnValidate()
        {
            if (questChannel == null)
                Debug.LogError($"[SurvivorRescueZone] QuestChannel is not assigned on {name}!", this);
        }

        protected override void ApplyEffect(GameObject other)
        {
            if (!other.CompareTag("Player")) return;

            questChannel.Raise(GameTopic.SurvivorRescued);
            gameObject.SetActive(false);    // hide the survivor after rescue
        }
    }
}