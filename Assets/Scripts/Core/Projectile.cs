using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class Projectile : MonoBehaviour
{
    public readonly struct LaunchData
    {
        public LaunchData(float speed, int damage, float lifetime, bool isExplosive, float explosionRadius, int explosionDamage)
        {
            Speed = speed;
            Damage = damage;
            Lifetime = lifetime;
            IsExplosive = isExplosive;
            ExplosionRadius = explosionRadius;
            ExplosionDamage = explosionDamage;
        }

        public float Speed { get; }
        public int Damage { get; }
        public float Lifetime { get; }
        public bool IsExplosive { get; }
        public float ExplosionRadius { get; }
        public int ExplosionDamage { get; }
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

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy != null)
        {
            if (isExplosive)
            {
                Explode(enemy);
            }
            else
            {
                enemy.TakeDamage(damage);
                Destroy(gameObject);
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

    private void Explode(Enemy directHitEnemy)
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
                Enemy enemy = overlap.GetComponent<Enemy>();
                if (enemy == null || enemy == directHitEnemy)
                {
                    continue;
                }

                enemy.TakeDamage(explosionDamage);
            }

            if (directHitEnemy != null)
            {
                directHitEnemy.TakeDamage(explosionDamage);
            }
        }

        Destroy(gameObject);
    }
}
