using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerWeapon : MonoBehaviour
{
    private const float MinFireCooldown = 0.06f;

    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private WeaponConfig weaponConfig;

    private SurvivalArenaGame game;
    private Transform projectileParent;
    private int baseMagazineSize;
    private int bonusMagazineSize;
    private float baseFireCooldown;
    private float fireCooldownBonus;
    private float reloadDuration;
    private float projectileSpeed;
    private int baseProjectileDamage;
    private int bonusProjectileDamage;
    private float projectileLifetime;
    private float nextShotTime;
    private Coroutine reloadRoutine;
    private bool fireHeld;

    public int Ammo { get; private set; }
    public int MagazineSize => baseMagazineSize + bonusMagazineSize;
    public float FireCooldown => Mathf.Max(MinFireCooldown, baseFireCooldown - fireCooldownBonus);
    public int ProjectileDamage => baseProjectileDamage + bonusProjectileDamage;
    public bool IsReloading { get; private set; }

    public event Action Changed;

    private void Awake()
    {
        ApplyWeaponConfig();
    }

    public void BindSession(SurvivalArenaGame owner, Transform parent)
    {
        game = owner;
        projectileParent = parent;
        ResetRuntimeState();
    }

    public void OnFire(InputAction.CallbackContext context)
    {
        fireHeld = !context.canceled && context.ReadValueAsButton();
    }

    public void OnReload(InputAction.CallbackContext context)
    {
        if (!context.performed || game == null || !game.IsRunActive)
        {
            return;
        }

        StartReload();
    }

    private void ApplyWeaponConfig()
    {
        baseMagazineSize = weaponConfig.MagazineSize;
        bonusMagazineSize = 0;
        baseFireCooldown = Mathf.Max(MinFireCooldown, weaponConfig.FireCooldown);
        fireCooldownBonus = 0f;
        reloadDuration = weaponConfig.ReloadDuration;
        projectileSpeed = weaponConfig.BulletSpeed;
        baseProjectileDamage = weaponConfig.BulletDamage;
        bonusProjectileDamage = 0;
        projectileLifetime = weaponConfig.BulletLifetime;
        ResetRuntimeState();
    }

    private void ResetRuntimeState()
    {
        nextShotTime = 0f;
        if (reloadRoutine != null)
        {
            StopCoroutine(reloadRoutine);
            reloadRoutine = null;
        }

        IsReloading = false;
        Ammo = MagazineSize;
        Changed?.Invoke();
    }

    public void AddProjectileDamageBonus(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        bonusProjectileDamage += amount;
        Changed?.Invoke();
    }

    public void AddFireRateBonus(float cooldownReduction)
    {
        if (cooldownReduction <= 0f)
        {
            return;
        }

        fireCooldownBonus += cooldownReduction;
        Changed?.Invoke();
    }

    private void Update()
    {
        if (game == null || !game.IsRunActive)
        {
            return;
        }

        HandleShootIntent();
    }

    public void CancelReload()
    {
        if (reloadRoutine != null)
        {
            StopCoroutine(reloadRoutine);
            reloadRoutine = null;
        }

        IsReloading = false;
        Changed?.Invoke();
    }

    private void HandleShootIntent()
    {
        if (!fireHeld || Time.time < nextShotTime)
        {
            return;
        }

        TryShoot();
    }

    private void TryShoot()
    {
        if (IsReloading || Ammo <= 0)
        {
            return;
        }

        Ammo--;
        nextShotTime = Time.time + FireCooldown;
        SpawnProjectile();
        Changed?.Invoke();

        if (Ammo <= 0)
        {
            StartReload();
        }
    }

    private void StartReload()
    {
        if (IsReloading || reloadRoutine != null || Ammo >= MagazineSize)
        {
            return;
        }

        reloadRoutine = StartCoroutine(ReloadRoutine());
    }

    private void SpawnProjectile()
    {
        Vector2 direction = projectileSpawnPoint.right;
        GameObject projectileObject = Instantiate(projectilePrefab, projectileSpawnPoint.position, projectileSpawnPoint.rotation, projectileParent);
        projectileObject.GetComponent<Projectile>().Launch(game, direction, projectileSpeed, ProjectileDamage, projectileLifetime);
    }

    private IEnumerator ReloadRoutine()
    {
        if (IsReloading)
        {
            yield break;
        }

        IsReloading = true;
        Changed?.Invoke();
        yield return new WaitForSeconds(reloadDuration);

        if (game == null || game.IsRunEnded)
        {
            IsReloading = false;
            reloadRoutine = null;
            Changed?.Invoke();
            yield break;
        }

        Ammo = MagazineSize;

        IsReloading = false;
        reloadRoutine = null;
        Changed?.Invoke();
    }
}
