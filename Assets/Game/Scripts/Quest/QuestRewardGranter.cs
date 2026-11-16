using UnityEngine;
using UnityEngine.Events;

namespace SOAP
{
    /// <summary>
    /// Pays out every quest's RewardItems automatically. Place ONE of these
    /// in the persistent scene (next to QuestManager) — quests carry their own
    /// rewards as data, so there is no per-quest scene wiring.
    ///
    /// Listens on the quest channel for grantOn (default QuestTurnedIn, raised
    /// with the quest as payload by QuestTurnInZone). Use QuestCompleted
    /// instead if items should arrive the moment objectives finish.
    ///
    /// A persisted flag per quest makes the payout once-only, so a repeated
    /// event can never duplicate gear.
    ///
    /// Inspector setup:
    ///   questChannel         → QuestChannel SO
    ///   inventory            → the player's InventorySO
    ///   grantOn              → QuestTurnedIn or QuestCompleted
    ///   overflowPickupPrefab → prefab with an ItemPickup (persistentId EMPTY)
    ///   overflowPoint        → where overflow lands (usually the player)
    ///   onItemsGranted       → hook a "received items" toast or sound
    /// </summary>
    public class QuestRewardGranter : MonoBehaviour
    {
        [SerializeField] private GameChannel questChannel;
        [SerializeField] private InventorySO inventory;
        [SerializeField] private GameTopic grantOn = GameTopic.QuestTurnedIn;

        [Header("Overflow (backpack full)")]
        [SerializeField] private ItemPickup overflowPickupPrefab;
        [SerializeField] private Transform overflowPoint;

        [Header("Events")]
        [SerializeField] private UnityEvent onItemsGranted;

        private void OnValidate()
        {
            if (questChannel == null)
                Debug.LogError($"[QuestRewardGranter] QuestChannel is not assigned on {name}!", this);
            if (inventory == null)
                Debug.LogError($"[QuestRewardGranter] Inventory is not assigned on {name}!", this);
        }

        private void OnEnable()
        {
            if (questChannel != null)
                questChannel.Subscribe(grantOn, OnQuestEvent);
        }

        private void OnDisable()
        {
            if (questChannel != null)
                questChannel.Unsubscribe(grantOn, OnQuestEvent);
        }

        private void OnQuestEvent(object data)
        {
            if (!(data is QuestDefinitionSO quest)) return;
            if (quest.RewardItems == null || quest.RewardItems.Length == 0) return;

            bool trackable = !string.IsNullOrEmpty(quest.QuestId);
            string key = $"{quest.QuestId}_items_granted";

            if (trackable && SaveSystem.RequestLoad(key) == "1") return;

            Vector3 overflowPosition = overflowPoint != null ? overflowPoint.position : transform.position;
            ItemGrantUtility.Grant(inventory, quest.RewardItems, overflowPickupPrefab, overflowPosition, this);

            if (trackable)
            {
                SaveSystem.Set(key, "1");
                SaveSystem.Flush();
            }

            onItemsGranted?.Invoke();
        }
    }
}
