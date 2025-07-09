using System.Collections;
using UnityEngine;

public class Teleporter : MonoBehaviour
{
    [Header("Teleporter Settings")]
    [SerializeField] private float activationRadius = 2f;
    [SerializeField] private float teleportDelay = 1f;
    [SerializeField] private bool requiresPlayerInput = true;
    [SerializeField] private KeyCode activationKey = KeyCode.E;
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject teleportEffect;
    [SerializeField] private ParticleSystem particles;
    [SerializeField] private AudioClip teleportSound;
    [SerializeField] private float glowIntensity = 1f;
    [SerializeField] private Color teleporterColor = Color.cyan;
    
    [Header("UI")]
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private Canvas promptCanvas;
    
    private DungeonMapGenerator mapGenerator;
    private AudioSource audioSource;
    private SpriteRenderer spriteRenderer;
    private bool playerInRange = false;
    private bool isTeleporting = false;
    private GameObject player;
    private Animator animator;
    
    // Events
    public System.Action OnTeleportStarted;
    public System.Action OnTeleportCompleted;
    public System.Action OnNewLevelGenerated;

    void Start()
    {
        SetupTeleporter();
        FindMapGenerator();
        SetupAudio();
        SetupVisuals();
        SetupUI();
    }

    void Update()
    {
        if (!isTeleporting)
        {
            CheckPlayerProximity();
            HandleInput();
        }
        
        UpdateVisuals();
    }

    private void SetupTeleporter()
    {
        // Add collider if not present
        if (GetComponent<Collider2D>() == null)
        {
            CircleCollider2D col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = activationRadius;
        }
        
        // Get or add animator
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = gameObject.AddComponent<Animator>();
        }
    }

    private void FindMapGenerator()
    {
        mapGenerator = FindObjectOfType<DungeonMapGenerator>();
        if (mapGenerator == null)
        {
            Debug.LogError("Teleporter: Could not find DungeonMapGenerator in scene!");
        }
    }

    private void SetupAudio()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
    }

    private void SetupVisuals()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        
        spriteRenderer.color = teleporterColor;
        
        // Create default sprite if none assigned
        if (spriteRenderer.sprite == null)
        {
            CreateDefaultSprite();
        }
        
        // Setup particles if available
        if (particles != null)
        {
            particles.startColor = teleporterColor;
            particles.Play();
        }
    }

    private void CreateDefaultSprite()
    {
        // Create a simple circle sprite
        Texture2D texture = new Texture2D(64, 64);
        Color[] colors = new Color[64 * 64];
        Vector2 center = new Vector2(32, 32);
        
        for (int x = 0; x < 64; x++)
        {
            for (int y = 0; y < 64; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance <= 30)
                {
                    float alpha = 1f - (distance / 30f);
                    colors[y * 64 + x] = new Color(teleporterColor.r, teleporterColor.g, teleporterColor.b, alpha);
                }
                else
                {
                    colors[y * 64 + x] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        
        spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
    }

    private void SetupUI()
    {
        if (interactionPrompt == null)
        {
            CreateInteractionPrompt();
        }
        
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
    }

    private void CreateInteractionPrompt()
    {
        // Create a simple UI prompt
        GameObject promptObj = new GameObject("TeleporterPrompt");
        promptObj.transform.SetParent(transform);
        promptObj.transform.localPosition = Vector3.up * 2f;
        
        Canvas canvas = promptObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        canvas.sortingOrder = 10;
        
        promptObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        promptObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        // Create text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(promptObj.transform);
        textObj.transform.localPosition = Vector3.zero;
        textObj.transform.localScale = Vector3.one * 0.01f;
        
        UnityEngine.UI.Text text = textObj.AddComponent<UnityEngine.UI.Text>();
        text.text = $"Press {activationKey} to Enter Next Level";
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 14;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        
        // Set rect transform
        RectTransform rectTransform = textObj.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(200, 50);
        
        interactionPrompt = promptObj;
        promptCanvas = canvas;
    }

    private void CheckPlayerProximity()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }
        
        if (player != null)
        {
            float distance = Vector2.Distance(transform.position, player.transform.position);
            bool wasInRange = playerInRange;
            playerInRange = distance <= activationRadius;
            
            // Show/hide prompt based on proximity
            if (playerInRange != wasInRange)
            {
                if (interactionPrompt != null)
                {
                    interactionPrompt.SetActive(playerInRange);
                }
                
                if (playerInRange)
                {
                    OnPlayerEnterRange();
                }
                else
                {
                    OnPlayerExitRange();
                }
            }
        }
    }

    private void HandleInput()
    {
        if (playerInRange && requiresPlayerInput)
        {
            if (Input.GetKeyDown(activationKey))
            {
                ActivateTeleporter();
            }
        }
        else if (playerInRange && !requiresPlayerInput)
        {
            ActivateTeleporter();
        }
    }

    private void UpdateVisuals()
    {
        if (spriteRenderer != null)
        {
            // Pulsing glow effect
            float pulse = (Mathf.Sin(Time.time * 2f) + 1f) * 0.5f;
            float intensity = glowIntensity * (0.5f + pulse * 0.5f);
            
            Color currentColor = teleporterColor;
            currentColor.a = intensity;
            spriteRenderer.color = currentColor;
            
            // Scale pulsing
            float scale = 1f + pulse * 0.1f;
            transform.localScale = Vector3.one * scale;
        }
    }

    public void ActivateTeleporter()
    {
        if (isTeleporting || mapGenerator == null) return;
        
        StartCoroutine(TeleportSequence());
    }

    private IEnumerator TeleportSequence()
    {
        isTeleporting = true;
        OnTeleportStarted?.Invoke();
        
        // Hide interaction prompt
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
        
        // Play teleport sound
        if (audioSource != null && teleportSound != null)
        {
            audioSource.PlayOneShot(teleportSound);
        }
        
        // Trigger teleport effect
        if (teleportEffect != null)
        {
            Instantiate(teleportEffect, transform.position, transform.rotation);
        }
        
        // Enhanced visual effect
        if (particles != null)
        {
            particles.Emit(50);
        }
        
        // Animate teleporter
        StartCoroutine(TeleportAnimation());
        
        // Wait for delay
        yield return new WaitForSeconds(teleportDelay);
        
        // Generate new level
        GenerateNewLevel();
        
        // Complete teleportation
        OnTeleportCompleted?.Invoke();
        isTeleporting = false;
        
        Debug.Log("Teleporter activated! New level generated.");
    }

    private IEnumerator TeleportAnimation()
    {
        float duration = teleportDelay;
        float elapsed = 0f;
        Vector3 originalScale = transform.localScale;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // Spin and scale up
            transform.Rotate(0, 0, 360 * Time.deltaTime);
            float scale = 1f + progress * 2f;
            transform.localScale = originalScale * scale;
            
            // Intensify color
            if (spriteRenderer != null)
            {
                Color color = teleporterColor;
                color.a = 1f + progress;
                spriteRenderer.color = color;
            }
            
            yield return null;
        }
        
        // Reset
        transform.localScale = originalScale;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = teleporterColor;
        }
    }

    private void GenerateNewLevel()
    {
        if (mapGenerator != null)
        {
            // Clear existing objects that might interfere
            ClearExistingEntities();
            
            // Generate new map
            mapGenerator.GenerateNewMap();
            
            // Move player to new start position
            MovePlayerToStart();
            
            // Destroy this teleporter (new one will be spawned)
            DestroyTeleporter();
            
            OnNewLevelGenerated?.Invoke();
        }
    }

    private void ClearExistingEntities()
    {
        // Clear existing teleporters
        Teleporter[] teleporters = FindObjectsOfType<Teleporter>();
        foreach (var teleporter in teleporters)
        {
            if (teleporter != this)
            {
                Destroy(teleporter.gameObject);
            }
        }
    }

    private void MovePlayerToStart()
    {
        if (player != null && mapGenerator != null)
        {
            var mapData = mapGenerator.GetCurrentMapData();
            if (mapData != null)
            {
                var startRoom = mapData.GetStartRoom();
                if (startRoom != null)
                {
                    player.transform.position = new Vector3(startRoom.worldPosition.x, startRoom.worldPosition.y, player.transform.position.z);
                    Debug.Log($"Player moved to start room at {startRoom.worldPosition}");
                }
            }
        }
    }

    private void DestroyTeleporter()
    {
        // Fade out effect
        StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        float duration = 0.5f;
        float elapsed = 0f;
        Color originalColor = spriteRenderer.color;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / duration);
            
            if (spriteRenderer != null)
            {
                Color color = originalColor;
                color.a = alpha;
                spriteRenderer.color = color;
            }
            
            yield return null;
        }
        
        Destroy(gameObject);
    }

    // Collision detection
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            player = other.gameObject;
            playerInRange = true;
            OnPlayerEnterRange();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            OnPlayerExitRange();
        }
    }

    private void OnPlayerEnterRange()
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(true);
        }
        
        Debug.Log("Player entered teleporter range");
    }

    private void OnPlayerExitRange()
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
        
        Debug.Log("Player exited teleporter range");
    }

    // Public methods for external access
    public void SetMapGenerator(DungeonMapGenerator generator)
    {
        mapGenerator = generator;
    }

    public bool IsPlayerInRange()
    {
        return playerInRange;
    }

    public bool IsTeleporting()
    {
        return isTeleporting;
    }

    // Spawning method to be called by the map generator
    public void SpawnInRoom(Room room)
    {
        if (room != null)
        {
        // Convert to tile coordinates to match your tilemap system
            Vector3 tilePosition = new Vector3(
                Mathf.Round(room.worldPosition.x), 
                Mathf.Round(room.worldPosition.y), 
                transform.position.z
            );
        
            transform.position = tilePosition;
            Debug.Log($"Teleporter spawned in room {room.uniqueId} at tile position {tilePosition}");
        }
    }
    // Gizmos for debugging
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, activationRadius);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationRadius);
    }
}