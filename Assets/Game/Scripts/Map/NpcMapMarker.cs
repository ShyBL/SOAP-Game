using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// NPC-specific marker. Resolves its NpcQuestGiver by npcId via the same
    /// registry QuestGiverZone uses, and swaps icon by quest state. Wired
    /// automatically by NpcSceneSetup.SpawnNpcInScene.
    ///
    /// Npc is abstract with subtypes beyond NpcQuestGiver, so this falls back
    /// to defaultIcon for a resolved Npc that isn't a quest giver (e.g. a
    /// purely decorative or wandering NPC) rather than special-casing it.
    /// </summary>
    public class NpcMapMarker : MapMarker
    {
        [SerializeField] private string npcId;
        [SerializeField] private QuestGiverRegistry registry;
 
        [Header("Quest State Icons")]
        [SerializeField] private Sprite offerableIcon;
        [SerializeField] private Sprite turnInIcon;
        [SerializeField] private Sprite defaultIcon;
 
        private NpcQuestGiver resolvedNpc;
 
        private void Awake()
        {
            if (registry != null)
                resolvedNpc = registry.FindByNpcId(npcId);
        }
 
        public override Sprite Icon
        {
            get
            {
                if (resolvedNpc != null)
                {
                    if (resolvedNpc.HasOfferableQuest) return offerableIcon;
                    if (resolvedNpc.HasPendingTurnIn) return turnInIcon;
                }
                return defaultIcon;
            }
        }
    }
}