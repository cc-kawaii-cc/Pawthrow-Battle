using Unity.Netcode;
using UnityEngine;

public class ItemSpawner : NetworkBehaviour
{
    [Header("Item Settings")]
    public GameObject[] itemPrefabs;
    public float spawnInterval = 5f;
    
    [Header("Random Area Settings")]
    public Vector3 spawnCenter = Vector3.zero;
    public Vector2 spawnAreaSize = new Vector2(20f, 20f);
    public float spawnHeight = 5f;

    [Header("Spawn Limit")]
    public int maxItemsOnMap = 20;

    private float timer;

    void Update()
    {
        if (!IsSpawned || !IsServer || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            SpawnItem();
            timer = 0f;
        }
    }

    void SpawnItem()
    {
        if (itemPrefabs.Length == 0) return;

        int currentCount = FindObjectsOfType<ThrowableItem>().Length;
        if (currentCount >= maxItemsOnMap) return;

        int randomItem = Random.Range(0, itemPrefabs.Length);
        float randomX = spawnCenter.x + Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f);
        float randomZ = spawnCenter.z + Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f);
        Vector3 randomPosition = new Vector3(randomX, spawnHeight, randomZ);
        GameObject spawnedItem = Instantiate(itemPrefabs[randomItem], randomPosition, Quaternion.identity);
        if (spawnedItem.TryGetComponent(out NetworkObject netObj))
        {
            netObj.Spawn();
        }
        else
        {
            Debug.LogWarning($"Prefab {spawnedItem.name} There is no NetworkObject! Please include this Component");
            Destroy(spawnedItem);
        }
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1, 0, 0.5f);
        Vector3 center = new Vector3(spawnCenter.x, spawnHeight, spawnCenter.z);
        Vector3 size = new Vector3(spawnAreaSize.x, 0.2f, spawnAreaSize.y); 
        Gizmos.DrawWireCube(center, size);
    }
}