using UnityEngine;

/// <summary>
/// 加速欢呼值（亢奋观众）道具 (5金币)
/// </summary>
[CreateAssetMenu(fileName = "Cheer Boost Item", menuName = "Shop/Items/Cheer Boost")]
public class CheerBoostItem : ItemEffect
{
    [Header("道具效果")]
    [Range(0.5f, 0.9f)]
    public float progressMultiplier = 0.7f;  // 进度条填充速度倍数（0.7 = 减少30%时间）

    private void OnEnable()
    {
        itemName = "亢奋观众";
        itemDescription = "加速欢呼值积累";
        itemPrice = 5;
        isConsumable = false;
        isPermanent = true;
    }

    public override void OnPurchase()
    {
        Debug.Log($"购买了 {itemName}，欢呼值积累速度提升！");

        // 查找进度条控制器
        ProgressBarController progressBar = FindObjectOfType<ProgressBarController>();

        if (progressBar != null)
        {
            // 减少填充所需时间
            progressBar.fillDuration *= progressMultiplier;
            Debug.Log($"进度条填充时间缩短为: {progressBar.fillDuration} 秒");

            // 如果进度条正在填充，重新开始以应用新速度
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
        return $"欢呼值积累速度提升 {speedIncrease}%";
    }
}