using UnityEngine;

[CreateAssetMenu(fileName = "New Upgrade", menuName = "Upgrades/Upgrade Data")]
public class UpgradeDataSO : ScriptableObject
{
    // ─────────────────────────────────────────
    //  ENUMS
    //  Defines every upgrade category and every
    //  weapon that can be targeted.
    // ─────────────────────────────────────────
    public enum UpgradeType
    {
        Speed,          // Increases player movement speed
        UnlockGun,      // Unlocks a new weapon slot
        GunUpgrade,     // Buffs an existing weapon (damage / ammo / range)
        CoinPurse,      // Increases coin capacity / economy bonus
        StompUpgrade    // Modifies stomp ability (radius / damage)
    }

    public enum TargetWeapon
    {
        None,           // Use for non-weapon upgrades (Speed, CoinPurse, Stomp)
        Pistol,
        Shotgun,
        MachineGun
    }

    // ─────────────────────────────────────────
    //  UI INFO
    //  Everything the shop card UI needs to
    //  display this upgrade to the player.
    // ─────────────────────────────────────────
    [Header("UI Info")]
    public string upgradeName;

    [TextArea(2, 4)]
    public string description;

    public Sprite icon;

    public int cost;

    // ─────────────────────────────────────────
    //  UPGRADE CLASSIFICATION
    //  Tells the UpgradeManager WHAT to do and
    //  on WHICH weapon to do it.
    // ─────────────────────────────────────────
    [Header("Upgrade Type")]
    public UpgradeType upgradeType;
    public TargetWeapon targetWeapon = TargetWeapon.None;

    // ─────────────────────────────────────────
    //  STAT MODIFIERS
    //  Leave any irrelevant modifiers at 0 –
    //  the UpgradeManager will skip them.
    //  A single card CAN fill in both
    //  damageIncrease AND ammoIncrease to buff
    //  two stats at once.
    // ─────────────────────────────────────────
    [Header("Stat Modifiers (leave unused fields at 0)")]

    [Tooltip("Flat bonus added to weapon's baseDamagePerShot")]
    public int damageIncrease;

    [Tooltip("Flat bonus added to weapon's magazineCapacity")]
    public int ammoIncrease;

    [Tooltip("Multiplier added to player move speed (e.g. 0.5 = +50%)")]
    public float speedIncrease;

    [Tooltip("Flat bonus added to stomp / cone radius")]
    public float radiusIncrease;

    [Tooltip("Increases total coin capacity or economy multiplier")]
    public int capacityIncrease;

    // ─────────────────────────────────────────
    //  UNLOCK CONDITIONS
    //  The shop / upgrade UI should hide or
    //  grey-out this card if the player hasn't
    //  reached the required dungeon level yet.
    // ─────────────────────────────────────────
    [Header("Unlock Conditions")]

    [Tooltip("This upgrade only appears once the player reaches this dungeon floor")]
    public int minDungeonLevel = 1;
}