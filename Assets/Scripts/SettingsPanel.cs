using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsPanel : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Toggle musicToggle;
    [SerializeField] private TMP_Dropdown difficultyDropdown;
    [SerializeField] private Button saveButton;
    [SerializeField] private ScreenManager screenManager;

    [Header("Audio")]
    [SerializeField] private AudioSource backgroundMusic; // assign your AudioSource here

    [Header("Screen Manager")]
    [SerializeField] private int modeIndex = 1; // index of the panel to return to (e.g., choose game)

    private readonly string[] difficulties = { "Easy", "Medium", "Hard", "Expert", "Master", "Extreme" };

    private void Start()
    {
        Debug.Log("SettingsPanel Start()");
        // Populate dropdown
        difficultyDropdown.ClearOptions();
        difficultyDropdown.AddOptions(new System.Collections.Generic.List<string>(difficulties));

        // Load current settings
        if (Settings.Instance == null)
        {
            Debug.LogError("Settings.Instance is NULL in Start()!");
            return;
        }

        // Load current settings
        musicToggle.isOn = Settings.Instance.isMusicOn;
        
        int index = System.Array.IndexOf(difficulties, Settings.Instance.difficulty);
        difficultyDropdown.value = index >= 0 ? index : 0;

        ApplyMusicState();
        Debug.Log($"Loading setting: isMusicOn = {Settings.Instance.isMusicOn}. Setting toggle to {musicToggle.isOn}");
        // Listeners
        musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);
        difficultyDropdown.onValueChanged.AddListener(OnDifficultyChanged);
        saveButton.onClick.AddListener(SaveSettings);
    }

    private void OnMusicToggleChanged(bool value)
    {
        Debug.Log($"OnMusicToggleChanged called with value: {value}");
        
        if (Settings.Instance == null)
        {
            Debug.LogError("Settings.Instance is NULL in OnMusicToggleChanged()!");
            return;
        }
        Settings.Instance.SetMusic(value);
        Debug.Log($"Settings.Instance.isMusicOn is now: {Settings.Instance.isMusicOn}");
        ApplyMusicState();
    }

    private void ApplyMusicState()
    {
        if (backgroundMusic == null)
        {
            Debug.LogError("backgroundMusic AudioSource is NULL!");
            return;
        }
        Debug.Log($"ApplyMusicState called. Target state (isMusicOn): {Settings.Instance.isMusicOn}. AudioSource currently playing: {backgroundMusic.isPlaying}");
        if (Settings.Instance.isMusicOn)
        {
            if (!backgroundMusic.isPlaying)
            {
                Debug.Log("Setting is ON and music is NOT playing. Calling backgroundMusic.Play()");
                backgroundMusic.Play();
                
                // Add a check after trying to play
                if (!backgroundMusic.isPlaying)
                {
                    Debug.LogWarning("Called Play() but isPlaying is still false. Check AudioSource config (Volume, Mute, AudioClip)!");
                }
            }
            else
            {
                Debug.Log("Setting is ON and music is already playing. Doing nothing.");
            }
        }
        else
        {
            if (backgroundMusic.isPlaying)
            {
                Debug.Log("Setting is OFF and music is playing. Calling backgroundMusic.Pause()");
                backgroundMusic.Pause();
            }
            else
            {
                Debug.Log("Setting is OFF and music is already NOT playing. Doing nothing.");
            }
        }
    }

    private void OnDifficultyChanged(int index)
    {
        Settings.Instance.SetDifficulty(difficulties[index]);
    }

    private void SaveSettings()
    {
        // Navigate back to desired panel
        screenManager.ShowScreen(modeIndex);

    }
}
