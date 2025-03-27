using UnityEngine;
using System.Collections; // For coroutine

public class Enemy : MonoBehaviour
{
    public int health = 100;
    public float knockbackForce = 5f; // Adjust this value for stronger knockback
    public float knockbackDuration = 0.2f; // Time before enemy can move again

    private Rigidbody2D rb;
    private bool isKnockedBack = false;

    public delegate void EnemyDeathHandler();
    public event EnemyDeathHandler OnDeath;


    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("No Rigidbody2D found on Enemy!");
        }
    }

    public void TakeDamage(int amount, Vector2 knockbackVector)
    {
        health -= amount;
        Debug.Log("Enemy took damage: " + amount + " | Health left: " + health);

        // Apply Knockback
        if (rb != null)
        {
            StartCoroutine(KnockbackCoroutine(knockbackVector));
        }

        // Destroy Enemy if Health is 0
        if (health <= 0)
        {
            OnDeath?.Invoke();
            Destroy(gameObject);
        }
    }

    private IEnumerator KnockbackCoroutine(Vector2 force)
    {
        isKnockedBack = true;
        rb.linearVelocity = Vector2.zero; // Reset velocity before applying force
        rb.linearVelocity = force; // Directly set velocity instead of AddForce

        yield return new WaitForSeconds(knockbackDuration); // Wait for knockback effect

        isKnockedBack = false;
    }
}
