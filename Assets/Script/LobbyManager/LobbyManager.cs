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
using System.Text.RegularExpressions;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    public string waitingRoomSceneName = "WaitingScene";
    private Lobby currentLobby;
    public string PlayerName { get; private set; }
    private void Start()
    {
      
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }
    
    private void OnClientDisconnected(ulong clientId)
    {
        
        if (!NetworkManager.Singleton.IsServer && clientId == 0)
        {
            Debug.Log("หัวห้องออกจากเกม! เซิร์ฟเวอร์ปิดตัวลง กำลังพากลับเมนูหลัก...");
            LeaveLobbyGracefully();
        }
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }
    
    public async Task<bool> AuthenticateAsync(string playerNameInput)
    {
        PlayerName = string.IsNullOrEmpty(playerNameInput) ? "Player_" + Random.Range(100, 999) : playerNameInput;

        try
        {
            string safeProfileName = Regex.Replace(PlayerName, "[^a-zA-Z0-9_-]", "");
            
            if (string.IsNullOrEmpty(safeProfileName)) safeProfileName = "User_" + Random.Range(100, 999);
            if (safeProfileName.Length > 30) safeProfileName = safeProfileName.Substring(0, 30);

            InitializationOptions options = new InitializationOptions();
            options.SetProfile(safeProfileName); 
            
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

    public async void UpdateLobbyStateToWaiting()
    {
        if (currentLobby != null && currentLobby.HostId == AuthenticationService.Instance.PlayerId)
        {
            try
            {
                UpdateLobbyOptions options = new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        { "State", new DataObject(DataObject.VisibilityOptions.Public, "Waiting", DataObject.IndexOptions.S1) }
                    }
                };
                currentLobby = await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, options);
            }
            catch (LobbyServiceException e) { Debug.LogError(e); }
        }
    }

    private float heartbeatTimer;
    private float lobbyPollTimer; 

    private void Update()
    {
        HandleHeartbeat();
        HandleLobbyPollForUpdates();
    }

    private async void HandleHeartbeat()
    {
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
        if (currentLobby != null)
        {
            lobbyPollTimer += Time.deltaTime;
            if (lobbyPollTimer > 1.5f)
            {
                lobbyPollTimer = 0f;
                try
                {
                    currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
                }
                catch (LobbyServiceException e)
                {
                    Debug.LogWarning("Lobby Poll Error (อาจจะห้องโดนยุบไปแล้ว): " + e.Message);
                    currentLobby = null;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("Lobby Poll Unexpected Error: " + e.Message);
                }
            }
        }
    }
   
    
    public async void LeaveLobby()
    {
        if (currentLobby != null)
        {
            try
            {
                if (currentLobby.HostId == AuthenticationService.Instance.PlayerId)
                {
                    
                    await LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);
                }
                else
                {
                   
                    await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId);
                }
            }
            catch (LobbyServiceException e)
            {
                Debug.LogWarning("Error leaving lobby (อาจจะห้องโดนลบไปแล้ว): " + e.Message);
            }
            
            currentLobby = null;
        }
    }

   
    public void LeaveLobbyGracefully()
    {
    
        LeaveLobby(); 

       
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
            Destroy(NetworkManager.Singleton.gameObject);
        }
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
      
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
    
    

    private void OnDestroy()
    {
        
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
        
       
    }

    public Lobby GetCurrentLobby() => currentLobby;
}