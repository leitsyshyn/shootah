using UnityEngine;

public sealed class EnemyJumper : EnemyBase
{
    [SerializeField] private float jumpInterval = 1.15f;
    [SerializeField] private float jumpDistance = 2.1f;
    [SerializeField] private float jumpDuration = 0.45f;
    [SerializeField] private float landingRadius = 1.1f;
    [SerializeField] private int landingDamage = 15;
    [SerializeField] private Sprite indicatorSprite;

    private float nextJumpTime;
    private bool isJumping;
    private Vector2 jumpStartPos;
    private Vector2 jumpTargetPos;
    private float jumpStartTime;

    protected override bool UseContactDamage => false;

    protected override void OnSessionBegan()
    {
        nextJumpTime = Time.time + Random.Range(0.2f, 0.5f);
        isJumping = false;
        StopBody();
    }

    protected override Vector2 GetMovementDirection(Vector2 toPlayer)
    {
        return Vector2.zero;
    }

    protected override void ApplyMovement(Vector2 direction)
    {
        Body.linearVelocity = Vector2.zero;
    }

    protected override void OnAfterMovement(Vector2 toPlayer, Vector2 movementDirection)
    {
        if (isJumping)
        {
            float elapsed = Time.time - jumpStartTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, jumpDuration));
            float eased = EaseOutQuad(t);
            Body.MovePosition(Vector2.Lerp(jumpStartPos, jumpTargetPos, eased));

            if (t >= 1f)
            {
                isJumping = false;
                StopBody();
                PerformLandingDamage();
                ShowLandingIndicator();
                nextJumpTime = Time.time + Mathf.Max(0.2f, jumpInterval);
            }

            return;
        }

        if (!CanAct || Time.time < nextJumpTime)
        {
            return;
        }

        Vector2 direction = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : Vector2.right;
        jumpStartPos = Body.position;
        jumpTargetPos = jumpStartPos + direction * Mathf.Max(0.2f, jumpDistance);
        jumpStartTime = Time.time;
        isJumping = true;
    }

    private static float EaseOutQuad(float t)
    {
        return t * (2f - t);
    }

    private void PerformLandingDamage()
    {
        if (landingDamage <= 0 || landingRadius <= 0f || PlayerHealth == null || PlayerHealth.IsDead)
        {
            return;
        }

        float radius = Mathf.Max(0.1f, landingRadius);
        Vector2 playerPosition = PlayerHealth.transform.position;
        if (((Vector2)transform.position - playerPosition).sqrMagnitude <= radius * radius)
        {
            PlayerHealth.TakeDamage(landingDamage);
        }
    }

    private void ShowLandingIndicator()
    {
        if (indicatorSprite == null)
        {
            return;
        }

        GameObject indicator = new GameObject("LandingAoE");
        indicator.transform.position = transform.position;
        indicator.transform.localScale = Vector3.one * (landingRadius * 2f);
        SpriteRenderer sr = indicator.AddComponent<SpriteRenderer>();
        sr.sprite = indicatorSprite;
        sr.color = new Color(1f, 0.55f, 0f, 0.35f);
        sr.sortingOrder = 5;
        Destroy(indicator, 0.35f);
    }
}
