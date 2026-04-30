using UnityEngine;

public class PlayerSpawnZone : MonoBehaviour
{
    [Header("Spawn Settings")]
    public Vector2 spawnAreaSize = new Vector2(20f, 20f);
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Gizmos.DrawCube(transform.position, new Vector3(spawnAreaSize.x, 1f, spawnAreaSize.y));
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(spawnAreaSize.x, 1f, spawnAreaSize.y));
    }
}