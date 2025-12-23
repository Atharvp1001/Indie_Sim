using UnityEngine;
using TMPro;

public class AchievementMenuController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject AchievementMenuPanel;

    [Header("Achievement UI Text Fields")]
    [SerializeField] private TextMeshProUGUI killAchievementTitleCount;
    [SerializeField] private TextMeshProUGUI CoinAchievementTitleCount;

    private void Start()
    {
        // Give the UI references to the AchievementManager
        if (AchievementManager.Instance != null)
        {
            // Update the manager's UI references (they get cleared when scene changes)
            AchievementManager.Instance.SetUIReferences(killAchievementTitleCount, CoinAchievementTitleCount);

            // Now update the UI with current data
            AchievementManager.Instance.UpdateAchievementUI();
        }
    }

    public void enableAchievemnetPanel()
    {

        // Update UI when opening the panel (in case data changed)
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.UpdateAchievementUI();
        }

        // Enable achievement panel
        if (!AchievementMenuPanel.activeSelf)
        AchievementMenuPanel.SetActive(true);
        else AchievementMenuPanel.SetActive(false);




    }

    public void disableAchievemnetPanel()
    {
        // Disable achievement panel
        AchievementMenuPanel.SetActive(false);
    }
}
