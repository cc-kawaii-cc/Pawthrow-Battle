using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameMenuUI : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject gameOverPanel;

    void Start()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }
    public void LeaveGame()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }
        Destroy(NetworkManager.Singleton.gameObject);
        SceneManager.LoadScene("MainMenu"); 
    }
    public void ShowGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
