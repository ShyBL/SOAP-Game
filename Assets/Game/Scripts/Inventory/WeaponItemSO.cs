using System.Collections.Generic;
using StarterAssets;
using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Wearable weapon. Wraps your existing WeaponDataSO so combat data
    /// (damage, category) stays exactly where Hitbox already reads it — this
    /// asset only adds the inventory-facing side (icon, slot, two-handed rule).
    /// </summary>
    [CreateAssetMenu(fileName = "New_Weapon", menuName = "SOAP/Items/Weapon")]
    public class WeaponItemSO : EquipmentItemSO
    {
        [Header("Weapon")]
        [Tooltip("Combat data consumed through the equipped weapon (see EquipmentWeaponBridge).")]
        public WeaponDataSO Weapon;

        [Tooltip("Two-handed weapons evict the off-hand item and block off-hand equipping while held.")]
        public bool TwoHanded;

        public override bool OccupiesOffHand => TwoHanded;

        protected override void AppendPrimaryStatLines(List<string> lines)
        {
            if (Weapon != null)
                lines.Add($"Damage: {Weapon.LightAttackDamage}");

            if (TwoHanded)
                lines.Add("Two-handed");
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            if (Weapon == null)
                Debug.LogWarning($"[WeaponItemSO] '{name}': Weapon (WeaponDataSO) is not assigned.", this);

            if (Slot != EquipSlotType.MainHand && Slot != EquipSlotType.OffHand)
                Debug.LogWarning($"[WeaponItemSO] '{name}': Slot is {Slot}. Weapons normally use MainHand or OffHand.", this);
        }
    }
}
