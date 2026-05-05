using Unity.Netcode;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    public float maxHealth = 100f;

    // ให้เขียนค่าได้เฉพาะ Server แต่ทุกคนอ่านค่าเพื่อไปโชว์หลอดเลือดได้
    public NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        100f, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    private bool isDead = false;

    // [โค้ดเดิมของคุณ: ตัวแปรอื่นๆ เช่น แอนิเมชัน, เอฟเฟกต์ตอนโดนตี]

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
            isDead = false;
        }
    }

    // --- NEW HEAL FUNCTION ---
    public bool Heal(float amount)
    {
        // Only the Server can heal, and dead players can't be healed
        if (!IsServer || isDead) return false;

        // Don't heal if already at max health
        if (currentHealth.Value >= maxHealth) return false;

        // Apply healing, capped at maxHealth
        currentHealth.Value = Mathf.Min(currentHealth.Value + amount, maxHealth);
        return true; // Successfully healed
    }

    // ฟังก์ชันรับดาเมจและ Knockback
    public bool TakeDamage(float amount, float knockback = 0f, Vector3 direction = default)
    {
        if (!IsServer || isDead) return false;

        currentHealth.Value -= amount;

        // ถ้ารับ Knockback (ไม่ใช่ burn damage ที่ knockback เป็น 0)
        if (knockback > 0f && direction != Vector3.zero)
        {
            if (TryGetComponent(out PlayerController pc))
            {
                pc.AddImpactClientRpc(direction * knockback);
            }
        }

        // เช็คตาย
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

        // [โค้ดเดิมของคุณ: ลอจิกการตาย, เล่นแอนิเมชันตุย, ดรอปของ ฯลฯ]

        // เรียกเก็บ Analytics ตอนตายทันที (รันบน Server)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RecordEliminationStat(OwnerClientId.ToString());
            
            // แจ้ง GameManager ว่าผู้เล่นคนนี้ขิตแล้ว
            GameManager.Instance.OnPlayerDied(OwnerClientId);
        }
            
        // [โค้ดเดิมของคุณ: จัดการเรื่อง NetworkObject เช่น Despawn หรือเปลี่ยนสถานะไปเป็นผู้ชม]
    }
}