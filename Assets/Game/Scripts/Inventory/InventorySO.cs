using System;
using System.Text;
using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Runtime backpack: a fixed array of single-stack slots (not grid-space
    /// based). Referenced by asset from pickups, loot, rewards, and UI — no
    /// singleton lookups.
    ///
    /// Mutations raise Changed (UI refresh) and save through SaveSystem.
    /// Multi-step edits (InventoryOperations) write slots silently with
    /// SetSlot, then call NotifyChanged once.
    ///
    /// Like SavedIntVariable, state loads in OnEnable during play. Reset on
    /// every enable so Fast Play Mode (no domain reload) starts clean.
    ///
    /// Inspector setup:
    ///   SlotCount     → must match the number of InventorySlotUI in the scene
    ///   Database      → ItemDatabaseSO (required to restore a save)
    ///   SaveKey       → SaveSystem key, empty = not persisted
    ///   StartingItems → given only when no save exists (new game / testing)
    /// </summary>
    [CreateAssetMenu(fileName = "New_Inventory", menuName = "SOAP/Items/Inventory")]
    public class InventorySO : ScriptableObject
    {
        private const string SaveHeader = "inv1#";

        [Header("Layout")]
        [Min(1)]
        public int SlotCount = 24;

        [Header("Persistence")]
        public ItemDatabaseSO Database;
        public string SaveKey = "player_inventory";

        [Header("New Game")]
        [Tooltip("Added only when no save exists yet. Handy for testing gear.")]
        public ItemStack[] StartingItems;

        [NonSerialized] private ItemStack[] slots;

        /// <summary>Raised after any change. UI subscribes in OnEnable, unsubscribes in OnDisable.</summary>
        public event Action Changed;

        private void OnEnable()
        {
            slots = new ItemStack[Mathf.Max(1, SlotCount)];

            if (Application.isPlaying)
                Load();
        }

        private void OnValidate()
        {
            if (SlotCount < 1)
                SlotCount = 1;

            if (!string.IsNullOrEmpty(SaveKey) && Database == null)
                Debug.LogWarning($"[InventorySO] '{name}': SaveKey is set but Database is not assigned — saves cannot be restored.", this);
        }

        // ── Reads ─────────────────────────────────────────────────────────────

        public bool IsValidIndex(int index)
        {
            EnsureSlots();
            return index >= 0 && index < slots.Length;
        }

        public ItemStack GetStack(int index)
        {
            return IsValidIndex(index) ? slots[index] : default;
        }

        /// <summary>First empty slot index, or -1 if the backpack is full.</summary>
        public int FindEmptySlot()
        {
            EnsureSlots();

            for (int i = 0; i < slots.Length; i++)
                if (slots[i].IsEmpty) return i;

            return -1;
        }

        public int CountEmptySlots()
        {
            EnsureSlots();

            int empty = 0;
            for (int i = 0; i < slots.Length; i++)
                if (slots[i].IsEmpty) empty++;

            return empty;
        }

        public int CountOf(ItemDefinitionSO item)
        {
            if (item == null) return 0;
            EnsureSlots();

            int total = 0;
            for (int i = 0; i < slots.Length; i++)
                if (!slots[i].IsEmpty && slots[i].Item == item)
                    total += slots[i].Count;

            return total;
        }

        public bool Contains(ItemDefinitionSO item, int count = 1) => CountOf(item) >= count;

        // ── Mutations ─────────────────────────────────────────────────────────

        /// <summary>
        /// Adds as many as fit, topping up existing stacks first. Returns the
        /// REMAINDER that did not fit (0 = everything was added).
        /// </summary>
        public int Add(ItemDefinitionSO item, int count)
        {
            int remainder = AddInternal(item, count);

            if (remainder != count)
                NotifyChanged();

            return remainder;
        }

        /// <summary>Removes up to count of item. Returns how many were actually removed.</summary>
        public int Remove(ItemDefinitionSO item, int count)
        {
            if (item == null || count <= 0) return 0;
            EnsureSlots();

            int remaining = count;

            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                if (slots[i].IsEmpty || slots[i].Item != item) continue;

                int taken = Mathf.Min(slots[i].Count, remaining);
                slots[i].Count -= taken;
                remaining -= taken;

                if (slots[i].Count <= 0)
                    slots[i] = default;
            }

            int removed = count - remaining;
            if (removed > 0)
                NotifyChanged();

            return removed;
        }

        /// <summary>
        /// Overwrites one slot WITHOUT raising Changed or saving. Callers doing
        /// several writes must finish with NotifyChanged().
        /// </summary>
        public void SetSlot(int index, ItemStack stack)
        {
            if (!IsValidIndex(index)) return;
            slots[index] = stack.IsEmpty ? default : stack;
        }

        public void NotifyChanged()
        {
            Changed?.Invoke();
            Save();
        }

        public void Clear()
        {
            EnsureSlots();
            Array.Clear(slots, 0, slots.Length);
            NotifyChanged();
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
            Array.Clear(slots, 0, slots.Length);

            string data = string.IsNullOrEmpty(SaveKey) ? null : SaveSystem.RequestLoad(SaveKey);

            if (string.IsNullOrEmpty(data))
                ApplyStartingItems();
            else
                Deserialize(data);

            Changed?.Invoke();
        }

        private void ApplyStartingItems()
        {
            if (StartingItems == null) return;

            for (int i = 0; i < StartingItems.Length; i++)
                AddInternal(StartingItems[i].Item, StartingItems[i].Count);
        }

        private string Serialize()
        {
            // Header guarantees the string is never empty, so "saved but empty
            // backpack" is distinguishable from "no save yet".
            var builder = new StringBuilder(SaveHeader);
            bool first = true;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].IsEmpty) continue;

                if (!first) builder.Append('|');
                first = false;

                builder.Append(i).Append(':').Append(slots[i].Item.ItemId).Append(':').Append(slots[i].Count);
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
                if (parts.Length != 3) continue;
                if (!int.TryParse(parts[0], out int index) || !IsValidIndex(index)) continue;
                if (!int.TryParse(parts[2], out int count) || count <= 0) continue;

                ItemDefinitionSO item = Database != null ? Database.FindById(parts[1]) : null;
                if (item == null)
                {
                    Debug.LogWarning($"[InventorySO] '{name}': unknown item id '{parts[1]}' in save — skipped.", this);
                    continue;
                }

                slots[index] = new ItemStack(item, Mathf.Min(count, item.MaxStack));
            }
        }

        // ── Internals ─────────────────────────────────────────────────────────

        private int AddInternal(ItemDefinitionSO item, int count)
        {
            if (item == null || count <= 0) return count;
            EnsureSlots();

            int remaining = count;

            if (item.IsStackable)
            {
                for (int i = 0; i < slots.Length && remaining > 0; i++)
                {
                    if (slots[i].IsEmpty || slots[i].Item != item) continue;

                    int space = item.MaxStack - slots[i].Count;
                    if (space <= 0) continue;

                    int moved = Mathf.Min(space, remaining);
                    slots[i].Count += moved;
                    remaining -= moved;
                }
            }

            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                if (!slots[i].IsEmpty) continue;

                int moved = Mathf.Min(item.MaxStack, remaining);
                slots[i] = new ItemStack(item, moved);
                remaining -= moved;
            }

            return remaining;
        }

        private void EnsureSlots()
        {
            int wanted = Mathf.Max(1, SlotCount);

            if (slots == null)
                slots = new ItemStack[wanted];
            else if (slots.Length != wanted)
                Array.Resize(ref slots, wanted);
        }
    }
}
