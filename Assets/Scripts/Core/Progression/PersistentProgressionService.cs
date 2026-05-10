using System;
using System.IO;
using UnityEngine;

public sealed class PersistentProgressionService
{
    private const string ConfigResourcePath = "Progression/PersistentProgressionConfig";
    private const string SaveFileName = "persistent_progression.json";

    private static PersistentProgressionService instance;

    private readonly PersistentProgressionConfig config;
    private readonly string savePath;
    private PersistentProgressionProfileData profile;

    public static PersistentProgressionService Instance => instance ??= CreateInstance();

    public event Action ProfileChanged;

    public int TotalCurrency => profile != null ? Mathf.Max(0, profile.TotalCurrency) : 0;
    public string SavePath => savePath;
    public string SelectedWeaponId => profile != null ? profile.SelectedWeaponId : string.Empty;
    public PersistentProgressionConfig Config => config;

    public static void Warmup()
    {
        _ = Instance;
    }

    private PersistentProgressionService(PersistentProgressionConfig loadedConfig)
    {
        config = loadedConfig;
        savePath = Path.Combine(Application.persistentDataPath, SaveFileName);
        LoadProfile();
    }

    public void AddPersistentCurrency(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        profile.TotalCurrency += amount;
        SaveProfile();
        ProfileChanged?.Invoke();
    }

    public void RemovePersistentCurrency(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        profile.TotalCurrency = Mathf.Max(0, profile.TotalCurrency - amount);
        SaveProfile();
        ProfileChanged?.Invoke();
    }

    public int GetUpgradeLevel(string upgradeId)
    {
        PersistentProgressionProfileData.UpgradeLevelEntry entry = TryFindUpgradeEntry(upgradeId);
        return entry != null ? Mathf.Max(0, entry.Level) : 0;
    }

    public float GetPermanentStatBonus(PersistentProgressionStatType statType)
    {
        if (config == null || config.PermanentUpgrades == null)
        {
            return 0f;
        }

        float totalBonus = 0f;
        for (int i = 0; i < config.PermanentUpgrades.Count; i++)
        {
            PersistentProgressionConfig.PermanentUpgradeDefinition definition = config.PermanentUpgrades[i];
            if (definition == null || definition.AffectedStat != statType)
            {
                continue;
            }

            int level = GetUpgradeLevel(definition.Id);
            totalBonus += definition.ValuePerLevel * level;
        }

        return totalBonus;
    }

    public bool TryPurchaseUpgrade(string upgradeId)
    {
        if (config == null || !config.TryGetUpgrade(upgradeId, out PersistentProgressionConfig.PermanentUpgradeDefinition definition))
        {
            return false;
        }

        PersistentProgressionProfileData.UpgradeLevelEntry entry = GetOrCreateUpgradeEntry(upgradeId);
        if (entry == null)
        {
            return false;
        }

        int currentLevel = Mathf.Max(0, entry.Level);
        if (currentLevel >= definition.MaxLevel)
        {
            return false;
        }

        int cost = GetUpgradeCost(definition, currentLevel);
        if (profile.TotalCurrency < cost)
        {
            return false;
        }

        profile.TotalCurrency -= cost;
        entry.Level = currentLevel + 1;
        SaveProfile();
        ProfileChanged?.Invoke();
        return true;
    }

    public bool IsWeaponUnlocked(string weaponId)
    {
        PersistentProgressionProfileData.WeaponUnlockEntry entry = TryFindWeaponEntry(weaponId);
        return entry != null && entry.Unlocked;
    }

    public bool UnlockWeapon(string weaponId)
    {
        if (config == null || !config.TryGetWeaponDefinition(weaponId, out _))
        {
            return false;
        }

        PersistentProgressionProfileData.WeaponUnlockEntry entry = GetOrCreateWeaponEntry(weaponId);
        if (entry == null || entry.Unlocked)
        {
            return entry != null;
        }

        entry.Unlocked = true;
        SaveProfile();
        ProfileChanged?.Invoke();
        return true;
    }

    public bool TryPurchaseWeaponUnlock(string weaponId)
    {
        if (config == null || !config.TryGetWeaponDefinition(weaponId, out PersistentProgressionConfig.WeaponUnlockDefinition definition))
        {
            return false;
        }

        PersistentProgressionProfileData.WeaponUnlockEntry entry = GetOrCreateWeaponEntry(weaponId);
        if (entry == null || entry.Unlocked)
        {
            return false;
        }

        int unlockCost = GetWeaponUnlockCost(definition);
        if (profile.TotalCurrency < unlockCost)
        {
            return false;
        }

        profile.TotalCurrency -= unlockCost;
        entry.Unlocked = true;
        SaveProfile();
        ProfileChanged?.Invoke();
        return true;
    }

    public bool TrySetSelectedWeapon(string weaponId)
    {
        if (string.IsNullOrWhiteSpace(weaponId) || !IsWeaponUnlocked(weaponId))
        {
            return false;
        }

        profile.SelectedWeaponId = weaponId;
        SaveProfile();
        ProfileChanged?.Invoke();
        return true;
    }

    public void ResetProfile()
    {
        profile = CreateDefaultProfile();
        SaveProfile();
        ProfileChanged?.Invoke();
    }

    public string GetDebugSummary()
    {
        return $"Persistent currency={TotalCurrency}, selectedWeapon={SelectedWeaponId}, " +
               $"healthLevel={GetUpgradeLevel(PersistentProgressionIds.MaxHealthUpgrade)}, " +
               $"speedLevel={GetUpgradeLevel(PersistentProgressionIds.MoveSpeedUpgrade)}, " +
               $"shotgunUnlocked={IsWeaponUnlocked(PersistentProgressionIds.ShotgunWeapon)}, " +
               $"sniperUnlocked={IsWeaponUnlocked(PersistentProgressionIds.SniperRifleWeapon)}, " +
               $"grenadeUnlocked={IsWeaponUnlocked(PersistentProgressionIds.GrenadeLauncherWeapon)}";
    }

    public int GetUpgradeCost(PersistentProgressionConfig.PermanentUpgradeDefinition definition, int currentLevel)
    {
        if (definition == null)
        {
            return 0;
        }

        float multiplier = config != null && config.ShopConfig != null
            ? config.ShopConfig.UpgradeCostMultiplier
            : 1f;
        int baseCost = definition.GetCostForLevel(currentLevel);
        int adjustedCost = Mathf.CeilToInt(baseCost * multiplier);
        return Mathf.Max(0, adjustedCost);
    }

    public int GetWeaponUnlockCost(PersistentProgressionConfig.WeaponUnlockDefinition definition)
    {
        if (definition == null)
        {
            return 0;
        }

        float multiplier = config != null && config.ShopConfig != null
            ? config.ShopConfig.WeaponUnlockCostMultiplier
            : 1f;
        int baseCost = definition.UnlockCost;
        int adjustedCost = Mathf.CeilToInt(baseCost * multiplier);
        return Mathf.Max(0, adjustedCost);
    }

    private static PersistentProgressionService CreateInstance()
    {
        PersistentProgressionConfig loadedConfig = Resources.Load<PersistentProgressionConfig>(ConfigResourcePath);
        if (loadedConfig == null)
        {
            Debug.LogError($"Missing persistent progression config at Resources/{ConfigResourcePath}. Using empty defaults.");
            loadedConfig = ScriptableObject.CreateInstance<PersistentProgressionConfig>();
        }

        return new PersistentProgressionService(loadedConfig);
    }

    private void LoadProfile()
    {
        PersistentProgressionProfileData loadedProfile = null;

        if (File.Exists(savePath))
        {
            try
            {
                string json = File.ReadAllText(savePath);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    loadedProfile = JsonUtility.FromJson<PersistentProgressionProfileData>(json);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to load persistent progression profile from {savePath}. {exception.Message}");
            }
        }

        profile = loadedProfile ?? CreateDefaultProfile();
        bool changed = EnsureProfileDefaults();
        if (loadedProfile == null || changed)
        {
            SaveProfile();
        }
    }

    private PersistentProgressionProfileData CreateDefaultProfile()
    {
        PersistentProgressionProfileData defaultProfile = new PersistentProgressionProfileData
        {
            TotalCurrency = 0,
            SelectedWeaponId = config != null ? config.GetDefaultSelectedWeaponId() : string.Empty
        };

        if (config == null)
        {
            return defaultProfile;
        }

        if (config.PermanentUpgrades != null)
        {
            for (int i = 0; i < config.PermanentUpgrades.Count; i++)
            {
                PersistentProgressionConfig.PermanentUpgradeDefinition definition = config.PermanentUpgrades[i];
                if (definition == null || string.IsNullOrWhiteSpace(definition.Id))
                {
                    continue;
                }

                defaultProfile.UpgradeLevels.Add(new PersistentProgressionProfileData.UpgradeLevelEntry
                {
                    UpgradeId = definition.Id,
                    Level = 0
                });
            }
        }

        if (config.WeaponUnlocks != null)
        {
            for (int i = 0; i < config.WeaponUnlocks.Count; i++)
            {
                PersistentProgressionConfig.WeaponUnlockDefinition definition = config.WeaponUnlocks[i];
                if (definition == null || string.IsNullOrWhiteSpace(definition.WeaponId))
                {
                    continue;
                }

                defaultProfile.WeaponUnlocks.Add(new PersistentProgressionProfileData.WeaponUnlockEntry
                {
                    WeaponId = definition.WeaponId,
                    Unlocked = definition.DefaultUnlocked
                });
            }
        }

        return defaultProfile;
    }

    private bool EnsureProfileDefaults()
    {
        if (profile == null)
        {
            profile = CreateDefaultProfile();
            return true;
        }

        bool changed = false;

        int clampedCurrency = Mathf.Max(0, profile.TotalCurrency);
        if (profile.TotalCurrency != clampedCurrency)
        {
            profile.TotalCurrency = clampedCurrency;
            changed = true;
        }

        profile.UpgradeLevels ??= new System.Collections.Generic.List<PersistentProgressionProfileData.UpgradeLevelEntry>();
        profile.WeaponUnlocks ??= new System.Collections.Generic.List<PersistentProgressionProfileData.WeaponUnlockEntry>();

        if (config != null && config.PermanentUpgrades != null)
        {
            for (int i = 0; i < config.PermanentUpgrades.Count; i++)
            {
                PersistentProgressionConfig.PermanentUpgradeDefinition definition = config.PermanentUpgrades[i];
                if (definition == null || string.IsNullOrWhiteSpace(definition.Id))
                {
                    continue;
                }

                bool hadEntry = TryFindUpgradeEntry(definition.Id) != null;
                PersistentProgressionProfileData.UpgradeLevelEntry entry = GetOrCreateUpgradeEntry(definition.Id);
                if (entry == null)
                {
                    continue;
                }

                if (!hadEntry)
                {
                    changed = true;
                }

                int clampedLevel = Mathf.Clamp(entry.Level, 0, definition.MaxLevel);
                if (entry.Level != clampedLevel)
                {
                    entry.Level = clampedLevel;
                    changed = true;
                }
            }
        }

        if (config != null && config.WeaponUnlocks != null)
        {
            for (int i = 0; i < config.WeaponUnlocks.Count; i++)
            {
                PersistentProgressionConfig.WeaponUnlockDefinition definition = config.WeaponUnlocks[i];
                if (definition == null || string.IsNullOrWhiteSpace(definition.WeaponId))
                {
                    continue;
                }

                bool hadEntry = TryFindWeaponEntry(definition.WeaponId) != null;
                PersistentProgressionProfileData.WeaponUnlockEntry entry = GetOrCreateWeaponEntry(definition.WeaponId);
                if (entry == null)
                {
                    continue;
                }

                if (!hadEntry)
                {
                    changed = true;
                }

                if (definition.DefaultUnlocked && !entry.Unlocked)
                {
                    entry.Unlocked = true;
                    changed = true;
                }
            }
        }

        string fallbackWeaponId = config != null ? config.GetDefaultSelectedWeaponId() : string.Empty;
        if ((string.IsNullOrWhiteSpace(profile.SelectedWeaponId) || !IsWeaponUnlocked(profile.SelectedWeaponId)) &&
            !string.Equals(profile.SelectedWeaponId, fallbackWeaponId, StringComparison.Ordinal))
        {
            profile.SelectedWeaponId = fallbackWeaponId;
            changed = true;
        }

        return changed;
    }

    private PersistentProgressionProfileData.UpgradeLevelEntry TryFindUpgradeEntry(string upgradeId)
    {
        if (string.IsNullOrWhiteSpace(upgradeId) || profile == null || profile.UpgradeLevels == null)
        {
            return null;
        }

        for (int i = 0; i < profile.UpgradeLevels.Count; i++)
        {
            PersistentProgressionProfileData.UpgradeLevelEntry entry = profile.UpgradeLevels[i];
            if (entry != null && string.Equals(entry.UpgradeId, upgradeId, StringComparison.Ordinal))
            {
                return entry;
            }
        }

        return null;
    }

    private void SaveProfile()
    {
        try
        {
            string directory = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonUtility.ToJson(profile, true);
            string tempPath = savePath + ".tmp";
            File.WriteAllText(tempPath, json);

            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }

            File.Move(tempPath, savePath);
        }
        catch (Exception exception)
        {
            Debug.LogError($"Failed to save persistent progression profile to {savePath}. {exception.Message}");
        }
    }

    private PersistentProgressionProfileData.UpgradeLevelEntry GetOrCreateUpgradeEntry(string upgradeId)
    {
        if (string.IsNullOrWhiteSpace(upgradeId) || profile == null)
        {
            return null;
        }

        profile.UpgradeLevels ??= new System.Collections.Generic.List<PersistentProgressionProfileData.UpgradeLevelEntry>();
        PersistentProgressionProfileData.UpgradeLevelEntry existingEntry = TryFindUpgradeEntry(upgradeId);
        if (existingEntry != null)
        {
            return existingEntry;
        }

        PersistentProgressionProfileData.UpgradeLevelEntry newEntry = new PersistentProgressionProfileData.UpgradeLevelEntry
        {
            UpgradeId = upgradeId,
            Level = 0
        };
        profile.UpgradeLevels.Add(newEntry);
        return newEntry;
    }

    private PersistentProgressionProfileData.WeaponUnlockEntry TryFindWeaponEntry(string weaponId)
    {
        if (string.IsNullOrWhiteSpace(weaponId) || profile == null || profile.WeaponUnlocks == null)
        {
            return null;
        }

        for (int i = 0; i < profile.WeaponUnlocks.Count; i++)
        {
            PersistentProgressionProfileData.WeaponUnlockEntry entry = profile.WeaponUnlocks[i];
            if (entry != null && string.Equals(entry.WeaponId, weaponId, StringComparison.Ordinal))
            {
                return entry;
            }
        }

        return null;
    }

    private PersistentProgressionProfileData.WeaponUnlockEntry GetOrCreateWeaponEntry(string weaponId)
    {
        if (string.IsNullOrWhiteSpace(weaponId) || profile == null)
        {
            return null;
        }

        profile.WeaponUnlocks ??= new System.Collections.Generic.List<PersistentProgressionProfileData.WeaponUnlockEntry>();
        PersistentProgressionProfileData.WeaponUnlockEntry existingEntry = TryFindWeaponEntry(weaponId);
        if (existingEntry != null)
        {
            return existingEntry;
        }

        bool defaultUnlocked = config != null &&
                               config.TryGetWeaponDefinition(weaponId, out PersistentProgressionConfig.WeaponUnlockDefinition definition) &&
                               definition.DefaultUnlocked;

        PersistentProgressionProfileData.WeaponUnlockEntry newEntry = new PersistentProgressionProfileData.WeaponUnlockEntry
        {
            WeaponId = weaponId,
            Unlocked = defaultUnlocked
        };
        profile.WeaponUnlocks.Add(newEntry);
        return newEntry;
    }
}
