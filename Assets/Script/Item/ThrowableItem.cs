using Unity.Netcode;
using UnityEngine;

public class ThrowableItem : NetworkBehaviour
{
    private Rigidbody rb;
    private Collider col;
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }
    public void Grab(Transform playerRoot, Transform handPoint)
    {
        GetComponent<NetworkObject>().TrySetParent(playerRoot);
        transform.position = handPoint.position;
        transform.rotation = handPoint.rotation;
        SetPhysicsClientRpc(true);
    }
    public void Throw(Vector3 direction, float force)
    {
        GetComponent<NetworkObject>().TryRemoveParent();
        SetPhysicsClientRpc(false);
        rb.AddForce(direction * force, ForceMode.Impulse);
    }
    [ClientRpc]
    void SetPhysicsClientRpc(bool isHeld)
    {
        if (rb != null) rb.isKinematic = isHeld;
        if (col != null) col.enabled = !isHeld;
    }
}