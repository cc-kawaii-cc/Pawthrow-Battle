using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Lobbies.Models;

public class LobbyBrowserUI : MonoBehaviour
{
    [Header("UI References")]
    public Transform contentContainer;   
    public GameObject lobbyItemPrefab;   
    public Button refreshButton;         
    public Button closeButton;           

    [Header("External Ref")]
    public NetworkManagerUI networkUI;   

    private void Start()
    {
        if (refreshButton != null) refreshButton.onClick.AddListener(RefreshLobbyList);
        
        // ปุ่มปิด
        if (closeButton != null) closeButton.onClick.AddListener(CloseBrowser);
    }

    private void OnEnable()
    {
        RefreshLobbyList();
    }

    // [เพิ่มใหม่] ฟังก์ชันกดปิดหน้าต่าง
    public void CloseBrowser()
    {
        gameObject.SetActive(false);
    }

    public async void RefreshLobbyList()
    {
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }

        if (LobbyManager.Instance == null) return;

        // [แก้ไข] ดึงชื่อจากช่องกรอกข้อความมาล็อกอิน แทนการสุ่มมั่วๆ
        if (string.IsNullOrEmpty(LobbyManager.Instance.PlayerName))
        {
             string inputName = (networkUI != null && networkUI.nameInput != null) ? networkUI.nameInput.text : "";
             await LobbyManager.Instance.AuthenticateAsync(inputName);
        }

        List<Lobby> lobbies = await LobbyManager.Instance.GetLobbiesListAsync();
        
        if (lobbies != null)
        {
            foreach (Lobby lobby in lobbies)
            {
                GameObject itemGo = Instantiate(lobbyItemPrefab, contentContainer);
                LobbyItemUI itemUI = itemGo.GetComponent<LobbyItemUI>();
                itemUI.Setup(lobby, this);
            }
        }
    }

    public async void JoinLobbyFromList(string lobbyId)
    {
        if (networkUI != null) networkUI.statusText.text = "Joining Room from Browser...";
        
        // [แก้ไข] ดึงชื่อใหม่เผื่อผู้เล่นเพิ่งเปลี่ยนชื่อก่อนกดเข้าห้อง
        string currentName = (networkUI != null && networkUI.nameInput != null) ? networkUI.nameInput.text : "";
        await LobbyManager.Instance.AuthenticateAsync(currentName);

        bool isJoined = await LobbyManager.Instance.JoinLobbyByIdAsync(lobbyId);
        if (isJoined)
        {
            NetworkManager.Singleton.NetworkConfig.ConnectionData = networkUI.GetSelectedCharacterData();
            NetworkManager.Singleton.StartClient();
        }
        else
        {
            if (networkUI != null) networkUI.statusText.text = "Failed to join room! (It might be full or playing)";
        }
    }
}