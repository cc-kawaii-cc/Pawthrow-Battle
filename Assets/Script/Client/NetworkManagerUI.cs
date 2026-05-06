using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Analytics;
using TMPro; 

public class NetworkManagerUI : MonoBehaviour 
{
    [Header("UI Status (Canvas)")]
    public TextMeshProUGUI statusText; 

    [Header("Input Fields")]
    public TMP_InputField nameInput;     
    public TMP_InputField roomCodeInput; 

    [Header("Connection Buttons")]
    public Button createRoomBtn; 
    public Button joinRoomBtn;   

    [Header("Server Browser UI")]
    public GameObject serverBrowserPanel; // หน้าต่าง Server Browser ที่จะซ่อน/โชว์
    public Button openBrowserBtn;         // ปุ่มกดเพื่อเปิดหน้าต่าง Server Browser

    [Header("Character Selection")]
    [SerializeField] private Button[] charButtons; 
    public GameObject[] playerPrefabs;

    private int selectedCharIndex = 0; 
    public string waitingSceneName = "WaitingScene"; 

    private void Awake() 
    {
        // ซ่อนหน้าต่าง Browser ไว้ก่อนตอนเริ่มเกม
        if (serverBrowserPanel != null) serverBrowserPanel.SetActive(false);

        // --- ระบบปุ่มเปิดหน้าต่าง Browser ---
        if (openBrowserBtn != null)
        {
            openBrowserBtn.onClick.AddListener(() => 
            {
                if (serverBrowserPanel != null) serverBrowserPanel.SetActive(true);
            });
        }

        for (int i = 0; i < charButtons.Length; i++)
        {
            int index = i; 
            charButtons[i].onClick.AddListener(() => 
            {
                selectedCharIndex = index;
                statusText.text = $"Character: {playerPrefabs[index].name}";
            });
        }

        createRoomBtn.onClick.AddListener(async () => 
        {
            statusText.text = "Create Room...";
            SetButtonsInteractable(false);

            bool isLoggedIn = await LobbyManager.Instance.AuthenticateAsync(nameInput.text);
            if (isLoggedIn)
            {
                bool isCreated = await LobbyManager.Instance.CreateLobbyAsync(LobbyManager.Instance.PlayerName + "'s Room", 10);
                if (isCreated)
                {
                    RecordCharacterSelectionAnalytics(selectedCharIndex);
                    NetworkManager.Singleton.NetworkConfig.ConnectionData = System.BitConverter.GetBytes(selectedCharIndex);
                    SetupApprovalCallback(); 
                    
                    NetworkManager.Singleton.StartHost();
                    NetworkManager.Singleton.SceneManager.LoadScene(waitingSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
                }
                else statusText.text = "Can't Create Room!";
            }
            else statusText.text = "Login fail!";
            
            SetButtonsInteractable(true);
        });

        joinRoomBtn.onClick.AddListener(async () => 
        {
            string code = roomCodeInput.text.ToUpper();
            if (string.IsNullOrEmpty(code))
            {
                statusText.text = "Enter room code!";
                return;
            }

            statusText.text = "Joining room...";
            SetButtonsInteractable(false);

            bool isLoggedIn = await LobbyManager.Instance.AuthenticateAsync(nameInput.text);
            if (isLoggedIn)
            {
                bool isJoined = await LobbyManager.Instance.JoinLobbyByCodeAsync(code);
                if (isJoined)
                {
                    RecordCharacterSelectionAnalytics(selectedCharIndex);
                    NetworkManager.Singleton.NetworkConfig.ConnectionData = System.BitConverter.GetBytes(selectedCharIndex);
                    
                    NetworkManager.Singleton.StartClient();
                    statusText.text = "Going to lobby...";
                }
                else statusText.text = "Room not found or incorrect code!";
            }
            else statusText.text = "Login fail!";

            SetButtonsInteractable(true);
        });
    }

    private void SetButtonsInteractable(bool state)
    {
        createRoomBtn.interactable = state;
        joinRoomBtn.interactable = state;
        if (openBrowserBtn != null) openBrowserBtn.interactable = state;
        foreach(var btn in charButtons) btn.interactable = state;
    }

    private void SetupApprovalCallback()
    {
        NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
        uint[] characterHashes = new uint[playerPrefabs.Length];
        for (int i = 0; i < playerPrefabs.Length; i++) characterHashes[i] = playerPrefabs[i].GetComponent<NetworkObject>().PrefabIdHash;

        NetworkManager.Singleton.ConnectionApprovalCallback = (request, response) =>
        {
            int charIndex = (request.Payload != null && request.Payload.Length > 0) ? System.BitConverter.ToInt32(request.Payload, 0) : 0;
            if (charIndex < 0 || charIndex >= characterHashes.Length) charIndex = 0; 
            
            response.Approved = true;
            response.CreatePlayerObject = true;
            response.PlayerPrefabHash = characterHashes[charIndex]; 
            response.Position = new Vector3(0, 15f, 0); 
        };
    }

    private void RecordCharacterSelectionAnalytics(int charIndex)
    {
        try
        {
            if (AnalyticsManager.Instance != null && AnalyticsManager.Instance.disableAnalyticsForTesting) return;
            string charName = (playerPrefabs != null && charIndex < playerPrefabs.Length) ? playerPrefabs[charIndex].name : "Unknown";
            CustomEvent selectionEvent = new CustomEvent("character_selected")
            {
                { "character_name", charName }, { "character_index", charIndex }
            };
            AnalyticsService.Instance.RecordEvent(selectionEvent);
            AnalyticsService.Instance.Flush();
        }
        catch { }
    }

    public byte[] GetSelectedCharacterData()
    {
        return System.BitConverter.GetBytes(selectedCharIndex);
    }
}