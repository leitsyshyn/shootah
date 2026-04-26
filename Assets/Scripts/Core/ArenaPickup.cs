using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public sealed class ArenaPickup : MonoBehaviour
{
    [SerializeField] private PickupType pickupType = PickupType.Health;
    [SerializeField] private PickupConfig pickupConfig;

    private CircleCollider2D pickupCollider;
    private SurvivalArenaGame game;
    private PlayerHealth playerHealth;
    private bool isCollected;

    public PickupType PickupKind => pickupType;

    private void Awake()
    {
        pickupCollider = GetComponent<CircleCollider2D>();
    }

    public void BeginSession(SurvivalArenaGame owner, PlayerHealth targetHealth)
    {
        game = owner;
        playerHealth = targetHealth;
        isCollected = false;
        pickupCollider.enabled = true;
    }

    private void Update()
    {
        if (isCollected || game == null || game.IsRunEnded || playerHealth == null || pickupType != PickupType.Points)
        {
            return;
        }

        Vector3 playerPosition = playerHealth.transform.position;
        Vector3 currentPosition = transform.position;
        Vector3 toPlayer = playerPosition - currentPosition;
        float attractionRadius = pickupConfig.PointPickupAttractionRadius;
        if (toPlayer.sqrMagnitude > attractionRadius * attractionRadius)
        {
            return;
        }

        float attractionSpeed = pickupConfig.PointPickupAttractionSpeed;
        transform.position = Vector3.MoveTowards(currentPosition, playerPosition, attractionSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected || game == null || game.IsRunEnded || playerHealth == null)
        {
            return;
        }

        PlayerHealth otherHealth = other.GetComponent<PlayerHealth>();
        if (otherHealth != playerHealth)
        {
            return;
        }

        isCollected = true;
        pickupCollider.enabled = false;

        switch (pickupType)
        {
            case PickupType.Health:
                playerHealth.Heal(GetConfiguredAmount());
                break;
            case PickupType.Points:
                game.AddRunPoints(GetConfiguredAmount());
                break;
        }

        Destroy(gameObject);
    }

    private int GetConfiguredAmount()
    {
        return pickupType == PickupType.Health ? pickupConfig.HpPickupHealAmount : pickupConfig.PointPickupValue;
    }
}
