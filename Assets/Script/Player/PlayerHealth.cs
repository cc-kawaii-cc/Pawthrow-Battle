using Unity.Netcode;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour 
{
    public NetworkVariable<float> currentHealth = new NetworkVariable<float>(100f);
    private CharacterController controller;
    void Awake() 
    {
        controller = GetComponent<CharacterController>();
    }

    public bool TakeDamage(float amount, float knockback, Vector3 direction) 
    {
        if (!IsServer) return false;
        if (TryGetComponent(out PlayerController playerController)) 
        {
            if (playerController.isShieldActive.Value) 
            {
                Debug.Log($"Player {OwnerClientId} Use the shield to block the attack!");
                playerController.BreakShieldServerRpc(); 
                return false;
            }
        }
        currentHealth.Value -= amount;
        Debug.Log($"Player {OwnerClientId} HP: {currentHealth.Value}");
        ApplyKnockbackClientRpc(direction * knockback);
        if (currentHealth.Value <= 0)
        {
            Die();
        }
        
        return true; 
    }

    public void Die() 
    {
        if (!IsServer) return; 
        Debug.Log($"Player {OwnerClientId} Died!");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerDied(GetComponent<PlayerController>());
        }
        PlayerDiedClientRpc();
        GetComponent<NetworkObject>().Despawn(true);
    }

    [ClientRpc]
    void PlayerDiedClientRpc()
    {
        if (!IsOwner) return;
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.SetParent(null); 
            if (mainCam.GetComponent<SpectatorCameraController>() == null)
            {
                mainCam.gameObject.AddComponent<SpectatorCameraController>();
            }
        }
        GameMenuUI menuUI = FindObjectOfType<GameMenuUI>();
        if (menuUI != null)
        {
            menuUI.ShowGameOver();
        }
    }

    [ClientRpc]
    void ApplyKnockbackClientRpc(Vector3 force) 
    {
        if (!IsOwner) return;
        if (TryGetComponent(out PlayerController playerController)) 
        {
            playerController.AddImpact(force);
        }
    }

    public override void OnNetworkSpawn()
    {
        
    }
}