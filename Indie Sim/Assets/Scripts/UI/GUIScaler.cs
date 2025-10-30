using UnityEngine;
using UnityEngine.UI;

public class GUIScaler : MonoBehaviour
{
    [Header("Joystick References")]
    [SerializeField] private RectTransform[] joysticksToScale; // Drag all joysticks here

    [Header("Scale Settings")]
    [SerializeField] private float smallScale = 0.7f;
    [SerializeField] private float mediumScale = 1.0f;
    [SerializeField] private float largeScale = 1.3f;

    [Header("UI Feedback (Optional)")]
    [SerializeField] private Button smallButton;
    [SerializeField] private Button mediumButton;
    [SerializeField] private Button largeButton;
    [SerializeField] private Color selectedButtonColor = Color.green;
    [SerializeField] private Color normalButtonColor = Color.white;

    // Private variables
    private ScaleSize currentScale = ScaleSize.Medium;

    // Events for extensibility
    public System.Action<ScaleSize> OnScaleChanged;
    public System.Action<float> OnScaleValueChanged;

    // Enum for scale sizes
    public enum ScaleSize
    {
        Small,
        Medium,
        Large
    }

    private void Start()
    {
        InitializeGUIScaler();
    }

    #region Initialization

    private void InitializeGUIScaler()
    {
        // Load saved scale setting
        LoadScaleSetting();

        // Apply loaded scale
        ApplyScale(currentScale);

        // Update button visuals
        UpdateButtonVisuals();

        Debug.Log($"GUI Scaler initialized with {currentScale} scale");
    }

    #endregion

    #region Public Button Methods

    /// <summary>
    /// Sets joysticks to small scale - Call from Small button
    /// </summary>
    public void SetSmallScale()
    {
        SetScale(ScaleSize.Small);
    }

    /// <summary>
    /// Sets joysticks to medium scale - Call from Medium button
    /// </summary>
    public void SetMediumScale()
    {
        SetScale(ScaleSize.Medium);
    }

    /// <summary>
    /// Sets joysticks to large scale - Call from Large button
    /// </summary>
    public void SetLargeScale()
    {
        SetScale(ScaleSize.Large);
    }

    /// <summary>
    /// Generic method to set any scale size
    /// </summary>
    public void SetScale(ScaleSize scaleSize)
    {
        if (currentScale == scaleSize) return;

        currentScale = scaleSize;
        ApplyScale(scaleSize);
        SaveScaleSetting();
        UpdateButtonVisuals();

        // Trigger events
        OnScaleChanged?.Invoke(scaleSize);
        OnScaleValueChanged?.Invoke(GetScaleValue(scaleSize));

        Debug.Log($"GUI Scale changed to: {scaleSize}");
    }

    #endregion

    #region Scale Application

    private void ApplyScale(ScaleSize scaleSize)
    {
        if (joysticksToScale == null || joysticksToScale.Length == 0)
        {
            Debug.LogWarning("No joysticks assigned to scale!");
            return;
        }

        float targetScale = GetScaleValue(scaleSize);
        Vector3 targetScaleVector = Vector3.one * targetScale;

        // Apply scale immediately to all joysticks
        foreach (RectTransform joystick in joysticksToScale)
        {
            if (joystick != null)
            {
                joystick.localScale = targetScaleVector;
            }
        }
    }

    private float GetScaleValue(ScaleSize scaleSize)
    {
        switch (scaleSize)
        {
            case ScaleSize.Small: return smallScale;
            case ScaleSize.Medium: return mediumScale;
            case ScaleSize.Large: return largeScale;
            default: return mediumScale;
        }
    }

    #endregion

    #region UI Visual Feedback

    private void UpdateButtonVisuals()
    {
        // Reset all button colors first
        SetButtonColor(smallButton, normalButtonColor);
        SetButtonColor(mediumButton, normalButtonColor);
        SetButtonColor(largeButton, normalButtonColor);

        // Highlight current selection
        switch (currentScale)
        {
            case ScaleSize.Small:
                SetButtonColor(smallButton, selectedButtonColor);
                break;
            case ScaleSize.Medium:
                SetButtonColor(mediumButton, selectedButtonColor);
                break;
            case ScaleSize.Large:
                SetButtonColor(largeButton, selectedButtonColor);
                break;
        }
    }

    private void SetButtonColor(Button button, Color color)
    {
        if (button != null)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.selectedColor = color;
            button.colors = colors;
        }
    }

    #endregion

    #region Settings Persistence

    private void SaveScaleSetting()
    {
        PlayerPrefs.SetInt("JoystickScale", (int)currentScale);
        PlayerPrefs.Save();
    }

    private void LoadScaleSetting()
    {
        int savedScale = PlayerPrefs.GetInt("JoystickScale", (int)ScaleSize.Medium);
        currentScale = (ScaleSize)savedScale;
    }

    #endregion

    #region Public Getters

    public ScaleSize GetCurrentScale() { return currentScale; }
    public float GetCurrentScaleValue() { return GetScaleValue(currentScale); }

    #endregion

    #region Advanced Features (Future Extensibility)

    /// <summary>
    /// Add a new joystick to be scaled
    /// </summary>
    public void AddJoystickToScale(RectTransform joystick)
    {
        if (joystick == null) return;

        // Resize array and add new joystick
        RectTransform[] newArray = new RectTransform[joysticksToScale.Length + 1];
        joysticksToScale.CopyTo(newArray, 0);
        newArray[joysticksToScale.Length] = joystick;
        joysticksToScale = newArray;

        // Apply current scale to new joystick
        joystick.localScale = Vector3.one * GetCurrentScaleValue();

        Debug.Log($"Added joystick {joystick.name} to GUI Scaler");
    }

    /// <summary>
    /// Remove a joystick from scaling
    /// </summary>
    public void RemoveJoystickFromScale(RectTransform joystick)
    {
        if (joystick == null || joysticksToScale == null) return;

        System.Collections.Generic.List<RectTransform> joystickList =
            new System.Collections.Generic.List<RectTransform>(joysticksToScale);

        if (joystickList.Remove(joystick))
        {
            joysticksToScale = joystickList.ToArray();
            Debug.Log($"Removed joystick {joystick.name} from GUI Scaler");
        }
    }

    /// <summary>
    /// Set custom scale values
    /// </summary>
    public void SetCustomScaleValues(float small, float medium, float large)
    {
        smallScale = small;
        mediumScale = medium;
        largeScale = large;

        // Reapply current scale with new values
        ApplyScale(currentScale);

        Debug.Log($"Updated scale values - Small: {small}, Medium: {medium}, Large: {large}");
    }

    #endregion

    #region Debug Helpers

    [ContextMenu("Test Small Scale")]
    private void TestSmallScale()
    {
        SetSmallScale();
    }

    [ContextMenu("Test Medium Scale")]
    private void TestMediumScale()
    {
        SetMediumScale();
    }

    [ContextMenu("Test Large Scale")]
    private void TestLargeScale()
    {
        SetLargeScale();
    }

    #endregion
}
