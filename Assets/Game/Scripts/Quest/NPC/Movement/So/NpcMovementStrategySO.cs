using UnityEngine;

namespace Npc.Movement
{
    /// <summary>
    /// Base class for movement strategy assets. Being a ScriptableObject means
    /// you create each strategy as a project asset, assign it per-NPC in the
    /// inspector, and swap the whole decision algorithm just by dragging a
    /// different asset in. No code changes on the agent.
    /// </summary>
    public abstract class NpcMovementStrategySO : ScriptableObject, INpcMovementStrategy
    {
        public abstract RouteDecision Decide(PointArea area, NpcMovementContext context);
    }
}