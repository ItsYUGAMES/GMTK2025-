using UnityEngine;


/// <summary>
/// 额外生命道具
/// </summary>
[CreateAssetMenu(fileName = "Extra Life Item", menuName = "Shop/Items/Extra Life")]
public class ExtraLifeItem : ItemEffect
{
    [Header("道具效果")]
    public int extraLives = 1;  // 增加的生命数量

    private void OnEnable()
    {
        itemName = "额外生命";
        itemDescription = "增加一条生命";
        itemPrice = 10;

        isPermanent = true;
    }

    public override void OnPurchase()
    {
        Debug.Log($"购买了 {itemName}，增加 {extraLives} 条生命");

        // 通过PlayerDataManager设置效果状态
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.SetExtraLifeActive(true);
        }
    }

    public override string GetDetailedDescription()
    {
        return $"增加 {extraLives} 条额外生命";
    }
}