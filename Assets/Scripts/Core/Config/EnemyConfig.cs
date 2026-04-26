using UnityEngine;

[CreateAssetMenu(fileName = "EnemyConfig", menuName = "Shootah/Config/Enemy")]
public sealed class EnemyConfig : ScriptableObject
{
    [SerializeField] private int enemyHp = 3;
    [SerializeField] private float enemyMoveSpeed = 2.2f;
    [SerializeField] private int enemyContactDamage = 10;
    [SerializeField] private float enemyDamageCooldown = 0.6f;
    [SerializeField] private float spawnInterval = 1.2f;
    [SerializeField] private float minSpawnDistance = 1.5f;
    [SerializeField] private float maxSpawnDistance = 4f;
    [SerializeField] private LayerMask spawnBlockingMask;
    [SerializeField] private float spawnClearanceRadius = 0.8f;

    public int EnemyHp => Mathf.Max(1, enemyHp);
    public float EnemyMoveSpeed => Mathf.Max(0f, enemyMoveSpeed);
    public int EnemyContactDamage => Mathf.Max(0, enemyContactDamage);
    public float EnemyDamageCooldown => Mathf.Max(0f, enemyDamageCooldown);
    public float SpawnInterval => Mathf.Max(0.01f, spawnInterval);
    public float MinSpawnDistance => Mathf.Max(0f, minSpawnDistance);
    public float MaxSpawnDistance => Mathf.Max(MinSpawnDistance, maxSpawnDistance);
    public LayerMask SpawnBlockingMask => spawnBlockingMask;
    public float SpawnClearanceRadius => Mathf.Max(0f, spawnClearanceRadius);
}
