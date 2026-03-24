using Unity.Netcode;
using UnityEngine;

public class ThrowableItem : NetworkBehaviour
{
    public ItemData itemData;
    private Rigidbody rb;
    private Collider col;
    
    private bool isFlying = false; 
    private ulong throwerId; 

    void Awake() {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    public void Grab(Transform playerRoot, Transform handPoint) {
        isFlying = false;
        
        // เพิ่ม: จำ ID ของคนที่หยิบของชิ้นนี้ขึ้นมา
        if (playerRoot.TryGetComponent(out NetworkObject playerNetObj)) {
            throwerId = playerNetObj.OwnerClientId;
        }

        GetComponent<NetworkObject>().TrySetParent(playerRoot);
        transform.position = handPoint.position;
        transform.rotation = handPoint.rotation;
        SetPhysicsClientRpc(true);
    }

    public void Throw(Vector3 direction, float force) {
        GetComponent<NetworkObject>().TryRemoveParent();
        SetPhysicsClientRpc(false);
        isFlying = true; 
        rb.AddForce(direction * force, ForceMode.Impulse);
    }

    [ClientRpc]
    void SetPhysicsClientRpc(bool isHeld) {
        if (rb != null) rb.isKinematic = isHeld;
        if (col != null) col.enabled = !isHeld; 
    }

   
    private void OnCollisionEnter(Collision collision) 
    {
        if (!IsServer || !isFlying) return;

        if (collision.gameObject.TryGetComponent(out PlayerHealth targetHealth)) 
        {
            
            if (targetHealth.OwnerClientId == throwerId) return;

           
            Vector3 pushDirection = (targetHealth.transform.position - transform.position).normalized;
            pushDirection.y = 0.5f; 
            pushDirection = pushDirection.normalized;

            targetHealth.TakeDamage(itemData.damage, itemData.knockbackForce, pushDirection);
        }

        
        isFlying = false; 
    }
}