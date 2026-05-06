using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SafeZoneUI : MonoBehaviour
{
    [SerializeField] private GameObject warningPanel;
    [SerializeField] private TextMeshProUGUI warningText;
    [SerializeField] private Image warningBG;
    
    private PlayerController localPlayer;
    private float pulseTimer;
    
    private void Start() 
    {
        StartCoroutine(FindPlayer());
    }
    
    private IEnumerator FindPlayer() 
    {
        while (localPlayer == null) 
        {
            // ระบุชนิดที่ต้องการหา (PlayerController) ให้ชัดเจน
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
    
    private void Update()
    {
        if (localPlayer == null || SafeZoneManager.Instance == null)
        { 
            if (warningPanel.activeSelf) warningPanel.SetActive(false); 
            return; 
        }
        
        // คำนวณระยะห่างเฉพาะแกน X และ Z
        float dist = Vector3.Distance(
            new Vector3(localPlayer.transform.position.x, 0, localPlayer.transform.position.z),
            new Vector3(SafeZoneManager.Instance.GetCenter().x, 0, SafeZoneManager.Instance.GetCenter().z));
        
        bool outside = dist > SafeZoneManager.Instance.GetCurrentRadius();
        
        if (warningPanel.activeSelf != outside) 
        {
            warningPanel.SetActive(outside);
        }
        
        if (outside)
        {
            pulseTimer += Time.deltaTime * 3f;
            // สร้างเอฟเฟกต์สีแดงกระพริบแบบ Smooth
            warningBG.color = new Color(1f, 0.15f, 0.1f, 0.5f + Mathf.Sin(pulseTimer) * 0.3f);
            if (warningText != null) warningText.text = "⚠ ออกนอก Safe Zone!";
        }
    }
}