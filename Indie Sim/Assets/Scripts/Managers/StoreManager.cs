using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StoreManager : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("The main store panel (parent of UpgradesPanel)")]
    public GameObject storePanel;

    [Tooltip("Panel containing all upgrade buttons with Grid Layout Group")]
    public GameObject upgradesPanel;

    [Tooltip("Assign all upgrade buttons here")]
    public List<Button> allUpgradeButtons;

    [Tooltip("Button that appears after player selects an upgrade")]
    public Button continueButton;

    [Header("Configuration")]
    [Tooltip("Number of buttons to enable randomly (default: 2)")]
    public int buttonsToEnable = 2;

    private bool upgradeSelected = false;

    private void Start()
    {
        // Make sure the store is hidden at start
        if (storePanel != null)
        {
            storePanel.SetActive(false);
        }

        // Hide continue button at start
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Opens the store and randomizes which upgrades appear
    /// Called by RoguelikeManager after dungeon completion
    /// </summary>
    public void OpenStore()
    {
        EnableAllUpgradeButtons();
        // Validate setup
        if (allUpgradeButtons == null || allUpgradeButtons.Count == 0)
        {
            Debug.LogWarning("No upgrade buttons assigned to StoreManager!");
            return;
        }

        if (buttonsToEnable > allUpgradeButtons.Count)
        {
            Debug.LogWarning($"Buttons to enable ({buttonsToEnable}) is greater than total buttons ({allUpgradeButtons.Count}). Adjusting to maximum.");
            buttonsToEnable = allUpgradeButtons.Count;
        }

        // Reset state
        upgradeSelected = false;

        // Show the store panel
        if (storePanel != null)
        {
            storePanel.SetActive(true);
        }

        // Hide continue button
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(false);
        }

        // Randomize which upgrade buttons appear
        RandomizeStoreButtons();

        Debug.Log("Store opened with randomized upgrades");
    }

    /// <summary>
    /// Disables all buttons, then randomly enables the specified number
    /// </summary>
    private void RandomizeStoreButtons()
    {
        // First, disable all buttons
        foreach (Button button in allUpgradeButtons)
        {
            if (button != null)
            {
                button.gameObject.SetActive(false);
            }
        }

        // Create a copy of the buttons list to pick from
        List<Button> availableButtons = new List<Button>(allUpgradeButtons);

        // Randomly enable the specified number of buttons
        for (int i = 0; i < buttonsToEnable; i++)
        {
            if (availableButtons.Count == 0)
                break;

            // Pick a random button from the available list
            int randomIndex = Random.Range(0, availableButtons.Count);
            Button selectedButton = availableButtons[randomIndex];

            // Enable the button
            if (selectedButton != null)
            {
                selectedButton.gameObject.SetActive(true);
                Debug.Log($"Enabled button: {selectedButton.name}");
            }

            // Remove from available list so it can't be picked again
            availableButtons.RemoveAt(randomIndex);
        }

        Debug.Log($"Store randomized: {buttonsToEnable} buttons enabled out of {allUpgradeButtons.Count} total");
    }

    /// <summary>
    /// Call this when an upgrade button is clicked
    /// This will show the continue button
    /// </summary>
    public void OnUpgradeSelected()
    {
        if (upgradeSelected)
        {
            Debug.Log("Upgrade already selected");
            return;
        }

        upgradeSelected = true;

        // Show the continue button
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);
            Debug.Log("Continue button shown - player can now close the store");
        }

        // Optional: Disable all upgrade buttons so player can't select multiple
        DisableAllUpgradeButtons();
    }

    /// <summary>
    /// Disables all upgrade buttons after one is selected
    /// </summary>
    private void DisableAllUpgradeButtons()
    {
        foreach (Button button in allUpgradeButtons)
        {
            if (button != null)
            {
                button.interactable = false;
            }
        }
    }

    private void EnableAllUpgradeButtons()
    {
        foreach (Button button in allUpgradeButtons)
        {
            if (button != null)
            {
                button.interactable = true;
            }
        }
    }

    /// <summary>
    /// Closes the store panel
    /// Called by the Continue button
    /// </summary>
    public void CloseStore()
    {
        if (storePanel != null)
        {
            storePanel.SetActive(false);
        }

        Debug.Log("Store closed - dungeon continues");

        // Optional: Tell RoguelikeManager to continue the game
        // RoguelikeManager.Instance.ContinueDungeon();
    }
}
