using UnityEngine;
using Unity.Netcode;

public class HealPickup : NetworkBehaviour
{
    [Tooltip("Amount of health to restore.")]
    public int healAmount = 20;

    private void OnTriggerEnter(Collider other)
    {
        // ONLY the server handles pickup logic to prevent cheating/double pickups
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            
            if (playerHealth != null)
            {
                // Attempt to heal the player. 
                // Heal() returns true if successful, false if they are full HP or dead.
                bool wasHealed = playerHealth.Heal(healAmount);

                if (wasHealed)
                {
                    // Despawn removes the networked object for all connected clients
                    GetComponent<NetworkObject>().Despawn();
                }
            }
        }
    }
}
