using Unity.Netcode;
using UnityEngine;

public class FireZone : NetworkBehaviour
{
    [Header("Settings")]
    public float damagePerSecond = 12f;
    public float zoneDuration = 5f;
    public float zoneRadius = 2.5f;
    
    // กำหนดค่าเริ่มต้นให้กับ NetworkVariable
    private NetworkVariable<float> timeLeft = new NetworkVariable<float>(0f);
    private float tickTimer = 0f;

    public override void OnNetworkSpawn()
    {
        if (IsServer) 
        {
            timeLeft.Value = zoneDuration;
        }
    }

    void Update()
    {
        // ให้ Server เป็นคนรันการนับเวลาและการทำดาเมจเท่านั้น
        if (!IsServer) return;
        
        timeLeft.Value -= Time.deltaTime;
        if (timeLeft.Value <= 0f) 
        { 
            if (IsSpawned) GetComponent<NetworkObject>().Despawn(true); 
            return; 
        }
        
        tickTimer += Time.deltaTime;
        if (tickTimer >= 0.5f)
        {
            tickTimer = 0f;
            BurnPlayersInZone();
        }
    }

    void BurnPlayersInZone()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, zoneRadius);
        foreach (var col in hits)
        {
            // เช็คว่าเจอ PlayerHealth หรือไม่
            if (col.TryGetComponent(out PlayerHealth health))
            {
                // ดาเมจต่อ 0.5 วินาที คือ damagePerSecond * 0.5
                // ระยะกระเด็นเป็น 0 (ไฟลวกไม่ต้องกระเด็น) ทิศทางเป็น Vector3.zero
                health.TakeDamage(damagePerSecond * 0.5f, 0f, Vector3.zero);
            }
        }
    }

    void OnDrawGizmos()
    {
        // วาดวงกลมสีส้มโปร่งใสในหน้า Scene เพื่อให้ตั้งค่ารัศมีได้ง่ายขึ้น
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, zoneRadius);
    }
}