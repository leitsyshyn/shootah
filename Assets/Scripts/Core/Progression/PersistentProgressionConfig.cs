using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PersistentProgressionConfig", menuName = "Shootah/Config/Persistent Progression")]
public sealed class PersistentProgressionConfig : ScriptableObject
{
    [Serializable]
    public sealed class PermanentUpgradeDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName = "Upgrade";
        [SerializeField] private PersistentProgressionStatType affectedStat = PersistentProgressionStatType.MaxHealth;
        [SerializeField] private int maxLevel = 5;
        [SerializeField] private int baseCost = 10;
        [SerializeField] private int costPerLevel = 10;
        [SerializeField] private float valuePerLevel = 10f;

        public string Id => id;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? id : displayName;
        public PersistentProgressionStatType AffectedStat => affectedStat;
        public int MaxLevel => Mathf.Max(0, maxLevel);
        public int ValuePerLevelAsInt => Mathf.RoundToInt(ValuePerLevel);
        public int BaseCost => Mathf.Max(0, baseCost);
        public int CostPerLevel => Mathf.Max(0, costPerLevel);
        public float ValuePerLevel => Mathf.Max(0f, valuePerLevel);

        public int GetCostForLevel(int currentLevel)
        {
            return BaseCost + (Mathf.Max(0, currentLevel) * CostPerLevel);
        }
    }

    [Serializable]
    public sealed class WeaponUnlockDefinition
    {
        [SerializeField] private string weaponId;
        [SerializeField] private string displayName = "Weapon";
        [SerializeField] private WeaponConfig weaponConfig;
        [SerializeField] private int unlockCost = 25;
        [SerializeField] private bool defaultUnlocked;

        public string WeaponId => weaponId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? weaponId : displayName;
        public WeaponConfig WeaponConfig => weaponConfig;
        public int UnlockCost => Mathf.Max(0, unlockCost);
        public bool DefaultUnlocked => defaultUnlocked;
    }

    [Header("Shop")]
    [SerializeField] private ProgressionShopConfig shopConfig;

    [Header("Upgrades")]
    [SerializeField] private List<PermanentUpgradeDefinition> permanentUpgrades = new();

    [Header("Weapon Unlocks")]
    [SerializeField] private List<WeaponUnlockDefinition> weaponUnlocks = new();

    [Header("Defaults")]
    [SerializeField] private WeaponConfig defaultSelectedWeapon;

    public IReadOnlyList<PermanentUpgradeDefinition> PermanentUpgrades => permanentUpgrades;
    public IReadOnlyList<WeaponUnlockDefinition> WeaponUnlocks => weaponUnlocks;
    public ProgressionShopConfig ShopConfig => shopConfig;

    public bool TryGetUpgrade(string upgradeId, out PermanentUpgradeDefinition definition)
    {
        if (permanentUpgrades != null)
        {
            for (int i = 0; i < permanentUpgrades.Count; i++)
            {
                PermanentUpgradeDefinition candidate = permanentUpgrades[i];
                if (candidate != null && string.Equals(candidate.Id, upgradeId, StringComparison.Ordinal))
                {
                    definition = candidate;
                    return true;
                }
            }
        }

        definition = null;
        return false;
    }

    public bool TryGetWeaponDefinition(string weaponId, out WeaponUnlockDefinition definition)
    {
        if (weaponUnlocks != null)
        {
            for (int i = 0; i < weaponUnlocks.Count; i++)
            {
                WeaponUnlockDefinition candidate = weaponUnlocks[i];
                if (candidate != null && string.Equals(candidate.WeaponId, weaponId, StringComparison.Ordinal))
                {
                    definition = candidate;
                    return true;
                }
            }
        }

        definition = null;
        return false;
    }

    public bool TryGetWeaponId(WeaponConfig weaponConfig, out string weaponId)
    {
        if (weaponConfig != null && weaponUnlocks != null)
        {
            for (int i = 0; i < weaponUnlocks.Count; i++)
            {
                WeaponUnlockDefinition definition = weaponUnlocks[i];
                if (definition != null && definition.WeaponConfig == weaponConfig)
                {
                    weaponId = definition.WeaponId;
                    return true;
                }
            }
        }

        weaponId = string.Empty;
        return false;
    }

    public string GetDefaultSelectedWeaponId()
    {
        if (TryGetWeaponId(defaultSelectedWeapon, out string weaponId))
        {
            return weaponId;
        }

        if (weaponUnlocks == null)
        {
            return string.Empty;
        }

        for (int i = 0; i < weaponUnlocks.Count; i++)
        {
            WeaponUnlockDefinition definition = weaponUnlocks[i];
            if (definition != null && definition.DefaultUnlocked && !string.IsNullOrWhiteSpace(definition.WeaponId))
            {
                return definition.WeaponId;
            }
        }

        for (int i = 0; i < weaponUnlocks.Count; i++)
        {
            WeaponUnlockDefinition definition = weaponUnlocks[i];
            if (definition != null && !string.IsNullOrWhiteSpace(definition.WeaponId))
            {
                return definition.WeaponId;
            }
        }

        return string.Empty;
    }
}
