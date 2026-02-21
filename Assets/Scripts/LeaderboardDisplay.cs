using UnityEngine;
using TMPro;
using Unity.Services.Leaderboards;
using System.Collections.Generic;

public class LeaderboardDisplay : MonoBehaviour
{
    [Header("UI References")]
    public GameObject rowPrefab;        
    public Transform tableContent;      
    public GameObject leaderboardPanel; 

    // Safety check: Are we using additional buttons? (Optional)
    [SerializeField] private GameObject returnButton; 
    [SerializeField] private ScreenManager screenManager;
    int ModePanelIndex = 1;

    private void OnEnable()
    {
        // This runs automatically every time the GameObject is turned on
        ShowScores();
    }
    public async void ShowScores()
    {
        Debug.Log("Attempting to show scores...");

        // SAFETY CHECK 1: Are the UI elements connected?
        if (tableContent == null || rowPrefab == null || leaderboardPanel == null)
        {
            Debug.LogError("CRITICAL ERROR: UI References are missing in the Inspector! Check LeaderboardDisplay script.");
            return;
        }

        // 1. Open the window
        leaderboardPanel.SetActive(true);

        // 2. Clear old rows
        foreach (Transform child in tableContent)
        {
            Destroy(child.gameObject);
        }

        // SAFETY CHECK 2: Is the Settings system ready?
        string currentDifficulty = "Easy"; // Default fallback
        
        if (Settings.Instance == null)
        {
            Debug.LogWarning("Settings.Instance is NULL! Defaulting to 'Easy'.");
        }
        else if (string.IsNullOrEmpty(Settings.Instance.difficulty))
        {
            Debug.LogWarning("Settings difficulty is Empty! Defaulting to 'Easy'.");
        }
        else
        {
            currentDifficulty = Settings.Instance.difficulty;
        }

        string boardId = "board_" + currentDifficulty.ToLower();
        Debug.Log($"Fetching data for ID: {boardId}");

        try
        {
            // 3. Fetch Top 10 scores
            var response = await LeaderboardsService.Instance.GetScoresAsync(
                boardId, 
                new GetScoresOptions { Limit = 10 }
            );

            // 4. Create rows
            foreach (var entry in response.Results)
            {
                GameObject newRow = Instantiate(rowPrefab, tableContent);
                
                TextMeshProUGUI[] texts = newRow.GetComponentsInChildren<TextMeshProUGUI>();
                
                // SAFETY CHECK 3: Does the prefab have the text components?
                if (texts.Length < 3)
                {
                    Debug.LogError("Row Prefab is missing TextMeshPro components! It needs 3 (Rank, Name, Time).");
                    continue;
                }

                texts[0].text = (entry.Rank + 1).ToString(); 

                if (entry.PlayerName.Contains("#"))
                {
                     texts[1].text = entry.PlayerName.Split('#')[0];
                }
                else
                {
                     texts[1].text = entry.PlayerName;
                }

                System.TimeSpan t = System.TimeSpan.FromSeconds(entry.Score);
                texts[2].text = string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to fetch leaderboard: " + e);
        }
    }

    public void CloseLeaderboard()
    {
        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);
        screenManager.ShowScreen(ModePanelIndex); // Assuming 0 is the main menu or previous screen index
    }
}