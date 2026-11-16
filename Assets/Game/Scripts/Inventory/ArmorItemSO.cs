using System.Collections.Generic;
using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Wearable armor piece. ArmorValue is reported as the "armor" stat, so a
    /// damage system just asks EquipmentSO.GetStatTotal(ArmorItemSO.ArmorStatId).
    /// </summary>
    [CreateAssetMenu(fileName = "New_Armor", menuName = "SOAP/Items/Armor")]
    public class ArmorItemSO : EquipmentItemSO
    {
        public const string ArmorStatId = "armor";

        [Header("Armor")]
        [Min(0)]
        public int ArmorValue;

        public override void CollectModifiers(List<StatModifier> into)
        {
            base.CollectModifiers(into);

            if (ArmorValue != 0)
                into.Add(new StatModifier { StatId = ArmorStatId, Amount = ArmorValue });
        }

        protected override void AppendPrimaryStatLines(List<string> lines)
        {
            lines.Add($"Armor: {ArmorValue}");
        }
    }
}
