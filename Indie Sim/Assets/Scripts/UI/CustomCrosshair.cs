using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class CustomCrosshair : MonoBehaviour
{
    public static CustomCrosshair Instance;

    [Header("Crosshair UI")]
    [SerializeField] private Image crosshairImage;
    [SerializeField] private RectTransform crosshairRect;
    [SerializeField] private Canvas canvas;

    [Header("Hitmarker")]
    [SerializeField] private GameObject hitmarkerObject;
    [SerializeField] private float hitmarkerDuration = 0.15f;

    [Header("Scale Feedback")]
    [SerializeField] private bool enableScaleFeedback = true;
    [SerializeField] private float hitScaleMultiplier = 1.3f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 1, 1, 1);

    [Header("Scene Settings")]
    [SerializeField] private string mainMenuSceneName = "Main Menu"; // ← match exact name

    private bool isShowingHitFeedback = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        bool isMenuScene = SceneManager.GetActiveScene().name == mainMenuSceneName;
        SetCursorAndCrosshair(isMenuScene);

        if (hitmarkerObject != null)
            hitmarkerObject.SetActive(false);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool isMenuScene = scene.name == mainMenuSceneName;

        if (!isMenuScene)
        {
            // Re-grab canvas and crosshair references from new game scene
            canvas = FindFirstObjectByType<Canvas>();

            if (canvas != null)
            {
                Transform crosshairTransform = canvas.transform.Find("Crosshair"); // ← match exact name
                if (crosshairTransform != null)
                {
                    crosshairImage = crosshairTransform.GetComponent<Image>();
                    crosshairRect = crosshairTransform.GetComponent<RectTransform>();

                    // Re-grab hitmarker from crosshair's children
                    if (hitmarkerObject == null)
                        hitmarkerObject = crosshairTransform.Find("Hitmarker")?.gameObject; // ← match exact name
                }
                else
                    Debug.LogWarning("[Crosshair] Crosshair transform not found in canvas!");
            }
            else
                Debug.LogWarning("[Crosshair] Canvas not found in scene!");
        }

        SetCursorAndCrosshair(isMenuScene);

        Debug.Log($"[Crosshair] Scene: {scene.name}, isMenu: {isMenuScene}, cursor: {Cursor.visible}, crosshairImage: {crosshairImage}");
    }

    private void SetCursorAndCrosshair(bool isMenuScene)
    {
        // Menu — show cursor, hide crosshair
        // Game — hide cursor, show crosshair
        Cursor.visible = isMenuScene;
        Cursor.lockState = isMenuScene ? CursorLockMode.None : CursorLockMode.Confined;

        if (crosshairImage != null)
            crosshairImage.gameObject.SetActive(!isMenuScene);

        if (hitmarkerObject != null)
            hitmarkerObject.SetActive(false);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Cursor.visible = true;
    }

    void Update()
    {
        if (crosshairRect != null && canvas != null)
        {
            Vector2 mousePosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                Input.mousePosition,
                canvas.worldCamera,
                out mousePosition
            );

            crosshairRect.localPosition = mousePosition;
        }
    }

    public void ShowHitFeedback()
    {
        if (!isShowingHitFeedback)
            StartCoroutine(HitFeedbackCoroutine());
    }

    private IEnumerator HitFeedbackCoroutine()
    {
        isShowingHitFeedback = true;

        if (hitmarkerObject != null)
            hitmarkerObject.SetActive(true);

        Vector3 originalScale = crosshairRect.localScale;
        Vector3 targetScale = originalScale * hitScaleMultiplier;

        float elapsedTime = 0f;
        while (elapsedTime < hitmarkerDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / hitmarkerDuration;

            if (enableScaleFeedback)
                crosshairRect.localScale = Vector3.Lerp(targetScale, originalScale, t);

            yield return null;
        }

        if (hitmarkerObject != null)
            hitmarkerObject.SetActive(false);

        if (enableScaleFeedback)
            crosshairRect.localScale = originalScale;

        isShowingHitFeedback = false;
    }
}