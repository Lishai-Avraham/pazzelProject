using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameSettingsManager : MonoBehaviour
{
    // --- SINGLETON SETUP ---
    public static GameSettingsManager Instance;

    private void Awake()
    {
        // Ensure only one instance of this manager exists
        if (Instance == null)
        {
            Instance = this;
            // Keeps this GameObject alive when loading new scenes
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject); // Destroy duplicates
            return;
        }
        
        // Load settings and apply defaults if none are found
        LoadSettings();
    }

    // --- UI REFERENCES (Assign in Inspector) ---
    [Header("UI Elements")]
    public GameObject settingsPanel;
    public Toggle musicToggle;
    public TMP_Dropdown difficultyDropdown;
    
    // --- SETTING VARIABLES ---
    [HideInInspector] public bool isMusicOn = true;
    [HideInInspector] public int currentDifficulty = 1; // 1 = Medium (0-indexed)

    // --- INITIALIZATION AND LOADING ---
    private void LoadSettings()
    {
        // Default: Music ON (1), Difficulty Medium (1)
        isMusicOn = PlayerPrefs.GetInt("MusicOn", 1) == 1; 
        currentDifficulty = PlayerPrefs.GetInt("Difficulty", 1); 

        // Apply loaded values to UI controls
        musicToggle.isOn = isMusicOn;
        difficultyDropdown.value = currentDifficulty;

        // Apply settings to the game (e.g., mute music)
        ApplyMusicSetting(isMusicOn);
    }

    // --- UI CONTROL (Called by Button/Toggle/Dropdown) ---

    // Called by the SettingsButton to show/hide the panel
    public void ToggleSettingsPanel()
    {
        settingsPanel.SetActive(!settingsPanel.activeSelf);
    }
    
    // Called by the MusicToggle's OnValueChanged (Boolean) event
    public void SetMusic(bool isOn)
    {
        isMusicOn = isOn;
        PlayerPrefs.SetInt("MusicOn", isOn ? 1 : 0);
        ApplyMusicSetting(isOn);
        PlayerPrefs.Save();
    }

    // Called by the DifficultyDropdown's OnValueChanged (Int) event
    public void SetDifficulty(int index)
    {
        currentDifficulty = index;
        PlayerPrefs.SetInt("Difficulty", index);
        // You would typically trigger game state change here, e.g.:
        // GameManager.Instance.UpdateEnemyStats(index); 
        Debug.Log($"Difficulty set to index: {index}");
        PlayerPrefs.Save();
    }
    
    // --- GAME LOGIC APPLICATION ---
    
    // Replace this with your actual music control logic (e.g., AudioMixer)
    private void ApplyMusicSetting(bool isOn)
    {
        // Example: Find a Music Source and set its volume/mute state
        GameObject musicObject = GameObject.FindWithTag("Music"); 
        if (musicObject != null && musicObject.GetComponent<AudioSource>() != null)
        {
            musicObject.GetComponent<AudioSource>().mute = !isOn;
        }
        else
        {
            Debug.LogWarning("Music AudioSource not found or not tagged 'Music'.");
        }
    }
}