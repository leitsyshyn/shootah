using UnityEngine;

[CreateAssetMenu(fileName = "ProgressionShopConfig", menuName = "Shootah/Config/Progression Shop")]
public sealed class ProgressionShopConfig : ScriptableObject
{
    [SerializeField] private float upgradeCostMultiplier = 1f;
    [SerializeField] private float weaponUnlockCostMultiplier = 1f;
    [SerializeField] private int cheatCurrencyAmount = 25;

    public float UpgradeCostMultiplier => Mathf.Max(0f, upgradeCostMultiplier);
    public float WeaponUnlockCostMultiplier => Mathf.Max(0f, weaponUnlockCostMultiplier);
    public int CheatCurrencyAmount => Mathf.Max(0, cheatCurrencyAmount);
}
