using System.Collections.Generic;
using UnityEngine;

namespace Npc.Movement
{
    /// <summary>
    /// A queryable set of PointOfInterests representing one logical area (a
    /// camp, a dungeon room, a village quarter). Strategies ask this for
    /// points; they never reach into the scene directly.
    /// </summary>
    public class PointArea : MonoBehaviour
    {
        [SerializeField] private List<PointOfInterest> points = new List<PointOfInterest>();

        /// <summary>
        /// Returns a random point matching the tag, or null if none found.
        /// Multiple points with the same tag are intentional (e.g. three trees
        /// all tagged "Forest") — the NPC visits a different one each time.
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
    }
}
