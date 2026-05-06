using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class DecoyController : NetworkBehaviour
{
    // กำหนดค่าเริ่มต้นเป็น 0 ไว้ก่อน เดี๋ยว ServerRpc จะเป็นคนเซ็ตค่าให้ตอน Spawn
    public NetworkVariable<float> lifetime = new NetworkVariable<float>(0f);
    
    public ParticleSystem spawnVFX;
    public ParticleSystem despawnVFX;
    
    private bool isDespawning = false; // ป้องกันการเรียก Despawn ซ้ำซ้อน

    public override void OnNetworkSpawn()
    {
        // เล่น Effect ตอนเกิด (ทำงานทุก Client)
        if (spawnVFX != null) spawnVFX.Play();
    }

    void Update()
    {
        // ให้ Server เป็นคนจัดการเวลาเท่านั้น
        if (!IsServer || isDespawning) return;

        lifetime.Value -= Time.deltaTime;
        
        if (lifetime.Value <= 0f) 
        {
            StartCoroutine(DespawnRoutine());
        }
    }

    private IEnumerator DespawnRoutine()
    {
        isDespawning = true;
        
        // แจ้งให้ทุก Client เล่น Effect ทำลายร่างปลอมก่อน
        PlayDespawnVFXClientRpc();
        
        // รอให้ Effect เล่น (ปรับเวลาให้ตรงกับความยาวของ Effect คุณได้เลย)
        yield return new WaitForSeconds(0.5f);
        
        // Despawn ออกจาก Network
        if (IsSpawned) 
        {
            GetComponent<NetworkObject>().Despawn(true);
        }
    }

    [ClientRpc]
    void PlayDespawnVFXClientRpc()
    {
        if (despawnVFX != null) despawnVFX.Play();
    }

    // เมื่อถูกไอเทมปาใส่ ให้ Decoy หายทันที
    void OnTriggerEnter(Collider other)
    {
        if (!IsServer || isDespawning) return;

        // เช็คว่าสิ่งที่พุ่งเข้ามาชนมีสคริปต์ ThrowableItem หรือไม่
        if (other.GetComponent<ThrowableItem>() != null)
        {
            StartCoroutine(DespawnRoutine());
        }
    }
}