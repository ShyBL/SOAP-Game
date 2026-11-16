using UnityEngine;

namespace Npc.Movement
{
    /// <summary>
    /// Returned by a strategy's Decide() call.
    ///
    /// Supports two destination modes:
    ///   - SourcePoint != null → named PointOfInterest (uses its DwellSeconds and triggers OnArrival)
    ///   - SourcePoint == null → raw world position (used by RandomWanderStrategySO; uses OverrideDwellSeconds)
    ///
    /// Also carries the flag mutations the strategy wants applied on arrival,
    /// keeping all decision logic inside the strategy rather than on points.
    /// </summary>
    public struct RouteDecision
    {
        /// <summary>Set when destination is a named PointOfInterest. Null for wander destinations.</summary>
        public PointOfInterest SourcePoint;

        /// <summary>Used when SourcePoint is null (wander). Ignored otherwise.</summary>
        public Vector3 WorldPosition;

        /// <summary>Dwell duration when SourcePoint is null. Ignored when SourcePoint is set.</summary>
        public float OverrideDwellSeconds;

        /// <summary>Flag to add to context on arrival. Empty = no change.</summary>
        public string FlagToSet;

        /// <summary>Flag to remove from context on arrival. Empty = no change.</summary>
        public string FlagToClear;

        public Vector3 GetWorldPosition() =>
            SourcePoint != null ? SourcePoint.transform.position : WorldPosition;

        public float GetDwellSeconds() =>
            SourcePoint != null ? SourcePoint.DwellSeconds : OverrideDwellSeconds;

        public bool IsValid =>
            SourcePoint != null || WorldPosition != Vector3.zero;

        public static readonly RouteDecision Empty = new RouteDecision();
    }
}