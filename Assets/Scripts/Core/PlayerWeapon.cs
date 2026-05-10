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
    private Projectile resolvedProjectilePrefab;
    private int baseMagazineSize;
    private int bonusMagazineSize;
    private float baseFireCooldown;
    private float fireCooldownBonus;
    private float reloadDuration;
    private float projectileSpeed;
    private int baseProjectileDamage;
    private int bonusProjectileDamage;
    private float projectileLifetime;
    private int pelletsPerShot;
    private float spreadAngle;
    private bool isExplosive;
    private float explosionRadius;
    private int baseExplosionDamage;
    private float nextShotTime;
    private Coroutine reloadRoutine;
    private bool fireHeld;

    public int Ammo { get; private set; }
    public int MagazineSize => baseMagazineSize + bonusMagazineSize;
    public float FireCooldown => Mathf.Max(MinFireCooldown, baseFireCooldown - fireCooldownBonus);
    public int ProjectileDamage => baseProjectileDamage + bonusProjectileDamage;
    public int ExplosionDamage => baseExplosionDamage + bonusProjectileDamage;
    public string WeaponDisplayName => weaponConfig != null ? weaponConfig.DisplayName : "Weapon";
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
        if (weaponConfig == null)
        {
            Debug.LogError("PlayerWeapon requires a weapon config.", this);
            baseMagazineSize = 1;
            baseFireCooldown = MinFireCooldown;
            reloadDuration = 0f;
            projectileSpeed = 0f;
            baseProjectileDamage = 0;
            projectileLifetime = 0f;
            pelletsPerShot = 1;
            spreadAngle = 0f;
            isExplosive = false;
            explosionRadius = 0f;
            baseExplosionDamage = 0;
            resolvedProjectilePrefab = projectilePrefab != null ? projectilePrefab.GetComponent<Projectile>() : null;
            ResetRuntimeState();
            return;
        }

        baseMagazineSize = weaponConfig.MagazineSize;
        bonusMagazineSize = 0;
        baseFireCooldown = Mathf.Max(MinFireCooldown, weaponConfig.FireCooldown);
        fireCooldownBonus = 0f;
        reloadDuration = weaponConfig.ReloadDuration;
        projectileSpeed = weaponConfig.BulletSpeed;
        baseProjectileDamage = weaponConfig.BulletDamage;
        bonusProjectileDamage = 0;
        projectileLifetime = weaponConfig.BulletLifetime;
        pelletsPerShot = weaponConfig.PelletCount;
        spreadAngle = weaponConfig.SpreadAngle;
        isExplosive = weaponConfig.IsExplosive;
        explosionRadius = weaponConfig.ExplosionRadius;
        baseExplosionDamage = weaponConfig.ExplosionDamage;
        resolvedProjectilePrefab = weaponConfig.ProjectilePrefab != null
            ? weaponConfig.ProjectilePrefab
            : projectilePrefab != null ? projectilePrefab.GetComponent<Projectile>() : null;
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
        if (IsReloading || Ammo <= 0 || projectileSpawnPoint == null || resolvedProjectilePrefab == null)
        {
            return;
        }

        Ammo--;
        nextShotTime = Time.time + FireCooldown;
        SpawnProjectiles();
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

    private void SpawnProjectiles()
    {
        Projectile.LaunchData launchData = new Projectile.LaunchData(
            projectileSpeed,
            ProjectileDamage,
            projectileLifetime,
            isExplosive,
            explosionRadius,
            ExplosionDamage);

        int shotCount = Mathf.Max(1, pelletsPerShot);
        float baseAngle = projectileSpawnPoint.eulerAngles.z;
        for (int i = 0; i < shotCount; i++)
        {
            float angleOffset = GetShotAngleOffset(i, shotCount);
            Quaternion rotation = Quaternion.Euler(0f, 0f, baseAngle + angleOffset);
            Vector2 direction = rotation * Vector2.right;
            Projectile projectileInstance = Instantiate(resolvedProjectilePrefab, projectileSpawnPoint.position, rotation, projectileParent);
            projectileInstance.Launch(game, direction, launchData);
        }
    }

    private float GetShotAngleOffset(int pelletIndex, int shotCount)
    {
        if (spreadAngle <= 0f)
        {
            return 0f;
        }

        if (shotCount <= 1)
        {
            return UnityEngine.Random.Range(-spreadAngle * 0.5f, spreadAngle * 0.5f);
        }

        float t = shotCount == 1 ? 0.5f : pelletIndex / (float)(shotCount - 1);
        return Mathf.Lerp(-spreadAngle * 0.5f, spreadAngle * 0.5f, t);
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
