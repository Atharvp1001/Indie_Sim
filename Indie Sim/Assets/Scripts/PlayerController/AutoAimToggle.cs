using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AutoAimToggle : MonoBehaviour
{
    [SerializeField] private PlayerConeShooter playerShooter;
    [SerializeField] private PlayerAutoAimShooter autoAimShooter;
    [SerializeField] private Button toggleButton;
    [SerializeField] private TextMeshProUGUI statusText; // Text to show ON/OFF status
    [SerializeField] private Color enabledColor = Color.green;
    [SerializeField] private Color disabledColor = Color.white;

    private bool autoAimEnabled = false;

    private void Start()
    {
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleAutoAim);
            UpdateButtonVisual();
            UpdateStatusText();
        }
    }

    private void ToggleAutoAim()
    {
        autoAimEnabled = !autoAimEnabled;

        if (playerShooter != null)
            playerShooter.enabled = !autoAimEnabled;

        if (autoAimShooter != null)
            autoAimShooter.enabled = autoAimEnabled;

        UpdateButtonVisual();
        UpdateStatusText();
    }

    private void UpdateButtonVisual()
    {
        if (toggleButton != null)
        {
            ColorBlock colors = toggleButton.colors;
            colors.normalColor = autoAimEnabled ? enabledColor : disabledColor;
            colors.selectedColor = colors.normalColor;
            toggleButton.colors = colors;
        }
    }

    private void UpdateStatusText()
    {
        if (statusText != null)
        {
            statusText.text = autoAimEnabled ? "Auto Aim: ON" : "Auto Aim: OFF";
        }
    }
}
