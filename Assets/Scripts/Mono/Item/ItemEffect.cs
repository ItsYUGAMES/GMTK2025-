using UnityEngine;

/// <summary>
/// 道具效果基类 - 所有道具都继承这个基础类
/// </summary>
public abstract class ItemEffect : ScriptableObject
{
    [Header("道具基本信息")]
    public string itemName = "默认道具";
    public string itemDescription = "默认描述";
    public Sprite itemIcon;
    public int itemPrice = 10;
    public ItemType itemType;
    [Header("道具属性")]
   
    public bool isPermanent = true;     // 是否为永久效果

    /// <summary>
    /// 购买时会执行的效果
    /// </summary>
    public virtual void OnPurchase()
    {
        ShopManager shopManager = FindObjectOfType<ShopManager>();
        if (shopManager != null)
        {
            shopManager.CloseShop();
            Debug.Log("购买完成，商店已关闭");
        }
        else
        {
            Debug.LogWarning("未找到ShopManager，无法关闭商店");
        }
    }

    /// <summary>
    /// 游戏开始时执行的效果（可选）
    /// </summary>
    public virtual void OnGameStart() { }

    /// <summary>
    /// 每回合开始时执行的效果（可选）
    /// </summary>
    public virtual void OnRoundStart() { }

    /// <summary>
    /// 每回合结束时执行的效果（可选）
    /// </summary>
    public virtual void OnRoundEnd() { }

    /// <summary>
    /// 检查道具是否可购买（可重写添加额外的购买条件）
    /// </summary>
    /// <returns>返回道具是否可购买</returns>
    public virtual bool CanPurchase()
    {
        // 检查金币是否足够
        if (PlayerDataManager.Instance != null)
        {
            return PlayerDataManager.Instance.GetPlayerGold() >= itemPrice;
        }
        return false;
    }

    /// <summary>
    /// 获取道具的详细描述（可包含当前效果值等动态信息）
    /// </summary>
    /// <returns>返回详细描述文本</returns>
    public virtual string GetDetailedDescription()
    {
        return itemDescription;
    }

    /// <summary>
    /// 检查效果是否激活
    /// </summary>
    /// <returns>返回道具是否处于激活状态</returns>
    public virtual bool IsActive()
    {
        return true;
    }

    /// <summary>
    /// 重置道具效果（通常在游戏开始时）
    /// </summary>
    public virtual void ResetEffect()
    {
        // 子类可以重写此方法来重置特定效果
    }
}