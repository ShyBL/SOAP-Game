using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Npc.Movement
{
    /// <summary>
    /// The Strategy pattern's Context. Drives the NavMeshAgent and manages the
    /// decide → move → dwell loop. Knows nothing about destinations or rules —
    /// that is the strategy's job.
    ///
    /// Handles two RouteDecision modes transparently:
    ///   - SourcePoint != null → named PointOfInterest: uses its DwellSeconds,
    ///                           triggers its OnArrival hook, updates context.CurrentPoint
    ///   - SourcePoint == null → raw wander position: uses OverrideDwellSeconds,
    ///                           no OnArrival hook, context.CurrentPoint left as-is
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
                Debug.LogError($"NpcMovementAgent on '{gameObject.name}': NavMeshAgent component missing.", this);
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
        /// preserved so a strategy change mid-route (e.g. "flee" overriding
        /// "patrol") can still read existing flags if needed.
        /// Pass resetContext: true to wipe flags when the new strategy
        /// has an unrelated flag vocabulary.
        /// </summary>
        public void SetStrategy(NpcMovementStrategySO newStrategy, bool resetContext = false)
        {
            movementStrategy = newStrategy;

            if (resetContext)
            {
                context.ResetFlags();
            }
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
                Debug.LogWarning($"NpcMovementAgent on '{gameObject.name}': Strategy returned no valid destination. NPC is idle.", this);
                state = MovementState.Idle;
                return;
            }

            activeDecision = decision;
            agent.SetDestination(decision.GetWorldPosition());
            state = MovementState.Moving;
        }

        private void OnArrived()
        {
            state = MovementState.Dwelling;

            // Apply flag mutations the strategy declared for this arrival.
            // This feeds the next Decide() call with updated state.
            context.SetFlag(activeDecision.FlagToSet);
            context.ClearFlag(activeDecision.FlagToClear);

            if (activeDecision.SourcePoint != null)
            {
                // Named point: update context position and fire the cosmetic hook
                context.CurrentPoint = activeDecision.SourcePoint;
                activeDecision.SourcePoint.TriggerArrival();
            }
            // Wander point: no OnArrival hook, context.CurrentPoint unchanged

            if (dwellRoutine != null)
            {
                StopCoroutine(dwellRoutine);
            }

            dwellRoutine = StartCoroutine(DwellThenDecide(activeDecision.GetDwellSeconds()));
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
