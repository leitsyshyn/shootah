using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RunUpgradeConfig", menuName = "Shootah/Config/Run Upgrades")]
public sealed class RunUpgradeConfig : ScriptableObject
{
    [System.Serializable]
    public struct UpgradeDefinition
    {
        public RunUpgradeType type;
        public string displayName;
        public string description;
        public float value;
    }

    [SerializeField] private List<UpgradeDefinition> upgrades = new();
    [SerializeField] private List<int> xpPerLevel = new() { 5, 10, 20, 40, 80, 160, 320, 640 };
    [SerializeField] private int rerollBaseHpCost = 10;
    [SerializeField] private int choicesPerPopup = 3;

    public IReadOnlyList<UpgradeDefinition> Upgrades => upgrades;
    public IReadOnlyList<int> XpPerLevel => xpPerLevel;
    public int RerollBaseHpCost => Mathf.Max(1, rerollBaseHpCost);
    public int ChoicesPerPopup => Mathf.Clamp(choicesPerPopup, 2, 6);

    public int GetXpForLevel(int levelIndex)
    {
        if (xpPerLevel == null || xpPerLevel.Count == 0) return 5;
        if (levelIndex <= 0) return xpPerLevel[0];
        if (levelIndex >= xpPerLevel.Count) return xpPerLevel[^1];
        return xpPerLevel[levelIndex];
    }
}
