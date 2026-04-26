using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeTrackConfig", menuName = "Shootah/Config/Upgrade Track")]
public sealed class UpgradeTrackConfig : ScriptableObject
{
    [Serializable]
    public struct UpgradeEntry
    {
        [SerializeField] private RunUpgradeType upgradeType;
        [SerializeField] private float value;

        public RunUpgradeType UpgradeType => upgradeType;
        public float Value => value;
        public int IntValue => Mathf.RoundToInt(value);
    }

    [SerializeField] private int pointsPerUpgrade = 5;
    [SerializeField] private List<UpgradeEntry> upgradeEntries = new();

    public int PointsPerUpgrade => Mathf.Max(1, pointsPerUpgrade);
    public IReadOnlyList<UpgradeEntry> UpgradeEntries => upgradeEntries;

    public bool TryGetUpgrade(int upgradeIndex, out UpgradeEntry entry)
    {
        if (upgradeEntries == null || upgradeEntries.Count == 0)
        {
            entry = default;
            return false;
        }

        // The authored order now lives in the asset. Wrapping preserves the current endless rotation.
        int wrappedIndex = Mathf.Abs(upgradeIndex) % upgradeEntries.Count;
        entry = upgradeEntries[wrappedIndex];
        return true;
    }
}
