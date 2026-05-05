using Unity.Netcode;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    public float maxHealth = 100f;


    public NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );


    public bool isDead = false;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
            isDead = false;
        }
    }

    public bool Heal(float amount)
    {

        if (!IsServer || isDead) return false;
        if (currentHealth.Value >= maxHealth) return false;

        currentHealth.Value = Mathf.Min(currentHealth.Value + amount, maxHealth);
        return true;
    }

    public bool TakeDamage(float amount, float knockback = 0f, Vector3 direction = default)
    {
        if (!IsServer || isDead) return false;

        currentHealth.Value -= amount;

        if (knockback > 0f && direction != Vector3.zero)
        {
            if (TryGetComponent(out PlayerController pc))
            {
                pc.AddImpactClientRpc(direction * knockback);
            }
        }

        if (currentHealth.Value <= 0f)
        {
            currentHealth.Value = 0f;
            Die();
        }

        return true;
    }

    public void Die()
    {
        if (!IsServer || isDead) return;
        isDead = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RecordEliminationStat(OwnerClientId.ToString());
            GameManager.Instance.OnPlayerDied(OwnerClientId);
        }

        if (TryGetComponent(out PlayerController pc))
        {
            pc.DestroyCurrentItemServer();
        }

        PlayerDiedClientRpc();

        // [แก้ไข] เปลี่ยนจาก Invoke(nameof(DespawnPlayer)) มาเรียกใช้คำสั่งซ่อนตัวละครแทน
        HidePlayerClientRpc();
    }

    [ClientRpc]
    private void HidePlayerClientRpc()
    {
        if (TryGetComponent(out CharacterController cc)) cc.enabled = false;

        
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;

        
        foreach (var canvas in GetComponentsInChildren<Canvas>()) canvas.enabled = false;
    }
    public void ReviveOnServer()
    {
        if (!IsServer) return;
        
        isDead = false;
        currentHealth.Value = maxHealth; // คืนเลือดเต็มหลอด
        
        // สั่งให้ทุกเครื่อง (ทั้งหัวห้องและลูกห้อง) ยกเลิกการซ่อนตัว
        ReviveClientRpc(); 
    }

    [ClientRpc]
    private void ReviveClientRpc()
    {
        isDead = false;
        
        // เปิดโมเดล กล่องชน และหลอดเลือดให้กลับมามองเห็นได้อีกครั้งในทุกหน้าจอ!
        if (TryGetComponent(out CharacterController cc)) cc.enabled = true;
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = true;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = true;
        foreach (var canvas in GetComponentsInChildren<Canvas>()) canvas.enabled = true;
    }

    [ClientRpc]
    private void PlayerDiedClientRpc()
    {
        isDead = true;

        if (TryGetComponent(out PlayerController pc))
        {
            pc.enabled = false;
        }

        if (IsOwner)
        {
            GameMenuUI menuUI = FindObjectOfType<GameMenuUI>();
            if (menuUI != null)
            {
                menuUI.ShowGameOver();
            }


            if (Camera.main != null)
            {
                SpectatorCameraController specCam = Camera.main.GetComponent<SpectatorCameraController>();
                if (specCam != null)
                {
                    specCam.enabled = true;
                }
            }
        }
    }
}