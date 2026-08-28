using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// All-in-one component for a generic upgrade card button.
/// Replaces the old UpgradeButton + UpgradePreviewDisplay scripts.
///
/// Handles: displaying SO data, coin affordability, selection toggle,
/// stat preview text, and "insufficient coins" flash message.
///
/// Inspector wiring:
///   - Assign all UI child references in the Inspector
///   - Wire the Unity Button's OnClick() → this.OnClick()
///   - StoreManager calls Setup() each time the store opens
/// </summary>
public class UpgradeButtonUI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────
    //  INSPECTOR REFERENCES
    // ─────────────────────────────────────────────────────────────────
    [Header("Card UI Elements")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI previewText;       // "DMG: 10 → 15" etc.
    [SerializeField] private TextMeshProUGUI insufficientCoinsText;

    [Tooltip("A child object (border/glow/overlay) that shows when this card is selected.")]
    [SerializeField] private GameObject selectionHighlight;

    [Tooltip("The actual Unity Button component on this GameObject.")]
    [SerializeField] private Button button;

    [Header("Settings")]
    [SerializeField] private float insufficientMessageDuration = 2f;

    // ─────────────────────────────────────────────────────────────────
    //  STATE
    // ─────────────────────────────────────────────────────────────────
    private UpgradeDataSO _data;
    private CoinManager _coinManager;
    private WeaponInventory _weaponInventory;

    // StoreManager subscribes to this
    public System.Action<UpgradeButtonUI> OnButtonClicked;

    public UpgradeDataSO Data => _data;
    public bool IsSelected { get; private set; }

    // ─────────────────────────────────────────────────────────────────
    //  UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────
    private void Awake()
    {
        //_coinManager = FindObjectOfType<CoinManager>();
        _weaponInventory = FindObjectOfType<WeaponInventory>();

        if (button == null)
            button = GetComponent<Button>();

        if (insufficientCoinsText != null)
            insufficientCoinsText.gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────
    //  PUBLIC API — called by StoreManager
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Feed this button its upgrade data. Call every time the store opens or rerolls.
    /// </summary>
    public void Setup(UpgradeDataSO data)
    {
        _data = data;

        // ── Fill card visuals from the SO ────────────────────────────
        if (nameText != null)
            nameText.text = data.upgradeName;

        if (descriptionText != null)
            descriptionText.text = data.description;

        if (costText != null)
            costText.text = data.cost > 0 ? $"{data.cost} Coins" : "Free";

        if (iconImage != null && data.icon != null)
            iconImage.sprite = data.icon;

        // ── Build the stat preview line ──────────────────────────────
        if (previewText != null)
            previewText.text = BuildPreviewString();

        // ── Reset state ──────────────────────────────────────────────
        SetSelected(false);
        RefreshAffordability();
    }

    /// <summary>
    /// Wired to the Unity Button's OnClick in the Inspector.
    /// </summary>
    public void OnClick()
    {
        if (_data == null) return;

        // Can the player afford it?
        if (_coinManager != null && _coinManager.GetCurrentCoins() < _data.cost)
        {
            StartCoroutine(ShowInsufficientCoinsMessage());
            return;
        }

        // Notify StoreManager — it decides whether to select or deselect
        OnButtonClicked?.Invoke(this);
    }

    /// <summary>
    /// StoreManager calls this to visually select / deselect the card.
    /// </summary>
    public void SetSelected(bool selected)
    {
        IsSelected = selected;

        if (selectionHighlight != null)
            selectionHighlight.SetActive(selected);
    }

    /// <summary>
    /// Re-check coin affordability and grey out if needed.
    /// Call after coins change or on reroll.
    /// </summary>
    public void RefreshAffordability()
    {
        if (button == null || _data == null) return;

        // Use Instance instead of the cached _coinManager field
        bool canAfford = CoinManager.Instance == null || CoinManager.Instance.HasEnoughCoins(_data.cost);
        button.interactable = canAfford;
    }

    // ─────────────────────────────────────────────────────────────────
    //  STAT PREVIEW BUILDER
    //  Reads upgradeType from the SO and builds a "Before → After" string.
    // ─────────────────────────────────────────────────────────────────
    private string BuildPreviewString()
    {
        if (_data == null || UpgradeManager.Instance == null)
            return "";

        switch (_data.upgradeType)
        {
            case UpgradeDataSO.UpgradeType.Speed:
                {
                    float current = UpgradeManager.Instance.GetFinalSpeed();
                    float after = current + _data.speedIncrease;
                    return $"Speed: {current:0.#} → {after:0.#}";
                }

            case UpgradeDataSO.UpgradeType.UnlockGun:
                return $"Unlock {_data.targetWeapon}";

            case UpgradeDataSO.UpgradeType.CoinPurse:
                return $"Coin Capacity +{_data.capacityIncrease}";

            case UpgradeDataSO.UpgradeType.StompUpgrade:
                return $"Stomp Radius +{_data.radiusIncrease}";

            case UpgradeDataSO.UpgradeType.GunUpgrade:
                return BuildGunPreviewString();

            default:
                return _data.upgradeName;
        }
    }

    private string BuildGunPreviewString()
    {
        WeaponData weapon = FindWeapon(_data.targetWeapon);

        if (weapon == null)
            return $"{_data.targetWeapon}: Not yet unlocked";

        string result = "";

        if (_data.damageIncrease > 0)
        {
            int cur = UpgradeManager.Instance.GetFinalDamage(weapon);
            int after = cur + _data.damageIncrease;
            result += $"DMG: {cur} → {after}";
        }

        if (_data.ammoIncrease > 0)
        {
            int cur = UpgradeManager.Instance.GetFinalAmmo(weapon);
            int after = cur + _data.ammoIncrease;
            if (result.Length > 0) result += "  |  ";
            result += $"Ammo: {cur} → {after}";
        }

        return result.Length > 0 ? result : _data.upgradeName;
    }

    private WeaponData FindWeapon(UpgradeDataSO.TargetWeapon target)
    {
        if (_weaponInventory == null) return null;

        foreach (WeaponData w in _weaponInventory.GetAllWeapons())
        {
            if (w == null) continue;

            switch (target)
            {
                case UpgradeDataSO.TargetWeapon.Pistol:
                    if (w.weaponType == WeaponData.WeaponType.Piercer ||
                        w.weaponType == WeaponData.WeaponType.Standard)
                        return w;
                    break;

                case UpgradeDataSO.TargetWeapon.Shotgun:
                    if (w.weaponType == WeaponData.WeaponType.Shotgun)
                        return w;
                    break;

                case UpgradeDataSO.TargetWeapon.MachineGun:
                    if (w.weaponName.ToLower().Contains("machine") ||
                        w.weaponName.ToLower().Contains("ak"))
                        return w;
                    break;
            }
        }
        return null;
    }

    // ─────────────────────────────────────────────────────────────────
    //  INSUFFICIENT COINS FLASH
    // ─────────────────────────────────────────────────────────────────
    private IEnumerator ShowInsufficientCoinsMessage()
    {
        if (insufficientCoinsText == null) yield break;

        insufficientCoinsText.gameObject.SetActive(true);
        yield return new WaitForSeconds(insufficientMessageDuration);
        insufficientCoinsText.gameObject.SetActive(false);
    }
}