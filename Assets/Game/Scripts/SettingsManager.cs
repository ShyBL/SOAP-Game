using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameData gameData;
    
    #region Audio Settings
    
    [Header("Audio Settings")] 
    [SerializeField]
    private Slider masterVolumeSlider;

    [SerializeField] private TextMeshProUGUI masterVolumeValueText;

    [SerializeField] private Slider backgroundVolumeSlider;
    [SerializeField] private TextMeshProUGUI backgroundVolumeValueText;

    private AudioManager _audioManager;

    private void Start()
    {
        _audioManager = AudioManager.instance;

        UpdateAudio(masterVolumeSlider, masterVolumeValueText, "MasterVolume", gameData.masterVolume);
        UpdateAudio(backgroundVolumeSlider, backgroundVolumeValueText, "BackgroundVolume",
            gameData.backgroundVolume);
    }

    private void UpdateAudio(Slider slider, TextMeshProUGUI label, string mixerParam, float savedValue)
    {
        LoadVolumeFromPlayerData(slider, mixerParam, savedValue);
        UpdateVolumeLabel(slider, label);

        slider.onValueChanged.AddListener(value => ApplyVolume(value, slider, label, mixerParam));
    }

    private void LoadVolumeFromPlayerData(Slider slider, string mixerParam, float savedValue)
    {
        var clamped = Mathf.Clamp(savedValue, 1f, 100f);
        slider.value = clamped;

        float dB = Mathf.Clamp(Mathf.Log10(clamped / 100f) * 20f, -60f, 0f);
        _audioManager.audioMixer.SetFloat(mixerParam, dB);
    }

    private void ApplyVolume(float value, Slider slider, TextMeshProUGUI label, string mixerParam)
    {
        float dB = Mathf.Clamp(Mathf.Log10(value / 100f) * 20f, -60f, 0f);
        _audioManager.audioMixer.SetFloat(mixerParam, dB);

        if (mixerParam == "MasterVolume")
            gameData.masterVolume = value;
        else if (mixerParam == "BackgroundVolume")
            gameData.backgroundVolume = value;

        Debug.Log($"Applied and saved {mixerParam}: {value} → {dB} dB");
        UpdateVolumeLabel(slider, label);

        SaveManager.instance.Save();
    }

    private void UpdateVolumeLabel(Slider slider, TextMeshProUGUI label)
    {
        if (label != null)
            label.text = $"{Mathf.RoundToInt(slider.value)}";
    }

    #endregion
}