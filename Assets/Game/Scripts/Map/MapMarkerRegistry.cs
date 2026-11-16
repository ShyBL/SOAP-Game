using System;
using System.Collections.Generic;

namespace SOAP
{
    /// <summary>
    /// Static registry of active MapMarkers. Minimap/compass subscribe to the
    /// add/remove events instead of polling — decoupled from whatever spawns
    /// or despawns markers.
    /// </summary>
    public static class MapMarkerRegistry
    {
        private static readonly List<MapMarker> markers = new List<MapMarker>();
 
        public static IReadOnlyList<MapMarker> Markers => markers;
        public static event Action<MapMarker> OnMarkerRegistered;
        public static event Action<MapMarker> OnMarkerUnregistered;
 
        public static void Register(MapMarker marker)
        {
            if (marker == null || markers.Contains(marker)) return;
            markers.Add(marker);
            OnMarkerRegistered?.Invoke(marker);
        }
 
        public static void Unregister(MapMarker marker)
        {
            if (marker == null) return;
            if (markers.Remove(marker))
                OnMarkerUnregistered?.Invoke(marker);
        }
    }
}