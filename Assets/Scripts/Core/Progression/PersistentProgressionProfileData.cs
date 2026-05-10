using System;
using System.Collections.Generic;

[Serializable]
public sealed class PersistentProgressionProfileData
{
    [Serializable]
    public sealed class UpgradeLevelEntry
    {
        public string UpgradeId;
        public int Level;
    }

    [Serializable]
    public sealed class WeaponUnlockEntry
    {
        public string WeaponId;
        public bool Unlocked;
    }

    public int TotalCurrency;
    public string SelectedWeaponId;
    public List<UpgradeLevelEntry> UpgradeLevels = new();
    public List<WeaponUnlockEntry> WeaponUnlocks = new();
}
