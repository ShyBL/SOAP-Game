using Cinemachine;
using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Blends to a dedicated NPC camera on interaction and blends back when
    /// it ends. Reused by both the offer flow (QuestOfferUIController) and
    /// the turn-in flow (QuestTurnInUIController) for the same NPC — one
    /// camera, two independent open/close triggers.
    ///
    /// Works by swapping Cinemachine virtual camera priority. Cinemachine's
    /// brain handles the blend automatically once priority changes — no
    /// manual lerping needed.
    ///
    /// Inspector setup:
    ///   npcCamera        → the CinemachineVirtualCamera for this NPC's shot
    ///   defaultPriority  → priority when inactive (should lose to gameplay cam)
    ///   activePriority   → priority when active (should win the blend)
    /// </summary>
    public class NpcInteractionCamera : MonoBehaviour
    {
        [SerializeField] private CinemachineVirtualCamera npcCamera;
        [SerializeField] private int defaultPriority = 0;
        [SerializeField] private int activePriority = 20;

        private void OnValidate()
        {
            if (npcCamera == null)
                Debug.LogError($"[NpcInteractionCamera] NpcCamera is not assigned on {name}!", this);
        }

        private void Awake()
        {
            if (npcCamera != null)
                npcCamera.Priority = defaultPriority;
        }

        /// <summary>
        /// Raises this camera's priority so Cinemachine's brain blends to it.
        /// Call when an interaction panel opens (offer shown, turn-in shown).
        /// </summary>
        public void Activate()
        {
            if (npcCamera != null)
                npcCamera.Priority = activePriority;
        }

        /// <summary>
        /// Restores default priority so Cinemachine's brain blends back to
        /// whatever camera normally wins (typically the player follow cam).
        /// Call when the interaction panel closes (accept, decline, or
        /// turn-in continue).
        /// </summary>
        public void Deactivate()
        {
            if (npcCamera != null)
                npcCamera.Priority = defaultPriority;
        }
    }
}
