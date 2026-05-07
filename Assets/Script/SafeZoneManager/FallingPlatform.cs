using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class FallingPlatform : NetworkBehaviour
{
    [Header("Warning Flash Settings")]
    public float warningDuration = 3f;
    public float flashSpeed = 4f;

    [Header("Fall Settings")]
    public float fallSpeed = 8f;
    public float destroyAfterFallDistance = 30f;

    [Header("After Fall")]
    [Tooltip("หน่วงเวลากี่วินาทีหลังร่วงถึงจุดต่ำสุด แล้วค่อยปิด GameObject")]
    public float deactivateDelay = 0.5f;

    [Header("Danger Zone VFX")]
    public GameObject dangerZoneIndicator;

    // ── NetworkVariables ──────────────────────────────────────
    private NetworkVariable<bool> isWarning = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> isFalling = new NetworkVariable<bool>(false);

    // ── Private ───────────────────────────────────────────────
    private Renderer[] renderers;
    private Color[]    originalColors;
    private Coroutine  flashCoroutine;

    // ─────────────────────────────────────────────────────────
    private void Awake()
    {
        renderers      = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
            originalColors[i] = renderers[i].material.HasProperty("_Color")
                ? renderers[i].material.color
                : Color.white;
    }

    public override void OnNetworkSpawn()
    {
        isWarning.OnValueChanged += OnWarningStateChanged;
        isFalling.OnValueChanged += OnFallingStateChanged;

        if (dangerZoneIndicator != null)
            dangerZoneIndicator.SetActive(false);
    }

    public override void OnNetworkDespawn()
    {
        isWarning.OnValueChanged -= OnWarningStateChanged;
        isFalling.OnValueChanged -= OnFallingStateChanged;
    }

    // =========================================================
    //  Server — เรียกจาก MapDestructionManager
    // =========================================================

    [ServerRpc(RequireOwnership = false)]
    public void TriggerFallSequenceServerRpc() => TriggerFallSequence();

    public void TriggerFallSequence()
    {
        if (!IsServer) return;
        StartCoroutine(FallSequenceCoroutine());
    }

    private IEnumerator FallSequenceCoroutine()
    {
        // ── Phase 1 : เตือน (กระพริบแดง) ─────────────────────
        isWarning.Value = true;
        yield return new WaitForSeconds(warningDuration);
        isWarning.Value = false;

        // ── Phase 2 : ร่วง ────────────────────────────────────
        isFalling.Value = true;
        DisableCollidersClientRpc();   // ปิด Collider → ผู้เล่นบนนั้นจะตกตาม

        float fallen = 0f;
        while (fallen < destroyAfterFallDistance)
        {
            float step = fallSpeed * Time.deltaTime;
            transform.position += Vector3.down * step;
            fallen += step;
            yield return null;
        }

        // ── Phase 3 : หยุดแล้วค่อยปิด ────────────────────────
        yield return new WaitForSeconds(deactivateDelay);

        // บอกทุก Client ให้ปิด GameObject ชิ้นนี้
        DeactivatePlatformClientRpc();

        // Server ปิดเองด้วย
        gameObject.SetActive(false);
    }

    // =========================================================
    //  ClientRpc
    // =========================================================

    /// <summary>ปิด GameObject บน Client ทุกคน ให้ซิงค์กันทั้งห้อง</summary>
    [ClientRpc]
    private void DeactivatePlatformClientRpc()
    {
        gameObject.SetActive(false);
    }

    [ClientRpc]
    private void DisableCollidersClientRpc()
    {
        foreach (var col in GetComponentsInChildren<Collider>())
            col.enabled = false;
    }

    // =========================================================
    //  NetworkVariable Callbacks (Client-side visuals)
    // =========================================================

    private void OnWarningStateChanged(bool _, bool current)
    {
        if (current)
        {
            if (dangerZoneIndicator != null) dangerZoneIndicator.SetActive(true);
            flashCoroutine = StartCoroutine(FlashRedCoroutine());
        }
        else
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            SetColor(Color.red);   // ค้างแดงก่อนร่วง
        }
    }

    private void OnFallingStateChanged(bool _, bool current)
    {
        if (current && dangerZoneIndicator != null)
            dangerZoneIndicator.SetActive(false);
    }

    // ── Helpers ──────────────────────────────────────────────

    private IEnumerator FlashRedCoroutine()
    {
        bool toggle = false;
        while (true)
        {
            toggle = !toggle;
            SetColor(toggle ? Color.red : Color.white);
            yield return new WaitForSeconds(1f / flashSpeed);
        }
    }

    private void SetColor(Color color)
    {
        foreach (var r in renderers)
            if (r != null && r.material.HasProperty("_Color"))
                r.material.color = color;
    }
}