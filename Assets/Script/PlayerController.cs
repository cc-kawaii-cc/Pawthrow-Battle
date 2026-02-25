using Unity.Netcode;
using UnityEngine;
public class PlayerController : NetworkBehaviour 
{
    public float moveSpeed = 5f;
    void Update() 
    {
        if (!IsOwner) return;
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 move = transform.right * x + transform.forward * z;
        transform.position += move * moveSpeed * Time.deltaTime;
    }
}
