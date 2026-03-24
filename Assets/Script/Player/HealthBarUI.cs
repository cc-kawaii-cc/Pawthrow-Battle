using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth; // ลาก Script PlayerHealth ของตัวละครมาใส่
    [SerializeField] private Image healthFillImage;
    private Camera mainCamera;

    void Start()
    {
        // แทนที่จะลากใส่ใน Inspector ให้โค้ดหาเองจาก Object พ่อ (ตัวละคร)
        if (playerHealth == null)
        {
            playerHealth = GetComponentInParent<PlayerHealth>();
        }

        if (playerHealth != null)
        {
            playerHealth.currentHealth.OnValueChanged += UpdateHealthBar;
            UpdateHealthBar(0, playerHealth.currentHealth.Value); // อัปเดตครั้งแรก
        }
    }

    private void UpdateHealthBar(float previousValue, float newValue)
    {
        // คำนวณ % เลือด (สมมติเลือดเต็ม 100)
        healthFillImage.fillAmount = newValue / 100f;
    }

    void LateUpdate()
    {
        // หา Camera ที่เปิดใช้งานอยู่ตัวเดียวในเครื่องเรา
        if (mainCamera == null) mainCamera = Camera.main;

        if (mainCamera != null)
        {
            // ทำให้แถบเลือดหันหน้าเข้าหากล้องแบบนิ่งๆ
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                mainCamera.transform.rotation * Vector3.up);
        }
    }

    private void OnDestroy()
    {
        // ป้องกัน Error เมื่อ Object ถูกทำลาย
        if (playerHealth != null)
            playerHealth.currentHealth.OnValueChanged -= UpdateHealthBar;
    }
}