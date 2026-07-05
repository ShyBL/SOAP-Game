using System;
using System.Collections.Generic;
using UnityEngine;

namespace Npc.Movement
{
    /// <summary>
    /// A rule in the priority list. The strategy evaluates rules top-to-bottom
    /// and takes the first one whose flag condition is satisfied.
    /// </summary>
    [Serializable]
    public class RouteRule
    {
        [Tooltip("Flag to check against the NPC's context. Leave empty to always match (use this as your fallback/default rule).")]
        public string requiredFlag;

        [Tooltip("If true, rule matches when the flag is NOT set. Lets you express 'if I don't have timber yet'.")]
        public bool invertCondition;

        [Tooltip("Tag of the PointOfInterest to go to if this rule wins.")]
        public string destinationTag;

        [Tooltip("Flag to add to the NPC's context when it arrives at this point.")]
        public string setFlagOnArrival;

        [Tooltip("Flag to remove from the NPC's context when it arrives at this point.")]
        public string clearFlagOnArrival;
    }

    /// <summary>
    /// A strategy that evaluates a priority-ordered list of conditional rules
    /// and picks the first one whose flag condition is satisfied.
    ///
    /// THIS is the decision-making. The NPC isn't stepping through a sequence —
    /// it's evaluating its own state (flags) on every call and branching to a
    /// different destination based on what's true right now.
    ///
    /// Knight camp inspector setup (rules evaluated top to bottom):
    ///   Rule 0: requiredFlag="HasTimber"   invert=false → "Fire"        set="HasRested"  clear="HasTimber"
    ///   Rule 1: requiredFlag="HasRested"   invert=false → "CommandTent"  clear="HasRested"
    ///   Rule 2: requiredFlag=""  (fallback)             → "Forest"       set="HasTimber"
    ///
    /// No knight-specific code lives anywhere in these scripts. The camp
    /// context is entirely in the asset's inspector fields.
    /// </summary>
    [CreateAssetMenu(menuName = "Npc/Movement Strategy/Flag Driven Route", fileName = "FlagDrivenRouteStrategy")]
    public class FlagDrivenRouteSO : NpcMovementStrategySO
    {
        [SerializeField]
        [Tooltip("Rules evaluated in order. First matching rule wins. Put your fallback (empty requiredFlag) last.")]
        private List<RouteRule> rules = new List<RouteRule>();

        public override RouteDecision Decide(PointArea area, NpcMovementContext context)
        {
            if (area == null)
            {
                Debug.LogWarning("FlagDrivenRouteSO: PointArea is null.", this);
                return RouteDecision.Empty;
            }

            for (int i = 0; i < rules.Count; i++)
            {
                RouteRule rule = rules[i];

                if (!IsRuleConditionMet(rule, context))
                {
                    continue;
                }

                PointOfInterest destination = area.GetPointByTag(rule.destinationTag);

                if (destination == null)
                {
                    Debug.LogWarning($"FlagDrivenRouteSO: Rule {i} matched but no point found with tag '{rule.destinationTag}'.", this);
                    continue;
                }

                return new RouteDecision
                {
                    Destination = destination,
                    FlagToSet = rule.setFlagOnArrival,
                    FlagToClear = rule.clearFlagOnArrival
                };
            }

            Debug.LogWarning($"FlagDrivenRouteSO on {name}: No rule matched. Add a fallback rule with empty requiredFlag.", this);
            return RouteDecision.Empty;
        }

        private bool IsRuleConditionMet(RouteRule rule, NpcMovementContext context)
        {
            // Empty requiredFlag = unconditional match (fallback rule)
            if (string.IsNullOrEmpty(rule.requiredFlag))
            {
                return true;
            }

            bool flagIsSet = context.HasFlag(rule.requiredFlag);

            // invertCondition lets you express "go here when flag is NOT set"
            return rule.invertCondition ? !flagIsSet : flagIsSet;
        }
    }
}
