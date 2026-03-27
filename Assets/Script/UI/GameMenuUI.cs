using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; 

public class GameMenuUI : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject gameOverPanel;
    public GameObject gameWinPanel; 

    [Header("UI Text")]
    public TextMeshProUGUI winnerText; 
    public TextMeshProUGUI matchStatsText;

    private bool isGameEnded = false; 

    void Start()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (gameWinPanel != null) gameWinPanel.SetActive(false);
    }

    public void LeaveGame()
    {
        if (NetworkManager.Singleton != null) NetworkManager.Singleton.Shutdown();
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.gameObject != null) Destroy(NetworkManager.Singleton.gameObject);
        SceneManager.LoadScene("MainMenu"); 
    }

    public void ShowGameOver()
    {
        if (isGameEnded) return;
        if (gameWinPanel != null && gameWinPanel.activeSelf) return;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
    }

    public void ShowGameWin(string winnerName, string matchStats = "")
    {
        isGameEnded = true;
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        if (gameWinPanel != null)
        {
            gameWinPanel.SetActive(true);
            
            if (winnerText != null) winnerText.text = "WINNER\n" + winnerName;
            if (matchStatsText != null) matchStatsText.text = matchStats;

            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
    }
}