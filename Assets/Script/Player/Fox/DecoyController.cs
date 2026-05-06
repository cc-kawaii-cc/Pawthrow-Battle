using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class DecoyController : NetworkBehaviour
{
    [Header("Decoy Settings")]
    public float moveSpeed = 2.5f;     // ความเร็วเดิน (ช้าๆ เนียนๆ)
    public float rotationSpeed = 60f; // ความเร็วการหมุน (ทำให้เดินวน)
    public float lifetime = 5f;       // ระยะเวลาที่จะหายไปเอง

    [Header("VFX")]
    public ParticleSystem spawnVFX;
    public ParticleSystem despawnVFX;
    
    private bool isDespawning = false;

    public override void OnNetworkSpawn()
    {
        // เล่นเอฟเฟกต์ตอนเกิดทุกเครื่อง
        if (spawnVFX != null) spawnVFX.Play();
        
        // ให้ Server เริ่มนับเวลาถอยหลังทำลายตัวเอง
        if (IsServer)
        {
            Invoke(nameof(StartDespawn), lifetime);
        }
    }

    void Update()
    {
        // การเคลื่อนที่: ให้ขยับเฉพาะใน Server แล้ว NetworkTransform จะซิงก์ไป Client เอง
        if (!IsServer || isDespawning) return;

        // เดินไปข้างหน้าช้าๆ และหมุนตัวเล็กน้อยเพื่อให้เดินวน
        transform.position += transform.forward * moveSpeed * Time.deltaTime;
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }

    private void StartDespawn()
    {
        if (isDespawning) return;
        StartCoroutine(DespawnRoutine());
    }

    private IEnumerator DespawnRoutine()
    {
        isDespawning = true;
        
        // สั่งให้ทุกเครื่องเล่นเอฟเฟกต์ก่อนหายไป
        PlayDespawnVFXClientRpc();
        
        yield return new WaitForSeconds(0.5f); // รอให้เอฟเฟกต์เล่นจบเล็กน้อย
        
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

    // ถ้าโดนของปาใส่ ให้หายไปทันที
    void OnTriggerEnter(Collider other)
    {
        if (!IsServer || isDespawning) return;
        if (other.GetComponent<ThrowableItem>() != null)
        {
            StartDespawn();
        }
    }
}