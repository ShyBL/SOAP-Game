using System.Collections.Generic;
using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Place on enemy prefabs (and chests). Wire your death/open event to
    /// DropLoot() — the same UnityEvent pattern EnemyDeathReporter.ReportDeath
    /// uses. Nothing here knows how the enemy dies.
    ///
    /// Inspector setup:
    ///   lootTable    → LootTableSO to roll
    ///   pickupPrefab → a prefab with an ItemPickup (its inventory already assigned,
    ///                  autoPickup as you like, persistentId EMPTY)
    ///   dropOrigin   → optional, defaults to this transform
    ///   snapToGround → raycasts down so drops land on terrain instead of floating
    ///
    /// Call DropLoot BEFORE the enemy is destroyed or deactivated.
    /// </summary>
    public class LootDropper : MonoBehaviour
    {
        [SerializeField] private LootTableSO lootTable;
        [SerializeField] private ItemPickup pickupPrefab;
        [SerializeField] private Transform dropOrigin;

        [Header("Placement")]
        [SerializeField, Min(0f)] private float scatterRadius = 0.75f;
        [SerializeField] private float heightOffset = 0.3f;
        [SerializeField] private bool snapToGround = true;
        [SerializeField] private LayerMask groundMask = ~0;

        private readonly List<ItemStack> rolled = new List<ItemStack>();

        private void OnValidate()
        {
            if (lootTable == null)
                Debug.LogWarning($"[LootDropper] LootTable is not assigned on {name}.", this);
            if (pickupPrefab == null)
                Debug.LogError($"[LootDropper] PickupPrefab is not assigned on {name}!", this);
        }

        public void DropLoot()
        {
            if (lootTable == null || pickupPrefab == null) return;

            rolled.Clear();
            lootTable.Roll(rolled);

            Vector3 origin = dropOrigin != null ? dropOrigin.position : transform.position;

            for (int i = 0; i < rolled.Count; i++)
            {
                Vector2 offset = Random.insideUnitCircle * scatterRadius;
                Vector3 position = origin + new Vector3(offset.x, 0f, offset.y);

                if (snapToGround && Physics.Raycast(position + Vector3.up * 2f, Vector3.down,
                        out RaycastHit hit, 10f, groundMask, QueryTriggerInteraction.Ignore))
                {
                    position.y = hit.point.y;
                }

                position.y += heightOffset;

                ItemPickup pickup = Instantiate(pickupPrefab, position, Quaternion.identity);
                pickup.Configure(rolled[i].Item, rolled[i].Count);
            }
        }
    }
}
