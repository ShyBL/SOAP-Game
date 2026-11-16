using UnityEngine;
using UnityEngine.AI;

namespace Npc.Movement
{
    /// <summary>
    /// NavMeshAgent-driven animation controller for animal NPCs that share the
    /// Animator Controller / avatar used by the CreatureMover-driven player creature.
    /// Mirrors CreatureMover's AnimationHandler output (the "Vert" / "State" params
    /// and optional look-at IK) without depending on CreatureMover or MovePlayerInput.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    [DisallowMultipleComponent]
    public class CreatureAnimationController : MonoBehaviour
    {
        [Header("Animator Parameters")]
        [Tooltip("Must match the Animator Controller param name used by CreatureMover")]
        [SerializeField] private string m_VerticalID = "Vert";
        [Tooltip("Must match the Animator Controller param name used by CreatureMover")]
        [SerializeField] private string m_StateID = "State";

        [Header("Speed")]
        [Tooltip("How quickly Vert/State flow toward their target values (matches CreatureMover's k_InputFlow feel)")]
        [SerializeField] private float m_InputFlow = 4.5f;

        [Tooltip("Velocity threshold to consider the agent moving")]
        [SerializeField] private float m_MoveThreshold = 0.1f;

        [Header("Run State")]
        [Tooltip("If true, running is inferred from speed instead of SetRunning() calls")]
        [SerializeField] private bool m_AutoDetectRunState = false;
        [Tooltip("Fraction of agent.speed above which State is treated as running, when auto-detecting")]
        [SerializeField, Range(0f, 1f)] private float m_RunSpeedFraction = 0.66f;

        [Header("Rotation")]
        [Tooltip("How fast the creature rotates to face movement/target")]
        [SerializeField] private float m_RotationSpeed = 8f;
        [Tooltip("When within this extra distance, prefer facing the steering target")]
        [SerializeField] private float m_SteeringFaceDistance = 0.5f;
        [Tooltip("Minimum distance to steeringTarget before it's trusted as a facing direction (avoids idle jitter from noise)")]
        [SerializeField] private float m_SteeringDeadZone = 0.15f;

        [Header("Look At (optional)")]
        [SerializeField] private bool m_UseLookAtIK = false;
        [SerializeField] private Transform m_LookTarget;
        [SerializeField] private float m_LookWeight = 1f;
        [SerializeField, Range(0f, 1f)] private float m_LookBodyWeight = 0.3f;
        [SerializeField, Range(0f, 1f)] private float m_LookHeadWeight = 0.7f;
        [SerializeField, Range(0f, 1f)] private float m_LookEyesWeight = 1f;

        private Animator m_Animator;
        private NavMeshAgent m_Agent;

        private int m_AnimIDVert;
        private int m_AnimIDState;

        private float m_FlowVert;
        private float m_FlowState;

        private bool m_IsRunning;
        private bool m_HasAnimator;
        private bool m_HasAgent;

        private void Start()
        {
            m_HasAnimator = TryGetComponent(out m_Animator);
            m_HasAgent = TryGetComponent(out m_Agent);

            if (m_HasAgent)
            {
                m_Agent.updateRotation = false;
            }

            m_AnimIDVert = Animator.StringToHash(m_VerticalID);
            m_AnimIDState = Animator.StringToHash(m_StateID);
        }

        private void Update()
        {
            if (!m_HasAnimator || !m_HasAgent) return;

            bool isIdle = DetermineIsIdle();

            UpdateRunState();
            UpdateAnimator(isIdle);
            UpdateRotation(isIdle);
        }

        /// <summary>
        /// Single source of truth for "is this creature effectively stationary".
        /// Uses path state rather than raw per-frame velocity, since agent.velocity
        /// spikes briefly even at rest (avoidance/steering corrections), which causes
        /// visible idle jitter if each system re-derives its own moving/idle check.
        /// </summary>
        private bool DetermineIsIdle()
        {
            if (!m_Agent.isOnNavMesh) return true;

            return !m_Agent.pathPending &&
                   m_Agent.remainingDistance <= m_Agent.stoppingDistance + m_SteeringFaceDistance &&
                   (!m_Agent.hasPath || m_Agent.velocity.sqrMagnitude < m_MoveThreshold * m_MoveThreshold);
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (!m_UseLookAtIK || m_LookTarget == null || m_Animator == null) return;

            m_Animator.SetLookAtPosition(m_LookTarget.position);
            m_Animator.SetLookAtWeight(m_LookWeight, m_LookBodyWeight, m_LookHeadWeight, m_LookEyesWeight);
        }

        /// <summary>
        /// Call from AI/behaviour logic to switch the run/walk blend (e.g. chase vs patrol).
        /// Ignored while AutoDetectRunState is enabled.
        /// </summary>
        public void SetRunning(bool isRunning)
        {
            m_IsRunning = isRunning;
        }

        private void UpdateRunState()
        {
            if (!m_AutoDetectRunState) return;

            float speedFraction = m_Agent.speed > 0f ? m_Agent.velocity.magnitude / m_Agent.speed : 0f;
            m_IsRunning = speedFraction >= m_RunSpeedFraction;
        }

        private void UpdateAnimator(bool isIdle)
        {
            // Normalize world-space agent speed into the 0-1 range CreatureMover's
            // AnimationHandler expects (it was originally fed a clamped input axis magnitude).
            float rawSpeed = m_Agent.velocity.magnitude;
            float targetVert = (!isIdle && m_Agent.speed > 0f)
                ? Mathf.Clamp01(rawSpeed / m_Agent.speed)
                : 0f;

            float targetState = m_IsRunning ? 1f : 0f;

            m_FlowVert = Mathf.MoveTowards(m_FlowVert, targetVert, m_InputFlow * Time.deltaTime);
            m_FlowState = Mathf.MoveTowards(m_FlowState, targetState, m_InputFlow * Time.deltaTime);
            
            m_Animator.SetFloat(m_AnimIDVert, m_FlowVert);
            m_Animator.SetFloat(m_AnimIDState, m_FlowState);
        }

        private void UpdateRotation(bool isIdle)
        {
            if (!m_Agent.isOnNavMesh || isIdle) return;

            Vector3 desiredDir = transform.forward;
            float speed = m_Agent.velocity.magnitude;

            if (speed > m_MoveThreshold)
            {
                desiredDir = m_Agent.velocity.normalized;
            }
            else
            {
                Vector3 toSteering = m_Agent.steeringTarget - transform.position;
                if (toSteering.sqrMagnitude > m_SteeringDeadZone * m_SteeringDeadZone)
                {
                    desiredDir = toSteering.normalized;
                }
                else
                {
                    return; // steeringTarget too close/noisy to trust - hold rotation
                }
            }

            if (desiredDir.sqrMagnitude < 0.0001f) return;

            Quaternion targetRot = Quaternion.LookRotation(desiredDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * m_RotationSpeed);
        }
    }
}