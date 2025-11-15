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
            "Easy" => 2,
            "Medium" => 4,
            "Hard" => 6,
            "Expert" => 8,
            "Master" => 10,
            "Extreme" => 20,
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
