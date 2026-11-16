using System;
using System.Collections.Generic;
using System.Text;
using StarterAssets;
using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// The player's worn gear. One entry in Slots per gear slot in your UI, in
    /// the same order as the EquipmentSlotUI children — duplicates are fine
    /// (two Rings).
    ///
    /// Other systems read this instead of knowing about the UI:
    ///   GetStatTotal("armor")  → damage reduction, max health bonuses, ...
    ///   EquippedWeapon         → the main-hand WeaponDataSO (combat)
    ///
    /// Like InventorySO: silent SetSlot for multi-step edits, then one
    /// NotifyChanged; persists through SaveSystem; loads in OnEnable during play.
    /// </summary>
    [CreateAssetMenu(fileName = "New_Equipment", menuName = "SOAP/Items/Equipment")]
    public class EquipmentSO : ScriptableObject
    {
        private const string SaveHeader = "eq1#";

        [Header("Layout")]
        [Tooltip("One entry per gear slot, in UI order. Do not reorder after saving — saves store slot indices.")]
        public EquipSlotType[] Slots =
        {
            EquipSlotType.Head, EquipSlotType.Amulet, EquipSlotType.Chest, EquipSlotType.Hands,
            EquipSlotType.Belt, EquipSlotType.Feet, EquipSlotType.Ring, EquipSlotType.Ring,
            EquipSlotType.MainHand, EquipSlotType.OffHand
        };

        [Header("Persistence")]
        public ItemDatabaseSO Database;
        public string SaveKey = "player_equipment";

        [Header("New Game")]
        [Tooltip("Equipped only when no save exists yet. Each goes in the first fitting slot.")]
        public EquipmentItemSO[] StartingEquipment;

        [NonSerialized] private EquipmentItemSO[] equipped;
        [NonSerialized] private readonly List<StatModifier> scratch = new List<StatModifier>();

        public event Action Changed;

        public int SlotCount => Slots != null ? Slots.Length : 0;

        private void OnEnable()
        {
            equipped = new EquipmentItemSO[SlotCount];

            if (Application.isPlaying)
                Load();
        }

        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(SaveKey) && Database == null)
                Debug.LogWarning($"[EquipmentSO] '{name}': SaveKey is set but Database is not assigned — saves cannot be restored.", this);
        }

        // ── Reads ─────────────────────────────────────────────────────────────

        public bool IsValidIndex(int index) => index >= 0 && index < SlotCount;

        public EquipSlotType GetSlotType(int index) => IsValidIndex(index) ? Slots[index] : default;

        public EquipmentItemSO GetEquipped(int index)
        {
            EnsureSlots();
            return IsValidIndex(index) ? equipped[index] : null;
        }

        /// <summary>The main-hand weapon's combat data, or null if none is held.</summary>
        public WeaponDataSO EquippedWeapon
        {
            get
            {
                EnsureSlots();

                for (int i = 0; i < equipped.Length; i++)
                {
                    if (Slots[i] != EquipSlotType.MainHand) continue;

                    if (equipped[i] is WeaponItemSO weaponItem && weaponItem.Weapon != null)
                        return weaponItem.Weapon;
                }

                return null;
            }
        }

        /// <summary>Sum of every equipped item's modifiers for statId.</summary>
        public float GetStatTotal(string statId)
        {
            EnsureSlots();
            scratch.Clear();

            for (int i = 0; i < equipped.Length; i++)
                if (equipped[i] != null)
                    equipped[i].CollectModifiers(scratch);

            float total = 0f;
            for (int i = 0; i < scratch.Count; i++)
                if (scratch[i].StatId == statId)
                    total += scratch[i].Amount;

            return total;
        }

        // ── Rules ─────────────────────────────────────────────────────────────

        /// <summary>Slot type matches. Does not consider two-handed blocking.</summary>
        public bool SlotAccepts(int index, ItemDefinitionSO item)
        {
            if (!IsValidIndex(index)) return false;
            return item is EquipmentItemSO gear && gear.Slot == Slots[index];
        }

        /// <summary>Slot type matches AND the off-hand is not blocked by a two-handed main hand.</summary>
        public bool CanEquip(int index, ItemDefinitionSO item)
        {
            if (!SlotAccepts(index, item)) return false;
            if (Slots[index] == EquipSlotType.OffHand && MainHandBlocksOffHand()) return false;
            return true;
        }

        /// <summary>
        /// Best slot for quick-equip: an empty fitting slot if any, otherwise the
        /// first fitting slot (its current item gets swapped out). -1 if none fit.
        /// </summary>
        public int FindSlotFor(ItemDefinitionSO item)
        {
            EnsureSlots();
            int firstFitting = -1;

            for (int i = 0; i < equipped.Length; i++)
            {
                if (!CanEquip(i, item)) continue;
                if (equipped[i] == null) return i;
                if (firstFitting < 0) firstFitting = i;
            }

            return firstFitting;
        }

        private bool MainHandBlocksOffHand()
        {
            for (int i = 0; i < equipped.Length; i++)
                if (Slots[i] == EquipSlotType.MainHand && equipped[i] != null && equipped[i].OccupiesOffHand)
                    return true;

            return false;
        }

        // ── Mutations ─────────────────────────────────────────────────────────

        /// <summary>Overwrites one slot WITHOUT validating, raising Changed, or saving.</summary>
        public void SetSlot(int index, EquipmentItemSO item)
        {
            EnsureSlots();
            if (!IsValidIndex(index)) return;
            equipped[index] = item;
        }

        public void NotifyChanged()
        {
            Changed?.Invoke();
            Save();
        }

        // ── Persistence ───────────────────────────────────────────────────────

        public void Save()
        {
            if (!Application.isPlaying || string.IsNullOrEmpty(SaveKey)) return;

            SaveSystem.Set(SaveKey, Serialize());
            SaveSystem.Flush();
        }

        public void Load()
        {
            EnsureSlots();
            Array.Clear(equipped, 0, equipped.Length);

            string data = string.IsNullOrEmpty(SaveKey) ? null : SaveSystem.RequestLoad(SaveKey);

            if (string.IsNullOrEmpty(data))
                ApplyStartingEquipment();
            else
                Deserialize(data);

            Changed?.Invoke();
        }

        private void ApplyStartingEquipment()
        {
            if (StartingEquipment == null) return;

            for (int i = 0; i < StartingEquipment.Length; i++)
            {
                EquipmentItemSO gear = StartingEquipment[i];
                if (gear == null) continue;

                int slot = FindSlotFor(gear);
                if (slot >= 0 && equipped[slot] == null)
                    equipped[slot] = gear;
            }
        }

        private string Serialize()
        {
            var builder = new StringBuilder(SaveHeader);
            bool first = true;

            for (int i = 0; i < equipped.Length; i++)
            {
                if (equipped[i] == null) continue;

                if (!first) builder.Append('|');
                first = false;

                builder.Append(i).Append(':').Append(equipped[i].ItemId);
            }

            return builder.ToString();
        }

        private void Deserialize(string data)
        {
            int headerEnd = data.IndexOf('#');
            string body = headerEnd >= 0 ? data.Substring(headerEnd + 1) : data;
            if (string.IsNullOrEmpty(body)) return;

            foreach (string entry in body.Split('|'))
            {
                string[] parts = entry.Split(':');
                if (parts.Length != 2) continue;
                if (!int.TryParse(parts[0], out int index) || !IsValidIndex(index)) continue;

                ItemDefinitionSO item = Database != null ? Database.FindById(parts[1]) : null;
                if (!(item is EquipmentItemSO gear))
                {
                    Debug.LogWarning($"[EquipmentSO] '{name}': unknown or non-equipment id '{parts[1]}' in save — skipped.", this);
                    continue;
                }

                equipped[index] = gear;
            }
        }

        private void EnsureSlots()
        {
            int wanted = SlotCount;

            if (equipped == null)
                equipped = new EquipmentItemSO[wanted];
            else if (equipped.Length != wanted)
                Array.Resize(ref equipped, wanted);
        }
    }
}
