using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Services.Analytics;

public class NetworkManagerUI : MonoBehaviour 
{
    [Header("Connection Buttons")]
    [SerializeField] private Button hostBtn;
    [SerializeField] private Button serverBtn;
    [SerializeField] private Button clientBtn;

    [Header("Character Selection")]
    [SerializeField] private Button[] charButtons; 
    public GameObject[] playerPrefabs;

    private int selectedCharIndex = 0; 
    public string gameSceneName = "GameScene";

    private void Awake() 
    {
        for (int i = 0; i < charButtons.Length; i++)
        {
            int index = i; 
            charButtons[i].onClick.AddListener(() => 
            {
                selectedCharIndex = index;
                Debug.Log("Choose a character: " + index);
            });
        }

        hostBtn.onClick.AddListener(() => 
        {
            RecordCharacterSelectionAnalytics(selectedCharIndex);
            NetworkManager.Singleton.NetworkConfig.ConnectionData = System.BitConverter.GetBytes(selectedCharIndex);
            SetupApprovalCallback(); 
            NetworkManager.Singleton.StartHost();
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
            HideUI();
        });

        clientBtn.onClick.AddListener(() => 
        {
            RecordCharacterSelectionAnalytics(selectedCharIndex);
            NetworkManager.Singleton.NetworkConfig.ConnectionData = System.BitConverter.GetBytes(selectedCharIndex);
            NetworkManager.Singleton.StartClient();
            HideUI();
        });

        serverBtn.onClick.AddListener(() => 
        {
            SetupApprovalCallback();
            NetworkManager.Singleton.StartServer();
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
            HideUI();
        });
    }

    private void SetupApprovalCallback()
    {
        NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
        uint[] characterHashes = new uint[playerPrefabs.Length];
        for (int i = 0; i < playerPrefabs.Length; i++)
        {
            characterHashes[i] = playerPrefabs[i].GetComponent<NetworkObject>().PrefabIdHash;
        }

        NetworkManager.Singleton.ConnectionApprovalCallback = (request, response) =>
        {
            int charIndex = 0;
            if (request.Payload != null && request.Payload.Length > 0)
            {
                charIndex = System.BitConverter.ToInt32(request.Payload, 0);
            }

            if (charIndex < 0 || charIndex >= characterHashes.Length) 
            {
                charIndex = 0; 
            }
            
            response.Approved = true;
            response.CreatePlayerObject = true;
            response.PlayerPrefabHash = characterHashes[charIndex];
            response.Position = new Vector3(0, 5, 0); 
            response.Rotation = Quaternion.identity;
        };
    }

    private void HideUI() 
    {
        gameObject.SetActive(false);
    }

    private void RecordCharacterSelectionAnalytics(int charIndex)
    {
        try
        {
            string characterName = "Unknown";
            if (playerPrefabs != null && charIndex >= 0 && charIndex < playerPrefabs.Length)
            {
                if (playerPrefabs[charIndex] != null)
                {
                    characterName = playerPrefabs[charIndex].name; 
                }
            }

            CustomEvent selectionEvent = new CustomEvent("character_selected")
            {
                { "character_name", characterName },
                { "character_index", charIndex }
            };
            
            AnalyticsService.Instance.RecordEvent(selectionEvent);
            AnalyticsService.Instance.Flush(); // บังคับส่งทันที
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Analytics Error: " + e.Message);
        }
    }
}