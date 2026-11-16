using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SOAP
{
    /// <summary>
    /// Top-screen compass. Each point marker is placed by its signed angular
    /// delta from the player's facing — continuous across 0°/360°, no
    /// scrolling content rect needed. Area markers (MapAreaMarker) widen
    /// instead of showing a pinpoint. A RectMask2D on the viewport clips edge
    /// overlap.
    /// </summary>
    public class CompassController : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private RectTransform markerPrefab;
        [SerializeField] private RectTransform areaMarkerPrefab;
        [SerializeField] private RectTransform markerParent;
        [SerializeField] private float fieldOfViewDegrees = 90f;
        [SerializeField] private float pixelsPerDegree = 6f;

        private class MarkerIcon
        {
            public RectTransform Rect;
            public Image Image;
        }

        private readonly Dictionary<MapMarker, MarkerIcon> markerIcons = new Dictionary<MapMarker, MarkerIcon>();

        private void OnEnable()
        {
            MapMarkerRegistry.OnMarkerRegistered += HandleRegistered;
            MapMarkerRegistry.OnMarkerUnregistered += HandleUnregistered;
            foreach (var marker in MapMarkerRegistry.Markers) HandleRegistered(marker);
        }

        private void OnDisable()
        {
            MapMarkerRegistry.OnMarkerRegistered -= HandleRegistered;
            MapMarkerRegistry.OnMarkerUnregistered -= HandleUnregistered;
        }

        private void Update()
        {
            if (player == null) return;
            float playerYaw = player.eulerAngles.y;

            foreach (var kvp in markerIcons)
            {
                Vector3 toMarker = kvp.Key.WorldPosition - player.position;
                float bearing = Mathf.Atan2(toMarker.x, toMarker.z) * Mathf.Rad2Deg;
                float delta = Mathf.DeltaAngle(playerYaw, bearing);

                bool visible = Mathf.Abs(delta) <= fieldOfViewDegrees * 0.5f;
                kvp.Value.Rect.gameObject.SetActive(visible);
                if (!visible) continue;

                kvp.Value.Rect.anchoredPosition = new Vector2(delta * pixelsPerDegree, 0f);

                if (kvp.Key is MapAreaMarker areaMarker)
                {
                    float distance = toMarker.magnitude;
                    float halfAngle = distance > areaMarker.Radius
                        ? Mathf.Asin(Mathf.Clamp01(areaMarker.Radius / distance)) * Mathf.Rad2Deg
                        : 90f;   // player standing inside the area
                    kvp.Value.Rect.sizeDelta = new Vector2(halfAngle * 2f * pixelsPerDegree, kvp.Value.Rect.sizeDelta.y);
                }
                else if (kvp.Value.Image != null)
                {
                    kvp.Value.Image.sprite = kvp.Key.Icon;
                }
            }
        }

        private void HandleRegistered(MapMarker marker)
        {
            if (markerIcons.ContainsKey(marker)) return;
            RectTransform prefab = marker is MapAreaMarker && areaMarkerPrefab != null ? areaMarkerPrefab : markerPrefab;
            var rect = Instantiate(prefab, markerParent);
            rect.TryGetComponent(out Image image);
            markerIcons[marker] = new MarkerIcon { Rect = rect, Image = image };
        }

        private void HandleUnregistered(MapMarker marker)
        {
            if (!markerIcons.TryGetValue(marker, out var icon)) return;
            Destroy(icon.Rect.gameObject);
            markerIcons.Remove(marker);
        }
    }
}