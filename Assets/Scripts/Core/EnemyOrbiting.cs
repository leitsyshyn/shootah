using UnityEngine;

public sealed class EnemyOrbiting : EnemyBase
{
    [SerializeField] private float moveSpeedOverride = 5f;
    [SerializeField] private float wobbleSpeed = 6f;
    [SerializeField] private float wobbleStrength = 0.5f;

    protected override void OnSessionBegan()
    {
        if (moveSpeedOverride > 0f)
        {
            moveSpeed = moveSpeedOverride;
        }
    }

    protected override Vector2 GetMovementDirection(Vector2 toPlayer)
    {
        if (toPlayer.sqrMagnitude <= 0.001f)
        {
            return Vector2.zero;
        }

        Vector2 radial = toPlayer.normalized;
        Vector2 perpendicular = new Vector2(-radial.y, radial.x);
        float wobble = Mathf.Sin(Time.time * wobbleSpeed) * wobbleStrength;
        Vector2 direction = (radial + perpendicular * wobble).normalized;
        return direction;
    }

    protected override Vector2 GetFacingDirection(Vector2 toPlayer, Vector2 movementDirection)
    {
        return movementDirection.sqrMagnitude > 0.001f ? movementDirection.normalized : toPlayer.normalized;
    }
}
