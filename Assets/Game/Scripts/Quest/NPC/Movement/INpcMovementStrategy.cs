namespace Npc.Movement
{
    /// <summary>
    /// A strategy reads the NPC's current context and decides where to go next.
    /// "Decides" is the operative word — implementations must evaluate some
    /// condition to earn that name, not just advance a counter.
    /// </summary>
    public interface INpcMovementStrategy
    {
        RouteDecision Decide(PointArea area, NpcMovementContext context);
    }
}