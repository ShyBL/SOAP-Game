using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SOAP
{
    /// <summary>
    /// Static top-down sprite behind a masked viewport. Player icon moves and
    /// rotates with facing; point markers plot from world position and stay
    /// fixed; area markers (MapAreaMarker) render as a circle of Radius.
    /// World→sprite conversion derives from the active Terrain's bounds — one
    /// terrain block, no manual calibration.
    /// </summary>
    public class MinimapController : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private RectTransform mapSprite;
        [SerializeField] private RectTransform playerIcon;
        [SerializeField] private RectTransform markerPrefab;
        [SerializeField] private RectTransform areaMarkerPrefab; // circle sprite; falls back to markerPrefab if unset
        [SerializeField] private RectTransform markerParent;

        private class MarkerIcon
        {
            public RectTransform Rect;
            public Image Image;
        }

        private Vector3 terrainOrigin;
        private Vector3 terrainSize;
        private readonly Dictionary<MapMarker, MarkerIcon> markerIcons = new Dictionary<MapMarker, MarkerIcon>();

        private void Awake()
        {
            var terrain = Terrain.activeTerrain;
            if (terrain == null)
            {
                Debug.LogError("[MinimapController] No active Terrain found.", this);
                enabled = false;
                return;
            }
            terrainOrigin = terrain.transform.position;
            terrainSize = terrain.terrainData.size;
        }

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

            playerIcon.anchoredPosition = UVToLocal(WorldToUV(player.position));
            playerIcon.localRotation = Quaternion.Euler(0f, 0f, -player.eulerAngles.y);

            foreach (var kvp in markerIcons)
            {
                kvp.Value.Rect.anchoredPosition = UVToLocal(WorldToUV(kvp.Key.WorldPosition));

                if (kvp.Key is MapAreaMarker areaMarker)
                {
                    float localRadius = WorldRadiusToLocal(areaMarker.Radius);
                    kvp.Value.Rect.sizeDelta = new Vector2(localRadius, localRadius) * 2f;
                }
                else if (kvp.Value.Image != null)
                {
                    kvp.Value.Image.sprite = kvp.Key.Icon;
                }
            }
        }

        private Vector2 WorldToUV(Vector3 worldPos) => new Vector2(
            Mathf.Clamp01((worldPos.x - terrainOrigin.x) / terrainSize.x),
            Mathf.Clamp01((worldPos.z - terrainOrigin.z) / terrainSize.z));

        private Vector2 UVToLocal(Vector2 uv)
        {
            Rect rect = mapSprite.rect;
            return new Vector2(rect.xMin + rect.width * uv.x, rect.yMin + rect.height * uv.y);
        }

        // Approximates using X scale; fine for a placeholder, revisit if terrain isn't square.
        private float WorldRadiusToLocal(float worldRadius) => (worldRadius / terrainSize.x) * mapSprite.rect.width;

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