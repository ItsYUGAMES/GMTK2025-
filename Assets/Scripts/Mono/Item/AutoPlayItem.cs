using UnityEngine;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// 自动播放道具 - 选择选定AD或JL控制器自动播放 (20金币)
/// </summary>
[CreateAssetMenu(fileName = "Auto Play Item", menuName = "Shop/Items/Auto Play")]
public class AutoPlayItem : ItemEffect
{
    [Header("道具效果")]
    [Range(0.8f, 1.0f)]
    public float autoPlayAccuracy = 0.95f;  // 自动播放准确率

    [Header("目标选择")]
    public ControllerType targetController = ControllerType.Auto;

    public enum ControllerType
    {
        Auto,       // 自动选择（基于策略）
        AD,         // 指定AD控制器
        JL,         // 指定JL控制器
        Random      // 随机选择
    }

    [Header("自动选择策略")]
    public AutoSelectStrategy autoSelectStrategy = AutoSelectStrategy.MostFails;

    public enum AutoSelectStrategy
    {
        MostFails,      // 失败次数最多
        LowestSuccess,  // 成功次数最少的
        LowestRatio     // 成功率最低的
    }

    private void OnEnable()
    {
        itemName = "自动播放";
        itemDescription = "让一个角色自动完成按键";
        itemPrice = 20;
        isPermanent = true;
    }

    public override void OnPurchase()
    {
        Debug.Log($"购买了 {itemName}");

        // 获取目标控制器
        RhythmKeyControllerBase selectedController = GetTargetController();

        if (selectedController == null)
        {
            Debug.LogWarning("未找到可用的目标控制器");
            // 退还金币
            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.AddPlayerGold(itemPrice);
            }
            return;
        }

        // 为选中的控制器启用自动模式
        selectedController.EnableAutoPlay(autoPlayAccuracy);

        // 保存每个控制器自动播放状态
        string prefKey = $"AutoPlay_{selectedController.keyConfigPrefix}";
        PlayerPrefs.SetInt(prefKey + "_Enabled", 1);
        PlayerPrefs.SetFloat(prefKey + "_Accuracy", autoPlayAccuracy);
        PlayerPrefs.Save();

        Debug.Log($"已为 {selectedController.keyConfigPrefix} 控制器启用自动播放模式");
    }

    private RhythmKeyControllerBase GetTargetController()
    {
        // 查找所有控制器
        ADController adController = FindObjectOfType<ADController>();
        JLController jlController = FindObjectOfType<JLController>();

        // 创建可用控制器列表
        List<RhythmKeyControllerBase> availableControllers = new List<RhythmKeyControllerBase>();

        // 添加AD控制器
        if (adController != null && !IsAutoPlayEnabled(adController))
        {
            availableControllers.Add(adController);
        }

        // 添加JL控制器
        if (jlController != null && !IsAutoPlayEnabled(jlController))
        {
            availableControllers.Add(jlController);
        }

        if (availableControllers.Count == 0)
        {
            Debug.LogWarning("没有可用的控制器：所有控制器已在自动模式或未找到");
            return null;
        }

        // 根据选择模式返回控制器
        switch (targetController)
        {
            case ControllerType.AD:
                return availableControllers.FirstOrDefault(c => c is ADController);

            case ControllerType.JL:
                return availableControllers.FirstOrDefault(c => c is JLController);

            case ControllerType.Random:
                return availableControllers[Random.Range(0, availableControllers.Count)];

            case ControllerType.Auto:
                return SelectByStrategy(availableControllers);

            default:
                return availableControllers[0];
        }
    }

    private RhythmKeyControllerBase SelectByStrategy(List<RhythmKeyControllerBase> controllers)
    {
        if (controllers.Count == 0) return null;
        if (controllers.Count == 1) return controllers[0];

        switch (autoSelectStrategy)
        {
            case AutoSelectStrategy.MostFails:
                return controllers.OrderByDescending(c => c.GetCurrentFailCount()).First();

            case AutoSelectStrategy.LowestSuccess:
                return controllers.OrderBy(c => c.successCount).First();

            case AutoSelectStrategy.LowestRatio:
                return controllers.OrderBy(c =>
                {
                    float total = c.successCount + c.GetCurrentFailCount();
                    return total > 0 ? c.successCount / total : 0f;
                }).First();

            default:
                return controllers[0];
        }
    }

    private bool IsAutoPlayEnabled(RhythmKeyControllerBase controller)
    {
        string prefKey = $"AutoPlay_{controller.keyConfigPrefix}_Enabled";
        return PlayerPrefs.GetInt(prefKey, 0) == 1;
    }

    public override string GetDetailedDescription()
    {
        string targetDesc = targetController switch
        {
            ControllerType.AD => "AD控制器（A/D键）",
            ControllerType.JL => "JL控制器（J/L键）",
            ControllerType.Random => "随机一个控制器",
            ControllerType.Auto => autoSelectStrategy switch
            {
                AutoSelectStrategy.MostFails => "失败最多的控制器",
                AutoSelectStrategy.LowestSuccess => "成功最少的控制器",
                AutoSelectStrategy.LowestRatio => "成功率最低的控制器",
                _ => "自动选择的控制器"
            },
            _ => "一个控制器"
        };

        return $"让{targetDesc}自动完成按键，准确率 {autoPlayAccuracy * 100}%";
    }

    public override bool CanPurchase()
    {
        if (!base.CanPurchase()) return false;

        // 检查是否有未启用自动模式的控制器
        ADController adController = FindObjectOfType<ADController>();
        JLController jlController = FindObjectOfType<JLController>();

        bool adAvailable = adController != null && !IsAutoPlayEnabled(adController);
        bool jlAvailable = jlController != null && !IsAutoPlayEnabled(jlController);

        return adAvailable || jlAvailable;
    }

    // 获取当前游戏中所有控制器状态（用于UI显示）
    public string GetControllersStatus()
    {
        ADController adController = FindObjectOfType<ADController>();
        JLController jlController = FindObjectOfType<JLController>();

        string status = "控制器状态:\n";

        if (adController != null)
        {
            bool adAuto = IsAutoPlayEnabled(adController);
            status += $"AD (A/D键): {(adAuto ? "自动" : "手动")} - ";
            status += $"成功{adController.successCount}次，失败{adController.GetCurrentFailCount()}次\n";
        }

        if (jlController != null)
        {
            bool jlAuto = IsAutoPlayEnabled(jlController);
            status += $"JL (J/L键): {(jlAuto ? "自动" : "手动")} - ";
            status += $"成功{jlController.successCount}次，失败{jlController.GetCurrentFailCount()}次";
        }

        return status;
    }

    public override void ResetEffect()
    {
        // 清除所有自动播放设置
        string[] prefixes = { "AD", "JL" };
        foreach (string prefix in prefixes)
        {
            string prefKey = $"AutoPlay_{prefix}";
            PlayerPrefs.DeleteKey(prefKey + "_Enabled");
            PlayerPrefs.DeleteKey(prefKey + "_Accuracy");
        }
        PlayerPrefs.Save();
    }
}