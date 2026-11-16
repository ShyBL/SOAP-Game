using System.Collections.Generic;

namespace Npc.Movement
{
    /// <summary>
    /// Per-NPC runtime state. This is what strategies READ to make decisions.
    ///
    /// Flags are plain strings ("HasTimber", "JustFilled", "VisitedBag") set or
    /// cleared when the NPC arrives at a point, as declared in each RouteDecision.
    /// Kept here (not on the SO asset) so multiple NPCs sharing the same strategy
    /// each maintain independent state.
    /// </summary>
    public class NpcMovementContext
    {
        private readonly HashSet<string> flags = new HashSet<string>();

        public PointOfInterest CurrentPoint { get; set; }

        public bool HasFlag(string flag) => flags.Contains(flag);

        public void SetFlag(string flag)
        {
            if (!string.IsNullOrEmpty(flag))
            {
                flags.Add(flag);
            }
        }

        public void ClearFlag(string flag)
        {
            if (!string.IsNullOrEmpty(flag))
            {
                flags.Remove(flag);
            }
        }

        public void ResetFlags() => flags.Clear();
    }
}