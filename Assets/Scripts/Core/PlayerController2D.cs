using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class PlayerController2D : MonoBehaviour
{
    [SerializeField] private PlayerConfig playerConfig;

    private Rigidbody2D body;
    private Camera aimCamera;
    private float baseMoveSpeed;
    private float bonusMoveSpeed;
    private Vector2 moveInput;
    private Vector2 aimScreenPosition;
    private bool controlsEnabled = true;

    public float MoveSpeed => baseMoveSpeed + bonusMoveSpeed;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        ApplyAuthoringConfig();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            ApplyAuthoringScale();
        }
    }

    public void AddMoveSpeedBonus(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        bonusMoveSpeed += amount;
    }

    public bool BindWorldCamera(Camera worldCamera)
    {
        if (worldCamera == null)
        {
            Debug.LogError("PlayerController2D requires a world camera for aim conversion.", this);
            return false;
        }

        aimCamera = worldCamera;
        return true;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (!controlsEnabled)
        {
            moveInput = Vector2.zero;
            return;
        }

        moveInput = context.canceled ? Vector2.zero : NormalizeMove(context.ReadValue<Vector2>());
    }

    public void OnAim(InputAction.CallbackContext context)
    {
        aimScreenPosition = context.canceled ? Vector2.zero : context.ReadValue<Vector2>();
    }

    private void Update()
    {
        if (!controlsEnabled)
        {
            return;
        }

        AimAtInput();
    }

    private void FixedUpdate()
    {
        if (!controlsEnabled)
        {
            return;
        }

        MovePlayer();
    }

    public void DisableControls()
    {
        controlsEnabled = false;
        moveInput = Vector2.zero;
        body.linearVelocity = Vector2.zero;
    }

    private void MovePlayer()
    {
        body.linearVelocity = moveInput * MoveSpeed;
    }

    private void AimAtInput()
    {
        if (aimCamera == null)
        {
            return;
        }

        float planeDistance = Mathf.Abs(transform.position.z - aimCamera.transform.position.z);
        Vector3 aimPoint = new Vector3(aimScreenPosition.x, aimScreenPosition.y, planeDistance);
        Vector3 aimWorldPoint = aimCamera.ScreenToWorldPoint(aimPoint);
        Vector2 aimDirection = (Vector2)aimWorldPoint - (Vector2)transform.position;
        if (aimDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void ApplyAuthoringConfig()
    {
        baseMoveSpeed = playerConfig.MoveSpeed;
        bonusMoveSpeed = 0f;
        controlsEnabled = true;
        ApplyAuthoringScale();
    }

    private void ApplyAuthoringScale()
    {
        if (playerConfig == null)
        {
            return;
        }

        transform.localScale = Vector3.one * (playerConfig.Radius * 2f);
    }

    private static Vector2 NormalizeMove(Vector2 input)
    {
        return input.sqrMagnitude > 1f ? input.normalized : input;
    }
}
