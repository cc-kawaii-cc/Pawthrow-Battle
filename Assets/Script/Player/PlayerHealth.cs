using Unity.Netcode;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour 
{
    // ใช้ NetworkVariable เพื่อให้เลือดตรงกันทุกคน และ Server เป็นคนแก้ค่าเท่านั้น
    public NetworkVariable<float> currentHealth = new NetworkVariable<float>(100f);
    private CharacterController controller;

    void Awake() {
        controller = GetComponent<CharacterController>();
    }

    // ฟังก์ชันรับดาเมจ (เรียกจาก Server เท่านั้น)
    public void TakeDamage(float amount, float knockback, Vector3 direction) {
        if (!IsServer) return;

        currentHealth.Value -= amount;
        Debug.Log($"Player {OwnerClientId} HP: {currentHealth.Value}");

        // สั่งให้เครื่อง Client ของคนที่โดนปา "ตัวเด้ง"
        ApplyKnockbackClientRpc(direction * knockback);

        if (currentHealth.Value <= 0) {
            Die(); // 🌟 เรียกใช้ฟังก์ชันตายตรงนี้
        }
    }

    // 🌟 ระบบทำลายตัวละครเมื่อเลือดหมด
    public void Die() {
        if (!IsServer) return; // Server เท่านั้นที่สั่งทำลายได้

        Debug.Log($"Player {OwnerClientId} ตายแล้ว!");
        
        // ลบตัวละครออกจากระบบ Network ของทุกคน
        GetComponent<NetworkObject>().Despawn(true);
    }

    [ClientRpc]
    void ApplyKnockbackClientRpc(Vector3 force) {
        if (!IsOwner) return;
        StartCoroutine(KnockbackRoutine(force));
    }

    private System.Collections.IEnumerator KnockbackRoutine(Vector3 force) {
        float timer = 0.2f; // ระยะเวลาที่โดนเด้ง
        while (timer > 0) {
            if (controller != null) {
                controller.Move(force * Time.deltaTime);
            }
            timer -= Time.deltaTime;
            yield return null;
        }
    }

    public override void OnNetworkSpawn()
    {
        // สำคัญมาก: เมื่อตัวละครเกิด ต้องสั่งให้ UI อัปเดตตามค่าเลือดปัจจุบันทันที
        if (currentHealth.Value > 0)
        {
           
        }
    }
}