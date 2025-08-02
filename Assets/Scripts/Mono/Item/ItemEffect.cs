using UnityEngine;

/// <summary>
/// 道具效果基类 - 所有道具都继承自这个抽象类
/// </summary>
public abstract class ItemEffect : ScriptableObject
{
    [Header("道具基本信息")]
    public string itemName = "道具名称";
    public string itemDescription = "道具描述";
    public Sprite itemIcon;
    public int itemPrice = 10;

    [Header("道具设置")]
    public bool isConsumable = false;  // 是否为消耗品（可以多次购买）
    public bool isPermanent = true;     // 是否为永久效果

    /// <summary>
    /// 购买时立即执行的效果
    /// </summary>
    public abstract void OnPurchase();

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
    /// 检查道具是否可以购买（可以添加额外的购买条件）
    /// </summary>
    /// <returns>返回是否可以购买</returns>
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
    /// 获取道具的详细描述（可以包含当前效果值等动态信息）
    /// </summary>
    /// <returns>返回详细描述文本</returns>
    public virtual string GetDetailedDescription()
    {
        return itemDescription;
    }

    /// <summary>
    /// 道具效果是否激活
    /// </summary>
    /// <returns>返回道具是否处于激活状态</returns>
    public virtual bool IsActive()
    {
        return true;
    }

    /// <summary>
    /// 重置道具效果（用于新游戏开始时）
    /// </summary>
    public virtual void ResetEffect()
    {
        // 子类可以重写此方法来重置特定的效果
    }
}