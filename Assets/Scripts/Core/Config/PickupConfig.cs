using UnityEngine;

[CreateAssetMenu(fileName = "PickupConfig", menuName = "Shootah/Config/Pickups")]
public sealed class PickupConfig : ScriptableObject
{
    [SerializeField] private int hpPickupHealAmount = 20;
    [SerializeField] private int pointPickupValue = 1;
    [SerializeField] private float pickupSpawnOffsetRadius = 0.2f;
    [SerializeField] private float pointPickupAttractionRadius = 2.5f;
    [SerializeField] private float pointPickupAttractionSpeed = 7f;
    [SerializeField] private int dropNoneWeight = 50;
    [SerializeField] private int dropPointsWeight = 35;
    [SerializeField] private int dropHealthWeight = 15;

    public int HpPickupHealAmount => Mathf.Max(0, hpPickupHealAmount);
    public int PointPickupValue => Mathf.Max(0, pointPickupValue);
    public float PickupSpawnOffsetRadius => Mathf.Max(0f, pickupSpawnOffsetRadius);
    public float PointPickupAttractionRadius => Mathf.Max(0f, pointPickupAttractionRadius);
    public float PointPickupAttractionSpeed => Mathf.Max(0f, pointPickupAttractionSpeed);
    public int DropNoneWeight => Mathf.Max(0, dropNoneWeight);
    public int DropPointsWeight => Mathf.Max(0, dropPointsWeight);
    public int DropHealthWeight => Mathf.Max(0, dropHealthWeight);
    public int TotalDropWeight => DropNoneWeight + DropPointsWeight + DropHealthWeight;
}
