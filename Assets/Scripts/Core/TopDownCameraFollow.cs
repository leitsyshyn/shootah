using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class TopDownCameraFollow : MonoBehaviour
{
    private static readonly Color BackgroundColor = new(0.06f, 0.075f, 0.09f, 1f);

    [SerializeField] private CameraConfig cameraConfig;

    private Camera targetCamera;
    private Transform target;
    private float followSharpness;

    public Camera WorldCamera => targetCamera;

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
        ApplyCameraConfig(targetCamera);
    }

    private void OnValidate()
    {
        if (!Application.isPlaying && cameraConfig != null)
        {
            Camera camera = GetComponent<Camera>();
            if (camera != null)
            {
                ApplyCameraConfig(camera);
            }
        }
    }

    public void SetFollowTarget(Transform followTarget)
    {
        target = followTarget;
        if (target != null)
        {
            transform.position = new Vector3(target.position.x, target.position.y, -10f);
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = new(target.position.x, target.position.y, -10f);
        if (followSharpness <= 0f)
        {
            transform.position = desiredPosition;
            return;
        }

        float interpolation = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, interpolation);
    }

    private void ApplyCameraConfig(Camera camera)
    {
        camera.orthographic = true;
        camera.orthographicSize = cameraConfig.OrthographicSize;
        camera.backgroundColor = BackgroundColor;
        followSharpness = cameraConfig.FollowSharpness;
    }
}
