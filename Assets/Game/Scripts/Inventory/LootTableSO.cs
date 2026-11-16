using System.Collections.Generic;
using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// What an enemy (or chest) can drop. Each entry rolls independently:
    /// Chance 1 = guaranteed, 0.1 = 10%. Count is picked between Min and Max.
    /// </summary>
    [CreateAssetMenu(fileName = "New_LootTable", menuName = "SOAP/Items/Loot Table")]
    public class LootTableSO : ScriptableObject
    {
        [System.Serializable]
        public struct LootEntry
        {
            public ItemDefinitionSO Item;

            [Min(1)] public int MinCount;
            [Min(1)] public int MaxCount;

            [Range(0f, 1f)] public float Chance;
        }

        public LootEntry[] Entries;

        /// <summary>Rolls every entry and appends what dropped to results.</summary>
        public void Roll(List<ItemStack> results)
        {
            if (Entries == null) return;

            for (int i = 0; i < Entries.Length; i++)
            {
                LootEntry entry = Entries[i];

                if (entry.Item == null) continue;
                if (entry.Chance <= 0f) continue;
                if (entry.Chance < 1f && Random.value > entry.Chance) continue;

                int min = Mathf.Max(1, entry.MinCount);
                int max = Mathf.Max(min, entry.MaxCount);

                results.Add(new ItemStack(entry.Item, Random.Range(min, max + 1)));
            }
        }

        private void OnValidate()
        {
            if (Entries == null) return;

            for (int i = 0; i < Entries.Length; i++)
            {
                if (Entries[i].MinCount < 1) Entries[i].MinCount = 1;
                if (Entries[i].MaxCount < Entries[i].MinCount) Entries[i].MaxCount = Entries[i].MinCount;
            }
        }
    }
}
