using UnityEngine;
using UnityEngine.UI;

public class HealthHeart : MonoBehaviour
{
    [Header("Heart Sprites")]
    public Sprite emptyHeart;
    public Sprite fullHeart;

    [Header("References")]
    private Image heartImage;

    private void Awake()
    {
        heartImage = GetComponent<Image>();
    }

    /// <summary>
    /// Set the heart to show full (25 health)
    /// </summary>
    public void SetFull()
    {
        if (heartImage != null && fullHeart != null)
        {
            heartImage.sprite = fullHeart;
            heartImage.enabled = true;
        }
    }

    /// <summary>
    /// Set the heart to show empty (0 health)
    /// </summary>
    public void SetEmpty()
    {
        if (heartImage != null && emptyHeart != null)
        {
            heartImage.sprite = emptyHeart;
            heartImage.enabled = true;
        }
    }
}
