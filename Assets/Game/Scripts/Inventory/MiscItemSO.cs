using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Non-equippable item: materials, quest items (e.g. Heart of the Gate),
    /// readable scrolls. Set MaxStack above 1 on the asset to make it stackable.
    /// </summary>
    [CreateAssetMenu(fileName = "New_MiscItem", menuName = "SOAP/Items/Misc Item")]
    public class MiscItemSO : ItemDefinitionSO
    {
    }
}
