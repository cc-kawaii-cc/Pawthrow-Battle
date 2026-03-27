using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Analytics;

public class AnalyticsManager : MonoBehaviour
{
    public static AnalyticsManager Instance { get; private set; }

    [Header("Debug Settings")]
    [Tooltip("Uncheck this box when you want to send Analytics data to the actual dashboard")]
    public bool disableAnalyticsForTesting = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); 
    }

    async void Start()
    {
        if (disableAnalyticsForTesting)
        {
            Debug.Log("Analytics is disabled (Testing Mode), so no data will be sent to the Dashboard");
            return; 
        }

        try
        {
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