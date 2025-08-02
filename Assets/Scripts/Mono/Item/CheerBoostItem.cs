using UnityEngine;

/// <summary>
/// 加速欢呼值填充道具 (5金币)
/// </summary>
[CreateAssetMenu(fileName = "Cheer Boost Item", menuName = "Shop/Items/Cheer Boost")]
public class CheerBoostItem : ItemEffect
{
    [Header("道具效果")]
    [Range(0.5f, 0.9f)]
    public float progressMultiplier = 0.7f;  // 减少填充所需时间倍数（0.7 = 减少30%时间）

    private void OnEnable()
    {
        itemName = "欢呼加速";
        itemDescription = "加速欢呼值填充";
        itemPrice = 5;
    
        isPermanent = true;
    }

    public override void OnPurchase()
    {
        Debug.Log($"购买了 {itemName}，欢呼值填充速度增加");

        // 查找进度条控制器
        ProgressBarController progressBar = FindObjectOfType<ProgressBarController>();

        if (progressBar != null)
        {
            // 减少填充所需时间
            progressBar.fillDuration *= progressMultiplier;
            Debug.Log($"进度条填充时间调整为: {progressBar.fillDuration} 秒");

            // 如果进度条正在填充，重新开始应用新速度
            progressBar.StopFilling();
            progressBar.StartFilling();
        }
        else
        {
            // 保存效果供后续使用
            PlayerPrefs.SetFloat("CheerBoostMultiplier", progressMultiplier);
            PlayerPrefs.Save();
        }
    }

    public override string GetDetailedDescription()
    {
        int speedIncrease = Mathf.RoundToInt((1f / progressMultiplier - 1f) * 100f);
        return $"欢呼值填充速度增加 {speedIncrease}%";
    }
}