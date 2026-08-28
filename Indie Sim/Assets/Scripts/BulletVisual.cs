using UnityEngine;
 
/// <summary>
/// Purely visual bullet — no Rigidbody, no collision, no damage.
/// Call Initialize() right after Instantiate to set direction and speed.
/// </summary>
public class BulletVisual : MonoBehaviour
{
    [SerializeField] private float lifetime = 3f;
 
    private Vector2 direction;
    private float speed;
 
    /// <summary>
    /// Called by the shooter immediately after Instantiate.
    /// </summary>
    public void Initialize(Vector2 moveDirection, float moveSpeed)
    {
        direction = moveDirection.normalized;
        speed = moveSpeed;
 
        // Rotate sprite to face movement direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
 
        Destroy(gameObject, lifetime);
    }
 
    void Update()
    {
        // Purely visual movement — no physics involved
        transform.Translate(Vector2.right * speed * Time.deltaTime, Space.Self);
        // Space.Self means it moves along its own forward axis (already rotated to face direction)
    }
}