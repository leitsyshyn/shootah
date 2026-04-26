using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class Projectile : MonoBehaviour
{
    private Rigidbody2D body;

    private SurvivalArenaGame game;
    private Vector2 direction;
    private float speed;
    private int damage;
    private float remainingLifetime;
    private bool isStopped;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    public void Launch(SurvivalArenaGame owner, Vector2 travelDirection, float travelSpeed, int hitDamage, float lifetime)
    {
        game = owner;
        direction = travelDirection.normalized;
        speed = travelSpeed;
        damage = hitDamage;
        remainingLifetime = lifetime;
        isStopped = false;
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
            Destroy(gameObject);
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
            enemy.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        if (!other.isTrigger)
        {
            Destroy(gameObject);
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
}
