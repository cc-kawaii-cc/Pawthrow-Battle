using Unity.Netcode;
using UnityEngine;

public class HazardCarCollision : NetworkBehaviour
{
    public enum HazardType { Lethal, Stun }

    [Header("Hazard Mode")]
    [Tooltip("เลือกประเภทของรถ: Lethal = ชนกระเด็นตายทันที, Stun = ชนเบาๆ แล้วติดสตัน")]
    public HazardType hazardType = HazardType.Lethal;

    [Header("Lethal Settings (วิ่งเร็ว)")]
    public float lethalDamage = 9999f;     // ดาเมจมหาศาล
    public float lethalKnockback = 50f;    // กระเด็นแรงมาก

    [Header("Stun Settings (วิ่งช้า)")]
    public float stunDamage = 10f;         // ดาเมจนิดหน่อย (หรือปรับเป็น 0 ก็ได้ถ้าไม่อยากให้ลด)
    public float stunKnockback = 15f;      // กระเด็นเบาๆ
    public float stunDuration = 3f;        // ระยะเวลาติดสตัน

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.TryGetComponent(out PlayerHealth targetHealth))
        {
            // คำนวณทิศทางการกระเด็น (ให้กระเด็นออกจากรถ)
            Vector3 pushDirection = (targetHealth.transform.position - transform.position).normalized;
            pushDirection.y = 0.5f; 
            pushDirection = pushDirection.normalized;

            if (hazardType == HazardType.Lethal)
            {
                // โหมดรถซิ่ง: ชนตายทันที + กระเด็นแรง
                targetHealth.TakeDamage(lethalDamage, lethalKnockback, pushDirection);
            }
            else if (hazardType == HazardType.Stun)
            {
                // โหมดรถช้า: ดาเมจเบา + กระเด็นนิดหน่อย
                bool isHit = targetHealth.TakeDamage(stunDamage, stunKnockback, pushDirection);

                // ตรวจสอบว่าโดนดาเมจจริงไหม (ถ้าตัวละครใช้สกิลโล่ป้องกันไว้ได้ TakeDamage จะ return false)
                // ถ้าโดนชนเต็มๆ (isHit = true) ให้เรียกคำสั่ง Stun
                if (isHit && other.TryGetComponent(out PlayerController targetController))
                {
                    targetController.ApplyStunClientRpc(stunDuration);
                }
            }
        }
    }
}