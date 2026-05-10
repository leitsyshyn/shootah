using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class EnemyBase : MonoBehaviour
{
    [SerializeField] private EnemyConfig enemyConfig;

    private Rigidbody2D body;
    private SurvivalArenaGame game;
    private Transform player;
    private PlayerHealth playerHealth;
    private PickupSpawner pickupSpawner;
    private SpriteRenderer spriteRenderer;
    private int hp;
    protected float moveSpeed;
    private int contactDamage;
    private float damageCooldown;
    private float nextDamageTime;
    private bool isStopped;
    private bool isDead;

    protected SurvivalArenaGame Game => game;
    protected Transform Player => player;
    protected PlayerHealth PlayerHealth => playerHealth;
    protected Rigidbody2D Body => body;
    protected float MoveSpeed => moveSpeed;
    protected bool CanAct => !isStopped && !isDead && game != null && game.IsRunActive && player != null;

    protected virtual float ContactDamageDistance => 0.9f;
    protected virtual bool UseContactDamage => true;

    protected virtual void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public virtual void BeginSession(SurvivalArenaGame owner, Transform target, PlayerHealth targetHealth, PickupSpawner spawner)
    {
        game = owner;
        player = target;
        playerHealth = targetHealth;
        pickupSpawner = spawner;

        if (enemyConfig == null)
        {
            Debug.LogError("EnemyBase requires an EnemyConfig reference.", this);
            hp = 1;
            moveSpeed = 0f;
            contactDamage = 0;
            damageCooldown = 0f;
        }
        else
        {
            hp = enemyConfig.EnemyHp;
            moveSpeed = enemyConfig.EnemyMoveSpeed;
            contactDamage = enemyConfig.EnemyContactDamage;
            damageCooldown = enemyConfig.EnemyDamageCooldown;
        }

        nextDamageTime = 0f;
        isStopped = false;
        isDead = false;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        OnSessionBegan();
    }

    protected virtual void OnSessionBegan()
    {
    }

    protected virtual void FixedUpdate()
    {
        if (!CanAct)
        {
            StopBody();
            return;
        }

        Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;
        Vector2 movementDirection = GetMovementDirection(toPlayer);
        ApplyMovement(movementDirection);
        Vector2 facingDirection = GetFacingDirection(toPlayer, movementDirection);
        RotateTowards(facingDirection);
        OnAfterMovement(toPlayer, movementDirection);
        DamagePlayerOnContact(toPlayer);
    }

    protected virtual void OnAfterMovement(Vector2 toPlayer, Vector2 movementDirection)
    {
    }

    protected abstract Vector2 GetMovementDirection(Vector2 toPlayer);

    protected virtual Vector2 GetFacingDirection(Vector2 toPlayer, Vector2 movementDirection)
    {
        return movementDirection;
    }

    protected virtual void ApplyMovement(Vector2 direction)
    {
        body.linearVelocity = direction * moveSpeed;
    }

    protected void RotateTowards(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0f)
        {
            return;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
        {
            return;
        }

        hp -= damage;
        if (hp > 0)
        {
            if (spriteRenderer != null)
            {
                StartCoroutine(HitFlashRoutine());
            }
        }
        else
        {
            isDead = true;
            StopBody();
            if (pickupSpawner != null)
            {
                pickupSpawner.TrySpawnPickup(transform.position);
            }

            StartCoroutine(DeathPopRoutine());
        }
    }

    private IEnumerator HitFlashRoutine()
    {
        if (spriteRenderer == null)
        {
            yield break;
        }

        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.06f);
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }

    private IEnumerator DeathPopRoutine()
    {
        Vector3 startScale = transform.localScale;
        Vector3 endScale = startScale * 1.6f;
        Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(startScale, endScale, t);
            if (spriteRenderer != null)
            {
                Color c = startColor;
                c.a = Mathf.Lerp(1f, 0f, t);
                spriteRenderer.color = c;
            }
            yield return null;
        }

        Destroy(gameObject);
    }

    public void StopMoving()
    {
        isStopped = true;
        StopBody();
    }

    protected void StopBody()
    {
        if (body == null)
        {
            return;
        }

        body.linearVelocity = Vector2.zero;
    }

    private void DamagePlayerOnContact(Vector2 toPlayer)
    {
        if (!UseContactDamage || game == null || !game.IsRunActive || playerHealth == null || playerHealth.IsDead)
        {
            return;
        }

        float contactDistance = ContactDamageDistance;
        if (toPlayer.sqrMagnitude <= contactDistance * contactDistance && Time.time >= nextDamageTime)
        {
            nextDamageTime = Time.time + damageCooldown;
            playerHealth.TakeDamage(contactDamage);
        }
    }
}
