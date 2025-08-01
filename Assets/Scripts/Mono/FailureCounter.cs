using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;

/// <summary>
/// 统计游戏中未暂停时的失败次数
/// </summary>
public class FailureCounter : MonoBehaviour
{
    [Header("UI")]
    
    [SerializeField] private TextMeshProUGUI adFailureCountText;  // AD控制器失败计数
    [SerializeField] private TextMeshProUGUI jlFailureCountText;  // JL控制器失败计数

    private int totalFailureCount = 0;
    
    [Header("节拍控制器")]
    [SerializeField] private List<RhythmKeyControllerBase> rhythmControllers = new List<RhythmKeyControllerBase>();

    // 存储每个控制器的失败次数
    private Dictionary<RhythmKeyControllerBase, int> controllerFailureCounts = new Dictionary<RhythmKeyControllerBase, int>();
    
    // 存储每个控制器上一帧的暂停状态
    private bool[] lastPausedStates;

    void Start()
    {
        // 初始化控制器状态
        lastPausedStates = new bool[rhythmControllers.Count];
        
        for (int i = 0; i < rhythmControllers.Count; i++)
        {
            var controller = rhythmControllers[i];
            if (controller != null)
            {
                controllerFailureCounts[controller] = 0;
                lastPausedStates[i] = controller.isPaused; // 记录初始状态
                Debug.Log($"[FailureCounter] 初始化控制器: {controller.keyConfigPrefix}");
            }
        }

        UpdateDisplay();
    }

    private void Update()
    {
        for (int i = 0; i < rhythmControllers.Count; i++)
        {
            var controller = rhythmControllers[i];
            if (controller != null)
            {
                bool currentPaused = controller.isPaused;
                bool lastPaused = lastPausedStates[i];

                // 检测从未暂停变为暂停（即发生失败）
                if (!lastPaused && currentPaused)
                {
                    CountingFailure(controller);
                }

                // 更新状态
                lastPausedStates[i] = currentPaused;
            }
        }
    }

    void CountingFailure(RhythmKeyControllerBase controller)
    {
        // 增加该控制器的失败计数
        controllerFailureCounts[controller]++;
        totalFailureCount++;

        Debug.Log($"[FailureCounter] {controller.keyConfigPrefix} 失败！当前失败次数: {controllerFailureCounts[controller]}");
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        // 更新总失败次数
       

        // 更新各控制器的失败次数
        for (int i = 0; i < rhythmControllers.Count; i++)
        {
            var controller = rhythmControllers[i];
            if (controller == null) continue;

            int failureCount = controllerFailureCounts[controller];

            // 根据控制器类型更新对应的UI
            if (controller.keyConfigPrefix == "AD" && adFailureCountText != null)
            {
                adFailureCountText.text = $"AD: {failureCount}";
            }
            else if (controller.keyConfigPrefix == "JL" && jlFailureCountText != null)
            {
                jlFailureCountText.text = $"JL: {failureCount}";
            }
        }
    }

    public void ResetCount()
    {
        totalFailureCount = 0;

        // 重置每个控制器的失败计数
        for (int i = 0; i < rhythmControllers.Count; i++)
        {
            var controller = rhythmControllers[i];
            if (controller != null)
            {
                controllerFailureCounts[controller] = 0;
            }
        }

        UpdateDisplay();
        Debug.Log("[FailureCounter] 重置所有失败计数");
    }

    /// <summary>
    /// 获取总失败次数
    /// </summary>
    public int GetFailureCount()
    {
        return totalFailureCount;
    }

    /// <summary>
    /// 获取指定控制器的失败次数
    /// </summary>
    public int GetControllerFailureCount(RhythmKeyControllerBase controller)
    {
        return controllerFailureCounts.ContainsKey(controller) ? controllerFailureCounts[controller] : 0;
    }

    /// <summary>
    /// 获取AD控制器失败次数
    /// </summary>
    public int GetADFailureCount()
    {
        foreach (var controller in rhythmControllers)
        {
            if (controller != null && controller.keyConfigPrefix == "AD")
            {
                return controllerFailureCounts[controller];
            }
        }
        return 0;
    }

    /// <summary>
    /// 获取JL控制器失败次数
    /// </summary>
    public int GetJLFailureCount()
    {
        foreach (var controller in rhythmControllers)
        {
            if (controller != null && controller.keyConfigPrefix == "JL")
            {
                return controllerFailureCounts[controller];
            }
        }
        return 0;
    }
}