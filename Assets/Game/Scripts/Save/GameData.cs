using UnityEngine;

[CreateAssetMenu(fileName = "GameData",menuName = "GameSO/Game Data")]
public class GameData : ScriptableObject
{
    [Header("Audio Settings")] 
    [Range(1f, 100f)]
    public float masterVolume = 80;
    [Range(1f, 100f)]
    public float backgroundVolume = 100;
}