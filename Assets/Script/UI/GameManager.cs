using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Analytics;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [Header("Game State")]
    private bool isGameOver = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void OnPlayerDied(ulong clientId)
    {
        if (!IsServer) return;

        List<PlayerController> aliveList = new List<PlayerController>();
        PlayerHealth[] allPlayers = FindObjectsOfType<PlayerHealth>();
        
        foreach (var hp in allPlayers)
        {
            if (!hp.isDead && hp.TryGetComponent(out PlayerController pc))
            {
                aliveList.Add(pc);
            }
        }

        if (aliveList.Count <= 1 && !isGameOver)
        {
            isGameOver = true;
            
            if (aliveList.Count == 1)
            {
                PlayerController winner = aliveList[0];
                string winnerCharType = winner.characterType.ToString();
                RecordGameWinAnalytics(winnerCharType);
                DeclareWinnerClientRpc(winnerCharType, winner.OwnerClientId, "Survivor!");
            }
            else
            {
                DeclareWinnerClientRpc("Draw", 999, "Everyone died!");
            }

            StartCoroutine(RestartGameLoopRoutine());
        }
    }

    private IEnumerator RestartGameLoopRoutine()
    {
        if (LobbyManager.Instance != null) LobbyManager.Instance.UpdateLobbyStateToWaiting();
        
        yield return new WaitForSeconds(10f);

        if (IsServer)
        {
            PlayerHealth[] allPlayers = FindObjectsOfType<PlayerHealth>();
            foreach (var hp in allPlayers)
            {
                // [แก้ไข] เช็คก่อนว่าตัวละครนี้ยังออนไลน์อยู่ (IsSpawned) ป้องกันบั๊กคนออกเกมก่อนเวลา!
                if (hp != null && hp.IsSpawned)
                {
                    hp.ReviveOnServer();
                }
            }

            isGameOver = false;
            NetworkManager.Singleton.SceneManager.LoadScene("WaitingScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }
    
    [ClientRpc]
    void DeclareWinnerClientRpc(string winnerCharType, ulong winnerId, string statsString)
    {
        GameMenuUI menuUI = FindObjectOfType<GameMenuUI>();
        if (menuUI != null)
        {
            if (NetworkManager.Singleton.LocalClientId == winnerId)
            {
                string winnerName = winnerCharType == "Draw" ? "DRAW!" : $"You Win! ({winnerCharType})";
                menuUI.ShowGameWin(winnerName, statsString);
            }
            
            menuUI.StartEndGameCountdown();
        }
    }

    private void RecordGameWinAnalytics(string winnerCharType)
    {
        if (AnalyticsManager.Instance != null && AnalyticsManager.Instance.disableAnalyticsForTesting) return;
        try { AnalyticsService.Instance.RecordEvent(new CustomEvent("game_win") { { "winning_character", winnerCharType } }); AnalyticsService.Instance.Flush(); } catch { }
    }

    public void RecordEliminationStat(string playerId)
    {
        if (!IsServer) return;
        if (AnalyticsManager.Instance?.disableAnalyticsForTesting == true) return;
        try { AnalyticsService.Instance.RecordEvent(new CustomEvent("player_eliminated") { { "client_id", playerId } }); AnalyticsService.Instance.Flush(); } catch { }
    }

    public void RecordMatchStat(string itemName, float totalDamage) { }
}