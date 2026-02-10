using System.Collections;
using UnityEngine;

public class CthulhuEyeEnemy : MonoBehaviour, IDamageable
{
    [Header("Activation Settings")]
    [SerializeField] private float activationRadius = 20f;
    [SerializeField] private float checkInterval = 0.5f;
    [SerializeField] private float activationDelay = 2f; // Delay before starting attacks after seeing player
    [SerializeField] private bool showDebugGizmos = true;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float preferredDistance = 8f;
    [SerializeField] private float distanceTolerance = 1f;

    [Header("Attack Settings")]
    [SerializeField] private GameObject escapePentagramPrefab;
    [SerializeField] private GameObject prisonPentagramPrefab;
    [SerializeField] private float attackCooldown = 4f;
    [SerializeField] [Range(0f, 1f)] private float escapePentagramChance = 0.5f;

    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    [Header("References")]
    private Transform player;
    private Rigidbody2D rb;
    private bool isActivated = false;
    private bool canAttack = false; // New: tracks if delay has passed
    private float nextAttackTime = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        StartCoroutine(ActivationCheck());
    }

    void FixedUpdate()
    {
        if (!isActivated || player == null) 
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        MaintainDistanceFromPlayer();

        // Only attack if delay has passed
        if (canAttack && Time.time >= nextAttackTime)
        {
            SpawnPentagram();
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    private IEnumerator ActivationCheck()
    {
        while (true)
        {
            if (player != null)
            {
                float distanceToPlayer = Vector2.Distance(transform.position, player.position);
                
                if (distanceToPlayer <= activationRadius && !isActivated)
                {
                    isActivated = true;
                    canAttack = false; // Reset attack permission
                    Debug.Log($"[CthulhuEye] Activated at distance {distanceToPlayer} - starting delay");
                    
                    // Start activation delay
                    StartCoroutine(ActivationDelayCoroutine());
                }
                else if (distanceToPlayer > activationRadius && isActivated)
                {
                    isActivated = false;
                    canAttack = false;
                    rb.linearVelocity = Vector2.zero;
                    Debug.Log($"[CthulhuEye] Deactivated");
                }
            }

            yield return new WaitForSeconds(checkInterval);
        }
    }

    private IEnumerator ActivationDelayCoroutine()
    {
        Debug.Log($"[CthulhuEye] Waiting {activationDelay} seconds before attacking...");
        yield return new WaitForSeconds(activationDelay);
        
        // Only enable attacks if still activated
        if (isActivated)
        {
            canAttack = true;
            Debug.Log("[CthulhuEye] Attack delay finished - can now attack!");
        }
    }

    private void MaintainDistanceFromPlayer()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        Vector2 directionToPlayer = (player.position - transform.position).normalized;

        if (distanceToPlayer < preferredDistance - distanceTolerance)
        {
            rb.linearVelocity = -directionToPlayer * moveSpeed;
        }
        else if (distanceToPlayer > preferredDistance + distanceTolerance)
        {
            rb.linearVelocity = directionToPlayer * moveSpeed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void SpawnPentagram()
    {
        if (player == null) return;

        // Randomly choose pentagram type
        bool isEscapePentagram = Random.value < escapePentagramChance;

        GameObject prefabToSpawn = isEscapePentagram ? escapePentagramPrefab : prisonPentagramPrefab;
        
        if (prefabToSpawn == null)
        {
            Debug.LogWarning($"[CthulhuEye] {(isEscapePentagram ? "Escape" : "Prison")} pentagram prefab not assigned!");
            return;
        }

        // Spawn at player's position
        Instantiate(prefabToSpawn, player.position, Quaternion.identity);

        Debug.Log($"[CthulhuEye] Spawned {(isEscapePentagram ? "ESCAPE" : "PRISON")} pentagram");
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"[CthulhuEye] Took {damage} damage. Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public bool IsDead()
    {
        return currentHealth <= 0;
    }

    public GameObject GetGameObject()
    {
        return gameObject;
    }

    private void Die()
    {
        Debug.Log("[CthulhuEye] Died");
        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, activationRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, preferredDistance);

        if (Application.isPlaying && isActivated && player != null)
        {
            // Red if can attack, yellow if in delay
            Gizmos.color = canAttack ? Color.red : Color.yellow;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }
}