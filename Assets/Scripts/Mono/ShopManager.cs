using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro; // 如果使用 TextMeshPro

public class ShopManager : MonoBehaviour
{
    [Header("商店UI设置")]
    public GameObject shopPanel; // 拖拽你的商店主面板到这里

    // 直接引用三个 ShopItemUI 实例
    public ShopItemUI shopItemUI1;
    public ShopItemUI shopItemUI2;
    public ShopItemUI shopItemUI3;

    [Header("商品数据")]
    public List<ShopItem> allAvailableItems; // 所有可能的商品列表
    // 假设你总是想显示前三个商品，或者你可以根据需求调整

    [Header("玩家数据")]
    public TextMeshProUGUI playerGoldText;
    private int playerGold = 500; // 示例玩家金币

    void Start()
    {
        // 确保商店面板初始是隐藏的
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("Shop Panel 未设置！请在 Inspector 中拖拽商店主面板。");
        }

        UpdatePlayerGoldUI(); // 初始化金币显示

        // 绑定购买按钮事件 (在商店初始化时只做一次)
        BindBuyButtonEvents();
    }

    // 打开商店面板
    [ContextMenu("打开商店")]
    public void OpenShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
            Debug.Log("商店已打开。");
            UpdatePlayerGoldUI(); // 每次打开商店时刷新金币显示
            PopulateShopItems(); // 每次打开商店时刷新商品显示
        }
    }

    // 关闭商店面板
    public void CloseShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
            Debug.Log("商店已关闭。");
        }
    }

    // 绑定购买按钮的点击事件
    void BindBuyButtonEvents()
    {
        if (shopItemUI1 != null && shopItemUI1.buyButton != null)
        {
            shopItemUI1.buyButton.onClick.RemoveAllListeners();
            // 使用 Lambda 表达式捕获索引 0 的商品
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

    // 根据索引处理购买点击
    void OnBuyButtonClicked(int itemIndex)
    {
        // 确保索引在商品列表范围内
        if (itemIndex >= 0 && itemIndex < allAvailableItems.Count)
        {
            ShopItem itemToBuy = allAvailableItems[itemIndex];
            if (itemToBuy != null)
            {
                if (DeductPlayerGold(itemToBuy.itemPrice))
                {
                    Debug.Log($"成功购买了：{itemToBuy.itemName}");
                    // TODO: 实际添加到玩家背包或解锁功能
                    // 购买成功后，可以禁用对应的购买按钮或改变其文本
                    DisableBuyButton(itemIndex);
                }
                else
                {
                    Debug.LogWarning($"金币不足，无法购买：{itemToBuy.itemName}");
                    // TODO: 播放金币不足音效或显示提示
                }
            }
        }
        else
        {
            Debug.LogWarning($"尝试购买的商品索引 {itemIndex} 超出范围。");
        }
    }

    // 禁用购买按钮的辅助方法
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
            // 可选：改变按钮文本
            // if (targetButton.GetComponentInChildren<TextMeshProUGUI>() != null)
            // {
            //     targetButton.GetComponentInChildren<TextMeshProUGUI>().text = "已购买";
            // }
        }
    }


    // 将商品数据显示到预设的 ShopItemUI 实例上
    void PopulateShopItems()
    {
        // 只显示前三个商品，如果 availableItems 不足三个，则显示空或不显示
        if (shopItemUI1 != null)
        {
            if (allAvailableItems.Count > 0) shopItemUI1.SetItem(allAvailableItems[0]);
            else shopItemUI1.gameObject.SetActive(false); // 隐藏没有商品的UI
        }
        if (shopItemUI2 != null)
        {
            if (allAvailableItems.Count > 1) shopItemUI2.SetItem(allAvailableItems[1]);
            else shopItemUI2.gameObject.SetActive(false);
        }
        if (shopItemUI3 != null)
        {
            if (allAvailableItems.Count > 2) shopItemUI3.SetItem(allAvailableItems[2]);
            else shopItemUI3.gameObject.SetActive(false);
        }

        // 确保按钮重新可用（如果之前被禁用过）
        if (shopItemUI1 != null && shopItemUI1.buyButton != null) shopItemUI1.buyButton.interactable = true;
        if (shopItemUI2 != null && shopItemUI2.buyButton != null) shopItemUI2.buyButton.interactable = true;
        if (shopItemUI3 != null && shopItemUI3.buyButton != null) shopItemUI3.buyButton.interactable = true;
    }

    // 获取玩家金币
    public int GetPlayerGold() { return playerGold; }

    // 扣除玩家金币
    public bool DeductPlayerGold(int amount)
    {
        if (playerGold >= amount)
        {
            playerGold -= amount;
            UpdatePlayerGoldUI();
            return true;
        }
        return false;
    }

    // 更新金币UI显示
    void UpdatePlayerGoldUI()
    {
        if (playerGoldText != null)
        {
            playerGoldText.text = $"金币: {playerGold}";
        }
    }
}