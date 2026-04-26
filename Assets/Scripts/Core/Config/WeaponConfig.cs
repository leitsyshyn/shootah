using UnityEngine;

[CreateAssetMenu(fileName = "WeaponConfig", menuName = "Shootah/Config/Weapon")]
public sealed class WeaponConfig : ScriptableObject
{
    [SerializeField] private int magazineSize = 12;
    [SerializeField] private float fireCooldown = 0.18f;
    [SerializeField] private float reloadDuration = 1.4f;
    [SerializeField] private float bulletSpeed = 14f;
    [SerializeField] private int bulletDamage = 1;
    [SerializeField] private float bulletLifetime = 2f;

    public int MagazineSize => Mathf.Max(1, magazineSize);
    public float FireCooldown => Mathf.Max(0f, fireCooldown);
    public float ReloadDuration => Mathf.Max(0f, reloadDuration);
    public float BulletSpeed => Mathf.Max(0f, bulletSpeed);
    public int BulletDamage => Mathf.Max(1, bulletDamage);
    public float BulletLifetime => Mathf.Max(0f, bulletLifetime);
}
