using StarterAssets;
using UnityEngine;
using UnityEngine.Events;

namespace SOAP
{
    /// <summary>
    /// Keeps combat in sync with the equipped weapon without coupling the two.
    /// Whenever the main-hand weapon changes (including once at startup and
    /// after a load), onWeaponChanged fires with that WeaponDataSO, or null
    /// when unarmed.
    ///
    /// Inspector setup (put this on the Player):
    ///   equipment        → the player's EquipmentSO
    ///   onWeaponChanged  → wire to whatever sets the controller's EquippedWeapon
    ///                      (Hitbox reads it from ThirdPersonController)
    /// </summary>
    public class EquipmentWeaponBridge : MonoBehaviour
    {
        [SerializeField] private EquipmentSO equipment;
        [SerializeField] private UnityEvent<WeaponDataSO> onWeaponChanged;

        private WeaponDataSO lastWeapon;
        private bool hasPublished;

        private void OnValidate()
        {
            if (equipment == null)
                Debug.LogError($"[EquipmentWeaponBridge] Equipment is not assigned on {name}!", this);
        }

        private void OnEnable()
        {
            if (equipment == null) return;

            equipment.Changed += HandleEquipmentChanged;
            hasPublished = false;
            HandleEquipmentChanged();
        }

        private void OnDisable()
        {
            if (equipment != null)
                equipment.Changed -= HandleEquipmentChanged;
        }

        private void HandleEquipmentChanged()
        {
            WeaponDataSO weapon = equipment.EquippedWeapon;

            if (hasPublished && weapon == lastWeapon) return;

            hasPublished = true;
            lastWeapon = weapon;
            onWeaponChanged?.Invoke(weapon);
        }
    }
}
