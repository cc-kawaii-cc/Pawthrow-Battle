using Unity.Netcode;
using UnityEngine;
public class PlayerController : NetworkBehaviour {
    public float moveSpeed = 5f;

    void Update() {
        // สำคัญมาก: ถ้าไม่ใช่ตัวละครของเรา (เครื่องเรา) ให้ข้ามไป ไม่ต้องคุม
        if (!IsOwner) return;

        // รับค่าจาก Keyboard
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        transform.position += move * moveSpeed * Time.deltaTime;
    }
}
