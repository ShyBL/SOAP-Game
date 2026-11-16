using UnityEngine;

namespace SOAP
{
    public enum SlotKind
    {
        Inventory,
        Equipment
    }

    /// <summary>Addresses one slot in either the backpack or the gear set.</summary>
    public readonly struct ItemLocation
    {
        public readonly SlotKind Kind;
        public readonly int Index;

        public ItemLocation(SlotKind kind, int index)
        {
            Kind = kind;
            Index = index;
        }
    }

    /// <summary>
    /// Every rule for moving items between and within InventorySO and
    /// EquipmentSO. UI controllers call these and nothing else, so drag-drop,
    /// right-click, and any future gamepad/hotkey path behave identically.
    ///
    /// All-or-nothing: a move either fully succeeds (and notifies once per
    /// touched container) or changes nothing and returns false.
    ///
    /// Rules:
    ///   - Same stackable item inventory-to-inventory merges up to MaxStack.
    ///   - Otherwise the two slots swap, but each side must accept what it receives
    ///     (a gear slot only takes matching equipment; swapping back must be legal too).
    ///   - Equipping a two-handed weapon in the main hand sends any off-hand
    ///     item to the backpack; if there is no room the move is refused.
    /// </summary>
    public static class InventoryOperations
    {
        public static ItemStack Read(InventorySO inventory, EquipmentSO equipment, ItemLocation location)
        {
            if (location.Kind == SlotKind.Inventory)
                return inventory.GetStack(location.Index);

            EquipmentItemSO gear = equipment.GetEquipped(location.Index);
            return gear != null ? new ItemStack(gear, 1) : default;
        }

        public static bool TryMove(InventorySO inventory, EquipmentSO equipment, ItemLocation from, ItemLocation to)
        {
            if (inventory == null || equipment == null) return false;
            if (from.Kind == to.Kind && from.Index == to.Index) return false;
            if (!IsValid(inventory, equipment, from) || !IsValid(inventory, equipment, to)) return false;

            ItemStack source = Read(inventory, equipment, from);
            if (source.IsEmpty) return false;

            ItemStack target = Read(inventory, equipment, to);

            // 1. Merge same stackable items inside the backpack.
            if (from.Kind == SlotKind.Inventory && to.Kind == SlotKind.Inventory
                && !target.IsEmpty && source.Item == target.Item && source.Item.IsStackable)
            {
                int space = target.Item.MaxStack - target.Count;

                if (space > 0)
                {
                    int moved = Mathf.Min(space, source.Count);
                    target.Count += moved;
                    source.Count -= moved;

                    inventory.SetSlot(to.Index, target);
                    inventory.SetSlot(from.Index, source.Count > 0 ? source : default);
                    inventory.NotifyChanged();
                    return true;
                }
                // Target stack is full — fall through and swap instead.
            }

            // 2. Both sides must accept what they would receive.
            if (!Accepts(equipment, to, source.Item)) return false;
            if (!target.IsEmpty && !Accepts(equipment, from, target.Item)) return false;

            // 3. Two-handed: check there is room for the evicted off-hand BEFORE changing anything.
            int evictions = CountOffHandEvictions(equipment, to, source.Item);
            if (evictions > 0)
            {
                int freeSlots = inventory.CountEmptySlots();

                // The source slot is vacated when nothing swaps back into it.
                if (from.Kind == SlotKind.Inventory && target.IsEmpty)
                    freeSlots++;

                if (evictions > freeSlots) return false;
            }

            // 4. Commit.
            Write(inventory, equipment, to, source);
            Write(inventory, equipment, from, target);

            bool evicted = evictions > 0;
            if (evicted)
                EvictOffHand(inventory, equipment, evictions);

            bool touchesInventory = from.Kind == SlotKind.Inventory || to.Kind == SlotKind.Inventory || evicted;
            bool touchesEquipment = from.Kind == SlotKind.Equipment || to.Kind == SlotKind.Equipment || evicted;

            if (touchesInventory) inventory.NotifyChanged();
            if (touchesEquipment) equipment.NotifyChanged();

            return true;
        }

        /// <summary>
        /// Right-click / double-click behaviour. A backpack item goes to its
        /// best gear slot; a worn item goes to the first free backpack slot.
        /// </summary>
        public static bool TryQuickMove(InventorySO inventory, EquipmentSO equipment, ItemLocation from)
        {
            if (inventory == null || equipment == null) return false;

            ItemStack stack = Read(inventory, equipment, from);
            if (stack.IsEmpty) return false;

            if (from.Kind == SlotKind.Equipment)
            {
                int free = inventory.FindEmptySlot();
                return free >= 0 && TryMove(inventory, equipment, from, new ItemLocation(SlotKind.Inventory, free));
            }

            int gearSlot = equipment.FindSlotFor(stack.Item);
            return gearSlot >= 0 && TryMove(inventory, equipment, from, new ItemLocation(SlotKind.Equipment, gearSlot));
        }

        // ── Internals ─────────────────────────────────────────────────────────

        private static bool IsValid(InventorySO inventory, EquipmentSO equipment, ItemLocation location)
        {
            return location.Kind == SlotKind.Inventory
                ? inventory.IsValidIndex(location.Index)
                : equipment.IsValidIndex(location.Index);
        }

        private static bool Accepts(EquipmentSO equipment, ItemLocation location, ItemDefinitionSO item)
        {
            if (location.Kind == SlotKind.Inventory) return true;
            return equipment.CanEquip(location.Index, item);
        }

        private static void Write(InventorySO inventory, EquipmentSO equipment, ItemLocation location, ItemStack stack)
        {
            if (location.Kind == SlotKind.Inventory)
            {
                inventory.SetSlot(location.Index, stack);
                return;
            }

            equipment.SetSlot(location.Index, stack.IsEmpty ? null : stack.Item as EquipmentItemSO);
        }

        private static int CountOffHandEvictions(EquipmentSO equipment, ItemLocation to, ItemDefinitionSO incoming)
        {
            if (to.Kind != SlotKind.Equipment) return 0;
            if (equipment.GetSlotType(to.Index) != EquipSlotType.MainHand) return 0;

            EquipmentItemSO gear = incoming as EquipmentItemSO;
            if (gear == null || !gear.OccupiesOffHand) return 0;

            int count = 0;
            for (int i = 0; i < equipment.SlotCount; i++)
                if (equipment.GetSlotType(i) == EquipSlotType.OffHand && equipment.GetEquipped(i) != null)
                    count++;

            return count;
        }

        private static void EvictOffHand(InventorySO inventory, EquipmentSO equipment, int evictions)
        {
            for (int i = 0; i < equipment.SlotCount && evictions > 0; i++)
            {
                if (equipment.GetSlotType(i) != EquipSlotType.OffHand) continue;

                EquipmentItemSO offHand = equipment.GetEquipped(i);
                if (offHand == null) continue;

                int free = inventory.FindEmptySlot();   // capacity was verified before committing
                inventory.SetSlot(free, new ItemStack(offHand, 1));
                equipment.SetSlot(i, null);
                evictions--;
            }
        }
    }
}
