using UnityEngine;

[CreateAssetMenu(fileName = "RunConfig", menuName = "Shootah/Config/Run")]
public sealed class RunConfig : ScriptableObject
{
    [SerializeField] private int targetFrameRate = 120;
    [SerializeField] private float targetSurvivalDuration = 60f;

    public int TargetFrameRate => Mathf.Max(30, targetFrameRate);
    public float TargetSurvivalDuration => Mathf.Max(0f, targetSurvivalDuration);
}
