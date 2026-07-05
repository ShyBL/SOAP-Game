using System.Collections.Generic;

namespace Npc.Movement
{
    /// <summary>
    /// Per-NPC runtime state. This is what strategies READ to make decisions —
    /// the "input" to the decision algorithm.
    ///
    /// Flags are plain strings ("HasTimber", "HasRested") set or cleared when
    /// the NPC arrives at a point, as specified by each RouteDecision. Keeping
    /// them here (not on the SO) means multiple NPCs sharing the same strategy
    /// asset each maintain their own independent state.
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
