using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Shows an item's name and detail lines. The lines come from the item
    /// itself (ItemDefinitionSO.AppendTooltipLines), so this never checks
    /// item types.
    ///
    /// Inspector setup (on the tooltip panel, a child of the canvas, drawn on top):
    ///   titleText → TMP for the item name
    ///   bodyText  → TMP for the detail lines
    ///   offset    → pixel offset from the slot's top-right corner
    ///
    /// Set the tooltip RectTransform pivot to (0, 1) so it hangs down and to
    /// the right of the slot, and turn Raycast Target off on all its graphics.
    /// </summary>
    public class ItemTooltipUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private Vector2 offset = new Vector2(8f, 0f);

        private readonly List<string> lines = new List<string>();
        private readonly Vector3[] corners = new Vector3[4];

        private void OnValidate()
        {
            if (titleText == null)
                Debug.LogError($"[ItemTooltipUI] TitleText is not assigned on {name}!", this);
            if (bodyText == null)
                Debug.LogError($"[ItemTooltipUI] BodyText is not assigned on {name}!", this);
        }

        public void Show(ItemDefinitionSO item, RectTransform anchor)
        {
            if (item == null || titleText == null || bodyText == null) return;

            titleText.text = item.DisplayName;

            lines.Clear();
            item.AppendTooltipLines(lines);
            if (!string.IsNullOrEmpty(item.Description))
                lines.Add(item.Description);

            bodyText.text = string.Join("\n", lines);

            gameObject.SetActive(true);

            if (anchor != null)
            {
                anchor.GetWorldCorners(corners);          // 0 BL, 1 TL, 2 TR, 3 BR
                transform.position = corners[2] + (Vector3)offset;
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
