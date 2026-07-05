using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Npc.Movement
{
    /// <summary>
    /// The Strategy pattern's "Context" class. Owns the NavMeshAgent and
    /// handles movement execution, but holds no opinion about WHERE to go or
    /// WHEN — that is fully delegated to the assigned strategy via Decide().
    ///
    /// On arrival, it applies the RouteDecision's flag mutations to the context
    /// so the NEXT call to Decide() sees updated state — this is the feedback
    /// loop that makes flag-driven decisions work.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [DisallowMultipleComponent]
    public class NpcMovementAgent : MonoBehaviour
    {
        private enum MovementState { Idle, Moving, Dwelling }

        private const float ArrivalSpeedThreshold = 0.05f;

        [SerializeField] private PointArea pointArea;
        [SerializeField] private NpcMovementStrategySO movementStrategy;

        private NavMeshAgent agent;
        private NpcMovementContext context;
        private RouteDecision activeDecision;
        private MovementState state;
        private Coroutine dwellRoutine;

        private void Awake()
        {
            if (!TryGetComponent(out agent))
            {
                Debug.LogError($"NpcMovementAgent on '{gameObject.name}' is missing a NavMeshAgent.", this);
                enabled = false;
                return;
            }

            context = new NpcMovementContext();
        }

        private void Start()
        {
            MakeNextDecision();
        }

        private void Update()
        {
            if (state != MovementState.Moving)
            {
                return;
            }

            if (agent.pathPending)
            {
                return;
            }

            if (agent.remainingDistance > agent.stoppingDistance)
            {
                return;
            }

            if (agent.hasPath && agent.velocity.sqrMagnitude > ArrivalSpeedThreshold)
            {
                return;
            }

            OnArrived();
        }

        /// <summary>
        /// Swap the decision algorithm at runtime. The NPC's flag state is
        /// preserved unless you also call context.ResetFlags() — useful if
        /// the new strategy shares the same flag vocabulary.
        /// </summary>
        public void SetStrategy(NpcMovementStrategySO newStrategy)
        {
            movementStrategy = newStrategy;
        }

        private void MakeNextDecision()
        {
            if (pointArea == null || movementStrategy == null)
            {
                Debug.LogWarning($"NpcMovementAgent on '{gameObject.name}': PointArea or Strategy not assigned.", this);
                return;
            }

            RouteDecision decision = movementStrategy.Decide(pointArea, context);

            if (!decision.IsValid)
            {
                Debug.LogWarning($"NpcMovementAgent on '{gameObject.name}': Strategy returned no valid destination.", this);
                return;
            }

            activeDecision = decision;
            agent.SetDestination(decision.Destination.transform.position);
            state = MovementState.Moving;
        }

        private void OnArrived()
        {
            state = MovementState.Dwelling;

            // Update context with where we are
            context.CurrentPoint = activeDecision.Destination;

            // Apply the flag mutations the strategy specified for this arrival.
            // This is what feeds the next Decide() call — the strategy set these
            // flags to encode "after visiting Forest, record that I have timber"
            // so the next evaluation can branch on it.
            context.SetFlag(activeDecision.FlagToSet);
            context.ClearFlag(activeDecision.FlagToClear);

            // Cosmetic/gameplay hook on the point itself (animations etc.)
            activeDecision.Destination.TriggerArrival();

            if (dwellRoutine != null)
            {
                StopCoroutine(dwellRoutine);
            }

            dwellRoutine = StartCoroutine(DwellThenDecide(activeDecision.Destination.DwellSeconds));
        }

        private IEnumerator DwellThenDecide(float duration)
        {
            if (duration > 0f)
            {
                yield return new WaitForSeconds(duration);
            }

            dwellRoutine = null;
            MakeNextDecision();
        }
    }
}
