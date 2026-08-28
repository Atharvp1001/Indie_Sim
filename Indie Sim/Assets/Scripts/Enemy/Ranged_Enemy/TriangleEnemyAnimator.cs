using UnityEngine;

[RequireComponent(typeof(TriangleEnemy))]
public class TriangleEnemyAnimator : MonoBehaviour
{
    private TriangleEnemy enemy;
    private Animator animator;
    private bool wasAttacking = false;
    
    void Awake()
    {
        enemy = GetComponent<TriangleEnemy>();
        animator = GetComponentInChildren<Animator>();
        
        if (animator == null)
        {
            Debug.LogError("[TriangleEnemyAnimator] No Animator found on child Sprite object!", this);
        }
    }
    
    void Update()
    {
        if (animator == null || enemy == null) return;
        
        bool isAttackingNow = enemy.IsInAttackMode;
        
        // Match the Capital "I" from your Animator window
        animator.SetBool("IsAttacking", isAttackingNow);

        // Logic to restart the animation exactly once when transition happens
        if (isAttackingNow && !wasAttacking)
        {
            // Play state "Attack" (matches Image 5)
            animator.Play("Attack", 0, 0f); 
        }
        else if (!isAttackingNow && wasAttacking)
        {
            // Play state "Movement" (matches Image 4)
            animator.Play("Movement", 0, 0f);
        }
        
        wasAttacking = isAttackingNow;
    }
}