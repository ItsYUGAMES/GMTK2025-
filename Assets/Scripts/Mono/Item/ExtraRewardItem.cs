using UnityEngine;

/// <summary>
/// 完成后获得额外奖励道具 (5金币)
/// </summary>
[CreateAssetMenu(fileName = "Extra Reward Item", menuName = "Shop/Items/Extra Reward")]
public class ExtraRewardItem : ItemEffect
{
    [Header("道具效果")]
    public int extraCoins = 5;  // 额外获得的金币数量

    private void OnEnable()
    {
        itemName = "额外奖励";
        itemDescription = "完成后获得额外金币";
        itemPrice = 5;
     
        isPermanent = true;  // 一次性效果
    }

    public override void OnPurchase()
    {
        Debug.Log($"购买了 {itemName}，增加额外奖励");

        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.SetExtraRewardActive(true);
        }
        base.OnPurchase();
    }

    public override string GetDetailedDescription()
    {
        return $"完成关卡额外获得 {extraCoins} 金币";
    }

    public override bool IsActive()
    {
        // 这是一次性道具，购买后立即生效
        return true;
    }
}