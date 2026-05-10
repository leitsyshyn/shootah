using UnityEngine;

public sealed class EnemyRanged : EnemyBase
{
    [SerializeField] private EnemyProjectile projectilePrefab;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private float moveSpeedOverride;
    [SerializeField] private float desiredRange = 4.5f;
    [SerializeField] private float stopRangeTolerance = 0.6f;
    [SerializeField] private float fireCooldown = 1.2f;
    [SerializeField] private float projectileSpeed = 7.5f;
    [SerializeField] private int projectileDamage = 1;
    [SerializeField] private float projectileLifetime = 2.5f;

    private float nextShotTime;

    protected override bool UseContactDamage => false;

    protected override void OnSessionBegan()
    {
        nextShotTime = 0f;
        if (moveSpeedOverride > 0f)
        {
            moveSpeed = moveSpeedOverride;
        }
    }

    protected override Vector2 GetMovementDirection(Vector2 toPlayer)
    {
        float distance = toPlayer.magnitude;
        if (distance <= 0.001f)
        {
            return Vector2.zero;
        }

        float minRange = Mathf.Max(0f, desiredRange - stopRangeTolerance);
        float maxRange = desiredRange + stopRangeTolerance;
        if (distance > maxRange)
        {
            return toPlayer.normalized;
        }

        if (distance < minRange)
        {
            return -toPlayer.normalized;
        }

        return Vector2.zero;
    }

    protected override Vector2 GetFacingDirection(Vector2 toPlayer, Vector2 movementDirection)
    {
        return toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : movementDirection;
    }

    protected override void OnAfterMovement(Vector2 toPlayer, Vector2 movementDirection)
    {
        if (!CanAct || projectilePrefab == null || projectileSpeed <= 0f || Time.time < nextShotTime)
        {
            return;
        }

        Vector2 aimDirection = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : Vector2.right;
        FireProjectile(aimDirection);
    }

    private void FireProjectile(Vector2 direction)
    {
        nextShotTime = Time.time + Mathf.Max(0.05f, fireCooldown);
        Transform spawnPoint = projectileSpawnPoint != null ? projectileSpawnPoint : transform;
        Quaternion rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        EnemyProjectile projectileInstance = Instantiate(projectilePrefab, spawnPoint.position, rotation);
        projectileInstance.Launch(direction, projectileSpeed, Mathf.Max(0, projectileDamage), Mathf.Max(0.1f, projectileLifetime));
    }
}
