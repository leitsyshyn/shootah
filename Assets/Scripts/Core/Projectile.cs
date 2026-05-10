using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class Projectile : MonoBehaviour
{
    public readonly struct LaunchData
    {
        public LaunchData(float speed, int damage, float lifetime, bool isExplosive, float explosionRadius, int explosionDamage, int pierceCount = 0)
        {
            Speed = speed;
            Damage = damage;
            Lifetime = lifetime;
            IsExplosive = isExplosive;
            ExplosionRadius = explosionRadius;
            ExplosionDamage = explosionDamage;
            PierceCount = pierceCount;
        }

        public float Speed { get; }
        public int Damage { get; }
        public float Lifetime { get; }
        public bool IsExplosive { get; }
        public float ExplosionRadius { get; }
        public int ExplosionDamage { get; }
        public int PierceCount { get; }
    }

    private Rigidbody2D body;

    private SurvivalArenaGame game;
    private Vector2 direction;
    private float speed;
    private int damage;
    private bool isExplosive;
    private float explosionRadius;
    private int explosionDamage;
    private float remainingLifetime;
    private bool isStopped;
    private bool hasExploded;
    private int pierceCount;
    private int enemiesPierced;

    [Header("Deceleration")]
    [SerializeField] private bool hasDeceleration;
    [SerializeField] private float decelerationRate;

    [Header("Fragmentation")]
    [SerializeField] private bool spawnFragmentsOnExplode;
    [SerializeField] private int fragmentCount;
    [SerializeField] private float fragmentSpeed;
    [SerializeField] private int fragmentDamage;
    [SerializeField] private float fragmentSpreadAngle;
    [SerializeField] private float fragmentScaleMultiplier;
    [SerializeField] private Projectile fragmentPrefab;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    public void Launch(SurvivalArenaGame owner, Vector2 travelDirection, LaunchData launchData)
    {
        game = owner;
        direction = travelDirection.normalized;
        speed = launchData.Speed;
        damage = launchData.Damage;
        isExplosive = launchData.IsExplosive;
        explosionRadius = launchData.ExplosionRadius;
        explosionDamage = launchData.ExplosionDamage;
        remainingLifetime = launchData.Lifetime;
        pierceCount = launchData.PierceCount;
        enemiesPierced = 0;
        isStopped = false;
        hasExploded = false;
        transform.right = direction;
        StopBody();
    }

    private void FixedUpdate()
    {
        if (isStopped || game == null || game.IsRunEnded)
        {
            StopBody();
            return;
        }

        if (hasDeceleration && speed > 0f)
        {
            speed = Mathf.Max(0f, speed - decelerationRate * Time.fixedDeltaTime);
        }

        body.MovePosition(body.position + direction * (speed * Time.fixedDeltaTime));

        remainingLifetime -= Time.fixedDeltaTime;
        if (remainingLifetime <= 0f)
        {
            ResolveLifetimeEnd();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isStopped || game == null || game.IsRunEnded)
        {
            return;
        }

        EnemyBase enemyBase = other.GetComponent<EnemyBase>();
        if (enemyBase != null)
        {
            if (isExplosive)
            {
                Explode(enemyBase);
            }
            else
            {
                enemyBase.TakeDamage(damage);
                enemiesPierced++;
                if (enemiesPierced > pierceCount)
                {
                    Destroy(gameObject);
                }
            }
            return;
        }

        if (!other.isTrigger)
        {
            if (isExplosive)
            {
                Explode(null);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    public void StopMoving()
    {
        isStopped = true;
        StopBody();
    }

    private void StopBody()
    {
        body.linearVelocity = Vector2.zero;
    }

    private void ResolveLifetimeEnd()
    {
        if (isExplosive)
        {
            Explode(null);
            return;
        }

        Destroy(gameObject);
    }

    private void Explode(EnemyBase directHitEnemy)
    {
        if (hasExploded)
        {
            return;
        }

        hasExploded = true;
        StopMoving();

        if (directHitEnemy != null && damage > 0)
        {
            directHitEnemy.TakeDamage(damage);
        }

        if (explosionDamage > 0 && explosionRadius > 0f)
        {
            Collider2D[] overlaps = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
            foreach (Collider2D overlap in overlaps)
            {
                EnemyBase enemyBase = overlap.GetComponent<EnemyBase>();
                if (enemyBase == null || enemyBase == directHitEnemy)
                {
                    continue;
                }

                enemyBase.TakeDamage(explosionDamage);
            }

            if (directHitEnemy != null)
            {
                directHitEnemy.TakeDamage(explosionDamage);
            }
        }

        if (spawnFragmentsOnExplode && fragmentPrefab != null && fragmentCount > 0)
        {
            SpawnFragments();
        }

        Destroy(gameObject);
    }

    private void SpawnFragments()
    {
        float angleStep = fragmentSpreadAngle / fragmentCount;
        float halfSpread = fragmentSpreadAngle * 0.5f;

        for (int i = 0; i < fragmentCount; i++)
        {
            float baseAngle = Mathf.Lerp(-halfSpread, halfSpread, fragmentCount > 1 ? i / (float)(fragmentCount - 1) : 0.5f);
            float jitter = UnityEngine.Random.Range(-angleStep * 0.4f, angleStep * 0.4f);
            float angle = baseAngle + jitter;
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
            Vector2 direction = rotation * Vector2.right;

            Projectile fragment = Instantiate(fragmentPrefab, transform.position, rotation);
            fragment.transform.localScale *= fragmentScaleMultiplier;

            float fragmentLifetime = Mathf.Max(0.4f, remainingLifetime);

            Projectile.LaunchData fragmentLaunchData = new Projectile.LaunchData(
                fragmentSpeed,
                fragmentDamage,
                fragmentLifetime,
                false,
                0f,
                0
            );

            fragment.Launch(game, direction, fragmentLaunchData);
        }
    }
}
