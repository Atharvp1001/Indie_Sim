using UnityEngine;
using UnityEngine.EventSystems;

public class GunController : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 10f;

    void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began && !IsTouchOverUI(touch))
            {
                FireBullet(touch.position);
            }
        }
    }

    void FireBullet(Vector2 touchPosition)
    {
        // Convert screen position to world position
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(touchPosition);
        worldPosition.z = 0f; // Ensure bullet stays in 2D plane

        // Calculate direction
        Vector2 direction = (worldPosition - firePoint.position).normalized;

        // Calculate rotation angle
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        Quaternion rotation = Quaternion.Euler(0, 0, angle);

        // Instantiate bullet
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, rotation);

        // Pass direction to the bullet
        BulletBehaviour bulletScript = bullet.GetComponent<BulletBehaviour>();
        if (bulletScript != null)
        {
            bulletScript.SetDirection(direction);
        }
    }

    // Check if the touch is over any UI element (like the joystick)
    bool IsTouchOverUI(Touch touch)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = touch.position;
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        return results.Count > 0; // Returns true if the touch is on UI
    }
}
