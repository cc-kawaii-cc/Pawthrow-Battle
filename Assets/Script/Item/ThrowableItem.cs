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
        
        // เช็คว่าชนผู้เล่นหรือไม่
        bool hitPlayer = collision.gameObject.TryGetComponent(out PlayerHealth targetHealth);

        if (hitPlayer) 
        {
            // ถ้าชนคนปาเอง ให้ข้ามไป (ป้องกันชนตัวเองตอนเพิ่งปล่อยมือ)
            if (targetHealth.OwnerClientId == throwerId) return;
            
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

        // --- Destroy on hit player ---
        if (itemData.destroyOnHitPlayer && hitPlayer)
        {
            Invoke(nameof(DespawnSelf), itemData.destroyDelay);
            return;
        }

        // --- Destroy on hit ground (or wall/props) ---  
        if (itemData.destroyOnHitGround && !hitPlayer)
        {
            Invoke(nameof(DespawnSelf), itemData.destroyDelay);
        }
    }

    // ฟังก์ชันสำหรับทำลายไอเทมตัวเอง
    private void DespawnSelf()
    {
        // ต้องเช็คว่าเป็น Server และ Object ยังอยู่บน Network หรือไม่
        if (IsServer && IsSpawned) 
        {
            GetComponent<NetworkObject>().Despawn(true);
        }
    }
}