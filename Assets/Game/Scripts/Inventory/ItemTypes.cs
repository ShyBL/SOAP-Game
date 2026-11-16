using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Which gear slot an equipment item fits. EquipmentSO.Slots may list the
    /// same type more than once (e.g. two Rings).
    ///
    /// Append new values at the END only — serialized assets store the
    /// underlying integer, so reordering silently re-slots existing items.
    /// </summary>
    public enum EquipSlotType
    {
        Head,
        Amulet,
        Chest,
        Hands,
        Belt,
        Feet,
        Ring,
        MainHand,
        OffHand
    }

    /// <summary>
    /// A generic stat bonus carried by equipment. Systems that care about a
    /// stat (damage reduction, max health, ...) ask EquipmentSO.GetStatTotal
    /// with the id they know — nothing here knows what any stat means.
    /// </summary>
    [System.Serializable]
    public struct StatModifier
    {
        [Tooltip("Free-form stat key, e.g. 'armor' or 'max_health'.")]
        public string StatId;

        public float Amount;
    }

    /// <summary>
    /// One inventory slot's contents. Also used as a serialized "item x count"
    /// pair in inspectors (ItemGranter, InventorySO.StartingItems).
    /// </summary>
    [System.Serializable]
    public struct ItemStack
    {
        public ItemDefinitionSO Item;
        public int Count;

        public ItemStack(ItemDefinitionSO item, int count)
        {
            Item = item;
            Count = count;
        }

        public bool IsEmpty => Item == null || Count <= 0;
    }
}
