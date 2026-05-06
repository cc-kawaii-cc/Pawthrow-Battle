using UnityEngine;

public class PlayerSpawnZone : MonoBehaviour
{
    [Header("Spawn Area")]
    public Vector2 spawnAreaSize = new Vector2(20f, 20f);

    [Header("Ground Detection")]
    [Tooltip("เลือก Layer ของพื้น/ถนนที่อนุญาตให้เกิด เช่น 'Ground', 'Road'\n" +
             "วิธีตั้ง: ไปที่ Edit > Project Settings > Tags and Layers\n" +
             "แล้วตั้ง Layer ให้ Object พื้นในเกม จากนั้นเลือก Layer นั้นตรงนี้")]
    public LayerMask groundLayerMask = ~0; // Default = ทุก Layer (แก้ใน Inspector)

    [Tooltip("ความสูง Y สูงสุดที่ถือว่าเป็น 'พื้น' — Object ที่ถูก Raycast โดนแล้วสูงกว่านี้ = หลังคา ให้ข้ามไป")]
    public float maxGroundY = 1.5f;

    [Tooltip("ความสูงจากพื้นที่ปล่อย Player ลงมา (Drop Effect)")]
    public float dropHeight = 15f;

    [Tooltip("จำนวนครั้งสูงสุดที่จะสุ่มหาจุดเกิด")]
    public int maxSpawnAttempts = 50;

    // =========================================================
    //  Public API: เรียกจาก PlayerController
    // =========================================================

    /// <summary>
    /// หาตำแหน่งเกิดที่ปลอดภัย (บนพื้น/ถนน ไม่ใช่หลังคา)
    /// คืนค่าตำแหน่งบนฟ้า dropHeight เมตรเหนือจุดที่พบ
    /// เพื่อให้ Player ร่วงลงมาสวยๆ
    /// </summary>
    public Vector3 GetSafeSpawnPosition()
    {
        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            // 1. สุ่มจุดใน XZ ภายใน Zone
            float randomX = transform.position.x + Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f);
            float randomZ = transform.position.z + Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f);

            // 2. ยิง Ray จากฟ้าลงมา โดนเฉพาะ Layer ที่เลือกไว้เท่านั้น
            Vector3 rayOrigin = new Vector3(randomX, transform.position.y + 60f, randomZ);

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 100f, groundLayerMask))
            {
                // 3. เช็คว่าจุดที่โดนสูงเกิน maxGroundY ไหม (ถ้าเกิน = หลังคา)
                if (hit.point.y > maxGroundY)
                {
                    // เป็นหลังคา → ข้ามไปสุ่มใหม่
                    continue;
                }

                // 4. เจอพื้นจริงๆ → คืนตำแหน่งฟ้าเหนือจุดนั้น dropHeight เมตร
                return new Vector3(hit.point.x, hit.point.y + dropHeight, hit.point.z);
            }
        }

        // Fallback: ถ้าหาไม่เจอใน maxSpawnAttempts ครั้ง → เกิดตรงกลาง Zone
        Debug.LogWarning("[PlayerSpawnZone] หาจุดเกิดที่ปลอดภัยไม่ได้ใน " + maxSpawnAttempts + " ครั้ง! ใช้ตำแหน่งกลาง Zone แทน\n" +
                         "แนะนำ: ตรวจสอบว่า Object พื้น/ถนน ได้ตั้ง Layer ที่เลือกไว้ใน groundLayerMask แล้วหรือยัง");
        return transform.position + new Vector3(0f, dropHeight, 0f);
    }

    // =========================================================
    //  Gizmos: แสดง Zone ใน Scene View
    // =========================================================
    private void OnDrawGizmos()
    {
        // พื้นที่ Spawn
        Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
        Gizmos.DrawCube(transform.position, new Vector3(spawnAreaSize.x, 1f, spawnAreaSize.y));
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(spawnAreaSize.x, 1f, spawnAreaSize.y));

        // เส้น maxGroundY
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
        Vector3 lineCenter = new Vector3(transform.position.x, maxGroundY, transform.position.z);
        Gizmos.DrawWireCube(lineCenter, new Vector3(spawnAreaSize.x, 0.05f, spawnAreaSize.y));

        // Label บอก Layer (แสดงเฉพาะใน Editor)
#if UNITY_EDITOR
        UnityEditor.Handles.color = Color.yellow;
        UnityEditor.Handles.Label(
            transform.position + new Vector3(-spawnAreaSize.x / 2f, 2f, -spawnAreaSize.y / 2f),
            $"SpawnZone\nMax Ground Y: {maxGroundY}\nDrop H: {dropHeight}m"
        );
#endif
    }
}