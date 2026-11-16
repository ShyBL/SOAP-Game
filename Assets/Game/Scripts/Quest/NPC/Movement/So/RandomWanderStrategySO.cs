using UnityEngine;
using UnityEngine.AI;

namespace Npc.Movement
{
    /// <summary>
    /// Picks a random reachable point on the NavMesh within the PointArea's
    /// wander radius. Has no knowledge of PointOfInterests — the area bounds
    /// are the only thing it reads from PointArea.
    ///
    /// RouteDecision.SourcePoint is null for wander decisions. NpcMovementAgent
    /// handles this case by using OverrideDwellSeconds and skipping OnArrival.
    /// </summary>
    [CreateAssetMenu(menuName = "Npc/Movement Strategy/Random Wander", fileName = "RandomWanderStrategy")]
    public class RandomWanderStrategySO : NpcMovementStrategySO
    {
        [SerializeField, Tooltip("How long the NPC pauses at each random spot before picking the next one.")]
        private float dwellSeconds = 2f;

        [SerializeField, Tooltip("How many times to retry NavMesh sampling before giving up. Increase for sparse meshes.")]
        private int maxSampleAttempts = 10;

        [SerializeField, Tooltip("Max distance from the candidate point that NavMesh.SamplePosition will search. Smaller = more accurate, larger = more forgiving on uneven terrain.")]
        private float sampleTolerance = 2f;

        public override RouteDecision Decide(PointArea area, NpcMovementContext context)
        {
            if (area == null)
            {
                Debug.LogWarning("RandomWanderStrategySO: PointArea is null.", this);
                return RouteDecision.Empty;
            }

            if (area.WanderRadius <= 0f)
            {
                Debug.LogWarning($"RandomWanderStrategySO: PointArea '{area.name}' has WanderRadius of 0. Set it in the inspector.", area);
                return RouteDecision.Empty;
            }

            if (TryGetRandomNavMeshPoint(area.transform.position, area.WanderRadius, out Vector3 point))
            {
                return new RouteDecision
                {
                    WorldPosition = point,
                    OverrideDwellSeconds = dwellSeconds
                    // SourcePoint intentionally null — this is a wander, not a named point
                };
            }

            Debug.LogWarning($"RandomWanderStrategySO: Failed to find a valid NavMesh point in '{area.name}' after {maxSampleAttempts} attempts.", area);
            return RouteDecision.Empty;
        }

        private bool TryGetRandomNavMeshPoint(Vector3 center, float radius, out Vector3 result)
        {
            for (int i = 0; i < maxSampleAttempts; i++)
            {
                // Random point in a flat circle (no vertical scatter — let NavMesh handle Y)
                Vector2 flatRandom = Random.insideUnitCircle * radius;
                Vector3 candidate = center + new Vector3(flatRandom.x, 0f, flatRandom.y);

                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, sampleTolerance, NavMesh.AllAreas))
                {
                    result = hit.position;
                    return true;
                }
            }

            result = Vector3.zero;
            return false;
        }
    }
}
