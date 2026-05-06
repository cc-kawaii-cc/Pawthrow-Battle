using Unity.Netcode;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Services.Lobbies.Models;

public class WaitingRoomUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI roomInfoText;
    public TextMeshProUGUI playerListText;
    public Button startGameButton;

    void Start()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            startGameButton.gameObject.SetActive(true);
            startGameButton.onClick.AddListener(() => 
            {
                gameObject.SetActive(false); 
                
                // อัปเดตสถานะห้องว่ากำลังเล่นอยู่ คนอื่นจะได้เข้าไม่ได้
                if (LobbyManager.Instance != null)
                {
                    LobbyManager.Instance.UpdateLobbyStateToPlaying();
                }

                NetworkManager.Singleton.SceneManager.LoadScene("CITY", UnityEngine.SceneManagement.LoadSceneMode.Single);
            });
        }
        else
        {
            startGameButton.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (LobbyManager.Instance != null && LobbyManager.Instance.GetCurrentLobby() != null)
        {
            Lobby currentLobby = LobbyManager.Instance.GetCurrentLobby();
            
            roomInfoText.text = $"Room Code: {currentLobby.LobbyCode}\nPlayer: {currentLobby.Players.Count} / {currentLobby.MaxPlayers}";

            string playersInfo = "Player List:\n";
            foreach (Player player in currentLobby.Players)
            {
                if (player.Data != null && player.Data.ContainsKey("PlayerName"))
                {
                    playersInfo += $"- {player.Data["PlayerName"].Value}\n";
                }
                else
                {
                    playersInfo += "- Player (Connecting)\n";
                }
            }
            playerListText.text = playersInfo;
        }
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}