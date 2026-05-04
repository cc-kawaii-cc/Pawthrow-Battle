using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerNameTag : NetworkBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject canvasRoot;
    [SerializeField] private Transform canvasTransform;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image hpBar;
    
    private NetworkVariable<FixedString64Bytes> syncedName = new NetworkVariable<FixedString64Bytes>("", 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server);

    private PlayerHealth playerHealth;
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            string playerName = LobbyManager.Instance != null && !string.IsNullOrEmpty(LobbyManager.Instance.PlayerName) 
                ? LobbyManager.Instance.PlayerName 
                : "Player";
                
            SetNameServerRpc(playerName);
            if (canvasRoot != null)
            {
                canvasRoot.SetActive(false);
            }
        }
        
        syncedName.OnValueChanged += OnNameChanged;
        if (nameText != null) nameText.text = syncedName.Value.ToString();
        playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.currentHealth.OnValueChanged += OnHPChanged;
            OnHPChanged(0, playerHealth.currentHealth.Value);
        }
    }
    
    [ServerRpc]
    private void SetNameServerRpc(FixedString64Bytes name)
    {
        syncedName.Value = name;
    }
    
    private void OnNameChanged(FixedString64Bytes prev, FixedString64Bytes next)
    {
        if (nameText != null)
        {
            nameText.text = next.ToString();
        }
    }
    
    private void OnHPChanged(float prev, float next)
    {
        if (hpBar == null) return;

        hpBar.fillAmount = next / 100f;

        if (next > 60f)
            hpBar.color = Color.green;
        else if (next > 30f)
            hpBar.color = Color.yellow;
        else
            hpBar.color = Color.red;
    }
    
    private void LateUpdate()
    {
        if (Camera.main != null && canvasRoot != null && canvasRoot.activeSelf)
        {
            canvasTransform.LookAt(canvasTransform.position + Camera.main.transform.rotation * Vector3.forward, 
                                   Camera.main.transform.rotation * Vector3.up);
        }
    }
    
    public override void OnNetworkDespawn()
    {
        syncedName.OnValueChanged -= OnNameChanged;
        if (playerHealth != null)
        {
            playerHealth.currentHealth.OnValueChanged -= OnHPChanged;
        }
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.currentHealth.OnValueChanged -= OnHPChanged;
        }
    }
}