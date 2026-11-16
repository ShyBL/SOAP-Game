using UnityEngine;
using UnityEngine.Events;

namespace SOAP
{
    /// <summary>
    /// Orchestrates the quest offer moment: receives the offered quest,
    /// shows the panel, blends the camera in, and handles Accept/Decline.
    ///
    /// Not itself a TopicResponder — works alongside a
    /// QuestDefinitionSOTopicResponder component on the same GameObject.
    ///
    /// Inspector setup:
    ///   Add a QuestDefinitionSOTopicResponder to this GameObject.
    ///     channel = QuestChannel, topicToListenFor = QuestOffered
    ///     onTypedResponse → QuestOfferUIController.ShowOffer
    ///
    ///   questChannel     → QuestChannel SO (used to raise QuestAccepted/QuestDeclined)
    ///   panel            → the QuestOfferPanel to populate and show/hide
    ///   npcCamera        → this NPC's NpcInteractionCamera (nullable — camera
    ///                       blend is optional per NPC)
    ///   onDeclined       → wire to this NPC's dialogue/bark system for a
    ///                       rejection line
    ///
    ///   Accept button  → QuestOfferUIController.Accept()
    ///   Decline button → QuestOfferUIController.Decline()
    /// </summary>
    public class QuestOfferUIController : MonoBehaviour
    {
        [SerializeField] private GameChannel questChannel;
        [SerializeField] private QuestOfferPanel panel;
        [SerializeField] private NpcInteractionCamera npcCamera;

        [Space]
        [Tooltip("Fired when the player declines. Wire to this NPC's dialogue " +
                 "system for a rejection bark. Local per-NPC hook — separate from " +
                 "the QuestDeclined channel event, which is for anything global.")]
        public UnityEvent OnDeclined;

        private QuestDefinitionSO pendingQuest;

        private void OnValidate()
        {
            if (questChannel == null)
                Debug.LogError($"[QuestOfferUIController] QuestChannel is not assigned on {name}!", this);
            if (panel == null)
                Debug.LogError($"[QuestOfferUIController] Panel is not assigned on {name}!", this);
        }

        /// <summary>
        /// Wired to QuestDefinitionSOTopicResponder.onTypedResponse for
        /// GameTopic.QuestOffered. Caches the quest, populates and shows the
        /// panel, and blends the camera in if one is assigned.
        /// </summary>
        public void ShowOffer(QuestDefinitionSO quest)
        {
            pendingQuest = quest;
            panel.Populate(quest);
            panel.Show();

            if (npcCamera != null)
                npcCamera.Activate();
        }

        /// <summary>
        /// Wired to the Accept button. Activates the quest — this is the
        /// only place QuestActive gets set to true for an offered quest.
        /// </summary>
        public void Accept()
        {
            if (pendingQuest == null) return;

            pendingQuest.QuestActive.Set(true);
            questChannel.Raise(GameTopic.QuestAccepted, pendingQuest);

            ClosePanel();
        }

        /// <summary>
        /// Wired to the Decline button. No state change — the quest remains
        /// offerable, the player can re-trigger the offer later. Raises
        /// QuestDeclined (for anything global) and invokes OnDeclined (for
        /// this NPC's immediate reaction bark).
        /// </summary>
        public void Decline()
        {
            if (pendingQuest == null) return;

            questChannel.Raise(GameTopic.QuestDeclined, pendingQuest);
            OnDeclined?.Invoke();

            ClosePanel();
        }

        private void ClosePanel()
        {
            panel.Hide();

            if (npcCamera != null)
                npcCamera.Deactivate();

            pendingQuest = null;
        }
    }
}
