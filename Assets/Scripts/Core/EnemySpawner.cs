using UnityEngine;

public sealed class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    private struct EnemySpawnEntry
    {
        public GameObject prefab;
        public int weight;
    }

    [System.Serializable]
    private struct DifficultyWave
    {
        public float startTime;
        public float spawnInterval;
        public int enemiesPerSpawn;
        public int[] enemyTypeWeights;
    }

    [SerializeField] private Camera arenaCamera;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform enemyParent;
    [SerializeField] private EnemyConfig enemyConfig;
    [SerializeField] private EnemySpawnEntry[] additionalEnemyPrefabs;
    [SerializeField] private DifficultyWave[] difficultyWaves;

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
    private int totalWeight;
    private GameObject[] pooledPrefabs;
    private int[] pooledWeights;
    private int enemiesPerSpawn = 1;

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

        ApplyCurrentWave();
        nextSpawnTime = Time.time + spawnInterval;

        for (int i = 0; i < enemiesPerSpawn; i++)
        {
            SpawnEnemy();
        }
    }

    private void ApplyCurrentWave()
    {
        float elapsed = game.ElapsedRunTime;

        if (difficultyWaves == null || difficultyWaves.Length == 0)
        {
            spawnInterval = enemyConfig.SpawnInterval;
            enemiesPerSpawn = 1;
            BuildWeightedPool(null);
            return;
        }

        DifficultyWave wave = difficultyWaves[0];
        for (int i = difficultyWaves.Length - 1; i >= 0; i--)
        {
            if (elapsed >= difficultyWaves[i].startTime)
            {
                wave = difficultyWaves[i];
                break;
            }
        }

        spawnInterval = Mathf.Max(0.01f, wave.spawnInterval);
        enemiesPerSpawn = Mathf.Max(1, wave.enemiesPerSpawn);
        BuildWeightedPool(wave.enemyTypeWeights);
    }

    public void StopSpawning()
    {
        isSpawning = false;
    }

    private void BuildWeightedPool(int[] waveWeights)
    {
        int additionalCount = additionalEnemyPrefabs != null ? additionalEnemyPrefabs.Length : 0;
        int defaultWeight = GetWaveWeight(waveWeights, 0);

        int activeCount = 0;
        if (defaultWeight > 0 && enemyPrefab != null) activeCount++;
        for (int i = 0; i < additionalCount; i++)
        {
            if (GetWaveWeight(waveWeights, 1 + i) > 0 && additionalEnemyPrefabs[i].prefab != null)
                activeCount++;
        }

        pooledPrefabs = new GameObject[activeCount];
        pooledWeights = new int[activeCount];
        totalWeight = 0;
        int idx = 0;

        if (defaultWeight > 0 && enemyPrefab != null)
        {
            pooledPrefabs[idx] = enemyPrefab;
            pooledWeights[idx] = defaultWeight;
            totalWeight += defaultWeight;
            idx++;
        }

        for (int i = 0; i < additionalCount; i++)
        {
            EnemySpawnEntry entry = additionalEnemyPrefabs[i];
            int w = GetWaveWeight(waveWeights, 1 + i);
            if (w <= 0 || entry.prefab == null) continue;

            pooledPrefabs[idx] = entry.prefab;
            pooledWeights[idx] = w;
            totalWeight += w;
            idx++;
        }
    }

    private int GetWaveWeight(int[] waveWeights, int index)
    {
        if (waveWeights == null)
        {
            if (index == 0) return 100;
            int additionalIdx = index - 1;
            if (additionalIdx >= 0 && additionalIdx < additionalEnemyPrefabs.Length)
                return Mathf.Max(0, additionalEnemyPrefabs[additionalIdx].weight);
            return 0;
        }

        if (index >= waveWeights.Length) return 0;
        return Mathf.Max(0, waveWeights[index]);
    }

    private GameObject PickRandomPrefab()
    {
        if (pooledPrefabs == null || pooledPrefabs.Length == 0)
        {
            return enemyPrefab;
        }

        int roll = Random.Range(0, totalWeight);
        int cumulative = 0;
        for (int i = 0; i < pooledPrefabs.Length; i++)
        {
            if (pooledPrefabs[i] == null)
            {
                continue;
            }

            cumulative += pooledWeights[i];
            if (roll < cumulative)
            {
                return pooledPrefabs[i];
            }
        }

        return pooledPrefabs[0] != null ? pooledPrefabs[0] : enemyPrefab;
    }

    private void SpawnEnemy()
    {
        Vector2 spawnPosition = FindSpawnPosition();
        GameObject selectedPrefab = PickRandomPrefab();
        if (selectedPrefab == null)
        {
            return;
        }

        GameObject enemyObject = Instantiate(selectedPrefab, spawnPosition, Quaternion.identity, enemyParent);
        Collider2D collider = enemyObject.GetComponent<Collider2D>();
        if (playerCollider != null && collider != null)
        {
            Physics2D.IgnoreCollision(collider, playerCollider, true);
        }

        enemyObject.GetComponent<EnemyBase>().BeginSession(game, player, playerHealth, pickupSpawner);
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
