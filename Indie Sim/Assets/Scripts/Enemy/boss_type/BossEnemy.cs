using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BossEnemy : MonoBehaviour, IDamageable
{
    [Header("Boss Spawning Requirements")]
    [SerializeField] private int minTeleporterUses = 2;
    [SerializeField] private int maxTeleporterUses = 5;
    [SerializeField] private float maxSpawnTimeSeconds = 180f;

    [Header("References")]
    [SerializeField] private GameObject playerObject;
    [SerializeField] private BulletPool bulletPool; // Drag BulletPool from scene here
    [SerializeField] private LayerMask wallLayers;
    [SerializeField] private LayerMask playerLayer;

    [Header("Boss Stats")]
    [SerializeField] private int maxHealth = 500;
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackCooldown = 1.5f;

    [Header("Pinball Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float maxSpeed = 15f;
    [SerializeField] private float speedIncreasePerBounce = 0.5f;
    [SerializeField] private float directionChangeInterval = 2f;

    [Header("Shield System")]
    [SerializeField] private int shieldHealth = 100;
    [SerializeField] private Color shieldColor = new Color(0f, 0.8f, 1f, 0.5f);
    [SerializeField] private GameObject shieldVisualPrefab;

    [Header("Wall Stick Mechanic")]
    [SerializeField] private float wallStickDuration = 5f;
    [SerializeField] private float stickDistance = 0.5f;

    [Header("Circular Bullet Attack")]
    [SerializeField] private int bulletsPerWave = 20; // Total bullets in full circle
    [SerializeField] private float angleBetweenBullets = 18f; // Degrees between each bullet
    [SerializeField] private float timeBetweenWaves = 0.3f; // Time between wave 1 and wave 2
    [SerializeField] private float timeBetweenAttacks = 1.5f; // Time before starting next attack cycle
    [SerializeField] private float bulletSpeed = 8f;
    [SerializeField] private float bulletLifetime = 5f;
    [SerializeField] private int bulletDamage = 5;
    [SerializeField] private float wallCheckDistance = 1f; // How far to raycast for walls

    [Header("Visual Feedback")]
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private Color stuckColor = Color.yellow;
    [SerializeField] private SpriteRenderer whiteSpriteRenderer;

    [Header("Loot Drop")]
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private int minCoins = 10;
    [SerializeField] private int maxCoins = 20;
    [SerializeField] private float coinDropForce = 5f;
    [SerializeField] private float coinSpreadRadius = 2f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip bounceSound;
    [SerializeField] private AudioClip shieldBreakSound;
    [SerializeField] private AudioClip shootSound;

    // Components
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Transform playerTransform;
    private GameObject shieldVisual;
    
    // State
    private int currentHealth;
    private int currentShieldHealth;
    private bool hasShield = true;
    private bool isDead = false;
    private bool isStuckToWall = false;
    private float lastAttackTime = 0f;
    private Color originalColor;
    private Coroutine flashCoroutine;
    private Vector2 moveDirection;
    private float nextDirectionChangeTime;
    private float currentSpeed;
    private Vector2 stuckPosition;

    // Events
    public System.Action OnDeath;
    public System.Action<int, int> OnHealthChanged;
    public System.Action OnShieldBroken;

    void Start()
    {
        InitializeBoss();
    }

    private void InitializeBoss()
    {
        currentHealth = maxHealth;
        currentShieldHealth = shieldHealth;
        currentSpeed = moveSpeed;

        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();

        rb.linearDamping = 0f;
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Get player reference (only for initial direction)
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }
        else
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                playerObject = player;
                Debug.LogWarning("Player not assigned in Inspector, found by tag instead.");
            }
            else
            {
                Debug.LogError("Player not found!");
            }
        }

        // Check if bullet pool is assigned
        if (bulletPool == null)
        {
            Debug.LogError("BulletPool not assigned to Boss! Drag BulletPool from scene into Inspector.");
        }

        // Set INITIAL direction towards player (or random)
        UpdateMoveDirection();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // Remove this line - no more periodic direction changes!
        // nextDirectionChangeTime = Time.time + directionChangeInterval;

        if (shieldVisualPrefab != null)
        {
            shieldVisual = Instantiate(shieldVisualPrefab, transform);
        }

        UpdateShieldVisual();
        Debug.Log("Boss Enemy spawned with shield - PURE PINBALL MODE!");
    }
    void Update()
    {
        if (isDead) return;

        if (!isStuckToWall)
        {
            if (Time.time >= nextDirectionChangeTime)
            {
                UpdateMoveDirection();
                nextDirectionChangeTime = Time.time + directionChangeInterval;
            }
        }
    }

    void FixedUpdate()
    {
        if (isDead) return;

        if (isStuckToWall)
        {
            rb.linearVelocity = Vector2.zero;
            transform.position = stuckPosition;
        }
        else
        {
            rb.linearVelocity = moveDirection * currentSpeed;
        }
    }

    private void UpdateMoveDirection()
    {
        // Only used for initial direction
        if (playerTransform != null)
        {
            moveDirection = (playerTransform.position - transform.position).normalized;
        }
        else
        {
            // Random initial direction if no player found
            moveDirection = Random.insideUnitCircle.normalized;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision == null || collision.gameObject == null)
        {
            Debug.LogError("Invalid collision detected!");
            return;
        }

        int collisionLayer = collision.gameObject.layer;

        if (IsInLayerMask(collisionLayer, wallLayers))
        {
            if (!hasShield)
            {
                StickToWall(collision);
            }
            else
            {
                BounceOffWall(collision);
            }
        }

        if (IsInLayerMask(collisionLayer, playerLayer))
        {
            AttemptAttackPlayer(collision.gameObject);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision == null || collision.gameObject == null) return;

        if (IsInLayerMask(collision.gameObject.layer, playerLayer))
        {
            AttemptAttackPlayer(collision.gameObject);
        }
    }

    private bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return layerMask == (layerMask | (1 << layer));
    }

    private void BounceOffWall(Collision2D collision)
    {
        if (collision.contacts.Length == 0)
        {
            Debug.LogWarning("Wall collision has no contact points!");
            return;
        }

        Vector2 normal = collision.contacts[0].normal;
        moveDirection = Vector2.Reflect(moveDirection, normal).normalized;
        
        currentSpeed = Mathf.Min(currentSpeed + speedIncreasePerBounce, maxSpeed);
        rb.linearVelocity = moveDirection * currentSpeed;

        if (audioSource != null && bounceSound != null)
        {
            audioSource.PlayOneShot(bounceSound, 0.4f);
        }

        Debug.Log($"Boss bounced off {collision.gameObject.name}! Speed: {currentSpeed}");
    }

    private void StickToWall(Collision2D collision)
    {
        if (collision.contacts.Length == 0)
        {
            Debug.LogWarning("Wall collision has no contact points!");
            return;
        }

        isStuckToWall = true;

        // Handle multiple contact points (corners)
        if (collision.contacts.Length >= 2)
        {
            Vector2 averageNormal = Vector2.zero;
            Vector2 averagePoint = Vector2.zero;

            for (int i = 0; i < collision.contacts.Length; i++)
            {
                averageNormal += collision.contacts[i].normal;
                averagePoint += collision.contacts[i].point;
            }

            averageNormal = (averageNormal / collision.contacts.Length).normalized;
            averagePoint = averagePoint / collision.contacts.Length;

            // Position boss AWAY from wall, not at contact point
            stuckPosition = averagePoint + averageNormal * stickDistance;
            
            Debug.Log($"Boss stuck in CORNER with {collision.contacts.Length} contact points!");
        }
        else
        {
            Vector2 normal = collision.contacts[0].normal;
            Vector2 contactPoint = collision.contacts[0].point;
            
            // Move boss away from wall by stickDistance
            stuckPosition = contactPoint + normal * stickDistance;
            
            Debug.Log($"Boss stuck to single wall!");
        }
        
        rb.linearVelocity = Vector2.zero;
        StartCoroutine(SetKinematicNextFrame());

        if (spriteRenderer != null)
        {
            spriteRenderer.color = stuckColor;
        }

        StartCoroutine(WallAttackSequence());
    }
    private IEnumerator SetKinematicNextFrame()
    {
        yield return new WaitForFixedUpdate();
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    private IEnumerator WallAttackSequence()
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < wallStickDuration && isStuckToWall)
        {
            // First wave
            ShootCircularBulletWave(0f);
            yield return new WaitForSeconds(timeBetweenWaves);
            
            // Second wave (offset by half the angle)
            ShootCircularBulletWave(angleBetweenBullets / 2f);
            yield return new WaitForSeconds(timeBetweenAttacks);
            
            elapsedTime += timeBetweenWaves + timeBetweenAttacks;
        }

        LeaveWall();
    }

    private void ShootCircularBulletWave(float angleOffset)
    {
        if (bulletPool == null)
        {
            Debug.LogError("BulletPool not assigned!");
            return;
        }

        // Get all valid shooting directions (not blocked by walls)
        List<float> validAngles = GetValidShootingAngles(angleOffset);

        if (validAngles.Count == 0)
        {
            Debug.LogWarning("No valid shooting angles - boss completely surrounded?");
            return;
        }

        int bulletsFired = 0;

        // Shoot bullets only in valid directions
        foreach (float angle in validAngles)
        {
            Vector2 direction = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            ).normalized;

            Vector2 velocity = direction * bulletSpeed;
            bulletPool.SpawnBullet(transform.position, velocity, bulletDamage, bulletLifetime);
            bulletsFired++;
        }

        if (bulletsFired > 0 && audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound, 0.5f);
        }

        Debug.Log($"Fired {bulletsFired} bullets in valid directions");
    }

    private List<float> GetValidShootingAngles(float angleOffset)
    {
        List<float> validAngles = new List<float>();
        
        // Calculate how many angles to check in full 360°
        int totalAngles = Mathf.CeilToInt(360f / angleBetweenBullets);

        for (int i = 0; i < totalAngles; i++)
        {
            float angle = (i * angleBetweenBullets) + angleOffset;
            
            Vector2 direction = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            ).normalized;

            // Check if this direction is clear of walls
            if (!IsWallInDirection(direction))
            {
                validAngles.Add(angle);
            }
        }

        return validAngles;
    }

    private bool IsWallInDirection(Vector2 direction)
    {
        // Start raycast slightly away from boss center to avoid self-collision
        Vector2 rayOrigin = (Vector2)transform.position + (direction.normalized * 0.3f);
        
        // Raycast from offset position
        RaycastHit2D hit = Physics2D.Raycast(
            rayOrigin, 
            direction, 
            wallCheckDistance, 
            wallLayers
        );

        // Visual debug
        Color rayColor = hit.collider != null ? Color.red : Color.green;
        Debug.DrawRay(rayOrigin, direction * wallCheckDistance, rayColor, 0.5f);

        return hit.collider != null;
    }
    private Vector2 GetBestFiringDirection()
    {
        // Cast rays in all directions to find the largest gap
        int rayCount = 36; // Check every 10 degrees
        List<float> clearAngles = new List<float>();

        for (int i = 0; i < rayCount; i++)
        {
            float angle = (360f / rayCount) * i;
            Vector2 direction = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );

            if (!IsWallInDirection(direction))
            {
                clearAngles.Add(angle);
            }
        }

        if (clearAngles.Count == 0)
        {
            // Completely surrounded - shoot in random direction
            return Random.insideUnitCircle.normalized;
        }

        // Find the center of the largest continuous gap
        float largestGapCenter = FindLargestGapCenter(clearAngles);
        
        return new Vector2(
            Mathf.Cos(largestGapCenter * Mathf.Deg2Rad),
            Mathf.Sin(largestGapCenter * Mathf.Deg2Rad)
        ).normalized;
    }

    private float FindLargestGapCenter(List<float> clearAngles)
    {
        if (clearAngles.Count == 0) return 0f;
        if (clearAngles.Count == 1) return clearAngles[0];

        // Sort angles
        clearAngles.Sort();

        // Find largest continuous gap
        float maxGapSize = 0f;
        float maxGapStart = clearAngles[0];
        float maxGapEnd = clearAngles[0];

        float currentGapStart = clearAngles[0];
        
        for (int i = 1; i < clearAngles.Count; i++)
        {
            float angleDiff = clearAngles[i] - clearAngles[i - 1];
            
            // If gap is too large, we've found a new section
            if (angleDiff > angleBetweenBullets * 2)
            {
                float gapSize = clearAngles[i - 1] - currentGapStart;
                if (gapSize > maxGapSize)
                {
                    maxGapSize = gapSize;
                    maxGapStart = currentGapStart;
                    maxGapEnd = clearAngles[i - 1];
                }
                currentGapStart = clearAngles[i];
            }
        }

        // Check final gap
        float finalGapSize = clearAngles[clearAngles.Count - 1] - currentGapStart;
        if (finalGapSize > maxGapSize)
        {
            maxGapStart = currentGapStart;
            maxGapEnd = clearAngles[clearAngles.Count - 1];
        }

        // Return center of largest gap
        return (maxGapStart + maxGapEnd) / 2f;
    }
    private void LeaveWall()
    {
        isStuckToWall = false;
        rb.bodyType = RigidbodyType2D.Dynamic;
        
        currentSpeed = moveSpeed;
        UpdateMoveDirection();
        
        hasShield = true;
        currentShieldHealth = shieldHealth;
        UpdateShieldVisual();

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        Debug.Log("Boss left wall and shield restored!");
    }

    private void AttemptAttackPlayer(GameObject player)
    {
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            // Try PlayerHealth first (your main system)
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                if (!playerHealth.IsDead() && !playerHealth.IsOnDamageCooldown())
                {
                    playerHealth.TakeDamage(attackDamage, transform.position);
                    lastAttackTime = Time.time;
                    Debug.Log($"Boss attacked player for {attackDamage} damage");
                }
                return;
            }

            // Fallback to IDamageable interface
            IDamageable damageable = player.GetComponent<IDamageable>();
            if (damageable != null && !damageable.IsDead())
            {
                damageable.TakeDamage(attackDamage);
                lastAttackTime = Time.time;
                Debug.Log($"Boss attacked damageable for {attackDamage} damage");
            }
        }
    }

    #region IDamageable Implementation

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        if (hasShield)
        {
            currentShieldHealth -= damage;
            
            if (currentShieldHealth <= 0)
            {
                BreakShield();
            }
            else
            {
                if (spriteRenderer != null)
                {
                    if (flashCoroutine != null) StopCoroutine(flashCoroutine);
                    flashCoroutine = StartCoroutine(FlashDamage());
                }
            }

            if (audioSource != null && damageSound != null)
            {
                audioSource.PlayOneShot(damageSound, 0.7f);
            }
        }
        else
        {
            currentHealth -= damage;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

            if (spriteRenderer != null)
            {
                if (flashCoroutine != null) StopCoroutine(flashCoroutine);
                flashCoroutine = StartCoroutine(FlashDamage());
            }

            if (audioSource != null && damageSound != null)
            {
                audioSource.PlayOneShot(damageSound);
            }

            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            Debug.Log($"Boss took {damage} damage. Health: {currentHealth}/{maxHealth}");

            if (currentHealth <= 0)
            {
                Die();
            }
        }
    }

    public bool IsDead() => isDead;
    public GameObject GetGameObject() => gameObject;

    #endregion

    private void BreakShield()
    {
        hasShield = false;
        currentShieldHealth = 0;
        UpdateShieldVisual();

        if (audioSource != null && shieldBreakSound != null)
        {
            audioSource.PlayOneShot(shieldBreakSound);
        }

        OnShieldBroken?.Invoke();

        Debug.Log("Boss shield broken!");
    }

    private void UpdateShieldVisual()
    {
        if (shieldVisual != null)
        {
            shieldVisual.SetActive(hasShield);
        }

        if (spriteRenderer != null && hasShield)
        {
            spriteRenderer.color = Color.Lerp(originalColor, shieldColor, 0.3f);
        }
        else if (spriteRenderer != null && !hasShield && !isStuckToWall)
        {
            spriteRenderer.color = originalColor;
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        StopAllCoroutines();

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        DropCoins();

        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        OnDeath?.Invoke();

        Debug.Log("Boss defeated!");

        Destroy(gameObject, 2f);

        BossSceneManager bossManager = FindObjectOfType<BossSceneManager>();
        if (bossManager != null)
            bossManager.OnBossDefeated();
    }

    private void DropCoins()
    {
        if (coinPrefab == null) return;

        int coinCount = Random.Range(minCoins, maxCoins + 1);

        for (int i = 0; i < coinCount; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * coinSpreadRadius;
            Vector3 spawnPosition = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0f);

            GameObject coin = Instantiate(coinPrefab, spawnPosition, Quaternion.identity);

            Rigidbody2D coinRb = coin.GetComponent<Rigidbody2D>();
            if (coinRb != null)
            {
                Vector2 randomForce = new Vector2(
                    Random.Range(-coinDropForce, coinDropForce),
                    Random.Range(coinDropForce * 0.5f, coinDropForce)
                );
                coinRb.AddForce(randomForce, ForceMode2D.Impulse);
                coinRb.AddTorque(Random.Range(-5f, 5f), ForceMode2D.Impulse);
            }
        }
    }

    private IEnumerator FlashDamage()
    {
        // How many times to flash and how long each flash lasts
        int flashCount = 3;
        float flashOnDuration = 0.07f;
        float flashOffDuration = 0.07f;

        // Safety — make sure white sprite starts hidden
        if (whiteSpriteRenderer != null)
            whiteSpriteRenderer.enabled = false;

        for (int i = 0; i < flashCount; i++)
        {
            // Hide normal sprite, show white sprite
            if (spriteRenderer != null) spriteRenderer.enabled = false;
            if (whiteSpriteRenderer != null) whiteSpriteRenderer.enabled = true;

            yield return new WaitForSeconds(flashOnDuration);

            // Show normal sprite, hide white sprite
            if (spriteRenderer != null) spriteRenderer.enabled = true;
            if (whiteSpriteRenderer != null) whiteSpriteRenderer.enabled = false;

            yield return new WaitForSeconds(flashOffDuration);
        }

        // Guarantee clean state at the end
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        if (whiteSpriteRenderer != null) whiteSpriteRenderer.enabled = false;
    }

    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public float GetHealthPercentage() => (float)currentHealth / maxHealth;
    public bool HasShield() => hasShield;
    public int GetShieldHealth() => currentShieldHealth;

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Draw health bar
        Vector3 healthBarPos = transform.position + Vector3.up * 2.5f;
        float healthPercentage = GetHealthPercentage();

        Gizmos.color = Color.red;
        Gizmos.DrawLine(healthBarPos - Vector3.right * 1f, healthBarPos + Vector3.right * 1f);

        Gizmos.color = Color.green;
        Vector3 healthEnd = healthBarPos + Vector3.right * (healthPercentage * 2f - 1f);
        Gizmos.DrawLine(healthBarPos - Vector3.right * 1f, healthEnd);

        // Draw shield bar
        if (hasShield)
        {
            Vector3 shieldBarPos = transform.position + Vector3.up * 3f;
            float shieldPercentage = (float)currentShieldHealth / shieldHealth;

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(shieldBarPos - Vector3.right * 1f, shieldBarPos + Vector3.right * 1f);

            Gizmos.color = Color.blue;
            Vector3 shieldEnd = shieldBarPos + Vector3.right * (shieldPercentage * 2f - 1f);
            Gizmos.DrawLine(shieldBarPos - Vector3.right * 1f, shieldEnd);
        }

        Gizmos.color = hasShield ? Color.cyan : Color.magenta;
        Gizmos.DrawWireSphere(transform.position, 1.5f);

        // Draw valid firing directions when stuck
        if (isStuckToWall)
        {
            List<float> validAngles = GetValidShootingAngles(0f);
            
            foreach (float angle in validAngles)
            {
                Vector2 direction = new Vector2(
                    Mathf.Cos(angle * Mathf.Deg2Rad),
                    Mathf.Sin(angle * Mathf.Deg2Rad)
                );

                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, (Vector2)transform.position + direction * 2f);
            }

            // Draw blocked directions
            int totalAngles = Mathf.CeilToInt(360f / angleBetweenBullets);
            for (int i = 0; i < totalAngles; i++)
            {
                float angle = i * angleBetweenBullets;
                Vector2 direction = new Vector2(
                    Mathf.Cos(angle * Mathf.Deg2Rad),
                    Mathf.Sin(angle * Mathf.Deg2Rad)
                );

                if (IsWallInDirection(direction))
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(transform.position, (Vector2)transform.position + direction * 1f);
                }
            }
        }
    }
}