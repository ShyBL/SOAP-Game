using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SOAP
{
    /// <summary>
    /// Drives the Diablo-style inventory screen you already built in uGUI.
    /// It binds your existing slot objects to InventorySO / EquipmentSO,
    /// refreshes them on Changed events, opens/closes the panel from an input
    /// action, and turns pointer events from the slots into
    /// InventoryOperations calls. All rules live in InventoryOperations.
    ///
    /// Inspector setup:
    ///   inventory / equipment  → the player's two assets
    ///   panelRoot              → the visual panel. MUST be a CHILD of the object
    ///                            holding this script — a script on a hidden
    ///                            object stops listening for the toggle key
    ///   toggleAction           → InputActionReference (e.g. the "Inventory" action)
    ///   inventorySlotsParent   → parent of every InventorySlotUI (child order = index)
    ///   equipmentSlotsParent   → parent of every EquipmentSlotUI (child order = slot index)
    ///   dragGhost              → an Image at the end of the canvas hierarchy (draws on top)
    ///   tooltip                → optional ItemTooltipUI
    ///   onOpened / onClosed    → wire to pausing, look-input locking, sound
    /// </summary>
    public class InventoryPanelController : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private InventorySO inventory;
        [SerializeField] private EquipmentSO equipment;

        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private InputActionReference toggleAction;
        [SerializeField] private bool startOpen;
        [Tooltip("Unlock and show the cursor while open; restore the previous state on close.")]
        [SerializeField] private bool manageCursor = true;

        [Header("Slots")]
        [SerializeField] private Transform inventorySlotsParent;
        [SerializeField] private Transform equipmentSlotsParent;

        [Header("Drag & Tooltip")]
        [SerializeField] private Image dragGhost;
        [SerializeField] private ItemTooltipUI tooltip;

        [Header("Events")]
        [SerializeField] private UnityEvent onOpened;
        [SerializeField] private UnityEvent onClosed;

        private InventorySlotUI[] inventorySlots = new InventorySlotUI[0];
        private EquipmentSlotUI[] equipmentSlots = new EquipmentSlotUI[0];
        private ItemSlotUI dragSource;

        private bool cursorCaptured;
        private CursorLockMode previousLockMode;
        private bool previousCursorVisible;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        private void OnValidate()
        {
            if (inventory == null)
                Debug.LogError($"[InventoryPanelController] Inventory is not assigned on {name}!", this);
            if (equipment == null)
                Debug.LogError($"[InventoryPanelController] Equipment is not assigned on {name}!", this);
            if (panelRoot == null)
                Debug.LogError($"[InventoryPanelController] PanelRoot is not assigned on {name}!", this);
            if (inventorySlotsParent == null)
                Debug.LogError($"[InventoryPanelController] InventorySlotsParent is not assigned on {name}!", this);
            if (equipmentSlotsParent == null)
                Debug.LogError($"[InventoryPanelController] EquipmentSlotsParent is not assigned on {name}!", this);
        }

        private void Awake()
        {
            CollectSlots();

            if (dragGhost != null)
            {
                dragGhost.raycastTarget = false;
                dragGhost.enabled = false;
            }

            if (tooltip != null)
                tooltip.Hide();
        }

        private void OnEnable()
        {
            if (inventory == null || equipment == null || panelRoot == null) return;

            inventory.Changed += HandleDataChanged;
            equipment.Changed += HandleDataChanged;

            if (toggleAction != null)
            {
                toggleAction.action.Enable();
                toggleAction.action.performed += OnTogglePerformed;
            }

            RefreshAll();

            panelRoot.SetActive(false);
            cursorCaptured = false;

            if (startOpen)
                SetOpen(true);
        }

        private void OnDisable()
        {
            if (inventory != null) inventory.Changed -= HandleDataChanged;
            if (equipment != null) equipment.Changed -= HandleDataChanged;

            if (toggleAction != null)
                toggleAction.action.performed -= OnTogglePerformed;

            CancelDrag();
            ReleaseCursor();
        }

        // ── Open / close ──────────────────────────────────────────────────────

        private void OnTogglePerformed(InputAction.CallbackContext context) => Toggle();

        public void Toggle() => SetOpen(!IsOpen);
        public void Open() => SetOpen(true);
        public void Close() => SetOpen(false);

        public void SetOpen(bool open)
        {
            if (panelRoot == null || panelRoot.activeSelf == open) return;

            panelRoot.SetActive(open);

            if (open)
            {
                RefreshAll();
                CaptureCursor();
                onOpened?.Invoke();
            }
            else
            {
                CancelDrag();
                HideTooltip();
                ReleaseCursor();
                onClosed?.Invoke();
            }
        }

        private void CaptureCursor()
        {
            if (!manageCursor || cursorCaptured) return;

            previousLockMode = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            cursorCaptured = true;
        }

        private void ReleaseCursor()
        {
            if (!cursorCaptured) return;

            Cursor.lockState = previousLockMode;
            Cursor.visible = previousCursorVisible;
            cursorCaptured = false;
        }

        // ── Slot binding & refresh ────────────────────────────────────────────

        private void CollectSlots()
        {
            if (inventorySlotsParent != null)
                inventorySlots = inventorySlotsParent.GetComponentsInChildren<InventorySlotUI>(true);

            if (equipmentSlotsParent != null)
                equipmentSlots = equipmentSlotsParent.GetComponentsInChildren<EquipmentSlotUI>(true);

            for (int i = 0; i < inventorySlots.Length; i++)
                inventorySlots[i].Bind(this, i);

            for (int i = 0; i < equipmentSlots.Length; i++)
                equipmentSlots[i].Bind(this, i);

            ValidateLayout();
        }

        private void ValidateLayout()
        {
            if (inventory != null && inventorySlots.Length != inventory.SlotCount)
                Debug.LogWarning($"[InventoryPanelController] UI has {inventorySlots.Length} inventory slots but " +
                                 $"InventorySO.SlotCount is {inventory.SlotCount}. Items in unmatched slots will be invisible.", this);

            if (equipment == null) return;

            if (equipmentSlots.Length != equipment.SlotCount)
                Debug.LogWarning($"[InventoryPanelController] UI has {equipmentSlots.Length} gear slots but " +
                                 $"EquipmentSO.Slots has {equipment.SlotCount}.", this);

            int shared = Mathf.Min(equipmentSlots.Length, equipment.SlotCount);
            for (int i = 0; i < shared; i++)
            {
                if (equipmentSlots[i].SlotType != equipment.GetSlotType(i))
                    Debug.LogWarning($"[InventoryPanelController] Gear slot {i} ('{equipmentSlots[i].name}') is " +
                                     $"{equipmentSlots[i].SlotType} in the UI but {equipment.GetSlotType(i)} in EquipmentSO.", this);
            }
        }

        private void HandleDataChanged()
        {
            HideTooltip();
            RefreshAll();
        }

        private void RefreshAll()
        {
            if (inventory != null)
            {
                int count = Mathf.Min(inventorySlots.Length, inventory.SlotCount);
                for (int i = 0; i < count; i++)
                {
                    ItemStack stack = inventory.GetStack(i);
                    inventorySlots[i].Show(stack.Item, stack.Count);
                }
            }

            if (equipment != null)
            {
                int count = Mathf.Min(equipmentSlots.Length, equipment.SlotCount);
                for (int i = 0; i < count; i++)
                {
                    EquipmentItemSO gear = equipment.GetEquipped(i);
                    equipmentSlots[i].Show(gear, gear != null ? 1 : 0);
                }
            }
        }

        // ── Called by slots ───────────────────────────────────────────────────

        public void BeginDrag(ItemSlotUI slot, PointerEventData eventData)
        {
            ItemStack stack = InventoryOperations.Read(inventory, equipment, slot.Location);
            if (stack.IsEmpty)
            {
                dragSource = null;
                return;
            }

            dragSource = slot;
            HideTooltip();

            if (dragGhost != null)
            {
                dragGhost.sprite = stack.Item.Icon;
                dragGhost.enabled = stack.Item.Icon != null;
                dragGhost.transform.SetAsLastSibling();
                MoveGhost(eventData);
            }
        }

        public void Drag(PointerEventData eventData)
        {
            if (dragSource != null)
                MoveGhost(eventData);
        }

        /// <summary>Always fires after Drop, whether or not the drop landed on a slot.</summary>
        public void EndDrag() => CancelDrag();

        public void Drop(ItemSlotUI target)
        {
            if (dragSource == null) return;

            InventoryOperations.TryMove(inventory, equipment, dragSource.Location, target.Location);
        }

        /// <summary>Right-click or double-click: equip from the backpack, unequip from gear.</summary>
        public void AlternateAction(ItemSlotUI slot)
        {
            InventoryOperations.TryQuickMove(inventory, equipment, slot.Location);
        }

        public void HoverEnter(ItemSlotUI slot)
        {
            if (tooltip == null || dragSource != null) return;

            ItemStack stack = InventoryOperations.Read(inventory, equipment, slot.Location);
            if (stack.IsEmpty)
            {
                tooltip.Hide();
                return;
            }

            tooltip.Show(stack.Item, slot.RectTransform);
        }

        public void HoverExit() => HideTooltip();

        // ── Helpers ───────────────────────────────────────────────────────────

        private void CancelDrag()
        {
            dragSource = null;

            if (dragGhost != null)
                dragGhost.enabled = false;
        }

        private void HideTooltip()
        {
            if (tooltip != null)
                tooltip.Hide();
        }

        private void MoveGhost(PointerEventData eventData)
        {
            RectTransform parent = dragGhost.rectTransform.parent as RectTransform;
            if (parent == null) return;

            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    parent, eventData.position, eventData.pressEventCamera, out Vector3 worldPoint))
            {
                dragGhost.rectTransform.position = worldPoint;
            }
        }
    }
}
