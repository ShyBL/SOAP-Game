using System.Collections.Generic;
using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Abstract base for anything wearable. Owns the gear slot it fits and a
    /// generic list of stat bonuses. Subtypes contribute their own primary
    /// stat by overriding CollectModifiers / AppendPrimaryStatLines.
    /// </summary>
    public abstract class EquipmentItemSO : ItemDefinitionSO
    {
        [Header("Equipment")]
        public EquipSlotType Slot;

        [Tooltip("Generic bonuses, e.g. StatId 'max_health'. Summed by EquipmentSO.GetStatTotal.")]
        public StatModifier[] Modifiers;

        /// <summary>
        /// True if holding this item blocks the off-hand slot (two-handed
        /// weapons). EquipmentSO and InventoryOperations read this — they never
        /// check for a specific weapon type.
        /// </summary>
        public virtual bool OccupiesOffHand => false;

        /// <summary>Adds every stat bonus this item grants while equipped.</summary>
        public virtual void CollectModifiers(List<StatModifier> into)
        {
            if (Modifiers == null) return;

            for (int i = 0; i < Modifiers.Length; i++)
                into.Add(Modifiers[i]);
        }

        public override void AppendTooltipLines(List<string> lines)
        {
            lines.Add(Slot.ToString());
            AppendPrimaryStatLines(lines);
            AppendModifierLines(lines);
        }

        /// <summary>Subtype hook for the headline stat (armor value, damage).</summary>
        protected virtual void AppendPrimaryStatLines(List<string> lines) { }

        private void AppendModifierLines(List<string> lines)
        {
            if (Modifiers == null) return;

            for (int i = 0; i < Modifiers.Length; i++)
            {
                StatModifier modifier = Modifiers[i];
                string sign = modifier.Amount >= 0f ? "+" : string.Empty;
                lines.Add($"{sign}{modifier.Amount} {modifier.StatId}");
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            MaxStack = 1;
        }
    }
}
