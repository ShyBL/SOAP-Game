/// <summary>
/// Plain C# class that gets serialized to disk via JSON.
/// Mirrors every field from PlayerData and GameData that must survive between sessions.
/// </summary>
[System.Serializable]
public class SavedData
{
    // Character (from PlayerData)
    public int currentLevel = 1;
        
    // Audio settings (from GameData)
    public float masterVolume  = 80f;
    public float backgroundVolume = 100f;
}