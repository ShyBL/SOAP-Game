using System.Collections.Generic;
using UnityEngine;

namespace Npc.Movement
{
    /// <summary>
    /// A queryable set of PointOfInterests representing one logical area.
    /// Also defines a wander zone used by RandomWanderStrategySO via WanderRadius.
    /// The two can coexist — a camp can have named points for duty-driven NPCs
    /// and a wander radius for idle ones, sharing the same PointArea.
    /// </summary>
    public class PointArea : MonoBehaviour
    {
        [SerializeField] private List<PointOfInterest> points = new List<PointOfInterest>();

        [SerializeField, Tooltip("Radius of the wander zone centered on this Transform. Used by RandomWanderStrategySO. Has no effect on FlagDrivenRouteSO.")]
        private float wanderRadius;

        public float WanderRadius => wanderRadius;

        /// <summary>
        /// Returns a random point matching the tag, or null if none found.
        /// Multiple points sharing the same tag are intentional (three trees all
        /// tagged "Forest") — a different one is picked each time.
        /// </summary>
        public PointOfInterest GetPointByTag(string tag)
        {
            List<PointOfInterest> matches = GetPointsByTags(new string[] { tag });

            if (matches.Count == 0)
            {
                Debug.LogWarning($"PointArea '{name}': No point found with tag '{tag}'.", this);
                return null;
            }

            return matches[Random.Range(0, matches.Count)];
        }

        /// <summary>
        /// Returns all points matching any of the given tags.
        /// Pass null or empty to return every point in the area.
        /// </summary>
        public List<PointOfInterest> GetPointsByTags(string[] tags)
        {
            List<PointOfInterest> matches = new List<PointOfInterest>();

            for (int i = 0; i < points.Count; i++)
            {
                PointOfInterest point = points[i];

                if (point == null)
                {
                    continue;
                }

                if (tags == null || tags.Length == 0)
                {
                    matches.Add(point);
                    continue;
                }

                for (int j = 0; j < tags.Length; j++)
                {
                    if (point.PointTag == tags[j])
                    {
                        matches.Add(point);
                        break;
                    }
                }
            }

            return matches;
        }

        [ContextMenu("Auto-Populate From Children")]
        private void AutoPopulateFromChildren()
        {
            points.Clear();
            PointOfInterest[] childPoints = GetComponentsInChildren<PointOfInterest>(true);
            for (int i = 0; i < childPoints.Length; i++)
            {
                points.Add(childPoints[i]);
            }
            Debug.Log($"PointArea '{name}': populated {points.Count} points from children.");
        }

        private void OnDrawGizmos()
        {
            if (wanderRadius <= 0f)
            {
                return;
            }

            // Wander zone — draw as a flat disc on the XZ plane
            Gizmos.color = new Color(0.4f, 1f, 0.4f, 0.15f);
            DrawFlatDisc(transform.position, wanderRadius, 32);

            Gizmos.color = new Color(0.4f, 1f, 0.4f, 0.6f);
            DrawFlatDiscOutline(transform.position, wanderRadius, 32);
        }

        private void DrawFlatDisc(Vector3 center, float radius, int segments)
        {
            float angleStep = 360f / segments;
            Vector3 prev = center + new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 next = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(center, next);
                prev = next;
            }
        }

        private void DrawFlatDiscOutline(Vector3 center, float radius, int segments)
        {
            float angleStep = 360f / segments;
            Vector3 prev = center + new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 next = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
    }
}
