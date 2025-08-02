using UnityEngine;

/// <summary>
/// 完成获得额外打赏道具 (5金币)
/// </summary>
[CreateAssetMenu(fileName = "Extra Reward Item", menuName = "Shop/Items/Extra Reward")]
public class ExtraRewardItem : ItemEffect
{
    [Header("道具效果")]
    public int extraCoins = 5;  // 额外掉落的金币数量

    private void OnEnable()
    {
        itemName = "额外打赏";
        itemDescription = "完成后获得额外金币";
        itemPrice = 5;
        isConsumable = true;  // 每次使用都需要购买
        isPermanent = false;  // 一次性效果
    }

    public override void OnPurchase()
    {
        Debug.Log($"购买了 {itemName}，完成后将获得 {extraCoins} 个额外金币");

        // 查找进度条控制器
        ProgressBarController progressBar = FindObjectOfType<ProgressBarController>();

        if (progressBar != null)
        {
            // 增加掉落的金币数量
            progressBar.numberOfCoins += extraCoins;
            Debug.Log($"金币掉落数量增加到: {progressBar.numberOfCoins}");
        }
        else
        {
            // 保存效果供后续使用
            int currentExtra = PlayerPrefs.GetInt("ExtraRewardCoins", 0);
            PlayerPrefs.SetInt("ExtraRewardCoins", currentExtra + extraCoins);
            PlayerPrefs.Save();
        }
    }

    public override string GetDetailedDescription()
    {
        return $"完成关卡后额外获得 {extraCoins} 个金币";
    }

    public override bool IsActive()
    {
        // 这是一次性道具，购买后立即生效
        return true;
    }
}