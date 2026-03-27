using Unity.Netcode;
using UnityEngine;
using Unity.Services.Analytics;
using System.Collections.Generic;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    [Header("Game State")]
    private bool isGameOver = false;
    private void Awake()
    {
        if (Instance == null) Instance = this;
    }
    public void OnPlayerDied(PlayerController deadPlayer)
    {
        if (!IsServer || isGameOver) return;
        PlayerController[] allPlayersInMap = FindObjectsOfType<PlayerController>();
        List<PlayerController> alivePlayers = new List<PlayerController>();
        foreach (PlayerController p in allPlayersInMap)
        {
            if (p != deadPlayer && p.gameObject.activeInHierarchy)
            {
                alivePlayers.Add(p);
            }
        }
        Debug.Log($"[GameManager] A player died. Remaining alive players: {alivePlayers.Count}");
        if (alivePlayers.Count == 1)
        {
            isGameOver = true;
            PlayerController winner = alivePlayers[0];
            string winnerCharType = winner.characterType.ToString();
            DeclareWinnerClientRpc(winnerCharType, winner.NetworkObjectId);
        }
        else if (alivePlayers.Count == 0 && !isGameOver)
        {
            isGameOver = true;
            Debug.Log("Draw! Everyone died.");
        }
    }

    [ClientRpc]
    private void DeclareWinnerClientRpc(string winnerCharType, ulong winnerNetworkObjectId)
    {
        Debug.Log("Game Over! Winner is: " + winnerCharType);
        RecordGameWinAnalytics(winnerCharType);
        GameMenuUI menuUI = FindObjectOfType<GameMenuUI>();
        if (menuUI != null)
        {
            menuUI.ShowGameWin(winnerCharType);
        }
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(winnerNetworkObjectId, out NetworkObject winnerObj))
        {
            Transform winnerTransform = winnerObj.transform;
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                if (mainCam.TryGetComponent(out SpectatorCameraController spectator))
                {
                    spectator.enabled = false;
                }
                WinnerLockOnCamera lockOnCam = mainCam.GetComponent<WinnerLockOnCamera>();
                if (lockOnCam == null)
                {
                    lockOnCam = mainCam.gameObject.AddComponent<WinnerLockOnCamera>();
                }
                lockOnCam.SetTarget(winnerTransform);
            }
        }
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
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Analytics Error: " + e.Message);
        }
    }
}