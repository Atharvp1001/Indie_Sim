using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Dumb view script — handles UI only, zero upgrade math.
///
/// Flow:
///   RoguelikeManager.OpenStore()
///     → StoreManager asks UpgradeManager for the available pool
///     → Picks 2 at random, feeds them to the 2 UpgradeButtonUI cards
///     → Player clicks a card → it toggles selected/deselected
///     → Continue button appears only when a card is selected
///     → Continue → UpgradeManager.ApplyUpgrade(selectedSO) → close
/// </summary>
public class StoreManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject storePanel;

    [Header("The 2 Generic Upgrade Cards")]
    [SerializeField] private UpgradeButtonUI upgradeButtonA;
    [SerializeField] private UpgradeButtonUI upgradeButtonB;

    [Header("Bottom Buttons")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button rerollButton;
    [SerializeField] private Canvas canvas;
    // ─────────────────────────────────────────────────────────────────
    //  PRIVATE STATE
    // ─────────────────────────────────────────────────────────────────
    private UpgradeButtonUI _selectedButton = null;

    // ─────────────────────────────────────────────────────────────────
    //  UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────
    private void Start()
    {
        upgradeButtonA.OnButtonClicked += HandleCardClicked;
        upgradeButtonB.OnButtonClicked += HandleCardClicked;

        continueButton.onClick.AddListener(OnContinueClicked);
        rerollButton.onClick.AddListener(RerollUpgrades);

        continueButton.gameObject.SetActive(false);
        storePanel.SetActive(false);
    }
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (canvas == null)
        {
            Debug.LogWarning("[StoreManager] canvas not assigned in the Inspector — skipping StorePanel lookup.");
            return;
        }

        Transform storePanelTransform = canvas.transform.Find("StorePanel"); // ← match exact name
        if (storePanelTransform != null)
        {
            storePanel = storePanelTransform.gameObject;
            continueButton = storePanelTransform.Find("nextscene")?.GetComponent<Button>();
            rerollButton = storePanelTransform.Find("RerollButton")?.GetComponent<Button>();
            upgradeButtonA = storePanelTransform.Find("UpgradeButtonA")?.GetComponent<UpgradeButtonUI>();
            upgradeButtonB = storePanelTransform.Find("UpgradeButtonB")?.GetComponent<UpgradeButtonUI>();

            Debug.Log($"[StoreManager] storePanel: {storePanel}, continueButton: {continueButton}");
        }
        else
            Debug.LogWarning("[StoreManager] StorePanel not found in canvas!");
    }
    private void OnDestroy()
    {
        upgradeButtonA.OnButtonClicked -= HandleCardClicked;
        upgradeButtonB.OnButtonClicked -= HandleCardClicked;
    }

    // ─────────────────────────────────────────────────────────────────
    //  OPEN — called by RoguelikeManager
    // ─────────────────────────────────────────────────────────────────
    public void OpenStore()
    {
        _selectedButton = null;
        continueButton.gameObject.SetActive(false);
        rerollButton.gameObject.SetActive(true);

        PopulateCards();
        storePanel.SetActive(true);

        RoguelikeManager.Instance?.SetGameplayInputEnabled(false);
    }

    // ─────────────────────────────────────────────────────────────────
    //  POPULATE — pick 2 from the available pool
    // ─────────────────────────────────────────────────────────────────
    private void PopulateCards()
    {
        List<UpgradeDataSO> pool = UpgradeManager.Instance.GetAvailableUpgrades();

        if (pool == null || pool.Count == 0)
        {
            Debug.Log("[StoreManager] No upgrades available.");
            CloseStore();
            return;
        }

        pool = Shuffle(pool);

        // If only 1 upgrade exists, show it on both cards (edge case)
        upgradeButtonA.Setup(pool[0]);
        upgradeButtonB.Setup(pool.Count > 1 ? pool[1] : pool[0]);

        Debug.Log($"[StoreManager] Store populated: '{pool[0].upgradeName}' | " +
                $"'{(pool.Count > 1 ? pool[1].upgradeName : pool[0].upgradeName)}'");
    }

    // ─────────────────────────────────────────────────────────────────
    //  REROLL
    // ─────────────────────────────────────────────────────────────────
    public void RerollUpgrades()
    {
        _selectedButton = null;
        continueButton.gameObject.SetActive(false);

        PopulateCards();
        Debug.Log("[StoreManager] Upgrades rerolled.");
    }

    // ─────────────────────────────────────────────────────────────────
    //  SELECTION TOGGLE
    // ─────────────────────────────────────────────────────────────────
    private void HandleCardClicked(UpgradeButtonUI clickedCard)
    {
        if (_selectedButton == clickedCard)
        {
            // Clicking the already-selected card → deselect it
            _selectedButton.SetSelected(false);
            _selectedButton = null;
        }
        else
        {
            // Deselect previous, select new
            _selectedButton?.SetSelected(false);
            _selectedButton = clickedCard;
            _selectedButton.SetSelected(true);
        }

        // Continue is only visible when something is selected
        bool hasSelection = _selectedButton != null;
        continueButton.gameObject.SetActive(hasSelection);
        rerollButton.gameObject.SetActive(!hasSelection); // hide reroll once selected
    }

    // ─────────────────────────────────────────────────────────────────
    //  CONTINUE — apply upgrade and close
    // ─────────────────────────────────────────────────────────────────
    private void OnContinueClicked()
    {
        if (_selectedButton == null)
        {
            Debug.LogWarning("[StoreManager] Continue pressed with nothing selected!");
            return;
        }

        // Spend the coins
        int cost = _selectedButton.Data.cost;

        // Use Instance instead of FindObjectOfType (faster)
        // AND check the return value — SpendCoins returns false if not enough coins
        if (cost > 0)
        {
            if (CoinManager.Instance == null || !CoinManager.Instance.SpendCoins(cost))
            {
                Debug.LogWarning("[StoreManager] Cannot afford this upgrade — purchase blocked.");
                return; // Don't apply the upgrade if coins failed
            }
        }

        Debug.Log("[StoreManager] Continue clicked after selecting upgrade ");


        // Hand the SO to UpgradeManager — all math happens there
        UpgradeManager.Instance.ApplyUpgrade(_selectedButton.Data);

        CloseStore();
    }

    // ─────────────────────────────────────────────────────────────────
    //  CLOSE
    // ─────────────────────────────────────────────────────────────────
    public void CloseStore()
    {
        storePanel.SetActive(false);
        _selectedButton = null;
        continueButton.gameObject.SetActive(false);
        Debug.Log("[StoreManager] Store closed.");

        RoguelikeManager.Instance?.SetGameplayInputEnabled(true);
    }

    // ─────────────────────────────────────────────────────────────────
    //  UTILITY
    // ─────────────────────────────────────────────────────────────────
    private List<T> Shuffle<T>(List<T> input)
    {
        List<T> list = new List<T>(input);
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }

    // Legacy compatibility — if anything else calls IsUpgradeSelected()
    public bool IsUpgradeSelected() => _selectedButton != null;
}