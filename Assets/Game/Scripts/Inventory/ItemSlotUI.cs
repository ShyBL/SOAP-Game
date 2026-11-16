using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SOAP
{
    /// <summary>
    /// Shared behaviour of every slot in the panel. Pure view + input
    /// forwarding: it shows what it is told and reports pointer events to the
    /// InventoryPanelController. It never touches InventorySO/EquipmentSO.
    ///
    /// Add InventorySlotUI or EquipmentSlotUI (not this) to each slot object.
    ///
    /// Requirements on the slot object:
    ///   - a Graphic (e.g. the slot background Image) with Raycast Target ON,
    ///     otherwise drag/drop/click never reach this script
    ///   - iconImage: a child Image, Raycast Target OFF
    ///
    /// Inspector setup:
    ///   iconImage   → child Image that shows the item icon
    ///   countText   → optional TMP text for stack counts (blank when count is 1)
    ///   emptySprite → optional sprite shown while empty (gear silhouettes)
    /// </summary>
    public abstract class ItemSlotUI : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler,
        IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private Sprite emptySprite;

        private InventoryPanelController owner;
        private int index;

        public abstract SlotKind Kind { get; }

        public int Index => index;
        public ItemLocation Location => new ItemLocation(Kind, index);
        public RectTransform RectTransform => (RectTransform)transform;

        private void OnValidate()
        {
            if (iconImage == null)
                Debug.LogError($"[{GetType().Name}] IconImage is not assigned on {name}!", this);

            if (!TryGetComponent(out Graphic graphic) || !graphic.raycastTarget)
                Debug.LogWarning($"[{GetType().Name}] {name} needs a Graphic with Raycast Target on, " +
                                 "otherwise drag/drop/click will not reach it.", this);
        }

        /// <summary>Called once by the panel controller.</summary>
        public void Bind(InventoryPanelController panelOwner, int slotIndex)
        {
            owner = panelOwner;
            index = slotIndex;
        }

        public void Show(ItemDefinitionSO item, int count)
        {
            if (iconImage != null)
            {
                Sprite sprite = item != null ? item.Icon : emptySprite;
                iconImage.sprite = sprite;
                iconImage.enabled = sprite != null;
            }

            if (countText != null)
                countText.text = count > 1 ? count.ToString() : string.Empty;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (owner != null) owner.BeginDrag(this, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (owner != null) owner.Drag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (owner != null) owner.EndDrag();
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (owner != null) owner.Drop(this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (owner == null) return;

            bool rightClick = eventData.button == PointerEventData.InputButton.Right;
            bool doubleClick = eventData.button == PointerEventData.InputButton.Left && eventData.clickCount >= 2;

            if (rightClick || doubleClick)
                owner.AlternateAction(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (owner != null) owner.HoverEnter(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (owner != null) owner.HoverExit();
        }
    }
}
