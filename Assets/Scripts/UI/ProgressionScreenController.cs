using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class ProgressionScreenController : MonoBehaviour
{
    [Header("Header")]
    [SerializeField] private Text currencyText;

    [Header("Sections")]
    [SerializeField] private Transform permanentUpgradesContainer;
    [SerializeField] private Transform weaponUnlocksContainer;
    [SerializeField] private ProgressionShopItemView shopItemPrefab;

    private readonly System.Collections.Generic.List<ProgressionShopItemView> upgradeViews = new();
    private readonly System.Collections.Generic.List<ProgressionShopItemView> weaponViews = new();
    private PersistentProgressionService progression;
    private PersistentProgressionConfig config;
    private bool built;

    private void Awake()
    {
        progression = PersistentProgressionService.Instance;
        config = progression.Config;
    }

    private void OnEnable()
    {
        progression ??= PersistentProgressionService.Instance;
        config ??= progression.Config;
        progression.ProfileChanged += Refresh;
        BuildIfNeeded();
        Refresh();
    }

    private void OnDisable()
    {
        if (progression != null)
        {
            progression.ProfileChanged -= Refresh;
        }
    }

    public void Refresh()
    {
        if (!built || progression == null || config == null)
        {
            return;
        }

        if (currencyText != null)
        {
            currencyText.text = $"Currency: {progression.TotalCurrency}";
        }

        RefreshUpgrades();
        RefreshWeaponUnlocks();
    }

    private void BuildIfNeeded()
    {
        if (built)
        {
            return;
        }

        if (config == null || shopItemPrefab == null || permanentUpgradesContainer == null || weaponUnlocksContainer == null)
        {
            Debug.LogError("ProgressionScreenController is missing required references.", this);
            return;
        }

        IReadOnlyList<PersistentProgressionConfig.PermanentUpgradeDefinition> upgrades = config.PermanentUpgrades;
        for (int i = 0; i < upgrades.Count; i++)
        {
            if (upgrades[i] == null)
            {
                continue;
            }

            upgradeViews.Add(Instantiate(shopItemPrefab, permanentUpgradesContainer));
        }

        IReadOnlyList<PersistentProgressionConfig.WeaponUnlockDefinition> weaponUnlocks = config.WeaponUnlocks;
        for (int i = 0; i < weaponUnlocks.Count; i++)
        {
            if (weaponUnlocks[i] == null)
            {
                continue;
            }

            weaponViews.Add(Instantiate(shopItemPrefab, weaponUnlocksContainer));
        }

        built = true;
    }

    private void RefreshUpgrades()
    {
        IReadOnlyList<PersistentProgressionConfig.PermanentUpgradeDefinition> upgrades = config.PermanentUpgrades;
        int viewIndex = 0;
        for (int i = 0; i < upgrades.Count && viewIndex < upgradeViews.Count; i++)
        {
            PersistentProgressionConfig.PermanentUpgradeDefinition definition = upgrades[i];
            if (definition == null)
            {
                continue;
            }

            ProgressionShopItemView view = upgradeViews[viewIndex++];
            int currentLevel = progression.GetUpgradeLevel(definition.Id);
            bool isMaxed = currentLevel >= definition.MaxLevel;
            int nextCost = progression.GetUpgradeCost(definition, currentLevel);
            bool canAfford = !isMaxed && progression.TotalCurrency >= nextCost;

            string costLabel = isMaxed ? "Cost: MAX" : $"Cost: {nextCost}";
            string stateLabel = isMaxed
                ? $"Level {currentLevel}/{definition.MaxLevel} • Maxed"
                : canAfford
                    ? $"Level {currentLevel}/{definition.MaxLevel} • Purchasable"
                    : $"Level {currentLevel}/{definition.MaxLevel} • Insufficient Currency";
            string buttonLabel = isMaxed ? "Maxed" : "Buy";
            ProgressionShopItemView.ShopItemVisualState visualState = isMaxed
                ? ProgressionShopItemView.ShopItemVisualState.Complete
                : canAfford
                    ? ProgressionShopItemView.ShopItemVisualState.Available
                    : ProgressionShopItemView.ShopItemVisualState.Unavailable;

            view.Bind(
                definition.DisplayName,
                costLabel,
                stateLabel,
                buttonLabel,
                canAfford,
                visualState,
                () => TryPurchaseUpgrade(definition.Id));
        }
    }

    private void RefreshWeaponUnlocks()
    {
        IReadOnlyList<PersistentProgressionConfig.WeaponUnlockDefinition> weaponUnlocks = config.WeaponUnlocks;
        int viewIndex = 0;
        for (int i = 0; i < weaponUnlocks.Count && viewIndex < weaponViews.Count; i++)
        {
            PersistentProgressionConfig.WeaponUnlockDefinition definition = weaponUnlocks[i];
            if (definition == null)
            {
                continue;
            }

            ProgressionShopItemView view = weaponViews[viewIndex++];
            bool isOwned = progression.IsWeaponUnlocked(definition.WeaponId);
            int unlockCost = progression.GetWeaponUnlockCost(definition);
            bool canAfford = !isOwned && progression.TotalCurrency >= unlockCost;
            bool isSelected = isOwned && string.Equals(progression.SelectedWeaponId, definition.WeaponId, System.StringComparison.Ordinal);

            string stateLabel = isOwned
                ? isSelected
                    ? "Equipped"
                    : "Owned"
                : canAfford
                    ? "Locked • Purchasable"
                    : "Locked • Insufficient Currency";
            string buttonLabel = isOwned
                ? isSelected
                    ? "Equipped"
                    : "Equip"
                : "Unlock";
            ProgressionShopItemView.ShopItemVisualState visualState = isOwned
                ? ProgressionShopItemView.ShopItemVisualState.Complete
                : canAfford
                    ? ProgressionShopItemView.ShopItemVisualState.Available
                    : ProgressionShopItemView.ShopItemVisualState.Unavailable;
            bool buttonInteractable = isOwned ? !isSelected : canAfford;
            UnityEngine.Events.UnityAction action = isOwned
                ? () => TryEquipWeapon(definition.WeaponId)
                : () => TryPurchaseWeaponUnlock(definition.WeaponId);

            view.Bind(
                definition.DisplayName,
                $"Cost: {unlockCost}",
                stateLabel,
                buttonLabel,
                buttonInteractable,
                visualState,
                action);
        }
    }

    private void TryPurchaseUpgrade(string upgradeId)
    {
        if (progression.TryPurchaseUpgrade(upgradeId))
        {
            Refresh();
        }
    }

    private void TryPurchaseWeaponUnlock(string weaponId)
    {
        if (progression.TryPurchaseWeaponUnlock(weaponId))
        {
            Refresh();
        }
    }

    private void TryEquipWeapon(string weaponId)
    {
        if (progression.TrySetSelectedWeapon(weaponId))
        {
            Refresh();
        }
    }
}
