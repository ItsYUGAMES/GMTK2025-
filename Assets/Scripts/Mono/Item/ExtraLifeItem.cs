using UnityEngine;

/// <summary>
/// 购买一次失败机会道具 (5金币)
/// </summary>
[CreateAssetMenu(fileName = "Extra Life Item", menuName = "Shop/Items/Extra Life")]
public class ExtraLifeItem : ItemEffect
{
    [Header("道具效果")]
    public int extraLives = 1;

    private void OnEnable()
    {
        itemName = "购买一次失败机会";
        itemDescription = "增加1次失败机会";
        itemPrice = 5;
    
        isPermanent = true;
    }

    public override void OnPurchase()
    {
        Debug.Log($"购买了 {itemName}，增加了 {extraLives} 次失败机会");

        RhythmKeyControllerBase[] controllers = FindObjectsOfType<RhythmKeyControllerBase>();

        if (controllers.Length == 0)
        {
            SaveEffectForLater();
        }
        else
        {
            foreach (var controller in controllers)
            {
                controller.AddExtraLife(extraLives);
            }
        }
    }

    private void SaveEffectForLater()
    {
        int currentPending = PlayerPrefs.GetInt("PendingExtraLives", 0);
        PlayerPrefs.SetInt("PendingExtraLives", currentPending + extraLives);
        PlayerPrefs.Save();
        Debug.Log($"保存了 {extraLives} 个待应用的额外生命");
    }
}