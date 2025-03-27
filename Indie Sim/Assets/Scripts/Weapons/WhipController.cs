using UnityEngine;

public class WhipController : MonoBehaviour
{

    public float damage = 10f;
    public float attackCooldown = 1f;
    private float lastAttackTime = 0f;

  

    public void Attack()
    {
        Debug.Log("Whip Attack!"); // Replace with actual attack logic
        // Later, you can add an attack animation and check for enemy collisions.
    }
}
