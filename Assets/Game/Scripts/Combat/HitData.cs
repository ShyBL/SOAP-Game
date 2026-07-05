using UnityEngine;

namespace StarterAssets
{
    /// <summary>
    /// Immutable payload passed from a Hitbox to a Hurtbox on contact.
    /// Carries enough context for the health/damage system to compute
    /// final damage without any additional lookups:
    ///   damage = Weapon.GetDamage(Type, Direction)
    /// </summary>
    public readonly struct HitData
    {
        /// <summary>The GameObject that owns the Hitbox (i.e. the weapon).</summary>
        public readonly GameObject Source;

        /// <summary>The root GameObject of the attacker (i.e. the character).</summary>
        public readonly GameObject Attacker;

        /// <summary>
        /// The equipped weapon at the time of the hit.
        /// Call Weapon.GetDamage(Type, Direction) to get final damage.
        /// Null if the attacker had no weapon equipped (unarmed).
        /// </summary>
        public readonly WeaponDataSO Weapon;

        /// <summary>World-space point of contact.</summary>
        public readonly Vector3 HitPoint;

        public HitData(
            GameObject source,
            GameObject attacker,

            WeaponDataSO weapon,
            Vector3 hitPoint)
        {
            Source    = source;
            Attacker  = attacker;
            Weapon    = weapon;
            HitPoint  = hitPoint;
        }
    }
}
