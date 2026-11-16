using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Single implementation of "give the player these items". Used by
    /// ItemGranter (scene-wired grants) and QuestRewardGranter (quest asset
    /// rewards) so overflow behaves identically everywhere.
    ///
    /// Rewards must never vanish: anything that does not fit in the backpack
    /// is spawned as an ItemPickup at overflowPosition.
    /// </summary>
    public static class ItemGrantUtility
    {
        /// <summary>Returns true if any stack did not fully fit in the backpack.</summary>
        public static bool Grant(InventorySO inventory, ItemStack[] stacks, ItemPickup overflowPrefab,
            Vector3 overflowPosition, Object logContext)
        {
            if (inventory == null || stacks == null) return false;

            bool overflowed = false;

            for (int i = 0; i < stacks.Length; i++)
            {
                ItemStack stack = stacks[i];
                if (stack.IsEmpty) continue;

                int remainder = inventory.Add(stack.Item, stack.Count);
                if (remainder <= 0) continue;

                overflowed = true;

                if (overflowPrefab == null)
                {
                    Debug.LogWarning($"[ItemGrantUtility] Backpack full and no overflow pickup prefab — " +
                                     $"{remainder}x {stack.Item.DisplayName} was lost.", logContext);
                    continue;
                }

                ItemPickup pickup = Object.Instantiate(overflowPrefab, overflowPosition, Quaternion.identity);
                pickup.Configure(stack.Item, remainder);
            }

            return overflowed;
        }
    }
}
