using Unity.Netcode;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class WaitingRoomUI : MonoBehaviour
{
    [Header("UI Elements")] public TextMeshProUGUI roomInfoText;
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

                if (LobbyManager.Instance != null)
                {
                    LobbyManager.Instance.UpdateLobbyStateToPlaying();
                }

                NetworkManager.Singleton.SceneManager.LoadScene("CITY",
                    UnityEngine.SceneManagement.LoadSceneMode.Single);
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
            var currentLobby = LobbyManager.Instance.GetCurrentLobby();

            // [แก้ไข] เปลี่ยนมานับ PlayerController แทน จะได้นับคนครบทุกคนแม้จะไม่ได้ใส่ป้ายชื่อ
            PlayerController[] allPlayersInGame = FindObjectsOfType<PlayerController>();

            roomInfoText.text =
                $"Room Code: {currentLobby.LobbyCode}\nPlayer: {allPlayersInGame.Length} / {currentLobby.MaxPlayers}";

            string playersInfo = "Player List:\n";
            foreach (var p in allPlayersInGame)
            {
                string pName = "Connecting...";

                // พยายามดึงชื่อจาก NameTag ถ้ามี
                if (p.TryGetComponent(out PlayerNameTag tag) && !string.IsNullOrEmpty(tag.GetPlayerName()))
                {
                    pName = tag.GetPlayerName();
                }

                playersInfo += $"- {pName}\n";
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