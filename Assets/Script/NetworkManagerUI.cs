using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour {
    [SerializeField] private Button hostBtn;
    [SerializeField] private Button serverBtn;
    [SerializeField] private Button clientBtn;

    private void Awake() {
        // เมื่อกดปุ่ม Host ให้เริ่มเป็น Host
        hostBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
            HideUI();
        });

        // เมื่อกดปุ่ม Server ให้เริ่มเป็น Server
        serverBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartServer();
            HideUI();
        });

        // เมื่อกดปุ่ม Client ให้เริ่มเป็น Client (เข้าจอยคนอื่น)
        clientBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartClient();
            HideUI();
        });
    }

    private void HideUI() {
        // เมื่อเริ่มเกมแล้ว ให้ซ่อนปุ่มพวกนี้ไป
        gameObject.SetActive(false);
    }
}