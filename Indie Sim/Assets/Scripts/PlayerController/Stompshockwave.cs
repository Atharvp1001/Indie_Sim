using UnityEngine;

/// <summary>
/// Creates an expanding shockwave circle effect for the stomp ability
/// Attach to a prefab with a SpriteRenderer showing a circle sprite
/// </summary>
public class StompShockwave : MonoBehaviour
{
    [Header("Shockwave Settings")]
    [SerializeField] private float expandDuration = 0.3f; // How fast the circle expands
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve alphaCurve = AnimationCurve.Linear(0, 1, 1, 0);
    
    [Header("Visual Settings")]
    [SerializeField] private Color shockwaveColor = new Color(0.5f, 0.8f, 1f, 1f); // Cyan-ish
    [SerializeField] private float lineThickness = 0.2f; // Thickness of the circle ring

    private SpriteRenderer spriteRenderer;
    private float targetRadius;
    private float elapsedTime;
    private Vector3 startScale;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (spriteRenderer == null)
        {
            Debug.LogError("StompShockwave requires a SpriteRenderer component!");
        }
    }

    /// <summary>
    /// Initialize the shockwave with target radius
    /// </summary>
    public void Initialize(float radius)
    {
        targetRadius = radius;
        startScale = Vector3.zero;
        transform.localScale = startScale;
        elapsedTime = 0f;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = shockwaveColor;
        }
    }

    private void Update()
    {
        if (elapsedTime >= expandDuration)
        {
            Destroy(gameObject);
            return;
        }

        elapsedTime += Time.deltaTime;
        float t = elapsedTime / expandDuration;

        // Scale expansion (using curve for smooth animation)
        float scaleValue = scaleCurve.Evaluate(t) * targetRadius * 2f; // *2 because radius to diameter
        transform.localScale = new Vector3(scaleValue, scaleValue, 1f);

        // Fade out alpha
        if (spriteRenderer != null)
        {
            Color currentColor = spriteRenderer.color;
            currentColor.a = alphaCurve.Evaluate(t);
            spriteRenderer.color = currentColor;
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, targetRadius);
    }
}