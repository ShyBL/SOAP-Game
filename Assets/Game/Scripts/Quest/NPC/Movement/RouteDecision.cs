namespace Npc.Movement
{
    /// <summary>
    /// Returned by a strategy's Decide() call.
    /// Bundles the destination together with the context mutations the strategy
    /// wants applied once the NPC arrives — so the strategy decides both WHERE
    /// to go AND what state-change that visit should produce, keeping all
    /// decision logic inside the strategy rather than scattered across points.
    /// </summary>
    public struct RouteDecision
    {
        public PointOfInterest Destination;

        /// <summary>Flag to add to context on arrival. Empty = no change.</summary>
        public string FlagToSet;

        /// <summary>Flag to remove from context on arrival. Empty = no change.</summary>
        public string FlagToClear;

        public bool IsValid => Destination != null;

        public static readonly RouteDecision Empty = new RouteDecision();
    }
}
