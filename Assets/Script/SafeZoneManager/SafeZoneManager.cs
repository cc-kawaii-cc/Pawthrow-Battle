using Unity.Netcode;
using UnityEngine;

public class SafeZoneManager : NetworkBehaviour
{
    public static SafeZoneManager Instance;
    
    [Header("Zone Config")]
    public float startRadius = 80f;
    public float minRadius = 8f;
    public float shrinkStartDelay = 60f;
    public float shrinkDuration = 180f;
    public float outsideDPS = 10f; // damage per second
    
    [Header("Visual")]
    public GameObject zoneBoundaryVisual; // cylinder/circle mesh สีแดง
    
    // กำหนด Data Type ให้ NetworkVariable อย่างชัดเจน
    private NetworkVariable<float> currentRadius = new NetworkVariable<float>(80f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<Vector3> zoneCenter = new NetworkVariable<Vector3>(Vector3.zero,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    private float gameTimer = 0f;
    private float dmgTimer = 0f;

    private void Awake() 
    { 
        Instance = this; 
    }

    public override void OnNetworkSpawn()
    {
        currentRadius.OnValueChanged += OnRadiusChanged;
        if (IsServer) 
        {
            zoneCenter.Value = Vector3.zero; // กำหนดให้อยู่กลาง map
            currentRadius.Value = startRadius; // กำหนดรัศมีเริ่มต้น
        }
    }

    private void Update()
    {
        if (IsServer) ServerUpdate();
        UpdateVisual();
    }

    private void ServerUpdate()
    {
        gameTimer += Time.deltaTime;
        
        // Shrink
        if (gameTimer > shrinkStartDelay)
        {
            float t = (gameTimer - shrinkStartDelay) / shrinkDuration;
            currentRadius.Value = Mathf.Lerp(startRadius, minRadius, Mathf.Clamp01(t));
        }
        
        // Damage players outside zone
        dmgTimer += Time.deltaTime;
        if (dmgTimer >= 1f)
        {
            dmgTimer = 0f;
            // ระบุชนิดที่ต้องการหา (PlayerHealth) ให้ชัดเจน
            foreach (var health in FindObjectsOfType<PlayerHealth>())
            {
                // คำนวณระยะห่างเฉพาะแกน X และ Z
                float dist = Vector3.Distance(
                    new Vector3(health.transform.position.x, 0, health.transform.position.z),
                    new Vector3(zoneCenter.Value.x, 0, zoneCenter.Value.z));
                    
                if (dist > currentRadius.Value)
                {
                    health.TakeDamage(outsideDPS, 0f, Vector3.zero); // burn ไม่มี knockback
                }
            }
        }
    }

    private void UpdateVisual()
    {
        if (zoneBoundaryVisual == null) return;
        float r = currentRadius.Value;
        
        // ขยาย Scale ให้สัมพันธ์กับรัศมี (คูณ 2 เพราะ Scale ของ Sphere/Cylinder คือเส้นผ่านศูนย์กลาง)
        zoneBoundaryVisual.transform.localScale = new Vector3(r * 2f, 50f, r * 2f);
        zoneBoundaryVisual.transform.position = new Vector3(zoneCenter.Value.x, 0f, zoneCenter.Value.z);
    }

    private void OnRadiusChanged(float prev, float next) { /* visual update ทำใน UpdateVisual ไปแล้ว */ }
    
    public float GetCurrentRadius() => currentRadius.Value;
    public Vector3 GetCenter() => zoneCenter.Value;

    public override void OnNetworkDespawn()
    {
        currentRadius.OnValueChanged -= OnRadiusChanged;
    }
}