using UnityEngine;

public sealed class PickupSpawner : MonoBehaviour
{
    [SerializeField] private GameObject healthPickupPrefab;
    [SerializeField] private GameObject pointPickupPrefab;
    [SerializeField] private Transform pickupParent;
    [SerializeField] private PickupConfig pickupConfig;

    private SurvivalArenaGame game;
    private PlayerHealth playerHealth;
    private float spawnOffsetRadius;

    private void Awake()
    {
        ApplyConfig();
    }

    public void BeginSession(SurvivalArenaGame owner, PlayerHealth targetHealth)
    {
        game = owner;
        playerHealth = targetHealth;
        ApplyConfig();
    }

    public void TrySpawnPickup(Vector2 origin)
    {
        if (game == null || game.IsRunEnded)
        {
            return;
        }

        PickupType? dropType = RollDropType();
        if (!dropType.HasValue)
        {
            return;
        }

        Vector2 spawnPosition = origin + Random.insideUnitCircle * spawnOffsetRadius;
        SpawnPickup(dropType.Value, spawnPosition);
    }

    private PickupType? RollDropType()
    {
        int totalWeight = pickupConfig.TotalDropWeight;
        if (totalWeight <= 0)
        {
            return null;
        }

        int dropNoneWeight = pickupConfig.DropNoneWeight;
        int dropPointsWeight = pickupConfig.DropPointsWeight;
        int roll = Random.Range(0, totalWeight);
        if (roll < dropNoneWeight)
        {
            return null;
        }

        if (roll < dropNoneWeight + dropPointsWeight)
        {
            return PickupType.Points;
        }

        return PickupType.Health;
    }

    private void SpawnPickup(PickupType pickupType, Vector2 spawnPosition)
    {
        GameObject pickupPrefab = pickupType == PickupType.Health ? healthPickupPrefab : pointPickupPrefab;
        GameObject pickupObject = Instantiate(pickupPrefab, spawnPosition, pickupPrefab.transform.rotation, pickupParent);
        pickupObject.GetComponent<ArenaPickup>().BeginSession(game, playerHealth);
    }

    private void ApplyConfig()
    {
        spawnOffsetRadius = pickupConfig.PickupSpawnOffsetRadius;
    }
}
