using UnityEngine;

public class Settings : MonoBehaviour
{
    public static Settings Instance;

    public bool isMusicOn = true;
    public string difficulty = "Easy";
    public int pieces = 2; // number of puzzle pieces, derived from difficulty

    private void Awake()
    {
        // TEMPORARY – run ONCE to reset old bad values
        PlayerPrefs.DeleteAll();

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadSettings();
    }

    // Set difficulty by name, automatically updates pieces
    public void SetDifficulty(string diff)
    {
        difficulty = diff;
        pieces = DifficultyToPieces(diff);
        PlayerPrefs.SetString("Difficulty", difficulty);
        PlayerPrefs.Save();
    }

    // Set music on/off
    public void SetMusic(bool on)
    {
        isMusicOn = on;
        PlayerPrefs.SetInt("MusicOn", on ? 1 : 0);
        PlayerPrefs.Save();
    }

    // Convert difficulty name to number of pieces
    private int DifficultyToPieces(string diff)
    {
        return diff switch
        {
            "Easy" => 4,
            "Medium" => 9,
            "Hard" => 16,
            "Expert" => 25,
            "Master" => 36,
            "Extreme" => 49,
            _ => 2
        };
    }

    private void LoadSettings()
    {
        isMusicOn = PlayerPrefs.GetInt("MusicOn", 1) == 1;
        difficulty = PlayerPrefs.GetString("Difficulty", "Easy");
        pieces = DifficultyToPieces(difficulty);
    }
}
