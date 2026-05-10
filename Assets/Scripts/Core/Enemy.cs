using UnityEngine;

public sealed class Enemy : EnemyBase
{
    protected override Vector2 GetMovementDirection(Vector2 toPlayer)
    {
        return toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : Vector2.zero;
    }
}
