using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Services.Lobbies.Models;

public class LobbyItemUI : MonoBehaviour
{
    public TextMeshProUGUI roomNameText;
    public TextMeshProUGUI playersCountText;
    public TextMeshProUGUI stateText;
    public Button joinButton;

    private Lobby lobby;
    private LobbyBrowserUI browserUI;

    public void Setup(Lobby _lobby, LobbyBrowserUI _browserUI)
    {
        lobby = _lobby;
        browserUI = _browserUI;

        roomNameText.text = lobby.Name;
        playersCountText.text = $"{lobby.Players.Count} / {lobby.MaxPlayers}";

        string currentState = "Waiting";
        if (lobby.Data != null && lobby.Data.ContainsKey("State"))
        {
            currentState = lobby.Data["State"].Value;
        }

        if (lobby.Players.Count >= lobby.MaxPlayers || currentState == "Playing")
        {
            stateText.text = (currentState == "Playing") ? "<color=orange>In-Game</color>" : "<color=red>Full</color>";
            joinButton.interactable = false; // ปิดปุ่มเข้าห้องถ้าห้องเต็มหรือเล่นอยู่
        }
        else
        {
            stateText.text = "<color=green>Waiting</color>";
            joinButton.interactable = true;
        }

        joinButton.onClick.AddListener(() => 
        {
            browserUI.JoinLobbyFromList(lobby.Id);
        });
    }
}