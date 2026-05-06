using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EchoMarkerUI : MonoBehaviour
{
    public static EchoMarkerUI Instance { get; private set; }

    private List<GameObject> activeMarkers = new List<GameObject>();
    private List<Vector3> targetWorldPositions = new List<Vector3>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ShowMarkers(Vector3[] worldPositions, ulong[] ids, float duration)
    {
        ClearMarkers();

        for (int i = 0; i < worldPositions.Length; i++)
        {
            // 1. สร้าง GameObject ของจุด (Dot)
            GameObject dotObj = new GameObject($"EchoMarker_{ids[i]}");
            dotObj.transform.SetParent(transform, false);

            RectTransform rect = dotObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(12f, 12f); // ขนาด 12px

            Image img = dotObj.AddComponent<Image>();
            img.color = new Color(1f, 0.5f, 0f); // สีส้ม

            // เพิ่ม CanvasGroup เพื่อใช้ทำ Fade-out
            dotObj.AddComponent<CanvasGroup>();

            // 2. สร้าง GameObject สำหรับข้อความ (Label)
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(dotObj.transform, false);

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchoredPosition = new Vector2(0f, 18f); // ลอยอยู่เหนือจุดเล็กน้อย
            labelRect.sizeDelta = new Vector2(100f, 20f);

            TextMeshProUGUI text = labelObj.AddComponent<TextMeshProUGUI>();
            text.text = "Enemy";
            text.fontSize = 12f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(1f, 0.5f, 0f); // สีส้ม
            text.fontStyle = FontStyles.Bold;

            activeMarkers.Add(dotObj);
            targetWorldPositions.Add(worldPositions[i]);
        }

        StartCoroutine(TrackAndFade(duration));
    }

    private void ClearMarkers()
    {
        foreach (var marker in activeMarkers)
        {
            if (marker != null)
            {
                Destroy(marker);
            }
        }
        activeMarkers.Clear();
        targetWorldPositions.Clear();
    }

    private IEnumerator TrackAndFade(float duration)
    {
        float elapsed = 0f;
        Camera cam = Camera.main;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            if (cam != null)
            {
                for (int i = 0; i < activeMarkers.Count; i++)
                {
                    if (activeMarkers[i] == null) continue;

                    // แปลงตำแหน่ง 3D เป็นตำแหน่ง 2D บนหน้าจอ
                    Vector3 screenPos = cam.WorldToScreenPoint(targetWorldPositions[i]);

                    // เช็คว่าตำแหน่งอยู่ด้านหน้ากล้องหรือไม่ (z > 0)
                    if (screenPos.z > 0)
                    {
                        activeMarkers[i].SetActive(true);
                        activeMarkers[i].transform.position = screenPos;
                    }
                    else
                    {
                        // ถ้าเป้าหมายอยู่หลังกล้อง ให้ซ่อนเอาไว้
                        activeMarkers[i].SetActive(false);
                    }

                    // จัดการการ Fade out ในช่วง 0.5 วินาทีสุดท้าย
                    float timeRemaining = duration - elapsed;
                    if (timeRemaining <= 0.5f)
                    {
                        CanvasGroup cg = activeMarkers[i].GetComponent<CanvasGroup>();
                        if (cg != null)
                        {
                            cg.alpha = Mathf.Lerp(0f, 1f, timeRemaining / 0.5f);
                        }
                    }
                }
            }

            yield return null;
        }

        ClearMarkers();
    }
}