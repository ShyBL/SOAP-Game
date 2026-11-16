using UnityEngine;
using UnityEngine.Events;

namespace SOAP
{
    /// <summary>
    /// Gives items straight to the backpack — quest rewards, chest contents,
    /// vendor-free gear drops. Wire RewardDispenser.OnRewardsGiven (or any
    /// UnityEvent) to Grant(), the way RewardDispenser already documents for
    /// non-numeric rewards.
    ///
    /// Rewards must never vanish: anything that does not fit is spawned at
    /// overflowPoint as an ItemPickup so the player can collect it later.
    ///
    /// Inspector setup:
    ///   inventory            → the player's InventorySO
    ///   grants               → item + count pairs
    ///   overflowPickupPrefab → prefab with an ItemPickup (persistentId EMPTY)
    ///   overflowPoint        → where overflow lands (e.g. the NPC or the player)
    /// </summary>
    public class ItemGranter : MonoBehaviour
    {
        [SerializeField] private InventorySO inventory;
        [SerializeField] private ItemStack[] grants;

        [Header("Overflow (backpack full)")]
        [SerializeField] private ItemPickup overflowPickupPrefab;
        [SerializeField] private Transform overflowPoint;

        [Header("Events")]
        [SerializeField] private UnityEvent onGranted;
        [SerializeField] private UnityEvent onOverflow;

        private void OnValidate()
        {
            if (inventory == null)
                Debug.LogError($"[ItemGranter] Inventory is not assigned on {name}!", this);

            if (grants == null) return;

            for (int i = 0; i < grants.Length; i++)
                if (grants[i].Item != null && grants[i].Count < 1)
                    grants[i].Count = 1;
        }

        public void Grant()
        {
            if (inventory == null || grants == null) return;

            Vector3 overflowPosition = overflowPoint != null ? overflowPoint.position : transform.position;
            bool overflowed = ItemGrantUtility.Grant(inventory, grants, overflowPickupPrefab, overflowPosition, this);

            if (overflowed)
                onOverflow?.Invoke();

            onGranted?.Invoke();
        }
    }
}
