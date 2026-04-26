using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class Enemy : MonoBehaviour
{
    private Rigidbody2D body;
    [SerializeField] private EnemyConfig enemyConfig;

    private SurvivalArenaGame game;
    private Transform player;
    private PlayerHealth playerHealth;
    private int hp;
    private float moveSpeed;
    private int contactDamage;
    private float damageCooldown;
    private float nextDamageTime;
    private bool isStopped;
    private bool isDead;
    private PickupSpawner pickupSpawner;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    public void BeginSession(SurvivalArenaGame owner, Transform target, PlayerHealth targetHealth, PickupSpawner spawner)
    {
        game = owner;
        player = target;
        playerHealth = targetHealth;
        pickupSpawner = spawner;
        hp = enemyConfig.EnemyHp;
        moveSpeed = enemyConfig.EnemyMoveSpeed;
        contactDamage = enemyConfig.EnemyContactDamage;
        damageCooldown = enemyConfig.EnemyDamageCooldown;
        nextDamageTime = 0f;
        isStopped = false;
        isDead = false;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void FixedUpdate()
    {
        if (isStopped || isDead || game == null || !game.IsRunActive || player == null)
        {
            StopBody();
            return;
        }

        Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;
        Vector2 direction = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : Vector2.zero;
        body.linearVelocity = direction * moveSpeed;

        if (direction.sqrMagnitude > 0f)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        DamagePlayerOnContact(toPlayer);
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
        {
            return;
        }

        hp -= damage;
        if (hp <= 0)
        {
            isDead = true;
            StopBody();
            if (pickupSpawner != null)
            {
                pickupSpawner.TrySpawnPickup(transform.position);
            }

            Destroy(gameObject);
        }
    }

    public void StopMoving()
    {
        isStopped = true;
        StopBody();
    }

    private void DamagePlayerOnContact(Vector2 toPlayer)
    {
        if (game == null || !game.IsRunActive || playerHealth == null || playerHealth.IsDead)
        {
            return;
        }

        const float contactDistance = 0.9f;
        if (toPlayer.sqrMagnitude <= contactDistance * contactDistance && Time.time >= nextDamageTime)
        {
            nextDamageTime = Time.time + damageCooldown;
            playerHealth.TakeDamage(contactDamage);
        }
    }

    private void StopBody()
    {
        body.linearVelocity = Vector2.zero;
    }
}
