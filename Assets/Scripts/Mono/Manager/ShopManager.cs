using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using TMPro;

public class ShopManager : MonoBehaviour
{
    [Header("商店UI设置")]
    public GameObject shopPanel;
    public ShopItemUI shopItemUI1;
    public ShopItemUI shopItemUI2;
    public ShopItemUI shopItemUI3;

    [Header("商品数据")]
    public ItemEffect[] itemEffects; // 在Inspector中设置ItemEffect资源
    private List<ShopItem> allAvailableItems = new List<ShopItem>();
    private List<ShopItem> currentShopItems = new List<ShopItem>();

    [SerializeField] private int itemsToShow = 3;

    void Start()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }
        InitializeShopItems();
        BindBuyButtonEvents();
    }

    void InitializeShopItems()
    {
        allAvailableItems.Clear();
        foreach (var effect in itemEffects)
        {
            if (effect != null)
            {
                var shopItem = new ShopItem();
                shopItem.InitializeFromItemEffect(effect);
                allAvailableItems.Add(shopItem);
            }
        }
        Debug.Log($"已初始化 {allAvailableItems.Count} 个商品");
    }

    private void RefreshShopItems()
    {
        currentShopItems.Clear();
        Debug.Log("开始刷新商店道具...");
        Debug.Log($"总共有 {allAvailableItems.Count} 个可用道具");

        // 过滤出未购买的道具
        List<ShopItem> availableItems = new List<ShopItem>();
        foreach (var item in allAvailableItems)
        {
            bool isPurchased = IsItemPurchased(item.itemType);
            string status = isPurchased ? "已购买" : "未购买";
            Debug.Log($"道具: {item.itemName}, 类型: {item.itemType}, 状态: {status}");

            if (!isPurchased)
            {
                availableItems.Add(item);
            }
        }

        Debug.Log($"过滤后可用的未购买道具数量: {availableItems.Count}");

        // 如果没有可用道具，直接返回
        if (availableItems.Count == 0)
        {
            Debug.Log("没有可用的未购买道具，商店将为空");
            return;
        }

        // 随机选择不重复的道具
        int itemCount = Mathf.Min(itemsToShow, availableItems.Count);
        HashSet<int> selectedIndices = new HashSet<int>();

        while (selectedIndices.Count < itemCount)
        {
            int randomIndex = Random.Range(0, availableItems.Count);
            if (selectedIndices.Add(randomIndex)) // HashSet.Add 如果添加成功返回 true
            {
                currentShopItems.Add(availableItems[randomIndex]);
                Debug.Log($"添加商品: {availableItems[randomIndex].itemName}");
            }
        }

        Debug.Log($"刷新商店完成，当前可购买商品数量：{currentShopItems.Count}");
    }

    private bool IsItemPurchased(ItemType itemType)
    {
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogWarning("PlayerDataManager 实例未找到，无法检查道具购买状态");
            return false;
        }

        bool isPurchased = false;

        switch (itemType)
        {
            case ItemType.CheerBoost:
                isPurchased = PlayerDataManager.Instance.cheerBoostActive;
                break;
            case ItemType.ExtraReward:
                isPurchased = PlayerDataManager.Instance.extraRewardActive;
                break;
            case ItemType.ExtraLife:
                isPurchased = PlayerDataManager.Instance.extraLifeActive;
                break;
            case ItemType.AutoPlay:
                isPurchased = PlayerDataManager.Instance.autoPlayActive;
                break;
            case ItemType.HoldButton:
                isPurchased = PlayerDataManager.Instance.holdModeActive;
                break;
            default:
                isPurchased = false;
                break;
        }

        Debug.Log($"检查道具类型 {itemType} 的购买状态: {isPurchased}");
        return isPurchased;
    }

    [ContextMenu("Open Shop")]
    public void OpenShop()
    {
        Time.timeScale = 0f;
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
            RefreshShopItems();
            PopulateShopItems();
            Debug.Log("商店已打开，商品已随机刷新");
        }
    }

    void OnBuyButtonClicked(int itemIndex)
    {
        if (itemIndex >= 0 && itemIndex < currentShopItems.Count)
        {
            ShopItem itemToBuy = currentShopItems[itemIndex];

            if (!PlayerDataManager.Instance.DeductPlayerGold(itemToBuy.itemPrice))
            {
                Debug.LogWarning($"金币不足，无法购买：{itemToBuy.itemName}");
                return;
            }

            // 激活道具效果
            switch (itemToBuy.itemType)
            {
                case ItemType.CheerBoost:
                    PlayerDataManager.Instance.SetCheerBoostActive(true);
                    break;
                case ItemType.ExtraReward:
                    PlayerDataManager.Instance.SetExtraRewardActive(true);
                    break;
                case ItemType.ExtraLife:
                    PlayerDataManager.Instance.SetExtraLifeActive(true);
                    break;
                case ItemType.AutoPlay:
                    PlayerDataManager.Instance.SetAutoPlayActive(true);
                    break;
                case ItemType.HoldButton:
                    PlayerDataManager.Instance.SetHoldModeActive(true);
                    break;
            }

            // 执行道具效果
            if (itemToBuy.itemEffect != null)
            {
                itemToBuy.itemEffect.OnPurchase();
            }

            Debug.Log($"成功购买了：{itemToBuy.itemName}");

            // 立即刷新商店显示
            RefreshShopItems();
            PopulateShopItems();

            // 如果没有可购买的商品了，关闭商店
            if (currentShopItems.Count == 0)
            {
                CloseShop();
            }
        }
    }
    [ContextMenu("Test Close")]
    public void TestClose()
    {
        Time.timeScale = 1f;
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
            Debug.Log("商店已关闭");
        }
    }
    public void CloseShop()
    {
        Time.timeScale = 1f;
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
            Debug.Log("商店已关闭");
        }
        GameManager.Instance.LoadNextLevel();
    }
    void PopulateShopItems()
    {
        if (shopItemUI1 != null)
        {
            shopItemUI1.enabled = true; // 启用组件
            shopItemUI1.gameObject.SetActive(currentShopItems.Count > 0);
            if (currentShopItems.Count > 0)
            {
                shopItemUI1.SetItem(currentShopItems[0]);
            }
        }

        if (shopItemUI2 != null)
        {
            shopItemUI2.enabled = true; // 启用组件
            shopItemUI2.gameObject.SetActive(currentShopItems.Count > 1);
            if (currentShopItems.Count > 1)
            {
                shopItemUI2.SetItem(currentShopItems[1]);
            }
        }

        if (shopItemUI3 != null)
        {
            shopItemUI3.enabled = true; // 启用组件
            shopItemUI3.gameObject.SetActive(currentShopItems.Count > 2);
            if (currentShopItems.Count > 2)
            {
                shopItemUI3.SetItem(currentShopItems[2]);
            }
        }
    }

    void BindBuyButtonEvents()
    {
        if (shopItemUI1?.buyButton != null)
            shopItemUI1.buyButton.onClick.AddListener(() => OnBuyButtonClicked(0));

        if (shopItemUI2?.buyButton != null)
            shopItemUI2.buyButton.onClick.AddListener(() => OnBuyButtonClicked(1));

        if (shopItemUI3?.buyButton != null)
            shopItemUI3.buyButton.onClick.AddListener(() => OnBuyButtonClicked(2));
    }
}

public enum ItemType
{
    ExtraLife,
    CheerBoost,
    ExtraReward,
    AutoPlay,
    HoldButton
}