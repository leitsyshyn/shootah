using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class ProgressionShopItemView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Text nameText;
    [SerializeField] private Text costText;
    [SerializeField] private Text stateText;
    [SerializeField] private Button actionButton;
    [SerializeField] private Text actionButtonText;

    [Header("State Colors")]
    [SerializeField] private Color availableColor = new(0.2f, 0.58f, 0.3f, 1f);
    [SerializeField] private Color unavailableColor = new(0.72f, 0.3f, 0.24f, 1f);
    [SerializeField] private Color completeColor = new(0.16f, 0.42f, 0.66f, 1f);

    private UnityAction cachedAction;

    public void Bind(string itemName, string costLabel, string stateLabel, string buttonLabel, bool buttonInteractable, ShopItemVisualState visualState, UnityAction purchaseAction)
    {
        if (nameText != null)
        {
            nameText.text = itemName;
        }

        if (costText != null)
        {
            costText.text = costLabel;
        }

        if (stateText != null)
        {
            stateText.text = stateLabel;
            stateText.color = GetStateColor(visualState);
        }

        if (actionButtonText != null)
        {
            actionButtonText.text = buttonLabel;
        }

        if (actionButton == null)
        {
            return;
        }

        if (cachedAction != null)
        {
            actionButton.onClick.RemoveListener(cachedAction);
            cachedAction = null;
        }

        actionButton.interactable = buttonInteractable;
        if (purchaseAction != null)
        {
            cachedAction = purchaseAction;
            actionButton.onClick.AddListener(cachedAction);
        }
    }

    private Color GetStateColor(ShopItemVisualState visualState)
    {
        return visualState switch
        {
            ShopItemVisualState.Available => availableColor,
            ShopItemVisualState.Complete => completeColor,
            _ => unavailableColor
        };
    }

    public enum ShopItemVisualState
    {
        Unavailable = 0,
        Available = 1,
        Complete = 2
    }
}
