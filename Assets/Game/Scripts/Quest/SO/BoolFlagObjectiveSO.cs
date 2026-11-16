using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Objective that sets a BoolVariable to true once.
    /// Complete when Flag.currentValue == true.
    ///
    /// MQ_001 examples:
    ///   HeartCollected — Flag: HeartOfGateCollected_Bool, EventId: heart_collected
    ///   BossDefeated   — Flag: BossDefeated_Bool,          EventId: boss_defeated
    ///   PortalSealed   — Flag: PortalEntered_Bool,         EventId: portal_sealed
    /// </summary>
    [CreateAssetMenu(fileName = "New_BoolFlagObjective", menuName = "SOAP/Quest/Bool Flag Objective")]
    public class BoolFlagObjectiveSO : QuestObjectiveSO
    {
        [Header("Bool Flag")]
        [Tooltip("The BoolVariable to set true when EventId fires.")]
        [SOCreate]
        public BoolVariable Flag;

        public override void OnEventFired() => Flag.Set(true);

        public override bool IsComplete => Flag.currentValue;

        public override string ProgressLabel => Flag.currentValue ? "Complete" : "Incomplete";

        public override string SaveValue => Flag.currentValue.ToString();

        public override void LoadValue(string savedValue)
        {
            if (bool.TryParse(savedValue, out var value))
                Flag.Set(value);
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            if (Flag == null)
                Debug.LogWarning($"[BoolFlagObjectiveSO] '{name}': Flag is not assigned.", this);
        }
    }
}
