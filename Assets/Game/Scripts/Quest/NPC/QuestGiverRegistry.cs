using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Project-wide lookup table for all quest-giving NPCs.
    /// One asset lives in the project — treat it as a singleton SO.
    ///
    /// Designer setup — do this once, never again per NPC:
    ///   Create the asset via SOAP/Quest/Quest Giver Registry.
    ///   Drag every NpcQuestGiverSO into the NpcGivers array.
    ///   Wire this asset into any QuestGiverZone that needs to look up an NPC.
    ///
    /// When a new NPC is added to the game:
    ///   1. Create an NpcQuestGiverSO for them.
    ///   2. Add it here.
    ///   That is the complete setup — no scene changes required.
    ///
    /// QuestGiverFactory uses FindByNpcId() at runtime to resolve an NPC
    /// identifier string into its SO. The zones themselves only store a
    /// string NpcId, keeping scene-level inspector wiring minimal.
    /// </summary>
    [CreateAssetMenu(fileName = "QuestGiverRegistry", menuName = "SOAP/Quest/Quest Giver Registry")]
    public class QuestGiverRegistry : ScriptableObject
    {
        [Tooltip("All quest-giving NPCs in the project. " +
                 "Add one entry per NPC. Order does not matter — lookup is by NpcId.")]
        public NpcQuestGiver[] NpcGivers;
 
        /// <summary>
        /// Finds the NpcQuestGiverSO whose NpcId matches the given id.
        /// Returns null and logs a warning if no match is found.
        /// Case-sensitive — NpcId values must be consistent across assets and scene components.
        /// </summary>
        public NpcQuestGiver FindByNpcId(string npcId)
        {
            if (NpcGivers == null) return null;
 
            foreach (var giver in NpcGivers)
            {
                if (giver != null && giver.NpcId == npcId)
                    return giver;
            }
 
            Debug.LogWarning($"[QuestGiverRegistry] No NpcQuestGiverSO found for NpcId '{npcId}'. " +
                             "Check that the NpcId matches exactly and the SO is registered here.");
            return null;
        }
 
        private void OnValidate()
        {
            if (NpcGivers == null || NpcGivers.Length == 0)
                Debug.LogWarning($"[QuestGiverRegistry] '{name}': NpcGivers array is empty.", this);
        }
    }
}