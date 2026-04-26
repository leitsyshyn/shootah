using UnityEngine;

[CreateAssetMenu(fileName = "PlayerConfig", menuName = "Shootah/Config/Player")]
public sealed class PlayerConfig : ScriptableObject
{
    [SerializeField] private int baseHp = 100;
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float radius = 0.45f;

    public int BaseHp => Mathf.Max(1, baseHp);
    public float MoveSpeed => Mathf.Max(0f, moveSpeed);
    public float Radius => Mathf.Max(0f, radius);
}
