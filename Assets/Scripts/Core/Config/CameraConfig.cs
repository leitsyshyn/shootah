using UnityEngine;

[CreateAssetMenu(fileName = "CameraConfig", menuName = "Shootah/Config/Camera")]
public sealed class CameraConfig : ScriptableObject
{
    [SerializeField] private float orthographicSize = 7f;
    [SerializeField] private float followSharpness = 0f;

    public float OrthographicSize => Mathf.Max(0.1f, orthographicSize);
    public float FollowSharpness => Mathf.Max(0f, followSharpness);
}
