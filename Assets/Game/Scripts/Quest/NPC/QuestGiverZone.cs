using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Abstract mid-level base for all NPC quest interaction zones.
    /// Extends CollisionHandler — inherits layer filtering, doOnce, and
    /// the onEnter/onStay/onExit UnityEvents at no cost.
    ///
    /// Owns the two fields every quest giver interaction needs:
    ///   questChannel → QuestChannel SO (shared across all quest events)
    ///   npcId        → matches the NpcId on the target NpcQuestGiverSO
    ///   registry     → QuestGiverRegistry SO (project-wide NPC lookup)
    ///
    /// Concrete subclasses (QuestOfferZone, QuestTurnInZone) implement
    /// ApplyEffect and call QuestGiverFactory to resolve which quest is
    /// currently actionable. They never hold a direct QuestDefinitionSO
    /// reference — the factory queries live SOAP variable state at the
    /// moment of interaction.
    ///
    /// Inheritance tree:
    ///   CollisionHandler (abstract — existing)
    ///     └── QuestGiverZone (abstract — this file)
    ///           ├── QuestOfferZone  (concrete)
    ///           └── QuestTurnInZone (concrete)
    ///
    /// Inspector setup (on the NPC GameObject, set once):
    ///   questChannel → QuestChannel SO
    ///   npcId        → e.g. "captain_aldric"  (must match NpcQuestGiverSO.NpcId)
    ///   registry     → QuestGiverRegistry SO
    ///   doOnce       → false  (player can interact repeatedly — each subclass
    ///                  guards against redundant state changes internally)
    /// </summary>
    public abstract class QuestGiverZone : CollisionHandler
    {
        [Header("Quest Giver")]
        [SerializeField] protected GameChannel questChannel;

        [Tooltip("Must match NpcQuestGiverSO.NpcId exactly. Case-sensitive.")]
        [SerializeField] protected string npcId;

        [SerializeField] protected QuestGiverRegistry registry;

        // Resolved once on first access, cached for the lifetime of the component.
        private NpcQuestGiver _resolvedNpc;

        /// <summary>
        /// Lazily resolves and caches the NpcQuestGiverSO for this zone's npcId.
        /// Subclasses read this property inside ApplyEffect.
        /// Returns null if the registry lookup fails — ApplyEffect should guard on null.
        /// </summary>
        protected NpcQuestGiver Npc
        {
            get
            {
                if (_resolvedNpc == null)
                    _resolvedNpc = registry?.FindByNpcId(npcId);
                return _resolvedNpc;
            }
        }

        private void OnValidate()
        {
            if (questChannel == null)
                Debug.LogError($"[{GetType().Name}] QuestChannel is not assigned on {name}!", this);
            if (string.IsNullOrEmpty(npcId))
                Debug.LogWarning($"[{GetType().Name}] NpcId is empty on {name}. " +
                                 "Set it to match NpcQuestGiverSO.NpcId.", this);
            if (registry == null)
                Debug.LogError($"[{GetType().Name}] QuestGiverRegistry is not assigned on {name}!", this);
        }
    }
    
    /// <summary>
    /// Place on the NPC's offer trigger collider (or the NPC GameObject itself).
    /// Handles the moment the player accepts a quest from this NPC.
    ///
    /// Inspector setup:
    ///   questChannel → QuestChannel SO
    ///   npcId        → e.g. "captain_aldric"
    ///   registry     → QuestGiverRegistry SO
    ///   doOnce       → false  (player may approach multiple times;
    ///                          the factory guard handles the real gating)
    ///   onEnter      → wire offer dialogue, quest UI popup, etc.
    ///                  Fires automatically from base class after ApplyEffect.
    ///
    /// Flow when Player enters the zone:
    ///   1. QuestGiverFactory queries the NPC's quest list for the first
    ///      offerable quest (QuestActive == false).
    ///   2. If none found — all quests already offered — silently returns.
    ///      Wire a "no quest available" dialogue to onEnter if you want
    ///      feedback in that case; ApplyEffect returns before the channel raise
    ///      so you can distinguish the two cases via a separate BoolVariable flag.
    ///   3. QuestActive is set to true → QuestManager begins routing events
    ///      for this quest's objectives immediately.
    ///   4. QuestOffered is raised on the channel → wire dialogue trigger,
    ///      journal update, HUD pop-in via a TopicResponder elsewhere.
    /// </summary>
    public class QuestOfferZone : QuestGiverZone
    {
        protected override void ApplyEffect(GameObject other)
        {
            if (!other.CompareTag("Player")) return;

            var quest = QuestGiverFactory.GetNextOfferable(Npc);
            if (quest == null) return;      // nothing to offer right now

            quest.QuestActive.Set(true);
            questChannel.Raise(GameTopic.QuestOffered, quest);
        }
    }
    
     /// <summary>
    /// Place on the NPC's turn-in trigger collider (or the same NPC GameObject).
    /// Handles the moment the player returns to hand in a completed quest.
    ///
    /// Inspector setup:
    ///   questChannel → QuestChannel SO
    ///   npcId        → e.g. "captain_aldric"  (same value as QuestOfferZone)
    ///   registry     → QuestGiverRegistry SO
    ///   doOnce       → false  (player may re-approach; TurnedIn flag gates re-trigger)
    ///   onEnter      → wire turn-in dialogue, completion fanfare, etc.
    ///                  Fires automatically from base class after ApplyEffect.
    ///
    /// Flow when Player enters the zone:
    ///   1. QuestGiverFactory queries the NPC's quest list for the first
    ///      quest where QuestActive == true, QuestComplete == true,
    ///      and TurnedIn == false.
    ///   2. If none found — nothing ready to hand in — silently returns.
    ///      The player hasn't finished a quest yet, or already turned it in.
    ///   3. TurnedIn is set to true → prevents this quest being handed
    ///      in a second time regardless of how many times the player
    ///      re-enters the collider.
    ///   4. QuestTurnedIn is raised on the channel with the quest as data.
    ///      RewardDispenser (or any TopicResponder) hears this and grants rewards.
    ///
    /// Note — QuestCompleted vs QuestTurnedIn:
    ///   QuestCompleted fires when all objectives are satisfied (raised by QuestManager).
    ///   QuestTurnedIn  fires when the player physically returns to the NPC.
    ///   Wire RewardDispenser to QuestTurnedIn if rewards should require the
    ///   return trip. Wire it to QuestCompleted if rewards are instant on finish.
    /// </summary>
    public class QuestTurnInZone : QuestGiverZone
    {
        protected override void ApplyEffect(GameObject other)
        {
            if (!other.CompareTag("Player")) return;
 
            var quest = QuestGiverFactory.GetPendingTurnIn(Npc);
            if (quest == null) return;      // nothing ready to turn in
 
            quest.TurnedIn.Set(true);
            questChannel.Raise(GameTopic.QuestTurnedIn, quest);
        }
    }
}