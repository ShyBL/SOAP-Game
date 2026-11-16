using UnityEngine;
using UnityEngine.Events;

namespace SOAP
{
    /// <summary>
    /// A world item that goes into the backpack. Replaces one-off pickup
    /// scripts: the same component serves a hand-placed scroll, a quest item,
    /// a chest reward, and enemy loot spawned at runtime.
    ///
    /// Extends InteractableZone — the player presses interact while in range
    /// (needs a trigger Collider), or set autoPickup to collect on contact.
    ///
    /// Inspector setup:
    ///   inventory     → the player's InventorySO asset
    ///   item / count  → what this pickup holds (loot spawned by LootDropper
    ///                   sets these through Configure)
    ///   visualAnchor  → optional parent for the item's WorldPrefab model
    ///   persistentId  → OPTIONAL. Non-empty = the pickup stays gone after a
    ///                   save/load (hand-placed one-offs like the Prologue scroll).
    ///                   Leave empty for loot. Must be unique per scene object.
    ///   questChannel + questEventId → OPTIONAL. Raised after a successful
    ///                   pickup, so a quest objective can advance exactly like
    ///                   it did with QuestItemPickup (reuse the same EventId).
    ///   onCollected   → runs after storing (e.g. QuestGrantPickup.Grant)
    ///   onInventoryFull → hook a "backpack full" toast
    ///
    /// If the backpack only has room for part of the stack, the rest stays in
    /// the world.
    /// </summary>
    public class ItemPickup : InteractableZone
    {
        [Header("Item")]
        [SerializeField] private InventorySO inventory;
        [SerializeField] private ItemDefinitionSO item;
        [SerializeField, Min(1)] private int count = 1;
        [SerializeField] private Transform visualAnchor;

        [Header("Behaviour")]
        [SerializeField] private bool autoPickup;
        [SerializeField] private string persistentId;

        [Header("Quest Hook (optional)")]
        [SerializeField] private GameChannel questChannel;
        [SerializeField, EventIdField] private string questEventId;

        [Header("Events")]
        [SerializeField] private UnityEvent onCollected;
        [SerializeField] private UnityEvent onInventoryFull;

        private bool collected;
        private GameObject spawnedVisual;

        private string SaveKey => $"pickup_{persistentId}";

        private void OnValidate()
        {
            if (inventory == null)
                Debug.LogError($"[ItemPickup] Inventory is not assigned on {name}!", this);
            if (item == null)
                Debug.LogWarning($"[ItemPickup] Item is not assigned on {name} (fine for a prefab configured at runtime).", this);
            if (count < 1)
                count = 1;
        }

        private void Start()
        {
            if (!string.IsNullOrEmpty(persistentId) && SaveSystem.RequestLoad(SaveKey) == "1")
            {
                collected = true;
                gameObject.SetActive(false);
                return;
            }

            RefreshVisual();
        }

        /// <summary>Sets what this pickup holds. Called by LootDropper right after Instantiate.</summary>
        public void Configure(ItemDefinitionSO newItem, int newCount)
        {
            item = newItem;
            count = Mathf.Max(1, newCount);
            RefreshVisual();
        }

        public override bool CanInteract(GameObject interactor) => !collected && item != null && inventory != null;

        public override string InteractionPrompt
        {
            get
            {
                if (item == null) return "Pick up";
                return count > 1 ? $"Pick up {item.DisplayName} x{count}" : $"Pick up {item.DisplayName}";
            }
        }

        protected override void OnRangeEnter(GameObject other)
        {
            base.OnRangeEnter(other);

            if (autoPickup && other.CompareTag("Player"))
                Interact(other);
        }

        public override void Interact(GameObject interactor)
        {
            if (collected) return;
            if (!interactor.CompareTag("Player")) return;

            if (item == null || inventory == null)
            {
                Debug.LogWarning($"[ItemPickup] {name} has no item or inventory assigned.", this);
                return;
            }

            int remainder = inventory.Add(item, count);

            if (remainder >= count)
            {
                onInventoryFull?.Invoke();      // nothing fit — stays in the world
                return;
            }

            if (remainder > 0)
            {
                count = remainder;              // partial fit — the rest stays in the world
                onInventoryFull?.Invoke();
                return;
            }

            collected = true;

            if (!string.IsNullOrEmpty(persistentId))
            {
                SaveSystem.Set(SaveKey, "1");
                SaveSystem.Flush();
            }

            if (questChannel != null && !string.IsNullOrEmpty(questEventId))
                questChannel.Raise(questEventId);

            onCollected?.Invoke();
            gameObject.SetActive(false);
        }

        private void RefreshVisual()
        {
            if (spawnedVisual != null)
            {
                Destroy(spawnedVisual);
                spawnedVisual = null;
            }

            if (item == null || item.WorldPrefab == null) return;

            Transform parent = visualAnchor != null ? visualAnchor : transform;
            spawnedVisual = Instantiate(item.WorldPrefab, parent);
        }
    }
}
