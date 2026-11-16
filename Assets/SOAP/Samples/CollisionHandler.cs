using UnityEngine;
using UnityEngine.Events;

namespace SOAP
{
    public abstract class CollisionHandler : MonoBehaviour
    {
        public PhysicsDisplayMode displayMode = PhysicsDisplayMode.TriggerOnly;
        
        [Header("Filtering")]
        public bool useLayerFilter = false;
        public LayerMask targetLayers;
        public bool doOnce = false;
        private bool hasFired = false;
        
        [Header("Local Events")] 
        public UnityEvent onEnter;
        public UnityEvent onStay;
        public UnityEvent onExit;
        
        private bool IsValidLayer(GameObject obj)
        {
            if (!useLayerFilter) return true;
            return (targetLayers.value & (1 << obj.layer)) > 0;
        }
        
        protected abstract void ApplyEffect(GameObject other);

        /// <summary>
        /// Fires once per physical range entry, independent of ApplyEffect,
        /// doOnce, and the onEnter UnityEvent. Always called regardless of
        /// doOnce state — intended for range/proximity tracking (e.g.
        /// registering an IInteractable) rather than gameplay effects.
        ///
        /// This exists because Process (below) calls ApplyEffect from BOTH
        /// OnTriggerEnter and OnTriggerExit — fine for one-shot triggers
        /// gated by doOnce, but unsafe for anything that needs to distinguish
        /// "entered range" from "left range" without a latch. Override this
        /// instead of relying on ApplyEffect for that purpose.
        ///
        /// Default implementation does nothing — existing subclasses that
        /// don't override this are completely unaffected.
        /// </summary>
        protected virtual void OnRangeEnter(GameObject other) { }

        /// <summary>
        /// Fires once per physical range exit — the counterpart to
        /// OnRangeEnter. Always called regardless of doOnce state.
        /// Default implementation does nothing.
        /// </summary>
        protected virtual void OnRangeExit(GameObject other) { }

        #region Physics Callbacks
        private void OnTriggerEnter(Collider other)
        {
            OnRangeEnter(other.gameObject);
            if (displayMode != PhysicsDisplayMode.CollisionOnly) Process(other.gameObject, onEnter);
        }

        private void OnTriggerExit(Collider other)
        {
            OnRangeExit(other.gameObject);
            if (displayMode != PhysicsDisplayMode.CollisionOnly) Process(other.gameObject, onExit);
        }

        private void OnCollisionEnter(Collision col)
        {
            OnRangeEnter(col.gameObject);
            if (displayMode != PhysicsDisplayMode.TriggerOnly) Process(col.gameObject, onEnter);
        }

        private void OnCollisionExit(Collision col)
        {
            OnRangeExit(col.gameObject);
            if (displayMode != PhysicsDisplayMode.TriggerOnly) Process(col.gameObject, onExit);
        }
        
        private void Process(GameObject other, UnityEvent evt)
        {
            if (!IsValidLayer(other)) return;
            if (doOnce && hasFired) return;

            ApplyEffect(other);
            evt?.Invoke();

            if (doOnce) hasFired = true;
        }
        #endregion
    }
}
