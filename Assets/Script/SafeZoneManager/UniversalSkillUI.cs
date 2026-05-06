using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // สำหรับจัดการ Text

// ตัวแปรสำหรับจับคู่ ตัวละคร <-> รูปไอคอนสกิล
[System.Serializable]
public class SkillIconMapping
{
    public PlayerController.CharacterType characterType;
    public Sprite skillIcon;
}

public class UniversalSkillUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Panel หลัก (ซ่อนเมื่อหาผู้เล่นไม่เจอ)")]
    public GameObject skillPanel; 
    
    [Tooltip("Image หลักสำหรับโชว์รูปสกิล")]
    public Image skillIconImage;       
    
    [Tooltip("Image สีดำโปร่งใส แบบ Filled 360")]
    public Image cooldownOverlay;      
    
    [Tooltip("ข้อความโชว์ตัวเลขคูลดาวน์ หรือ จำนวนกระโดดของไก่")]
    public TextMeshProUGUI cooldownText; 

    [Header("Icons Setup")]
    [Tooltip("เพิ่มขนาด Array แล้วลากรูปไอคอนสกิลมาใส่ให้ตรงกับชื่อตัวละคร")]
    public List<SkillIconMapping> skillIcons; 

    private PlayerController localPlayer;
    private PlayerController.CharacterType currentType;
    private bool isInitialized = false;

    void Start()
    {
        StartCoroutine(FindLocalPlayer());
    }

    private IEnumerator FindLocalPlayer()
    {
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
            yield return new WaitForSeconds(0.2f);
        }
    }

    void Update()
    {
        if (localPlayer == null) 
        {
            if (skillPanel.activeSelf) skillPanel.SetActive(false);
            return;
        }

        if (!skillPanel.activeSelf) skillPanel.SetActive(true);

        // 1. เช็คว่าถ้าเป็นครั้งแรก หรือเปลี่ยนตัวละคร ให้เปลี่ยนรูปไอคอนให้ตรงกัน
        if (!isInitialized || currentType != localPlayer.characterType)
        {
            currentType = localPlayer.characterType;
            UpdateSkillIcon(currentType);
            isInitialized = true;
        }

        // 2. อัปเดต % คูลดาวน์ และตัวหนังสือ
        UpdateUIState();
    }

    private void UpdateSkillIcon(PlayerController.CharacterType type)
    {
        foreach (var mapping in skillIcons)
        {
            if (mapping.characterType == type)
            {
                if (skillIconImage != null) skillIconImage.sprite = mapping.skillIcon;
                break;
            }
        }
    }

    private void UpdateUIState()
    {
        // === กรณีพิเศษ: น้องไก่ (ระบบจำนวนครั้ง/Charge) ===
        if (currentType == PlayerController.CharacterType.Chicken)
        {
            // ซ่อนวงกลมดำไปเลย เพราะไก่ไม่ได้ใช้เวลาคูลดาวน์
            if (cooldownOverlay != null) cooldownOverlay.fillAmount = 0f; 

            // โชว์ตัวเลขจำนวนที่กระโดดได้
            if (cooldownText != null)
            {
                int jumps = localPlayer.GetRemainingJumps();
                cooldownText.text = jumps > 0 ? jumps.ToString() : "";
            }
        }
        // === กรณีทั่วไป: ตัวละครอื่นๆ (ระบบเวลาคูลดาวน์) ===
        else
        {
            float cdPct = localPlayer.GetSkillCooldownPercentage();
            float timeLeft = localPlayer.GetSkillCooldownTimeLeft();

            if (cooldownOverlay != null) cooldownOverlay.fillAmount = cdPct;

            if (cooldownText != null)
            {
                if (timeLeft > 0)
                {
                    // โชว์ตัวเลขทศนิยม 1 ตำแหน่ง (เช่น 2.5)
                    cooldownText.text = timeLeft.ToString("F1");
                }
                else
                {
                    cooldownText.text = ""; // ถ้าพร้อมใช้ให้ลบตัวเลขทิ้ง
                }
            }
        }
    }
}