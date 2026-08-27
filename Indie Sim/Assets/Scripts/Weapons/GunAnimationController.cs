using System;
using UnityEngine;

/// <summary>
/// Script-driven gun animation — no Animator.
///
/// Put this on the gun / hands sprite child (it needs a SpriteRenderer).
/// Add one entry per weapon, assigning its shoot-frame list and reload-frame
/// list. Frame 0 of the shoot list doubles as the idle pose.
///
/// State priority each frame:  reload  >  shooting  >  idle.
///   • Reloading  -> reload frames play once, stretched over WeaponData.reloadTime.
///   • Shooting   -> shoot frames loop at shootFPS.
///   • Idle       -> shoot frame 0, held.
///
/// The active set is chosen from WeaponInventory.OnWeaponChanged.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class GunAnimationController : MonoBehaviour
{
    [Serializable]
    public class GunAnim
    {
        [Tooltip("The weapon asset this frame set belongs to (drag the same WeaponData used in WeaponInventory).")]
        public WeaponData weapon;

        [Tooltip("Shooting frames in play order. Frame 0 is also the idle pose.")]
        public Sprite[] shootFrames;

        [Tooltip("Reload frames in play order.")]
        public Sprite[] reloadFrames;

        [Tooltip("Shoot-loop playback speed (frames per second).")]
        public float shootFPS = 20f;
    }

    [Header("Per-Weapon Frame Sets")]
    [SerializeField] private GunAnim[] weapons;

    [Header("References (auto-found if left empty)")]
    [SerializeField] private PlayerConeShooter shooter;
    [SerializeField] private WeaponAmmoManager ammoManager;

    private SpriteRenderer spriteRenderer;
    private GunAnim active;

    // playback state
    private float animTimer;
    private int animFrame;
    private bool wasReloading;
    private bool wasShooting;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (shooter == null) shooter = GetComponentInParent<PlayerConeShooter>();
    }

    private void OnEnable()
    {
        WeaponInventory.OnWeaponChanged += HandleWeaponChanged;
    }

    private void OnDisable()
    {
        WeaponInventory.OnWeaponChanged -= HandleWeaponChanged;
    }

    private void Start()
    {
        // OnWeaponChanged may have fired before this component was enabled —
        // resolve the current weapon directly.
        if (active == null && shooter != null)
            HandleWeaponChanged(shooter.GetCurrentWeapon());
    }

    private void HandleWeaponChanged(WeaponData newWeapon)
    {
        active = FindSet(newWeapon);
        ResetToIdle();

        if (newWeapon != null && active == null)
            Debug.LogWarning($"[GunAnimationController] No frame set assigned for weapon '{newWeapon.weaponName}'.");
    }

    private GunAnim FindSet(WeaponData weapon)
    {
        if (weapon == null || weapons == null) return null;
        foreach (GunAnim g in weapons)
            if (g != null && g.weapon == weapon) return g;
        return null;
    }

    private void Update()
    {
        if (active == null) return;

        if (ammoManager == null) ammoManager = WeaponAmmoManager.Instance;

        bool isReloading = ammoManager != null && ammoManager.IsReloading();
        bool isShooting  = !isReloading && shooter != null && shooter.IsShooting();

        if (isReloading)
        {
            if (!wasReloading) StartClip();
            TickReload();
        }
        else if (isShooting)
        {
            if (!wasShooting || wasReloading) StartClip();
            TickShoot();
        }
        else
        {
            ResetToIdle();
        }

        wasReloading = isReloading;
        wasShooting = isShooting;
    }

    private void StartClip()
    {
        animTimer = 0f;
        animFrame = 0;
    }

    private void TickShoot()
    {
        Sprite[] frames = active.shootFrames;
        if (frames == null || frames.Length == 0) return;

        float fps = Mathf.Max(1f, active.shootFPS);
        animTimer += Time.deltaTime;

        while (animTimer >= 1f / fps)
        {
            animTimer -= 1f / fps;
            animFrame = (animFrame + 1) % frames.Length; // loop while firing
        }

        spriteRenderer.sprite = frames[animFrame];
    }

    private void TickReload()
    {
        Sprite[] frames = active.reloadFrames;
        if (frames == null || frames.Length == 0)
        {
            ResetToIdle();
            return;
        }

        float reloadTime = active.weapon != null ? Mathf.Max(0.01f, active.weapon.reloadTime) : 1f;
        float fps = frames.Length / reloadTime; // play the set exactly once over the reload

        animTimer += Time.deltaTime;
        while (animTimer >= 1f / fps && animFrame < frames.Length - 1)
        {
            animTimer -= 1f / fps;
            animFrame++;
        }

        spriteRenderer.sprite = frames[Mathf.Clamp(animFrame, 0, frames.Length - 1)];
    }

    private void ResetToIdle()
    {
        animTimer = 0f;
        animFrame = 0;

        if (active != null && active.shootFrames != null && active.shootFrames.Length > 0)
            spriteRenderer.sprite = active.shootFrames[0];
    }
}
