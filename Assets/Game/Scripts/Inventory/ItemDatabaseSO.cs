using System;
using System.Collections.Generic;
using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Resolves a saved ItemId back to its asset. Saves store ids, not asset
    /// references, so InventorySO and EquipmentSO need this to load.
    ///
    /// Setup: create one asset, then right-click its header in the inspector
    /// and run "Collect All Items In Project". Re-run whenever you add items.
    /// Duplicate or missing ids are reported in the console.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "SOAP/Items/Item Database")]
    public class ItemDatabaseSO : ScriptableObject
    {
        [SerializeField] private ItemDefinitionSO[] items;

        [NonSerialized] private Dictionary<string, ItemDefinitionSO> lookup;

        public IReadOnlyList<ItemDefinitionSO> Items => items;

        public ItemDefinitionSO FindById(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;

            if (lookup == null)
                BuildLookup();

            lookup.TryGetValue(itemId, out ItemDefinitionSO item);
            return item;
        }

        private void OnEnable()
        {
            lookup = null;
        }

        private void BuildLookup()
        {
            lookup = new Dictionary<string, ItemDefinitionSO>();

            if (items == null) return;

            foreach (ItemDefinitionSO item in items)
            {
                if (item == null) continue;

                if (string.IsNullOrEmpty(item.ItemId))
                {
                    Debug.LogWarning($"[ItemDatabaseSO] '{item.name}' has an empty ItemId — skipped.", this);
                    continue;
                }

                if (lookup.ContainsKey(item.ItemId))
                {
                    Debug.LogWarning($"[ItemDatabaseSO] Duplicate ItemId on '{item.name}' — skipped. " +
                                     "Clear the ItemId field on the duplicated asset.", this);
                    continue;
                }

                lookup.Add(item.ItemId, item);
            }
        }

        private void OnValidate()
        {
            lookup = null;
        }

#if UNITY_EDITOR
        [ContextMenu("Collect All Items In Project")]
        private void CollectAllItems()
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ItemDefinitionSO");
            var found = new List<ItemDefinitionSO>(guids.Length);

            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                ItemDefinitionSO asset = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemDefinitionSO>(path);
                if (asset != null) found.Add(asset);
            }

            items = found.ToArray();
            lookup = null;
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[ItemDatabaseSO] Collected {items.Length} items.", this);
        }
#endif
    }
}
