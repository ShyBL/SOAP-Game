using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Objective that sets a BoolVariable to true once.
    /// Complete when Flag.currentValue == true.
    ///
    /// MQ_001 examples:
    ///   HeartCollected — Flag: HeartOfGateCollected_Bool, Topic: QuestItemPickedUp
    ///   BossDefeated   — Flag: BossDefeated_Bool,          Topic: BossDefeated
    ///   PortalSealed   — Flag: PortalEntered_Bool,         Topic: PortalSealed
    /// </summary>
    [CreateAssetMenu(fileName = "New_BoolFlagObjective", menuName = "SOAP/Quest/Bool Flag Objective")]
    public class BoolFlagObjective : QuestObjective
    {
        [Header("Bool Flag")]
        [Tooltip("The BoolVariable to set true when TopicTrigger fires.")]
        public BoolVariable Flag;
 
        public override void OnEventFired() => Flag.Set(true);
 
        public override bool IsComplete => Flag.currentValue;
 
        protected override void OnValidate()
        {
            base.OnValidate();
 
            if (Flag == null)
                Debug.LogWarning($"[BoolFlagObjectiveSO] '{name}': Flag is not assigned.", this);
        }
    }
}