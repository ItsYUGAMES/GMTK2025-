using UnityEngine;
using System.Collections.Generic;
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
    public List<ShopItem> allAvailableItems;
    
    [Header("道具效果列表")]
    public List<ItemEffect> availableItemEffects;

    void Start()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("Shop Panel 未设置！");
        }

        InitializeShopItems();
        BindBuyButtonEvents();
    }

    void InitializeShopItems()
    {
        // 从ItemEffect列表创建ShopItem对象
        allAvailableItems.Clear();
        
        foreach (var itemEffect in availableItemEffects)
        {
            if (itemEffect != null)
            {
                ShopItem shopItem = new ShopItem();
                shopItem.InitializeFromItemEffect(itemEffect);
                allAvailableItems.Add(shopItem);
                Debug.Log($"已初始化商品: {itemEffect.itemName}");
            }
            else
            {
                Debug.LogWarning("发现空的 ItemEffect!");
            }
        }

        Debug.Log($"总共初始化了 {allAvailableItems.Count} 个商品");
    }

    [ContextMenu("打开商店")]
    public void OpenShop()
    {
        Time.timeScale = 0f;
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
            Debug.Log("商店已打开。");
            PopulateShopItems();
        }
    }

    public void CloseShop()
    {
        Time.timeScale = 1f; // 恢复游戏速度
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
            Debug.Log("商店已关闭。");
        }
    }

    void BindBuyButtonEvents()
    {
        if (shopItemUI1 != null && shopItemUI1.buyButton != null)
        {
            shopItemUI1.buyButton.onClick.RemoveAllListeners();
            shopItemUI1.buyButton.onClick.AddListener(() => OnBuyButtonClicked(0));
        }
        
        if (shopItemUI2 != null && shopItemUI2.buyButton != null)
        {
            shopItemUI2.buyButton.onClick.RemoveAllListeners();
            shopItemUI2.buyButton.onClick.AddListener(() => OnBuyButtonClicked(1));
        }
        
        if (shopItemUI3 != null && shopItemUI3.buyButton != null)
        {
            shopItemUI3.buyButton.onClick.RemoveAllListeners();
            shopItemUI3.buyButton.onClick.AddListener(() => OnBuyButtonClicked(2));
        }
    }

    void OnBuyButtonClicked(int itemIndex)
    {
        if (itemIndex >= 0 && itemIndex < allAvailableItems.Count)
        {
            ShopItem itemToBuy = allAvailableItems[itemIndex];
            if (itemToBuy != null && itemToBuy.itemEffect != null)
            {
                // 检查是否可以购买
                if (!itemToBuy.itemEffect.CanPurchase())
                {
                    Debug.LogWarning($"无法购买：{itemToBuy.itemName}");
                    return;
                }

                // 使用PlayerDataManager进行金币扣除
                if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.DeductPlayerGold(itemToBuy.itemPrice))
                {
                    Debug.Log($"成功购买了：{itemToBuy.itemName}");
                    
                    // 执行道具效果
                    itemToBuy.itemEffect.OnPurchase();
                    
                    // 如果是永久性道具，禁用购买按钮
                    if (itemToBuy.itemEffect.isPermanent)
                    {
                        DisableBuyButton(itemIndex);
                    }
                }
                else
                {
                    Debug.LogWarning($"金币不足，无法购买：{itemToBuy.itemName}");
                }
            }
        }
        else
        {
            Debug.LogWarning($"尝试购买的商品索引 {itemIndex} 超出范围。");
        }
    }

    void DisableBuyButton(int itemIndex)
    {
        Button targetButton = null;
        switch(itemIndex)
        {
            case 0: targetButton = shopItemUI1?.buyButton; break;
            case 1: targetButton = shopItemUI2?.buyButton; break;
            case 2: targetButton = shopItemUI3?.buyButton; break;
        }

        if (targetButton != null)
        {
            targetButton.interactable = false;
        }
    }

    void PopulateShopItems()
    {
        if (shopItemUI1 != null)
        {
            if (allAvailableItems.Count > 0)
            {
                shopItemUI1.gameObject.SetActive(true);
                shopItemUI1.SetItem(allAvailableItems[0]);
                shopItemUI1.buyButton.interactable = allAvailableItems[0].itemEffect.CanPurchase();
            }
            else 
            {
                shopItemUI1.gameObject.SetActive(false);
            }
        }
    
        if (shopItemUI2 != null)
        {
            if (allAvailableItems.Count > 1)
            {
                shopItemUI2.gameObject.SetActive(true);
                shopItemUI2.SetItem(allAvailableItems[1]);
                shopItemUI2.buyButton.interactable = allAvailableItems[1].itemEffect.CanPurchase();
            }
            else 
            {
                shopItemUI2.gameObject.SetActive(false);
            }
        }
    
        if (shopItemUI3 != null)
        {
            if (allAvailableItems.Count > 2)
            {
                shopItemUI3.gameObject.SetActive(true);
                shopItemUI3.SetItem(allAvailableItems[2]);
                shopItemUI3.buyButton.interactable = allAvailableItems[2].itemEffect.CanPurchase();
            }
            else 
            {
                shopItemUI3.gameObject.SetActive(false);
            }
        }
    }
}