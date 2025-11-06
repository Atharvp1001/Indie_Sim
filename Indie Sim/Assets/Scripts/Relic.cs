using UnityEngine;

public class Relic : MonoBehaviour
{
    [Header("Relic Settings")]
    [Tooltip("Unique ID for this relic (0-7 for 8 relics total)")]
    public int relicIndex = 0;

    [Tooltip("Tag that the player GameObject uses")]
    public string playerTag = "Player";

    [Header("Optional Visual Effects")]
    public GameObject collectEffect; // Optional particle effect when collected

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object that touched this relic is the player
        if (other.CompareTag(playerTag))
        {
            // Get the RelicManager component from the player
            RelicManager relicManager = other.GetComponent<RelicManager>();

            // Make sure the player has a RelicManager component
            if (relicManager != null)
            {
                // Tell the RelicManager that this relic was collected
                relicManager.CollectRelic(relicIndex);

                // Optional: Play a particle effect before destroying
                if (collectEffect != null)
                {
                    Instantiate(collectEffect, transform.position, Quaternion.identity);
                }

                // Remove this relic from the scene
                Destroy(gameObject);
            }
            else
            {
                Debug.LogError("Player does not have a RelicManager component!");
            }
        }
    }
}
