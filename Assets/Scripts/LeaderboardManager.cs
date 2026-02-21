using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards;
using System.Threading.Tasks;

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    private async void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this);
            // We still initialize services, but WE DO NOT SIGN IN here.
            await UnityServices.InitializeAsync(); 
        }
    }

    public async void SubmitScore(string difficulty, double timeInSeconds)
    {
        // Safety Check: If not signed in, don't crash, just log error
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogError("Cannot submit score: Player is not signed in!");
            return;
        }

        string leaderboardId = "board_" + difficulty.ToLower();
        
        try
        {
            var playerEntry = await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardId, timeInSeconds);
            int rank = playerEntry.Rank + 1; // Ranks are 0-indexed, so add 1 for display
            Debug.Log($"Score Submitted! Rank: {playerEntry.Rank}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to submit score: {e}");
        }
    }
}