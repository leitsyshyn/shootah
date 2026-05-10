using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class RunUpgradeChoicePopup : MonoBehaviour
{
    [SerializeField] private Button[] optionButtons;
    [SerializeField] private Text[] optionNameTexts;
    [SerializeField] private Text[] optionDescTexts;
    [SerializeField] private Button rerollButton;
    [SerializeField] private Text rerollButtonLabel;
    [SerializeField] private GameObject popupRoot;

    private SurvivalArenaGame game;
    private PlayerHealth playerHealth;
    private RunUpgradeConfig config;
    private Action<RunUpgradeType> onChosen;

    private List<RunUpgradeType> currentOptions = new();
    private int rerollCount;

    public void Init(SurvivalArenaGame owner, PlayerHealth health, RunUpgradeConfig cfg, Action<RunUpgradeType> callback)
    {
        game = owner;
        playerHealth = health;
        config = cfg;
        onChosen = callback;
        rerollCount = 0;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            int captured = i;
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => OnUpgradeClicked(captured));
        }

        rerollButton.onClick.RemoveAllListeners();
        rerollButton.onClick.AddListener(OnRerollClicked);

        ShowOptions();
    }

    private void ShowOptions()
    {
        currentOptions.Clear();
        var allUpgrades = config.Upgrades;
        if (allUpgrades.Count == 0) return;

        List<int> availableIndices = new List<int>();
        for (int i = 0; i < allUpgrades.Count; i++)
            availableIndices.Add(i);

        int choiceCount = Mathf.Min(config.ChoicesPerPopup, optionButtons.Length, allUpgrades.Count);
        for (int i = 0; i < choiceCount; i++)
        {
            int pick = UnityEngine.Random.Range(0, availableIndices.Count);
            int idx = availableIndices[pick];
            availableIndices.RemoveAt(pick);
            currentOptions.Add(allUpgrades[idx].type);

            optionNameTexts[i].text = allUpgrades[idx].displayName;
            optionDescTexts[i].text = allUpgrades[idx].description;
            optionButtons[i].gameObject.SetActive(true);
        }

        for (int i = choiceCount; i < optionButtons.Length; i++)
            optionButtons[i].gameObject.SetActive(false);

        rerollButtonLabel.text = $"REROLL ({GetRerollHpCost()} HP)";
        rerollButton.interactable = playerHealth.CurrentHp > GetRerollHpCost();
    }

    private void OnUpgradeClicked(int index)
    {
        if (index < 0 || index >= currentOptions.Count) return;
        onChosen?.Invoke(currentOptions[index]);
        Destroy(gameObject);
    }

    private void OnRerollClicked()
    {
        int cost = GetRerollHpCost();
        if (playerHealth.CurrentHp <= cost) return;

        playerHealth.TakeDamage(cost);
        rerollCount++;
        ShowOptions();
    }

    private int GetRerollHpCost()
    {
        return config.RerollBaseHpCost * (rerollCount + 1);
    }
}
