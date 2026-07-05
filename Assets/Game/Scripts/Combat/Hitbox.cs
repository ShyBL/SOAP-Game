using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarterAssets
{
    /// <summary>
    /// Sits on the weapon GameObject alongside its trigger Collider.
    /// Activated and deactivated by the owning attack system.
    ///
    /// Responsibilities:
    ///   - Detect Hurtbox overlaps while active
    ///   - Deduplicate: each Hurtbox is hit at most once per active window
    ///   - Forward a HitData to Hurtbox.ReceiveHit
    ///
    /// Does NOT know about damage values — that lives downstream.
    ///
    /// Self-hit prevention (REQUIRED setup):
    ///   Filtered at the engine level via the Physics Layer Collision Matrix —
    ///   OnTriggerEnter is never invoked for same-faction collisions.
    ///   No C# hierarchy traversal needed.
    ///
    ///   Required layers:
    ///     "PlayerHitbox"  — assign to this weapon's collider
    ///     "PlayerHurtbox" — assign to hurtbox colliders on the player's body
    ///     "EnemyHitbox"   — assign to enemy weapon colliders
    ///     "EnemyHurtbox"  — assign to hurtbox colliders on enemy bodies
    ///
    ///   In Edit > Project Settings > Physics, collision matrix:
    ///     DISABLE  PlayerHitbox  <-> PlayerHurtbox   (player can't self-hit)
    ///     DISABLE  EnemyHitbox   <-> EnemyHurtbox    (enemy can't self-hit)
    ///     ENABLE   PlayerHitbox  <-> EnemyHurtbox    (player hits enemies)
    ///     ENABLE   EnemyHitbox   <-> PlayerHurtbox   (enemies hit player)
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [Serializable]
    public class Hitbox : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Root of the character that owns this weapon. Passed through to HitData.Attacker " +
                 "for downstream damage systems. Self-hit prevention is handled by the Physics " +
                 "Layer Collision Matrix — not by a runtime hierarchy check.")]
        [SerializeField] private GameObject _attacker;

        // ── State ─────────────────────────────────────────────────────────────

        /// <summary>Hurtboxes already hit this swing window — cleared on Deactivate.</summary>
        private readonly HashSet<Hurtbox> _hitThisWindow = new HashSet<Hurtbox>();

        private Collider         _collider;
        private ThirdPersonController _controller; // Cached from attacker root for weapon lookup

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            TryGetComponent(out _collider);
            _collider.isTrigger = true;

            if (_attacker != null)
                _attacker.TryGetComponent(out _controller);

            Deactivate();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Enable the hitbox for a new swing window.
        /// Called from CombatController via OnAttackCommitted.
        /// </summary>
        public void Activate()
        {
            _hitThisWindow.Clear();
            _collider.enabled = true;
        }

        /// <summary>
        /// Disable the hitbox and end the swing window.
        /// Called when the attack animation settles or CombatController.EndAttack() fires.
        /// </summary>
        public void Deactivate()
        {
            _collider.enabled = false;
            _hitThisWindow.Clear();
        }

        // ── Trigger detection ─────────────────────────────────────────────────
        // OnTriggerEnter only fires when _collider.enabled — no redundant bool needed.

        private void OnTriggerEnter(Collider other)
        {
            // Self-hits excluded by the Physics Layer Collision Matrix.
            if (!other.TryGetComponent(out Hurtbox hurtbox)) return;

            if (_hitThisWindow.Contains(hurtbox)) return;
            _hitThisWindow.Add(hurtbox);

            hurtbox.ReceiveHit(new HitData(
                source:    gameObject,
                attacker:  _attacker,
                weapon:    _controller != null ? _controller.EquippedWeapon : null,
                hitPoint:  other.ClosestPoint(transform.position)
            ));
        }
    }
}
