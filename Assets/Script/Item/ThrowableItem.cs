using Unity.Netcode;
using UnityEngine;

public class ThrowableItem : NetworkBehaviour
{
    public ItemData itemData;
    private Rigidbody rb;
    private Collider col;
    
    private bool isFlying = false; 
    private ulong throwerId; 

    private float currentDamageMultiplier = 1f; 

    void Awake() 
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    public void Grab(Transform playerRoot, Transform handPoint) 
    {
        isFlying = false;
        if (playerRoot.TryGetComponent(out NetworkObject playerNetObj)) 
        {
            throwerId = playerNetObj.OwnerClientId;
        }
        GetComponent<NetworkObject>().TrySetParent(playerRoot);
        transform.position = handPoint.position;
        transform.rotation = handPoint.rotation;
        SetPhysicsClientRpc(true);
    }

    public void Throw(Vector3 direction, float force, float chargeMultiplier) 
    {
        GetComponent<NetworkObject>().TryRemoveParent();
        SetPhysicsClientRpc(false);
        isFlying = true; 
        currentDamageMultiplier = chargeMultiplier; 
        rb.AddForce(direction * (force * chargeMultiplier), ForceMode.Impulse);
    }

    [ClientRpc]
    void SetPhysicsClientRpc(bool isHeld) 
    {
        if (rb != null) rb.isKinematic = isHeld;
        if (col != null) col.enabled = !isHeld; 
    }

    private void OnCollisionEnter(Collision collision) 
    {
        if (!IsServer || !isFlying) return;

        // --- EXPLOSIVE: ระบบระเบิดตูมเดียว ---
        if (itemData.isExplosive)
        {
            isFlying = false;
            Explode(transform.position);
            return;
        }

        // --- NORMAL: ลอจิกการชนแบบ Single-target แบบเดิม ---
        bool hitPlayer = collision.gameObject.TryGetComponent(out PlayerHealth targetHealth);

        if (hitPlayer) 
        {
            if (targetHealth.OwnerClientId == throwerId) return; // ไม่โดนตัวเอง
            
            Vector3 pushDirection = (targetHealth.transform.position - transform.position).normalized;
            pushDirection.y = 0.5f; 
            pushDirection = pushDirection.normalized;
            
            bool attackSuccess = targetHealth.TakeDamage(itemData.damage * currentDamageMultiplier, itemData.knockbackForce * currentDamageMultiplier, pushDirection);
            
            if (attackSuccess && itemData.isHeavyItem)
            {
                if (collision.gameObject.TryGetComponent(out PlayerController targetController))
                {
                    targetController.ApplyStunClientRpc(itemData.stunDuration); 
                }
            }
        }
        
        isFlying = false; 

        // ==========================================
        // 🔥 FIRE SYSTEM: เสกกองไฟเมื่อชนผู้เล่นหรือพื้น
        // ==========================================
        if (itemData.isFireItem && itemData.fireZonePrefab != null)
        {
            Vector3 firePos = collision.contacts[0].point;
            firePos.y += 0.1f; // lift เล็กน้อยไม่ให้จมดิน
            SpawnFireZone(firePos);
        }

        // --- Destroy on hit player ---
        if (itemData.destroyOnHitPlayer && hitPlayer)
        {
            Invoke(nameof(DespawnSelf), itemData.destroyDelay);
            return;
        }

        // --- Destroy on hit ground ---  
        if (itemData.destroyOnHitGround && !hitPlayer)
        {
            Invoke(nameof(DespawnSelf), itemData.destroyDelay);
        }
    }

    private void SpawnFireZone(Vector3 pos)
    {
        // สร้างกองไฟและ Spawn ผ่าน Network ทันที (ฟังก์ชันนี้ถูกรันโดย Server อยู่แล้ว)
        GameObject go = Instantiate(itemData.fireZonePrefab, pos, Quaternion.identity);
        go.GetComponent<NetworkObject>().Spawn();
    }

    // ==========================================
    // 💥 EXPLOSION SYSTEM
    // ==========================================
    private void Explode(Vector3 center)
    {
        SpawnExplosionVFXClientRpc(center);
        
        Collider[] hits = Physics.OverlapSphere(center, itemData.explosionRadius);
        foreach (var col in hits)
        {
            if (!col.TryGetComponent(out PlayerHealth health)) continue;
            if (health.OwnerClientId == throwerId) continue; 
            
            Vector3 dir = (col.transform.position - center).normalized;
            dir.y = 0.5f; 
            dir = dir.normalized;
            
            bool hit = health.TakeDamage(itemData.explosionDamage * currentDamageMultiplier, itemData.explosionKnockback * currentDamageMultiplier, dir);
            
            if (hit && itemData.explosionStunDuration > 0f)
            {
                if (col.TryGetComponent(out PlayerController ctrl))
                {
                    ctrl.ApplyStunClientRpc(itemData.explosionStunDuration);
                }
            }
        }
        Invoke(nameof(DespawnSelf), 0.15f);
    }

    [ClientRpc]
    private void SpawnExplosionVFXClientRpc(Vector3 pos)
    {
        if (itemData.explosionVFXPrefab != null)
        {
            GameObject vfx = Instantiate(itemData.explosionVFXPrefab, pos, Quaternion.identity);
            Destroy(vfx, 3f); 
        }
    }

    private void DespawnSelf()
    {
        if (IsServer && IsSpawned) 
        {
            GetComponent<NetworkObject>().Despawn(true);
        }
    }
}