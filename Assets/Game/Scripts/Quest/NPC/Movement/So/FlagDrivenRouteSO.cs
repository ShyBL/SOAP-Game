using System.Collections.Generic;
using UnityEngine;

namespace Npc.Movement
{
    /// <summary>
    /// Evaluates a priority-ordered list of conditional rules against the NPC's
    /// current flag state and picks the first one that both matches AND passes
    /// its probability roll.
    ///
    /// Example — table worker (rules evaluated top to bottom):
    ///   Rule 0: requiredFlags=[VisitedBag]              prob=1.0  → Well   set=JustFilled  clear=VisitedBag
    ///   Rule 1: requiredFlags=[JustFilled]              prob=1.0  → Table                  clear=JustFilled
    ///   Rule 2: requiredFlags=[]  excludedFlags=[]      prob=0.3  → Bag    set=VisitedBag
    ///   Rule 3: requiredFlags=[]  (guaranteed fallback) prob=1.0  → Well   set=JustFilled
    ///
    /// No tag names or game-specific logic live in this script.
    /// </summary>
    [CreateAssetMenu(menuName = "Npc/Movement Strategy/Flag Driven Route", fileName = "FlagDrivenRouteStrategy")]
    public class FlagDrivenRouteSO : NpcMovementStrategySO
    {
        [SerializeField]
        [Tooltip("Evaluated top to bottom. First rule that matches AND passes its probability roll wins.\nPut a rule with probability=1 and empty flags last as a guaranteed fallback.")]
        private List<RouteRule> rules = new List<RouteRule>();

        public override RouteDecision Decide(PointArea area, NpcMovementContext context)
        {
            if (area == null)
            {
                Debug.LogWarning($"FlagDrivenRouteSO '{name}': PointArea is null.", this);
                return RouteDecision.Empty;
            }

            for (int i = 0; i < rules.Count; i++)
            {
                RouteRule rule = rules[i];

                if (!AreConditionsMet(rule, context))
                {
                    continue;
                }

                if (UnityEngine.Random.value > rule.probability)
                {
                    // Conditions matched but probability roll failed — fall through
                    // to the next rule. This is what lets an NPC "sometimes" deviate.
                    continue;
                }

                PointOfInterest destination = area.GetPointByTag(rule.destinationTag);

                if (destination == null)
                {
                    Debug.LogWarning($"FlagDrivenRouteSO '{name}': Rule {i} won but no point found with tag '{rule.destinationTag}'. Trying next rule.", this);
                    continue;
                }

                return new RouteDecision
                {
                    SourcePoint = destination,
                    FlagToSet = rule.setFlagOnArrival,
                    FlagToClear = rule.clearFlagOnArrival
                };
            }

            Debug.LogWarning($"FlagDrivenRouteSO '{name}': No rule produced a destination. Add a fallback rule with probability=1 and empty flag lists.", this);
            return RouteDecision.Empty;
        }

        private bool AreConditionsMet(RouteRule rule, NpcMovementContext context)
        {
            // All required flags must be set (AND)
            for (int i = 0; i < rule.requiredFlags.Count; i++)
            {
                if (!string.IsNullOrEmpty(rule.requiredFlags[i]) && !context.HasFlag(rule.requiredFlags[i]))
                {
                    return false;
                }
            }

            // No excluded flags may be set (AND NOT)
            for (int i = 0; i < rule.excludedFlags.Count; i++)
            {
                if (!string.IsNullOrEmpty(rule.excludedFlags[i]) && context.HasFlag(rule.excludedFlags[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
