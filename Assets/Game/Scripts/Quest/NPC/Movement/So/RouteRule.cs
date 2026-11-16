using System;
using System.Collections.Generic;
using UnityEngine;

namespace Npc.Movement
{
    /// <summary>
    /// One conditional branch in the priority list.
    ///
    /// Conditions use AND logic within each list:
    ///   - requiredFlags: ALL must be set
    ///   - excludedFlags: NONE may be set
    ///
    /// If both lists pass, the rule rolls against probability.
    /// If the roll fails, evaluation continues to the next rule —
    /// this is what makes "sometimes go to the bag first" possible
    /// without hard-coding any branching in the agent.
    /// </summary>
    [Serializable]
    public class RouteRule
    {
        [Tooltip("ALL of these flags must be set for this rule to be eligible. Leave empty to skip the check.")]
        public List<string> requiredFlags = new List<string>();

        [Tooltip("If ANY of these flags are set, this rule is skipped. Use instead of invertCondition.")]
        public List<string> excludedFlags = new List<string>();

        [Range(0f, 1f)]
        [Tooltip("Chance this rule is taken when eligible. 1 = always. If it fails, evaluation falls through to the next rule.\n\nMake your last rule probability 1 as a guaranteed fallback.")]
        public float probability = 1f;

        [Tooltip("Tag of the PointOfInterest to go to when this rule wins.")]
        public string destinationTag;

        [Tooltip("Flag to add to context on arrival at this point.")]
        public string setFlagOnArrival;

        [Tooltip("Flag to remove from context on arrival at this point.")]
        public string clearFlagOnArrival;
    }
}