using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Orchestrates the turn-in moment: receives the completed quest, shows
    /// the completion panel, blends the camera in, and closes on Continue.
    ///
    /// Not itself a TopicResponder — works alongside a
    /// QuestDefinitionSOTopicResponder component on the same GameObject.
    ///
    /// Inspector setup:
    ///   Add a QuestDefinitionSOTopicResponder to this GameObject.
    ///     channel = QuestChannel, topicToListenFor = QuestTurnedIn
    ///     onTypedResponse → QuestTurnInUIController.ShowCompletion
    ///
    ///   panel     → the QuestTurnInPanel to populate and show/hide
    ///   npcCamera → this NPC's NpcInteractionCamera. Can be the same
    ///               instance used by QuestOfferUIController — one NPC, one
    ///               camera, two independent open/close triggers.
    ///
    ///   Continue button → QuestTurnInUIController.Continue()
    /// </summary>
    public class QuestTurnInUIController : MonoBehaviour
    {
        [SerializeField] private QuestTurnInPanel panel;
        [SerializeField] private NpcInteractionCamera npcCamera;

        private void OnValidate()
        {
            if (panel == null)
                Debug.LogError($"[QuestTurnInUIController] Panel is not assigned on {name}!", this);
        }

        /// <summary>
        /// Wired to QuestDefinitionSOTopicResponder.onTypedResponse for
        /// GameTopic.QuestTurnedIn.
        /// </summary>
        public void ShowCompletion(QuestDefinitionSO quest)
        {
            panel.Populate(quest);
            panel.Show();

            if (npcCamera != null)
                npcCamera.Activate();
        }

        /// <summary>Wired to the Continue button.</summary>
        public void Continue()
        {
            panel.Hide();

            if (npcCamera != null)
                npcCamera.Deactivate();
        }
    }
}
