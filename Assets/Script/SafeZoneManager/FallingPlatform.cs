using Unity.Netcode;
using UnityEngine;
using System.Collections;

/// <summary>
/// ติดกับ GameObject แต่ละชิ้นที่ต้องการให้ร่วง
/// ต้องมี NetworkObject Component ด้วย
/// </summary>
public class FallingPlatform : NetworkBehaviour
{
    [Header("Warning Flash Settings")]
    [Tooltip("ระยะเวลากระพริบสีแดงก่อนร่วง (วินาที)")]
    public float warningDuration = 3f;

    [Tooltip("ความเร็วในการกระพริบ (ครั้งต่อวินาที)")]
    public float flashSpeed = 4f;

    [Header("Fall Settings")]
    [Tooltip("ความเร็วในการตกลง")]
    public float fallSpeed = 8f;

    [Tooltip("ระยะทางที่ตกลงก่อนถูกทำลาย")]
    public float destroyAfterFallDistance = 30f;

    [Header("Danger Zone VFX")]
    [Tooltip("ลาก Particle System หรือ Projector สำหรับแสดง Danger Zone บนพื้น (ไม่บังคับ)")]
    public GameObject dangerZoneIndicator;

    // NetworkVariables สำหรับ Sync สถานะไปทุก Client
    private NetworkVariable<bool> isWarning = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> isFalling = new NetworkVariable<bool>(false);

    private Renderer[] renderers;
    private Color[] originalColors;
    private Vector3 startPosition;
    private Coroutine flashCoroutine;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            // บันทึกสีเดิมของแต่ละ Renderer
            if (renderers[i].material.HasProperty("_Color"))
                originalColors[i] = renderers[i].material.color;
            else
                originalColors[i] = Color.white;
        }

        startPosition = transform.position;
    }

    public override void OnNetworkSpawn()
    {
        // Subscribe ฟังการเปลี่ยนแปลงค่าจาก Server
        isWarning.OnValueChanged += OnWarningStateChanged;
        isFalling.OnValueChanged += OnFallingStateChanged;

        // ซ่อน Danger Zone ไว้ก่อน
        if (dangerZoneIndicator != null)
            dangerZoneIndicator.SetActive(false);
    }

    public override void OnNetworkDespawn()
    {
        isWarning.OnValueChanged -= OnWarningStateChanged;
        isFalling.OnValueChanged -= OnFallingStateChanged;
    }

    // =========================================================
    //  Server-Side: เรียกจาก MapDestructionManager
    // =========================================================

    /// <summary>
    /// [Server Only] เริ่มกระบวนการเตือน → ร่วง
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void TriggerFallSequenceServerRpc()
    {
        if (!IsServer) return;
        StartCoroutine(FallSequenceCoroutine());
    }

    // เรียกตรงๆ ได้เลยถ้าเรียกจาก Server Script
    public void TriggerFallSequence()
    {
        if (!IsServer) return;
        StartCoroutine(FallSequenceCoroutine());
    }

    private IEnumerator FallSequenceCoroutine()
    {
        // --- Phase 1: Warning (กระพริบแดง) ---
        isWarning.Value = true;
        yield return new WaitForSeconds(warningDuration);
        isWarning.Value = false;

        // --- Phase 2: Fall ---
        isFalling.Value = true;

        // ปิด Collider ให้ผู้เล่นที่ยืนอยู่บนนั้นตกลงมาด้วย
        DisableCollidersClientRpc();

        float fallDistance = 0f;
        while (fallDistance < destroyAfterFallDistance)
        {
            float step = fallSpeed * Time.deltaTime;
            transform.position += Vector3.down * step;
            fallDistance += step;
            yield return null;
        }

        // ทำลาย Platform หลังร่วงพอสมควรแล้ว
        GetComponent<NetworkObject>().Despawn(true);
    }

    // =========================================================
    //  Client-Side: รับ Callback จาก NetworkVariable
    // =========================================================

    private void OnWarningStateChanged(bool previous, bool current)
    {
        if (current)
        {
            // เริ่มกระพริบแดง + แสดง Danger Zone
            if (dangerZoneIndicator != null)
                dangerZoneIndicator.SetActive(true);

            flashCoroutine = StartCoroutine(FlashRedCoroutine());
        }
        else
        {
            // หยุดกระพริบ คืนสีเดิม
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            SetColor(Color.red); // ค้างแดงก่อนร่วง
        }
    }

    private void OnFallingStateChanged(bool previous, bool current)
    {
        if (current)
        {
            // ซ่อน Danger Zone ตอนร่วงแล้ว
            if (dangerZoneIndicator != null)
                dangerZoneIndicator.SetActive(false);
        }
    }

    private IEnumerator FlashRedCoroutine()
    {
        bool toggle = false;
        while (true)
        {
            toggle = !toggle;
            SetColor(toggle ? Color.red : Color.white);
            // กระพริบเร็วขึ้นเรื่อยๆ ยิ่งใกล้ร่วงยิ่งเร็ว
            float interval = 1f / flashSpeed;
            yield return new WaitForSeconds(interval);
        }
    }

    private void SetColor(Color color)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material.HasProperty("_Color"))
                renderers[i].material.color = color;
        }
    }

    [ClientRpc]
    private void DisableCollidersClientRpc()
    {
        // ปิด Collider ทั้งหมดบนชิ้นนี้
        Collider[] cols = GetComponentsInChildren<Collider>();
        foreach (var col in cols)
            col.enabled = false;
    }
}