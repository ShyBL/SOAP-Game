using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Objective that counts events toward an integer target.
    /// Complete when Counter.currentValue >= Target.
    ///
    /// MQ_001 examples:
    ///   SurvivorsRescued — Counter: SurvivorsRescued_Int, Target: 6,  EventId: survivor_rescued
    ///   EnemiesKilled    — Counter: EnemiesKilled_Int,    Target: 10, EventId: enemy_killed
    ///   EchoesFound      — Counter: EchoesFound_Int,      Target: 3,  EventId: echo_discovered
    /// </summary>
    [CreateAssetMenu(fileName = "New_CountUpObjective", menuName = "SOAP/Quest/Count Up Objective")]
    public class CountUpObjectiveSO : QuestObjectiveSO
    {
        [Header("Count Up")]
        [Tooltip("The IntVariable to increment each time EventId fires.")]
        [SOCreate]
        public IntVariable Counter;

        [Tooltip("Counter must reach or exceed this value for the objective to be complete.")]
        public int Target;

        public override void OnEventFired() => Counter.Add(1);

        public override bool IsComplete => Counter.currentValue >= Target;

        public override string ProgressLabel => $"{Counter.currentValue} / {Target}";

        public override string SaveValue => Counter.currentValue.ToString();

        public override void LoadValue(string savedValue)
        {
            if (int.TryParse(savedValue, out var value))
                Counter.Set(value);
        }

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
