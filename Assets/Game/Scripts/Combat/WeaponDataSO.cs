using UnityEngine;

namespace StarterAssets
{
    /// <summary>
    /// Broad category used by CombatAnimator to route to the correct
    /// upper-body animation set. Does not affect hit or damage logic.
    /// </summary>
    public enum WeaponCategory
    {
        OneHanded, // Sword, axe, mace, dagger
        TwoHanded  // Greatsword, greataxe, warhammer
    }

    /// <summary>
    /// Per-weapon data container. Sits in your SOAP architecture as a typed SO —
    /// no MonoBehaviour, no scene dependency.
    ///
    /// Owns:
    ///   - Identity (name, icon, category)
    ///   - Damage values (light base + power multiplier, with per-direction overrides)
    ///   - Stamina cost for power attacks (was hardcoded in CombatController)
    ///   - Block damage reduction fraction
    ///   - Attack duration hint (informs CombatController.attackLockTimeout calibration)
    ///
    /// Does NOT own:
    ///   - Hitbox collider shape — stays on the weapon GameObject's Collider component
    ///   - Animation clips — stay in the Animator Controller
    ///   - Current stamina — stays in CombatController / StaminaVariableSO
    /// </summary>
    [CreateAssetMenu(fileName = "New Weapon", menuName = "Combat/Weapon Data")]
    public class WeaponDataSO : ScriptableObject
    {
        // ── Identity ──────────────────────────────────────────────────────────

        [Header("Identity")] [Tooltip("Display name shown in UI.")] [SerializeField]
        private string _weaponName = "Weapon";

        [Tooltip("Icon shown in HUD / inventory.")] [SerializeField]
        private Sprite _icon;

        [Tooltip("Drives upper-body animation set selection in CombatAnimator.")] [SerializeField]
        private WeaponCategory _category = WeaponCategory.OneHanded;

        public string WeaponName => _weaponName;
        public Sprite Icon => _icon;
        public WeaponCategory Category => _category;

        [Header("Attack Damage")] 
        [Tooltip("Base damage for a light attack.")] [SerializeField, Range(1f, 200f)]
        private float _lightAttackDamage = 20f;

        public float LightAttackDamage => _lightAttackDamage;
    }
}

