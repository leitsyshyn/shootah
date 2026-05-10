using UnityEngine;

[CreateAssetMenu(fileName = "WeaponConfig", menuName = "Shootah/Config/Weapon")]
public sealed class WeaponConfig : ScriptableObject
{
    [SerializeField] private string displayName = "Machine Gun";
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private int magazineSize = 12;
    [SerializeField] private float fireCooldown = 0.18f;
    [SerializeField] private float reloadDuration = 1.4f;
    [SerializeField] private float bulletSpeed = 14f;
    [SerializeField] private int bulletDamage = 1;
    [SerializeField] private float bulletLifetime = 2f;
    [SerializeField] private int pelletCount = 1;
    [SerializeField] private float spreadAngle = 0f;
    [SerializeField] private int pierceCount;
    [SerializeField] private bool explosive;
    [SerializeField] private float explosionRadius = 0f;
    [SerializeField] private int explosionDamage = 0;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public Projectile ProjectilePrefab => projectilePrefab;
    public int MagazineSize => Mathf.Max(1, magazineSize);
    public float FireCooldown => Mathf.Max(0f, fireCooldown);
    public float ReloadDuration => Mathf.Max(0f, reloadDuration);
    public float BulletSpeed => Mathf.Max(0f, bulletSpeed);
    public int BulletDamage => Mathf.Max(0, bulletDamage);
    public float BulletLifetime => Mathf.Max(0f, bulletLifetime);
    public int PelletCount => Mathf.Max(1, pelletCount);
    public float SpreadAngle => Mathf.Max(0f, spreadAngle);
    public int PierceCount => Mathf.Max(0, pierceCount);
    public bool IsExplosive => explosive && ExplosionRadius > 0f && ExplosionDamage > 0;
    public float ExplosionRadius => Mathf.Max(0f, explosionRadius);
    public int ExplosionDamage => Mathf.Max(0, explosionDamage);
}
