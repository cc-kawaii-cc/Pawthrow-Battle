using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Analytics;

public class AnalyticsManager : MonoBehaviour
{
    public static AnalyticsManager Instance { get; private set; }

    private void Awake()
    {
        // เช็คว่ามี AnalyticsManager อยู่ในฉากแล้วหรือยัง
        // ถ้ามีแล้วให้ทำลายตัวที่เพิ่งสร้างใหม่ทิ้ง เพื่อไม่ให้ทำงานซ้ำซ้อน
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
        // คำสั่งนี้ทำให้ GameObject ไม่ถูกลบตอนเปลี่ยน Scene
        DontDestroyOnLoad(gameObject); 
    }

    async void Start()
    {
        try
        {
            // ทำงานแค่ครั้งเดียวตอนเปิดเกม
            await UnityServices.InitializeAsync();
            AnalyticsService.Instance.StartDataCollection();
            
            Debug.Log("Unity Analytics Initialized Successfully!");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to initialize Unity Analytics: " + e.Message);
        }
    }
}