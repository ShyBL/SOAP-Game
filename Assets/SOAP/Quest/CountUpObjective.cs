using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Objective that counts events toward an integer target.
    /// Complete when Counter.currentValue >= Target.
    ///
    /// MQ_001 examples:
    ///   SurvivorsRescued — Counter: SurvivorsRescued_Int, Target: 6,  Topic: SurvivorRescued
    ///   EnemiesKilled    — Counter: EnemiesKilled_Int,    Target: 10, Topic: EnemyKilled
    ///   EchoesFound      — Counter: EchoesFound_Int,      Target: 3,  Topic: EchoDiscovered
    /// </summary>
    [CreateAssetMenu(fileName = "New_CountUpObjective", menuName = "SOAP/Quest/Count Up Objective")]
    public class CountUpObjective : QuestObjective
    {
        [Header("Count Up")]
        [Tooltip("The IntVariable to increment each time TopicTrigger fires.")]
        public IntVariable Counter;
 
        [Tooltip("Counter must reach or exceed this value for the objective to be complete.")]
        public int Target;
 
        public override void OnEventFired() => Counter.Add(1);
 
        public override bool IsComplete => Counter.currentValue >= Target;
 
        protected override void OnValidate()
        {
            base.OnValidate();
 
            if (Counter == null)
                Debug.LogWarning($"[CountUpObjectiveSO] '{name}': Counter is not assigned.", this);
            if (Target <= 0)
                Debug.LogWarning($"[CountUpObjectiveSO] '{name}': Target is {Target}. Should be > 0.", this);
        }
    }
}