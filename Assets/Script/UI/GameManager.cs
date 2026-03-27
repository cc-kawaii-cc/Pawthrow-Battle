using Unity.Netcode;
using UnityEngine;
using Unity.Services.Analytics;
using System.Collections.Generic;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    
    [Header("Game State")]
    private List<PlayerController> alivePlayers = new List<PlayerController>();
    private bool isGameOver = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }
    public void RegisterPlayer(PlayerController player)
    {
        if (!IsServer) return;
        
        if (!alivePlayers.Contains(player))
        {
            alivePlayers.Add(player);
            Debug.Log($"[GameManager] Player joined. Total alive: {alivePlayers.Count}");
        }
    }
    public void OnPlayerDied(PlayerController deadPlayer)
    {
        if (!IsServer || isGameOver) return;
        if (alivePlayers.Contains(deadPlayer))
        {
            alivePlayers.Remove(deadPlayer);
            Debug.Log($"[GameManager] Player died. Total alive: {alivePlayers.Count}");
        }

        CheckWinCondition();
    }

    private void CheckWinCondition()
    {
        if (alivePlayers.Count == 1)
        {
            isGameOver = true;
            PlayerController winner = alivePlayers[0];
            string winnerCharType = winner.characterType.ToString();
            DeclareWinnerClientRpc(winnerCharType);
        }
        else if (alivePlayers.Count == 0 && !isGameOver)
        {
            isGameOver = true;
            Debug.Log("Draw! Everyone died.");
        }
    }

    [ClientRpc]
    private void DeclareWinnerClientRpc(string winnerCharType)
    {
        Debug.Log("🏆 Game Over! Winner is: " + winnerCharType);
        RecordGameWinAnalytics(winnerCharType);

        // TODO: You can add the code to open the results summary UI window or a button to return to the menu here
    }

    private void RecordGameWinAnalytics(string characterName)
    {
        if (AnalyticsManager.Instance != null && AnalyticsManager.Instance.disableAnalyticsForTesting) return;
        try
        {
            CustomEvent winEvent = new CustomEvent("game_win")
            {
                { "character_name", characterName }
            };
            AnalyticsService.Instance.RecordEvent(winEvent);
            AnalyticsService.Instance.Flush();
            Debug.Log("Sent Analytics Event: game_win -> " + characterName);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Analytics Error: " + e.Message);
        }
    }
}
