using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour 
{
    [SerializeField] private Button hostBtn;
    [SerializeField] private Button serverBtn;
    [SerializeField] private Button clientBtn;
    private void Awake() 
    {
        hostBtn.onClick.AddListener(() => 
        {
            NetworkManager.Singleton.StartHost();
            HideUI();
        });
        serverBtn.onClick.AddListener(() => 
        {
            NetworkManager.Singleton.StartServer();
            HideUI();
        });
        
        clientBtn.onClick.AddListener(() => 
        {
            NetworkManager.Singleton.StartClient();
            HideUI();
        });
    }
    private void HideUI() 
    {
        gameObject.SetActive(false);
    }
}