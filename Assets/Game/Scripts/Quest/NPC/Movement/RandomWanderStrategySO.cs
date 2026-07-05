using System.Collections.Generic;
using UnityEngine;

namespace Npc.Movement
{
    /// <summary>
    /// A strategy that picks a random eligible point, optionally filtered by
    /// tags and never repeating the immediately previous point. Useful for
    /// idle wanderers (villagers, tavern guests) who have no fixed job.
    ///
    /// This shows what swapping a strategy asset actually means: the agent code
    /// is identical - only the decision algorithm changes. Assign this instead
    /// of FlagDrivenRouteSO and the same NPC that was doing a knight's duty
    /// routine will start wandering aimlessly, with no code changes anywhere.
    /// </summary>
    [CreateAssetMenu(menuName = "Npc/Movement Strategy/Random Wander", fileName = "RandomWanderStrategy")]
    public class RandomWanderStrategySO : NpcMovementStrategySO
    {
        [SerializeField, Tooltip("Only wander to points with these tags. Leave empty to consider all points in the area.")]
        private string[] allowedTags;

        [SerializeField, Tooltip("Skip the point we just came from so the NPC doesn't immediately backtrack.")]
        private bool avoidImmediateRepeat = true;

        public override RouteDecision Decide(PointArea area, NpcMovementContext context)
        {
            if (area == null)
            {
                Debug.LogWarning("RandomWanderStrategySO: PointArea is null.", this);
                return RouteDecision.Empty;
            }

            List<PointOfInterest> candidates = area.GetPointsByTags(allowedTags);

            if (avoidImmediateRepeat && candidates.Count > 1 && context.CurrentPoint != null)
            {
                candidates.Remove(context.CurrentPoint);
            }

            if (candidates.Count == 0)
            {
                Debug.LogWarning("RandomWanderStrategySO: No candidate points found in area.", this);
                return RouteDecision.Empty;
            }

            PointOfInterest destination = candidates[Random.Range(0, candidates.Count)];

            // No flag mutations - a wandering NPC doesn't track state
            return new RouteDecision { Destination = destination };
        }
    }
}
