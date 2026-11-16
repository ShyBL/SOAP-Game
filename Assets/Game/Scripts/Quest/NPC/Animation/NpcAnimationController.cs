using UnityEngine;
using UnityEngine.AI;

namespace Npc.Movement
{
    public class NpcAnimationController : MonoBehaviour
    {
        [Header("Rotation")]
        [Tooltip("How fast the NPC rotates to face movement/target")]
        [SerializeField] private float rotationSpeed = 8f;

        [Tooltip("Velocity threshold to consider the NPC moving")]
        [SerializeField] private float moveThreshold = 0.1f;

        [Tooltip("When within this extra distance, prefer facing the steering target")]
        [SerializeField] private float steeringFaceDistance = 0.5f;
        
        private Animator _animator;
        private NavMeshAgent _agent;
        
        private float _animationBlend;
        private int _animIDSpeed;
        private int _animIDMotionSpeed;
        
        private bool _hasAnimator;
        private bool _hasAgent;
        
        private void Start()
        {
            _hasAnimator = TryGetComponent(out _animator);
            _hasAgent = TryGetComponent(out _agent);
            _agent.updateRotation = false;
            
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);
            _hasAgent = TryGetComponent(out _agent);
            
            if (_hasAnimator && _hasAgent)
            {
                UpdateAnimator();
                UpdateRotation();
            }
        }

        private void UpdateAnimator()
        {
            float targetSpeed = _agent.velocity.magnitude;
            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * 10f);
            float motionSpeed = targetSpeed > 0.1f ? 1f : 0f;
            
            _animator.SetFloat(_animIDSpeed, _animationBlend);
            _animator.SetFloat(_animIDMotionSpeed, motionSpeed);
        }
        
        private void UpdateRotation()
        {
            // Decide desired facing direction
            Vector3 desiredDir = transform.forward;

            float speed = _agent.velocity.magnitude;

            if (speed > moveThreshold)
            {
                // Face movement direction while moving
                desiredDir = _agent.velocity.normalized;
            }
            else
            {
                // When nearly stopped, face the steering target (so it turns to face destination)
                Vector3 toSteering = _agent.steeringTarget - transform.position;
                if (toSteering.sqrMagnitude > 0.001f)
                {
                    // If close to destination, prefer steering target only if it's meaningful
                    if (_agent.remainingDistance <= _agent.stoppingDistance + steeringFaceDistance)
                        desiredDir = toSteering.normalized;
                    else
                        desiredDir = toSteering.normalized;
                }
            }

            // Avoid zero-length look vector
            if (desiredDir.sqrMagnitude < 0.0001f) return;

            Quaternion targetRot = Quaternion.LookRotation(desiredDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
        }
        
        private void OnFootstep(AnimationEvent animationEvent)
        {
            // NPCs don't use footstep sounds
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            // NPCs don't use landing sounds
        }
    }
}