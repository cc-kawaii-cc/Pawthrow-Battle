using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChargeBarUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject chargePanel;
    [SerializeField] private Image chargeFill;
    
    [Header("Golf Style Texts")]
    [SerializeField] private TextMeshProUGUI currentDmgText; // โชว์ดาเมจแบบเรียลไทม์
    [SerializeField] private TextMeshProUGUI halfDmgText;    // ป้ายบอกดาเมจตรงกลางหลอด (50%)
    [SerializeField] private TextMeshProUGUI maxDmgText;     // ป้ายบอกดาเมจสุดหลอด (100%)

    private PlayerController localPlayer;
    
    private void Start() 
    {
        StartCoroutine(FindPlayer());
    }
    
    private IEnumerator FindPlayer()
    {
        // วนหา PlayerController ที่เป็นตัวละครของเรา[cite: 20, 29]
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
        // ซ่อนหลอดถ้าไม่ได้ถือของ หรือหาตัวละครไม่เจอ[cite: 20, 29]
        if (localPlayer == null || localPlayer.currentItem == null)
        {
            if (chargePanel.activeSelf) chargePanel.SetActive(false); 
            return;
        }
        
        // โชว์หลอดเฉพาะตอนกดชาร์จ[cite: 20, 29]
        bool show = localPlayer.isCharging;
        if (chargePanel.activeSelf != show) chargePanel.SetActive(show);
        
        if (!show) return;
        
        // คำนวณเปอร์เซ็นต์หลอด (0.0 ถึง 1.0)[cite: 20, 29]
        float pct = Mathf.Clamp01(localPlayer.currentCharge / localPlayer.maxChargeTime);
        chargeFill.fillAmount = pct; //[cite: 29]

        // ดึงข้อมูลไอเทมในมือมาคำนวณโชว์บนหลอด[cite: 11, 20]
        if (localPlayer.currentItem.TryGetComponent(out ThrowableItem item))
        {
            float baseDmg = item.itemData.damage; //[cite: 11, 12]

            // ตามลอจิกตอนปา: chargeMultiplier = 1f + pct (ชาร์จเต็มคือ x2)[cite: 20]
            int maxDmg = Mathf.RoundToInt(baseDmg * 2f);      // ดาเมจ 100%
            int halfDmg = Mathf.RoundToInt(baseDmg * 1.5f);   // ดาเมจ 50%
            int currentDmg = Mathf.RoundToInt(baseDmg * (1f + pct)); // ดาเมจปัจจุบันที่กำลังชาร์จ

            // อัปเดตตัวเลขลงบน UI แบบเกมกอล์ฟ
            if (maxDmgText != null) maxDmgText.text = $"{maxDmg} Dmg";
            if (halfDmgText != null) halfDmgText.text = $"{halfDmg} Dmg";
            if (currentDmgText != null) currentDmgText.text = $"Power: {currentDmg}";
        }
    }
}