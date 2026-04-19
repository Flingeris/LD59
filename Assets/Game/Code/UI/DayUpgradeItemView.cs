using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DayUpgradeItemData
{
    public string UpgradeId;
    public string NameText;
    public string PriceText;
    public string EffectText;
    public bool CanBuy;
}

[DisallowMultipleComponent]
public class DayUpgradeItemView : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text effectText;
    [SerializeField] private Button buyButton;

    public event Action<string> BuyRequested;

    private bool isInitialized;
    private string currentUpgradeId;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void OnDestroy()
    {
        if (!isInitialized || buyButton == null)
        {
            return;
        }

        buyButton.onClick.RemoveListener(HandleBuyPressed);
        isInitialized = false;
    }

    public void Bind(DayUpgradeItemData itemData)
    {
        EnsureInitialized();
        currentUpgradeId = itemData?.UpgradeId ?? string.Empty;

        if (nameText != null)
        {
            nameText.text = itemData?.NameText ?? string.Empty;
        }

        if (priceText != null)
        {
            priceText.text = itemData?.PriceText ?? string.Empty;
        }

        if (effectText != null)
        {
            effectText.text = itemData?.EffectText ?? string.Empty;
        }

        if (buyButton != null)
        {
            buyButton.interactable = itemData != null
                && itemData.CanBuy
                && !string.IsNullOrWhiteSpace(itemData.UpgradeId);
        }
    }

    private void EnsureInitialized()
    {
        if (isInitialized || buyButton == null)
        {
            return;
        }

        buyButton.onClick.AddListener(HandleBuyPressed);
        isInitialized = true;
    }

    private void HandleBuyPressed()
    {
        if (string.IsNullOrWhiteSpace(currentUpgradeId))
        {
            return;
        }

        BuyRequested?.Invoke(currentUpgradeId);
    }
}
