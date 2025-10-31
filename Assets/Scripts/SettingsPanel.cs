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
        // Populate dropdown
        difficultyDropdown.ClearOptions();
        difficultyDropdown.AddOptions(new System.Collections.Generic.List<string>(difficulties));

        // Load current settings
        musicToggle.isOn = Settings.Instance.isMusicOn;
        int index = System.Array.IndexOf(difficulties, Settings.Instance.difficulty);
        difficultyDropdown.value = index >= 0 ? index : 0;

        ApplyMusicState();

        // Listeners
        musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);
        difficultyDropdown.onValueChanged.AddListener(OnDifficultyChanged);
        saveButton.onClick.AddListener(SaveSettings);
    }

    private void OnMusicToggleChanged(bool value)
    {
        Settings.Instance.SetMusic(value);
        ApplyMusicState();
    }

    private void ApplyMusicState()
    {
        if (backgroundMusic == null) return;

        if (Settings.Instance.isMusicOn)
        {
            if (!backgroundMusic.isPlaying)
                backgroundMusic.Play();
        }
        else
        {
            if (backgroundMusic.isPlaying)
                backgroundMusic.Pause();
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
