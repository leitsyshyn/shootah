using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class EnemyProjectile : MonoBehaviour
{
    private Rigidbody2D body;
    private Vector2 direction;
    private float speed;
    private int damage;
    private float remainingLifetime;
    private bool isStopped;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    public void Launch(Vector2 travelDirection, float projectileSpeed, int projectileDamage, float lifetime)
    {
        direction = travelDirection.normalized;
        speed = projectileSpeed;
        damage = projectileDamage;
        remainingLifetime = lifetime;
        isStopped = false;
        transform.right = direction;
        StopBody();
    }

    private void FixedUpdate()
    {
        if (isStopped)
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
        if (isStopped)
        {
            return;
        }

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        if (!other.isTrigger)
        {
            if (other.GetComponent<EnemyBase>() != null)
            {
                return;
            }

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
        if (body == null) return;
        body.linearVelocity = Vector2.zero;
    }
}
