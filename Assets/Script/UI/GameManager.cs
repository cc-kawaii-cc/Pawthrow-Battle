using Unity.Netcode;
using UnityEngine;
using Unity.Services.Analytics;
using System.Collections.Generic;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    
    [Header("Game State")]
    private bool isGameOver = false;
    
    private Dictionary<string, int> itemUsageCount = new Dictionary<string, int>();
    private Dictionary<string, float> itemTotalDamage = new Dictionary<string, float>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }
    public void RecordMatchStat(string itemName, float damage)
    {
        if (!IsServer) return; 
        
        if (!itemUsageCount.ContainsKey(itemName))
        {
            itemUsageCount[itemName] = 0;
            itemTotalDamage[itemName] = 0;
        }
        itemUsageCount[itemName]++;
        itemTotalDamage[itemName] += damage;
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
                PlayerHealth health = p.GetComponent<PlayerHealth>();
                if (health != null && health.currentHealth.Value > 0)
                {
                    alivePlayers.Add(p);
                }
            }
        }

        if (alivePlayers.Count == 1)
        {
            isGameOver = true;
            PlayerController winner = alivePlayers[0];
            string winnerCharType = winner.characterType.ToString();
            string statsString = "MATCH STATS:\n";
            if (itemUsageCount.Count == 0) statsString += "No items were used.\n";
            foreach (var kvp in itemUsageCount)
            {
                statsString += $"- {kvp.Key}: Thrown {kvp.Value} times (Total Dmg: {itemTotalDamage[kvp.Key]:F0})\n";
            }
            
            DeclareWinnerClientRpc(winnerCharType, winner.NetworkObjectId, statsString);
        }
        else if (alivePlayers.Count == 0 && !isGameOver)
        {
            isGameOver = true;
            Debug.Log("Draw! Everyone died.");
        }
    }

    [ClientRpc]
    private void DeclareWinnerClientRpc(string winnerCharType, ulong winnerNetworkObjectId, string matchStats)
    {
        RecordGameWinAnalytics(winnerCharType);

        GameMenuUI menuUI = FindObjectOfType<GameMenuUI>();
        if (menuUI != null)
        {
            menuUI.ShowGameWin(winnerCharType, matchStats);
        }
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(winnerNetworkObjectId, out NetworkObject winnerObj))
        {
            Transform winnerTransform = winnerObj.transform;
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                if (mainCam.TryGetComponent(out SpectatorCameraController spectator)) spectator.enabled = false;
                
                WinnerLockOnCamera lockOnCam = mainCam.GetComponent<WinnerLockOnCamera>();
                if (lockOnCam == null) lockOnCam = mainCam.gameObject.AddComponent<WinnerLockOnCamera>();
                
                lockOnCam.SetTarget(winnerTransform);
            }
        }
    }

    private void RecordGameWinAnalytics(string characterName)
    {
        if (!IsServer) return; 
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
        catch (System.Exception e) { Debug.LogWarning("Analytics Error: " + e.Message); }
    }
}