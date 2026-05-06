using Unity.Netcode;
using UnityEngine;
using System.Collections;

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
            if (renderers[i].material.HasProperty("_Color"))
                originalColors[i] = renderers[i].material.color;
            else
                originalColors[i] = Color.white;
        }

        startPosition = transform.position;
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

    [ServerRpc(RequireOwnership = false)]
    public void TriggerFallSequenceServerRpc()
    {
        if (!IsServer) return;
        StartCoroutine(FallSequenceCoroutine());
    }

    public void TriggerFallSequence()
    {
        if (!IsServer) return;
        StartCoroutine(FallSequenceCoroutine());
    }

    private IEnumerator FallSequenceCoroutine()
    {
        isWarning.Value = true;
        yield return new WaitForSeconds(warningDuration);
        isWarning.Value = false;
        isFalling.Value = true;

        DisableCollidersClientRpc();

        float fallDistance = 0f;
        while (fallDistance < destroyAfterFallDistance)
        {
            float step = fallSpeed * Time.deltaTime;
            transform.position += Vector3.down * step;
            fallDistance += step;
            yield return null;
        }

        GetComponent<NetworkObject>().Despawn(true);
    }

    private void OnWarningStateChanged(bool previous, bool current)
    {
        if (current)
        {
            if (dangerZoneIndicator != null)
                dangerZoneIndicator.SetActive(true);

            flashCoroutine = StartCoroutine(FlashRedCoroutine());
        }
        else
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            SetColor(Color.red);
        }
    }

    private void OnFallingStateChanged(bool previous, bool current)
    {
        if (current)
        {
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
        Collider[] cols = GetComponentsInChildren<Collider>();
        foreach (var col in cols)
            col.enabled = false;
    }
}