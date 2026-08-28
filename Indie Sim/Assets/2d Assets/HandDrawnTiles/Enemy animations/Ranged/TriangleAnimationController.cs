using UnityEngine;

public class TriangleAnimationController : MonoBehaviour
{
    private Animator anim;
    private TriangleEnemy logic;

    void Awake()
    {
        anim = GetComponent<Animator>();
        // Reaches up to the parent to find your TriangleEnemy logic
        logic = GetComponentInParent<TriangleEnemy>();
    }

    void Update()
    {
        if (logic == null || anim == null || logic.IsDead()) return;

        // Matches the "Is Attack" parameter in your screenshot exactly
        anim.SetBool("Is Attack", logic.IsInAttackMode);
    }
}