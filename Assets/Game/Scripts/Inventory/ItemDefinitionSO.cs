using System.Collections.Generic;
using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Abstract base for all item assets. One asset per item type.
    ///
    /// Concrete subtypes own only the fields they need — no discriminator
    /// enum, no dead fields:
    ///
    ///   MiscItemSO     — stackables, quest items, scrolls, materials
    ///   ArmorItemSO    — wearable, contributes an armor value
    ///   WeaponItemSO   — wearable, wraps a WeaponDataSO for combat
    ///
    /// To add a new item type: create one class that extends this (or
    /// EquipmentItemSO). Inventory, equipment, pickups, and UI are untouched.
    /// </summary>
    public abstract class ItemDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable save identity. Auto-filled with a GUID if left empty — safe to leave alone. " +
                 "Must not contain ':' or '|'. A duplicated asset copies this value: clear it on the copy.")]
        public string ItemId;

        public string DisplayName;

        [TextArea(2, 5)]
        public string Description;

        public Sprite Icon;

        [Header("Stacking")]
        [Tooltip("1 = unstackable. Equipment is always forced to 1.")]
        [Min(1)]
        public int MaxStack = 1;

        [Header("World")]
        [Tooltip("Optional model shown by ItemPickup while this item lies in the world.")]
        public GameObject WorldPrefab;

        public bool IsStackable => MaxStack > 1;

        /// <summary>
        /// Appends tooltip body lines (stats, slot, flavor) for this item.
        /// Each subtype adds its own — the tooltip UI never inspects types.
        /// </summary>
        public virtual void AppendTooltipLines(List<string> lines) { }

        protected virtual void OnValidate()
        {
            if (string.IsNullOrEmpty(ItemId))
            {
                ItemId = System.Guid.NewGuid().ToString("N");
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }

            if (MaxStack < 1)
                MaxStack = 1;

            if (ItemId.IndexOfAny(new[] { ':', '|' }) >= 0)
                Debug.LogWarning($"[{GetType().Name}] '{name}': ItemId contains ':' or '|', which breaks saving.", this);

            if (string.IsNullOrEmpty(DisplayName))
                Debug.LogWarning($"[{GetType().Name}] '{name}': DisplayName is empty.", this);
        }
    }
}
