    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Newtonsoft.Json;
    using UnityEngine;

    /// <summary>
    /// Handles all disk I/O
    /// Call SaveSystem.instance.Save() anywhere a change occurs;
    /// settings changed, level completed, etc.
    /// GameManager.Awake() calls Load() automatically.
    /// SOs (PlayerData / GameData) remain the live in-memory source
    /// of truth during a session — this class is only I/O.
    /// </summary>
    [DefaultExecutionOrder(-100)] 
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager instance;

        [SerializeField] private PlayerData playerData;
        [SerializeField] private GameData   gameData;

        private static string SavePath =>
            Path.Combine(Application.persistentDataPath, "Game.save");

        
        #region Unity

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        #endregion
        
        #region Public API

        /// <summary>
        /// Reads the save file and pushes values into the ScriptableObjects.
        /// Call once on startup (GameManager.Awake).
        /// </summary>
        public void Load()
        {
            try
            {
                if (!File.Exists(SavePath))
                {
                    Debug.Log("[SaveSystem] No save file found — using defaults.");
                    return;
                }

                string localJson = File.ReadAllText(SavePath);
                SavedData localData = JsonConvert.DeserializeObject<SavedData>(localJson);

                if (localData == null)
                {
                    Debug.LogWarning("[SaveSystem] Local save file was empty or malformed.");
                    return;
                }

                ApplyToPlayerData(localData);
                ApplyToGameData(localData);
                Debug.Log("[SaveSystem] Loaded from local disk.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Failed to load save file: {e.Message}");
            }
        }

        /// <summary>
        /// Reads current values from the ScriptableObjects and writes them to
        /// local disk, then mirrors the same bytes to Steam Cloud if available.
        /// Safe to call frequently — JSON serialization is fast for this payload.
        /// </summary>
        public void Save()
        {
            try
            {
                SavedData data = BuildFromScriptableObjects();
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(SavePath, json);
                Debug.Log($"[SaveSystem] Saved to local disk: {SavePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Failed to save: {e.Message}");
            }
        }

        /// <summary>
        /// Deletes the save file and resets both SOs to their default SO values.
        /// Useful for the "Reset Progression" developer button.
        /// </summary>
        public void DeleteSave()
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
                Debug.Log("[SaveSystem] Save file deleted.");
            }
            else
            {
                Debug.Log("[SaveSystem] No save file to delete.");
            }
        }

        #endregion
        
        #region Helpers
        
        private SavedData BuildFromScriptableObjects()
        {
            var d = new SavedData();

            // Character (PlayerData)
            d.currentLevel = playerData.currentLevel;

            // Audio (GameData)
            d.masterVolume     = gameData.masterVolume;
            d.backgroundVolume = gameData.backgroundVolume;

            return d;
        }
        private void ApplyToPlayerData(SavedData d)
        {
            playerData.currentLevel = d.currentLevel;
        }

        private void ApplyToGameData(SavedData d)
        {
            gameData.masterVolume     = d.masterVolume;
            gameData.backgroundVolume = d.backgroundVolume;
        }

        #endregion
    }