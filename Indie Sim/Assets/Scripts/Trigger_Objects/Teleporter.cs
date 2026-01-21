using System.Collections;
using UnityEngine;

public class Teleporter : MonoBehaviour
{
    [Header("Teleporter Settings")]
    [SerializeField] private float activationRadius = 2f;
    [SerializeField] private float teleportDelay = 1f;

    [Header("Effects (Assign in Inspector)")]
    [SerializeField] private GameObject teleportEffect;
    [SerializeField] private ParticleSystem particles;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip teleportSound;

    [Header("Visual Settings")]
    [SerializeField] private float glowIntensity = 1f;
    [SerializeField] private Color teleporterColor = Color.cyan;

    [Header("Cleanup Settings")]
    [SerializeField] private string[] enemyTags = { "Enemy", "EnemySpawner", "Coin", "Relic" };

    [Header("Teleporter Settings")]
    [Tooltip("Does the player need a key to use this teleporter?")]
    public bool requiresKey = true; // Check/uncheck this in the Inspector


    // Core dependencies
    private DungeonMapGenerator mapGenerator;
    private RoguelikeManager roguelikeManager;  // NEW: Reference to RoguelikeManager
    private GameObject player;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    // State tracking
    private bool playerInRange = false;
    private bool isTeleporting = false;

    // Events for external systems
    public System.Action OnTeleportStarted;
    public System.Action OnTeleportCompleted;
    public System.Action OnNewLevelGenerated;

    #region Unity Lifecycle
    void Start()
    {
        InitializeTeleporter();
    }

    void Update()
    {
        UpdateVisualEffects();
    }
    #endregion

    #region Initialization
    private void InitializeTeleporter()
    {
        // Cache required components
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        // Find map generator
        mapGenerator = FindFirstObjectByType<DungeonMapGenerator>();
        if (mapGenerator == null)
        {
            Debug.LogError($"Teleporter '{gameObject.name}': DungeonMapGenerator not found!");
        }

        // NEW: Find RoguelikeManager
        roguelikeManager = FindFirstObjectByType<RoguelikeManager>();
        if (roguelikeManager == null)
        {
            Debug.LogError($"Teleporter '{gameObject.name}': RoguelikeManager not found!");
        }

        // Setup collider for trigger detection
        SetupTriggerCollider();

        // Initialize visual settings
        if (spriteRenderer != null)
        {
            spriteRenderer.color = teleporterColor;
        }

        Debug.Log($"Teleporter '{gameObject.name}' initialized successfully");
    }

    private void SetupTriggerCollider()
    {
        CircleCollider2D triggerCollider = GetComponent<CircleCollider2D>();
        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<CircleCollider2D>();
            Debug.Log($"Added CircleCollider2D to teleporter '{gameObject.name}'");
        }

        triggerCollider.isTrigger = true;
        triggerCollider.radius = activationRadius;
    }
    #endregion

    #region Visual Effects
    private void UpdateVisualEffects()
    {
        if (spriteRenderer == null) return;

        // Create pulsing glow effect
        float pulse = (Mathf.Sin(Time.time * 2f) + 1f) * 0.5f;
        float intensity = glowIntensity * (0.5f + pulse * 0.5f);

        // Apply color with pulsing alpha
        Color currentColor = teleporterColor;
        currentColor.a = intensity;
        spriteRenderer.color = currentColor;

        // Subtle scale pulsing
        float scale = 1f + pulse * 0.1f;
        transform.localScale = Vector3.one * scale;
    }
    #endregion

    #region Teleportation Logic
    public void ActivateTeleporter()
    {
        if (isTeleporting)
        {
            Debug.LogWarning("Cannot activate teleporter: already teleporting");
            return;
        }

        StartCoroutine(ExecuteTeleportSequence());
    }

    private IEnumerator ExecuteTeleportSequence()
    {
        isTeleporting = true;
        OnTeleportStarted?.Invoke();

        PlayTeleportEffects();

        // Start teleport animation
        StartCoroutine(AnimateTeleporter());

        // Wait for teleport delay
        yield return new WaitForSeconds(teleportDelay);

        // NEW: Call RoguelikeManager to complete the dungeon
        if (roguelikeManager != null)
        {
            Debug.Log("<color=lime>Dungeon completed! Completing roguelike run...</color>");

            // Clear enemies before transition
            ClearEnemiesAndSpawners();

            // Tell RoguelikeManager the dungeon is complete
            // This will handle: difficulty scaling, player reset, dungeon cleanup, and new dungeon generation
            roguelikeManager.CompleteDungeon();
        }
        else
        {
            Debug.LogError("RoguelikeManager not found - cannot complete dungeon!");
        }

        // Complete teleportation
        OnTeleportCompleted?.Invoke();
        isTeleporting = false;

        Debug.Log("Teleportation completed successfully");
    }

    private void PlayTeleportEffects()
    {
        // Play sound effect
        if (audioSource != null && teleportSound != null)
        {
            audioSource.PlayOneShot(teleportSound);
        }

        // Spawn visual effect
        if (teleportEffect != null)
        {
            Instantiate(teleportEffect, transform.position, transform.rotation);
        }

        // Emit particles
        if (particles != null)
        {
            particles.Emit(50);
        }
    }

    private IEnumerator AnimateTeleporter()
    {
        float duration = teleportDelay;
        float elapsed = 0f;
        Vector3 originalScale = transform.localScale;
        Color originalColor = spriteRenderer != null ? spriteRenderer.color : teleporterColor;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            // Rotation animation
            transform.Rotate(0, 0, 360 * Time.deltaTime);

            // Scale animation
            float scaleMultiplier = 1f + progress * 2f;
            transform.localScale = originalScale * scaleMultiplier;

            // Color intensity animation
            if (spriteRenderer != null)
            {
                Color animColor = teleporterColor;
                animColor.a = 1f + progress;
                spriteRenderer.color = animColor;
            }

            yield return null;
        }

        // Reset visual properties
        transform.localScale = originalScale;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }
    #endregion

    #region Enemy Cleanup
    private void ClearEnemiesAndSpawners()
    {
        // Clear all enemies
        ClearGameObjectsByTags(enemyTags);

        Debug.Log("Cleared all enemies, spawners, and projectiles from current level");
    }

    private void ClearGameObjectsByTags(string[] tags)
    {
        foreach (string tag in tags)
        {
            ClearGameObjectsByTag(tag);
        }
    }

    private void ClearGameObjectsByTag(string tag)
    {
        GameObject[] objectsToDestroy = GameObject.FindGameObjectsWithTag(tag);

        foreach (GameObject obj in objectsToDestroy)
        {
            // Disable object first to prevent any ongoing behavior
            obj.SetActive(false);
            Destroy(obj);
        }

        Debug.Log($"Destroyed {objectsToDestroy.Length} objects with tag: {tag}");
    }
    #endregion

    #region Trigger Events
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            player = other.gameObject;
            playerInRange = true;

            // Check if key is required
            if (requiresKey)
            {
                // Key is required - check if player has key
                PlayerKeyManagement keyManager = other.GetComponent<PlayerKeyManagement>();
                if (keyManager != null && keyManager.HasKey)
                {
                    // Player has key - teleport
                    ActivateTeleporter();
                    Debug.Log("Player has key - Teleporting!");
                }
                else
                {
                    // Player doesn't have key - do nothing
                    Debug.Log("Player needs a key to use this teleporter!");
                }
            }
            else
            {
                // No key required - teleport immediately
                ActivateTeleporter();
                Debug.Log("No key required - Teleporting!");
            }
        }
    }


    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            Debug.Log("Player exited teleporter range");
        }
    }
    #endregion

    #region Public API
    public void SetMapGenerator(DungeonMapGenerator generator)
    {
        mapGenerator = generator;
    }

    // NEW: Set RoguelikeManager reference
    public void SetRoguelikeManager(RoguelikeManager manager)
    {
        roguelikeManager = manager;
    }

    public bool IsPlayerInRange() => playerInRange;
    public bool IsTeleporting() => isTeleporting;

    // Called by map generator when spawning teleporter
    public void SpawnInRoom(Room room)
    {
        if (room == null) return;

        Vector3 spawnPosition = new Vector3(
            Mathf.Round(room.worldPosition.x),
            Mathf.Round(room.worldPosition.y),
            transform.position.z
        );

        transform.position = spawnPosition;

        Debug.Log($"Teleporter spawned in room {room.uniqueId} at {spawnPosition}");
    }
    #endregion

    #region Debug Visualization
    private void OnDrawGizmos()
    {
        // Draw activation radius
        Gizmos.color = playerInRange ? Color.green : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, activationRadius);
    }

    private void OnDrawGizmosSelected()
    {
        // Highlight when selected
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationRadius);
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
    }
    #endregion
}
