namespace SOAP
{
    /// <summary>
    /// Add to every backpack slot object. Slot order = hierarchy order under
    /// the panel controller's inventorySlotsParent (child 0 is inventory index 0).
    /// </summary>
    public class InventorySlotUI : ItemSlotUI
    {
        public override SlotKind Kind => SlotKind.Inventory;
    }
}
