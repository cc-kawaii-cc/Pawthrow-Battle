using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// ตัวจัดการลำดับการทำลาย Map ทั้งหมด
/// ต้องมี NetworkObject Component ด้วย
/// Server เป็นคนควบคุมทั้งหมด
/// </summary>
public class MapDestructionManager : NetworkBehaviour
{
    public static MapDestructionManager Instance { get; private set; }

    // =========================================================
    //  Inspector Settings
    // =========================================================

    [Header("─── Destruction Waves ───")]
    [Tooltip("กำหนดลำดับคลื่นการทำลาย Map แต่ละ Wave")]
    public List<DestructionWave> waves = new List<DestructionWave>();

    [Header("─── Start Delay ───")]
    [Tooltip("หน่วงเวลาก่อนเริ่มทำลาย Map (วินาที) นับจากเกมเริ่ม")]
    public float initialDelay = 30f;

    [Header("─── Warning UI ───")]
    [Tooltip("ลาก Text ที่จะแสดงคำเตือน เช่น 'MAP IS COLLAPSING!'")]
    public TextMeshProUGUI warningText;

    [Tooltip("ระยะเวลาที่ข้อความเตือนโชว์ (วินาที)")]
    public float warningTextDuration = 3f;

    [Header("─── Screen Flash ───")]
    [Tooltip("ลาก UI Image (เต็มหน้าจอ) สำหรับกระพริบแดง (ไม่บังคับ)")]
    public UnityEngine.UI.Image screenFlashImage;

    // =========================================================
    //  Private
    // =========================================================
    private bool destructionStarted = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        StartCoroutine(StartDestructionSequence());
    }

    // =========================================================
    //  Server: Main Sequence
    // =========================================================

    private IEnumerator StartDestructionSequence()
    {
        yield return new WaitForSeconds(initialDelay);

        destructionStarted = true;

        for (int i = 0; i < waves.Count; i++)
        {
            DestructionWave wave = waves[i];

            // แจ้งเตือนทุก Client ก่อนเริ่ม Wave
            float warningBefore = wave.warningBeforeWave;
            ShowWarningClientRpc($"⚠ ZONE {i + 1} COLLAPSING IN {warningBefore:0}s!\nFIND SAFE GROUND!", warningBefore);

            yield return new WaitForSeconds(warningBefore);

            // สั่ง Platform ทุกตัวใน Wave นี้ให้เริ่มกระบวนการร่วง
            foreach (FallingPlatform platform in wave.platforms)
            {
                if (platform == null) continue;

                // สั่งแบบ stagger (ทยอย) ถ้าต้องการ
                platform.TriggerFallSequence();

                if (wave.staggerDelay > 0f)
                    yield return new WaitForSeconds(wave.staggerDelay);
            }

            // รอก่อน Wave ถัดไป
            yield return new WaitForSeconds(wave.delayAfterWave);
        }

        // Map ถูกทำลายหมดแล้ว
        Debug.Log("[MapDestructionManager] All waves complete!");
    }

    // =========================================================
    //  ClientRpc: UI Warning
    // =========================================================

    [ClientRpc]
    private void ShowWarningClientRpc(string message, float duration)
    {
        StartCoroutine(ShowWarningCoroutine(message, duration));
    }

    private IEnumerator ShowWarningCoroutine(string message, float duration)
    {
        // แสดงข้อความเตือน
        if (warningText != null)
        {
            warningText.text = message;
            warningText.gameObject.SetActive(true);
        }

        // กระพริบหน้าจอแดง
        if (screenFlashImage != null)
            StartCoroutine(ScreenFlashCoroutine(duration));

        yield return new WaitForSeconds(warningTextDuration);

        if (warningText != null)
            warningText.gameObject.SetActive(false);
    }

    private IEnumerator ScreenFlashCoroutine(float totalDuration)
    {
        if (screenFlashImage == null) yield break;

        screenFlashImage.gameObject.SetActive(true);
        float elapsed = 0f;
        bool toggle = false;

        while (elapsed < totalDuration)
        {
            toggle = !toggle;
            Color c = screenFlashImage.color;
            c.a = toggle ? 0.25f : 0f;
            screenFlashImage.color = c;

            yield return new WaitForSeconds(0.35f);
            elapsed += 0.35f;
        }

        screenFlashImage.color = new Color(1, 0, 0, 0);
        screenFlashImage.gameObject.SetActive(false);
    }

    // =========================================================
    //  Public API: เรียกจากที่อื่น เช่น GameManager
    // =========================================================

    /// <summary>
    /// เรียกเพื่อเริ่มทำลาย Map ทันที (ข้าม initialDelay)
    /// </summary>
    public void ForceStartDestruction()
    {
        if (!IsServer || destructionStarted) return;
        StopAllCoroutines();
        StartCoroutine(StartDestructionSequence());
    }
}

// =========================================================
//  Data Class: กำหนด Wave แต่ละชุด
// =========================================================

[System.Serializable]
public class DestructionWave
{
    [Header("Wave Info")]
    [Tooltip("ชื่อ Wave (เพื่อความง่ายในการ Edit ใน Inspector)")]
    public string waveName = "Wave 1";

    [Tooltip("Platform ทั้งหมดที่จะร่วงใน Wave นี้")]
    public List<FallingPlatform> platforms = new List<FallingPlatform>();

    [Header("Timing")]
    [Tooltip("เวลาเตือนก่อนเริ่ม Wave นี้ (วินาที) — ช่วงนี้ Platform จะกระพริบแดง")]
    public float warningBeforeWave = 5f;

    [Tooltip("ทยอยสั่งร่วงทีละชิ้นเป็นระยะห่างกี่วินาที (0 = พร้อมกัน)")]
    public float staggerDelay = 0.5f;

    [Tooltip("หยุดรอกี่วินาทีก่อนเริ่ม Wave ถัดไป")]
    public float delayAfterWave = 10f;
}