using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChargeBarUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject chargePanel;
    [SerializeField] private Image chargeFill;
    [SerializeField] private TextMeshProUGUI pctText;
    [SerializeField] private Image chargeIcon; // (Optional) ไอคอนบนหลอดชาร์จ
    
    [Header("Charge Colors")]
    [SerializeField] private Color colorLow = new Color(0.2f, 0.8f, 0.3f);    // เขียว
    [SerializeField] private Color colorMid = new Color(1f, 0.8f, 0.1f);      // เหลือง
    [SerializeField] private Color colorMax = new Color(1f, 0.25f, 0.15f);    // แดง
    
    private PlayerController localPlayer;
    
    private void Start() 
    {
        StartCoroutine(FindPlayer());
    }
    
    private IEnumerator FindPlayer()
    {
        // วนหา PlayerController ที่เป็นตัวละครของเรา (IsOwner == true)
        while (localPlayer == null)
        {
            foreach (var p in FindObjectsOfType<PlayerController>())
            {
                if (p.IsOwner) 
                { 
                    localPlayer = p; 
                    break; 
                }
            }
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    private void Update()
    {
        // ถ้าหา localPlayer ไม่เจอ หรือไม่ได้ถือไอเทมอยู่ ให้ซ่อน Panel
        if (localPlayer == null || localPlayer.currentItem == null)
        {
            if (chargePanel.activeSelf) chargePanel.SetActive(false); 
            return;
        }
        
        bool show = localPlayer.isCharging;
        if (chargePanel.activeSelf != show) chargePanel.SetActive(show);
        
        if (!show) return;
        
        // คำนวณเปอร์เซ็นต์ (0.0 ถึง 1.0)
        float pct = Mathf.Clamp01(localPlayer.currentCharge / localPlayer.maxChargeTime);
        chargeFill.fillAmount = pct;
        
        if (pctText != null)
            pctText.text = Mathf.RoundToInt(pct * 100) + "%";
        
        // เลื่อนเปลี่ยนสีแบบ Smooth ตามเปอร์เซ็นต์การชาร์จ
        Color targetColor = pct < 0.5f ? Color.Lerp(colorLow, colorMid, pct * 2f)
                                       : Color.Lerp(colorMid, colorMax, (pct - 0.5f) * 2f);
        chargeFill.color = Color.Lerp(chargeFill.color, targetColor, Time.deltaTime * 10f);
        
        // Pulse effect (ตุ้บๆ) ตอนชาร์จเต็ม
        if (pct >= 0.99f)
        {
            float pulse = Mathf.Sin(Time.time * 8f) * 0.08f + 1f;
            chargePanel.transform.localScale = Vector3.one * pulse;
        }
        else 
        {
            chargePanel.transform.localScale = Vector3.one;
        }
    }
}