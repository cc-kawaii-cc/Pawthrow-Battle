using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; 
using System.Collections;

public class GameMenuUI : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject gameOverPanel;
    public GameObject gameWinPanel; 

    [Header("UI Text")]
    public TextMeshProUGUI winnerText; 
    public TextMeshProUGUI matchStatsText;
    public TextMeshProUGUI countdownText; // [เพิ่มใหม่] ช่อง Text สำหรับโชว์เวลานับถอยหลัง

    private bool isGameEnded = false; 

    void Start()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (gameWinPanel != null) gameWinPanel.SetActive(false);
        if (countdownText != null) countdownText.gameObject.SetActive(false);
    }

    public void LeaveGame()
    {
     
        if (NetworkManager.Singleton != null) 
        {
            NetworkManager.Singleton.Shutdown();
            Destroy(NetworkManager.Singleton.gameObject); // ลบตัวเก่าทิ้งเพื่อกันบั๊กตอนเล่นใหม่
        }
        SceneManager.LoadScene("MainMenu"); 
    }

 
    public void SpectateAndStay()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        
       
        Cursor.lockState = CursorLockMode.Locked; 
        Cursor.visible = false;
    }

    public void ShowGameOver()
    {
        
        if (gameWinPanel != null && gameWinPanel.activeSelf) return;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            Cursor.lockState = CursorLockMode.None; 
            Cursor.visible = true;
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

   
    public void StartEndGameCountdown()
    {
        isGameEnded = true;
        StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        if (countdownText != null) countdownText.gameObject.SetActive(true);

        int time = 10;
        while (time > 0)
        {
            if (countdownText != null) 
            {
                countdownText.text = $"Returning to Lobby in {time}...";
            }
            yield return new WaitForSeconds(1f);
            time--;
        }
    }
    void Update()
    {
        // ถ้าหน้าต่างแพ้ หรือ ชนะ เปิดอยู่ บังคับโชว์เมาส์เสมอ! สคริปต์อื่นห้ามแย่ง!
        if ((gameOverPanel != null && gameOverPanel.activeSelf) || 
            (gameWinPanel != null && gameWinPanel.activeSelf))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}