using UnityEngine;
using UnityEngine.Events;

namespace Npc.Movement
{
    /// <summary>
    /// A named destination in a PointArea. The tag is plain text set in the
    /// inspector ("Forest", "Fire", "CommandTent") — domain meaning lives in
    /// data, not in code. Multiple points can share the same tag; GetPointByTag
    /// will pick one at random among matches (e.g. three different trees all
    /// tagged "Forest").
    /// </summary>
    public class PointOfInterest : MonoBehaviour
    {
        [SerializeField] private string pointTag;

        [SerializeField, Tooltip("Seconds the NPC dwells here before asking for the next decision.")]
        private float dwellSeconds = 2f;

        [SerializeField, Tooltip("Cosmetic/gameplay hook - play an animation, spawn a particle, etc. " +
                                  "State changes (flags) are handled by the strategy's RouteDecision, not here.")]
        private UnityEvent onArrival;

        public string PointTag => pointTag;
        public float DwellSeconds => dwellSeconds;

        public void TriggerArrival() => onArrival.Invoke();

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, 0.35f);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, pointTag);
#endif
        }
    }
}
