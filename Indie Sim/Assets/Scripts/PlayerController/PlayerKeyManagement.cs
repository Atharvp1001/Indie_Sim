using UnityEngine;

public class PlayerKeyManagement : MonoBehaviour
{
    [Header("Key Management")]
    [SerializeField] private bool hasKey = false;
    [SerializeField] private string keyTag = "Key";
    [SerializeField] private string teleporterTag = "Teleporter";

    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip keyPickupSound;
    [SerializeField] private AudioClip teleportDeniedSound;

    // Public property to check if player has key (for other scripts)
    public bool HasKey => hasKey;

    void OnTriggerEnter2D(Collider2D other)
    {
        // Handle key pickup
        if (other.CompareTag(keyTag))
        {
            PickupKey(other.gameObject);
        }
        // Handle teleporter interaction
        else if (other.CompareTag(teleporterTag))
        {
            TryUseTeleporter(other.gameObject);
        }
    }

    private void PickupKey(GameObject keyObject)
    {
        // Player now has the key
        hasKey = true;

        // Play pickup sound if available
        if (audioSource != null && keyPickupSound != null)
        {
            audioSource.PlayOneShot(keyPickupSound);
        }

        // Destroy the key object
        Destroy(keyObject);

        Debug.Log("Key picked up! Player now has the key.");
    }

    private void TryUseTeleporter(GameObject teleporter)
    {
        if (hasKey)
        {
            // Player has key - allow teleportation
            Teleporter teleporterScript = teleporter.GetComponent<Teleporter>();
            if (teleporterScript != null)
            {
                // Use the key (player loses it after teleporting)
                hasKey = false;

                // Activate the teleporter
                teleporterScript.ActivateTeleporter();

                Debug.Log("Key used! Teleporting to next level...");
            }
        }
        else
        {
            // Player doesn't have key - deny teleportation
            PlayDeniedSound();
            Debug.Log("You need a key to use the teleporter!");
        }
    }

    private void PlayDeniedSound()
    {
        if (audioSource != null && teleportDeniedSound != null)
        {
            audioSource.PlayOneShot(teleportDeniedSound);
        }
    }

    // Public methods for other scripts to use
    public void GiveKey()
    {
        hasKey = true;
        Debug.Log("Player was given a key.");
    }

    public void RemoveKey()
    {
        hasKey = false;
        Debug.Log("Player's key was removed.");
    }

    // Method to check key status from other scripts
    public bool CheckHasKey()
    {
        return hasKey;
    }
}
