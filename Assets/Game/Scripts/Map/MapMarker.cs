using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Generic world-position marker for minimap/compass. Attach to anything
    /// that should appear on both — survivors, items, generic POIs.
    /// NPCs use the NpcMapMarker subclass instead; regions use MapAreaMarker.
    /// </summary>
    public class MapMarker : MonoBehaviour
    {
        [SerializeField] private Sprite icon;
 
        public virtual Sprite Icon => icon;
        public Vector3 WorldPosition => transform.position;
 
        protected virtual void OnEnable()  => MapMarkerRegistry.Register(this);
        protected virtual void OnDisable() => MapMarkerRegistry.Unregister(this);
    }
}

