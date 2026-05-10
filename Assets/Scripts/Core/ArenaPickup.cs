using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public sealed class ArenaPickup : MonoBehaviour
{
    [SerializeField] private PickupType pickupType = PickupType.Health;
    [SerializeField] private PickupConfig pickupConfig;

    private CircleCollider2D pickupCollider;
    private SpriteRenderer spriteRenderer;
    private SurvivalArenaGame game;
    private PlayerHealth playerHealth;
    private bool isCollected;

    public PickupType PickupKind => pickupType;

    private void Awake()
    {
        pickupCollider = GetComponent<CircleCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void BeginSession(SurvivalArenaGame owner, PlayerHealth targetHealth)
    {
        game = owner;
        playerHealth = targetHealth;
        isCollected = false;
        pickupCollider.enabled = true;
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = 1f;
            spriteRenderer.color = c;
        }
        transform.localScale = Vector3.one * 0.15f;
    }

    private void Update()
    {
        if (isCollected || game == null || game.IsRunEnded || playerHealth == null)
        {
            return;
        }

        if (pickupType == PickupType.Points)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 4f) * 0.03f;
            transform.localScale = Vector3.one * 0.15f * pulse;
        }
        else
        {
            float pulse = 1f + Mathf.Sin(Time.time * 3f) * 0.02f;
            transform.localScale = Vector3.one * 0.15f * pulse;
        }

        if (pickupType != PickupType.Points)
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
                if (GameEffects.Instance != null)
                {
                    GameEffects.Instance.HealFlash();
                }
                break;
            case PickupType.Points:
                int pointAmount = GetConfiguredAmount();
                PersistentProgressionService.Instance.AddPersistentCurrency(pointAmount);
                game.AddRunPoints(pointAmount);
                break;
        }

        Destroy(gameObject);
    }

    private int GetConfiguredAmount()
    {
        return pickupType == PickupType.Health ? pickupConfig.HpPickupHealAmount : pickupConfig.PointPickupValue;
    }
}
