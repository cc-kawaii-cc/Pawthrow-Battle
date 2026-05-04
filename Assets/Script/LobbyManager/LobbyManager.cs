using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using Unity.Networking.Transport.Relay;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    public string waitingRoomSceneName = "WaitingScene";
    private Lobby currentLobby;
    public string PlayerName { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    // ... โค้ด Using ด้านบนเหมือนเดิม ...
    // (เพิ่ม using System.Text.RegularExpressions; ไว้ด้านบนสุดด้วยนะครับ)
    
    public async Task<bool> AuthenticateAsync(string playerNameInput)
    {
        // 1. ชื่อที่จะโชว์ให้คนอื่นเห็น (เว้นวรรคได้ ภาษาไทยได้)
        PlayerName = string.IsNullOrEmpty(playerNameInput) ? "Player_" + Random.Range(100, 999) : playerNameInput;

        try
        {
            // 2. ทำความสะอาดชื่อเพื่อเอาไปใช้ล็อกอิน Unity Auth (ลบช่องว่าง ลบอักษรพิเศษ)
            string safeProfileName = System.Text.RegularExpressions.Regex.Replace(PlayerName, "[^a-zA-Z0-9_-]", "");
            
            // ถ้าลบอักษรแปลกๆ ออกหมดแล้วมันว่างเปล่า ให้สุ่มชื่อให้ใหม่
            if (string.IsNullOrEmpty(safeProfileName)) safeProfileName = "User_" + Random.Range(100, 999);
            
            // ป้องกันชื่อยาวเกิน 30 ตัวอักษร
            if (safeProfileName.Length > 30) safeProfileName = safeProfileName.Substring(0, 30);

            InitializationOptions options = new InitializationOptions();
            options.SetProfile(safeProfileName); // ใช้ชื่อที่สะอาดแล้วล็อกอิน
            
            await UnityServices.InitializeAsync(options);

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Login Failed: " + e.Message);
            return false;
        }
    }

    private Player GetPlayer()
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, PlayerName) }
            }
        };
    }

    public async Task<bool> CreateLobbyAsync(string lobbyName, int maxPlayers)
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
            string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = GetPlayer(),
                Data = new Dictionary<string, DataObject>
                {
                    { "RelayJoinCode", new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) },
                    // บันทึกสถานะห้องเริ่มต้นว่า "Waiting"
                    { "State", new DataObject(DataObject.VisibilityOptions.Public, "Waiting", DataObject.IndexOptions.S1) }
                }
            };

            currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            
            return true;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Create Lobby Failed: " + e.Message);
            return false;
        }
    }

    public async Task<bool> JoinLobbyByCodeAsync(string lobbyCode)
    {
        try
        {
            JoinLobbyByCodeOptions options = new JoinLobbyByCodeOptions { Player = GetPlayer() };
            currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, options);

            string relayJoinCode = currentLobby.Data["RelayJoinCode"].Value;
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);
            
            RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            return true;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Join Lobby Failed: " + e.Message);
            return false;
        }
    }

    // ฟังก์ชันค้นหาห้องทั้งหมด
    public async Task<List<Lobby>> GetLobbiesListAsync()
    {
        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions
            {
                Count = 25, 
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT) 
                }
            };
            QueryResponse response = await Lobbies.Instance.QueryLobbiesAsync(options);
            return response.Results;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Search Lobby Failed: " + e.Message);
            return null;
        }
    }

    // ฟังก์ชันเข้าห้องจากหน้า List
    public async Task<bool> JoinLobbyByIdAsync(string lobbyId)
    {
        try
        {
            JoinLobbyByIdOptions options = new JoinLobbyByIdOptions { Player = GetPlayer() };
            currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, options);

            string relayJoinCode = currentLobby.Data["RelayJoinCode"].Value;
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);
            
            RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            return true;
        }
        catch (LobbyServiceException e) 
        { 
            Debug.LogError("Join by ID Failed: " + e.Message); 
            return false; 
        }
    }

    // ฟังก์ชันเปลี่ยนสถานะห้องเป็น Playing
    public async void UpdateLobbyStateToPlaying()
    {
        if (currentLobby != null && currentLobby.HostId == AuthenticationService.Instance.PlayerId)
        {
            try
            {
                UpdateLobbyOptions options = new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        { "State", new DataObject(DataObject.VisibilityOptions.Public, "Playing", DataObject.IndexOptions.S1) }
                    }
                };
                currentLobby = await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, options);
            }
            catch (LobbyServiceException e) { Debug.LogError(e); }
        }
    }

    private float heartbeatTimer;
    private float lobbyPollTimer; // [เพิ่มใหม่] ตัวจับเวลาสำหรับรีเฟรชรายชื่อคน

    private void Update()
    {
        HandleHeartbeat();
        HandleLobbyPollForUpdates();
    }

    private async void HandleHeartbeat()
    {
        // เลี้ยงห้องไม่ให้หายไป (เฉพาะ Host)
        if (currentLobby != null && currentLobby.HostId == AuthenticationService.Instance.PlayerId)
        {
            heartbeatTimer += Time.deltaTime;
            if (heartbeatTimer > 15f)
            {
                heartbeatTimer = 0f;
                try {
                    await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
                } catch { }
            }
        }
    }

    private async void HandleLobbyPollForUpdates()
    {
        // [เพิ่มใหม่] ฟังก์ชันทำให้ห้องเป็น Real-time! 
        // สั่งให้ทุกคน (ทั้งหัวห้องและลูกเรือ) ดึงข้อมูลอัปเดตทุกๆ 1.5 วินาที
        if (currentLobby != null)
        {
            lobbyPollTimer += Time.deltaTime;
            if (lobbyPollTimer > 1.5f)
            {
                lobbyPollTimer = 0f;
                try
                {
                    // โหลดข้อมูลล่าสุดจากเซิร์ฟเวอร์มาทับของเดิม
                    currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
                }
                catch (LobbyServiceException e)
                {
                    Debug.LogWarning("Lobby Poll Error (อาจจะห้องโดนยุบไปแล้ว): " + e.Message);
                }
            }
        }
    }

    public Lobby GetCurrentLobby() => currentLobby;
    
}