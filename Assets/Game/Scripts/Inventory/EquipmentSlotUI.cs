using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Add to every gear slot object (helm, amulet, chest, ...). Slot order =
    /// hierarchy order under the panel controller's equipmentSlotsParent, and
    /// must match EquipmentSO.Slots index for index.
    ///
    /// slotType is declared here only so the panel can warn you when the UI
    /// order and EquipmentSO.Slots disagree — the data side stays authoritative.
    /// Assign emptySprite (on the base class) to show a silhouette while empty.
    /// </summary>
    public class EquipmentSlotUI : ItemSlotUI
    {
        [SerializeField] private EquipSlotType slotType;

        public EquipSlotType SlotType => slotType;

        public override SlotKind Kind => SlotKind.Equipment;
    }
}
