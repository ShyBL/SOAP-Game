using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Region marker — represents an area rather than a point (e.g. "survivors
    /// somewhere in this district"). Minimap renders a circle of Radius around
    /// WorldPosition; compass widens its indicator instead of showing a pinpoint.
    /// Not used by anything yet — boilerplate for when survivor discovery moves
    /// from per-instance markers to area hints.
    /// </summary>
    public class MapAreaMarker : MapMarker
    {
        [SerializeField] private float radius = 10f;
        public float Radius => radius;
    }
}