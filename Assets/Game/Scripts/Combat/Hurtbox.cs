using System;
using UnityEngine;
using UnityEngine.Events;

namespace StarterAssets
{
    /// <summary>
    /// Sits on any GameObject that can receive melee hits — enemies, the player, destructibles.
    /// The Collider on this GameObject should NOT be a trigger; the Hitbox (trigger) detects it.
    ///
    /// Responsibilities:
    ///   - Receive a HitData from a Hitbox
    ///   - Fire OnHitReceived so the owning health/damage system can react
    ///
    /// Does NOT apply damage — that lives in whatever subscribes to OnHitReceived.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Hurtbox : MonoBehaviour
    {
        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>
        /// Fired when this hurtbox is struck by an active Hitbox.
        /// Subscribe from a HealthComponent, EnemyCombatController, etc.
        /// </summary>
        public UnityEvent<float> OnHitReceived;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Called directly by Hitbox.OnTriggerEnter.
        /// Do not call from other systems — go through the event.
        /// </summary>
        public void ReceiveHit(HitData hitData)
        {
            Debug.Log(hitData);
            OnHitReceived.Invoke(hitData.Weapon.LightAttackDamage);
        }
    }
}
