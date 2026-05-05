using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using Unity.Services.Analytics;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [Header("Game State")]
    public List<PlayerController> alivePlayers = new List<PlayerController>();
    private bool isGameOver = false;

    // [โค้ดเดิมของคุณ: ตัวแปรอื่นๆ เช่น เวลา, UI References]

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void OnPlayerDied(ulong clientId)
    {
        if (!IsServer) return;

        // [โค้ดเดิมของคุณ: จัดการลบผู้เล่นออกจาก alivePlayers]
        // ตัวอย่าง:
        // PlayerController deadPlayer = alivePlayers.Find(p => p.OwnerClientId == clientId);
        // if (deadPlayer != null) alivePlayers.Remove(deadPlayer);

        // เช็คว่าเหลือคนสุดท้ายหรือยัง
        if (alivePlayers.Count == 1 && !isGameOver)
        {
            isGameOver = true;
            PlayerController winner = alivePlayers[0];
            string winnerCharType = winner.characterType.ToString();

            // Record analytics บน Server (ถูกต้อง ตัวนี้รันบน Server แล้ว)
            RecordGameWinAnalytics(winnerCharType);

            // [โค้ดเดิมของคุณ: จัดเตรียมข้อมูล statsString]
            string statsString = "ตัวอย่าง Stats ยอดนักปา"; 

            // ส่งข้อมูลผู้ชนะไปให้ Client ทุกคนโชว์หน้า UI
            DeclareWinnerClientRpc(winnerCharType, winner.NetworkObjectId, statsString);
        }
    }

    [ClientRpc]
    void DeclareWinnerClientRpc(string winnerCharType, ulong winnerId, string statsString)
    {
        // [โค้ดเดิมของคุณ: ลอจิกจัดการ Camera, UI แสดงผลคนชนะ (ห้ามแก้)]
        
        Debug.Log($"ผู้ชนะคือ: {winnerCharType} ID: {winnerId}");
    }

    private void RecordGameWinAnalytics(string winnerCharType)
    {
        // Called from Server context only
        if (AnalyticsManager.Instance != null && AnalyticsManager.Instance.disableAnalyticsForTesting) return;

        try
        {
            CustomEvent winEvent = new CustomEvent("game_win")
            {
                { "winning_character", winnerCharType }
            };
            AnalyticsService.Instance.RecordEvent(winEvent);
            AnalyticsService.Instance.Flush();
        }
        catch (System.Exception e) 
        { 
            Debug.LogWarning("Analytics Error: " + e.Message); 
        }
    }

    public void RecordEliminationStat(string playerId)
    {
        // ฟังก์ชันนี้จะถูกเรียกจาก PlayerHealth.Die()
        if (!IsServer) return;
        if (AnalyticsManager.Instance?.disableAnalyticsForTesting == true) return;
        
        try 
        {
            AnalyticsService.Instance.RecordEvent(new CustomEvent("player_eliminated") 
            { 
                { "client_id", playerId } 
            });
            AnalyticsService.Instance.Flush();
        } 
        catch (System.Exception e)
        {
            Debug.LogWarning("Analytics Elimination Error: " + e.Message); 
        }
    }

    // [โค้ดเดิมของคุณ: ฟังก์ชันอื่นๆ เช่น RecordMatchStat ที่เอาไว้เก็บสถิติตอนปาของ]
    public void RecordMatchStat(string itemName, float totalDamage)
    {
        // [ลอจิกเดิม]
    }
}