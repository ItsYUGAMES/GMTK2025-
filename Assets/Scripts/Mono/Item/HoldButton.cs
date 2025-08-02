using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
[CreateAssetMenu(fileName = "HoldButton", menuName = "Shop/Items/HoldButton")]
public class HoldButton : ItemEffect
{
    public override void OnPurchase()
    {
        Debug.Log($"购买了 {itemName}，启用长按模式");
        
        // 查找名为 RightClick 的控制器并启用长按模式
        var controllers = FindObjectsOfType<RhythmKeyControllerBase>();
        foreach (var controller in controllers)
        {
            if (controller.gameObject.name == "RightClick")
            {
                controller.EnableHoldMode(); // 时间参数在这里不重要
                break;
            }
        }
        
        base.OnPurchase();
    }

    public override string GetDetailedDescription()
    {
        return "启用长按模式，按住按键即可完成判定";
    }
}

