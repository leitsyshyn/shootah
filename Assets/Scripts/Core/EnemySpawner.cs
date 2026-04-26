using UnityEngine;

public sealed class EnemySpawner : MonoBehaviour
{
    [SerializeField] private Camera arenaCamera;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform enemyParent;
    [SerializeField] private EnemyConfig enemyConfig;

    private SurvivalArenaGame game;
    private Transform player;
    private Collider2D playerCollider;
    private PlayerHealth playerHealth;
    private PickupSpawner pickupSpawner;
    private float spawnInterval;
    private float minSpawnDistance;
    private float maxSpawnDistance;
    private LayerMask spawnBlockingMask;
    private float spawnClearanceRadius;
    private float nextSpawnTime;
    private bool isSpawning = true;

    private void Awake()
    {
        ApplyConfig();
    }

    public void BeginSession(
        SurvivalArenaGame owner,
        Transform target,
        Collider2D targetCollider,
        PlayerHealth targetHealth,
        PickupSpawner spawner)
    {
        game = owner;
        player = target;
        playerCollider = targetCollider;
        playerHealth = targetHealth;
        pickupSpawner = spawner;
        ApplyConfig();
        isSpawning = true;
        nextSpawnTime = Time.time + 0.5f;
    }

    private void Update()
    {
        if (!isSpawning || game == null || !game.IsRunActive || player == null || Time.time < nextSpawnTime)
        {
            return;
        }

        nextSpawnTime = Time.time + spawnInterval;
        SpawnEnemy();
    }

    public void StopSpawning()
    {
        isSpawning = false;
    }

    private void SpawnEnemy()
    {
        Vector2 spawnPosition = FindSpawnPosition();

        GameObject enemyObject = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity, enemyParent);
        Collider2D collider = enemyObject.GetComponent<Collider2D>();
        if (playerCollider != null && collider != null)
        {
            Physics2D.IgnoreCollision(collider, playerCollider, true);
        }

        enemyObject.GetComponent<Enemy>().BeginSession(game, player, playerHealth, pickupSpawner);
    }

    private Vector2 FindSpawnPosition()
    {
        const int maxAttempts = 8;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector2 spawnPosition = GetViewportEdgeSpawnPosition();
            if (!IsSpawnLocationBlocked(spawnPosition))
            {
                return spawnPosition;
            }
        }

        return GetFallbackSpawnPosition();
    }

    private Vector2 GetViewportEdgeSpawnPosition()
    {
        float minPadding = Mathf.Max(0.5f, minSpawnDistance);
        float maxPadding = Mathf.Max(minPadding, maxSpawnDistance);
        float outwardPadding = Random.Range(minPadding, maxPadding);
        float lateralPadding = maxPadding;
        Vector3 cameraPosition = arenaCamera.transform.position;
        float halfHeight = arenaCamera.orthographicSize;
        float halfWidth = halfHeight * arenaCamera.aspect;

        return Random.Range(0, 4) switch
        {
            0 => new Vector2(
                cameraPosition.x - halfWidth - outwardPadding,
                cameraPosition.y + Random.Range(-halfHeight - lateralPadding, halfHeight + lateralPadding)),
            1 => new Vector2(
                cameraPosition.x + halfWidth + outwardPadding,
                cameraPosition.y + Random.Range(-halfHeight - lateralPadding, halfHeight + lateralPadding)),
            2 => new Vector2(
                cameraPosition.x + Random.Range(-halfWidth - lateralPadding, halfWidth + lateralPadding),
                cameraPosition.y + halfHeight + outwardPadding),
            _ => new Vector2(
                cameraPosition.x + Random.Range(-halfWidth - lateralPadding, halfWidth + lateralPadding),
                cameraPosition.y - halfHeight - outwardPadding)
        };
    }

    private Vector2 GetFallbackSpawnPosition()
    {
        Vector2 direction = Random.insideUnitCircle.normalized;
        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = Vector2.right;
        }

        float distance = Mathf.Max(1f, maxSpawnDistance);
        return (Vector2)player.position + direction * distance;
    }

    private bool IsSpawnLocationBlocked(Vector2 spawnPosition)
    {
        if (spawnBlockingMask.value == 0 || spawnClearanceRadius <= 0f)
        {
            return false;
        }

        return Physics2D.OverlapCircle(spawnPosition, spawnClearanceRadius, spawnBlockingMask) != null;
    }

    private void ApplyConfig()
    {
        spawnInterval = enemyConfig.SpawnInterval;
        minSpawnDistance = enemyConfig.MinSpawnDistance;
        maxSpawnDistance = enemyConfig.MaxSpawnDistance;
        spawnBlockingMask = enemyConfig.SpawnBlockingMask;
        spawnClearanceRadius = enemyConfig.SpawnClearanceRadius;
    }
}
