using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 统计暂停状态下连续成功的次数
/// </summary>
public class PauseSuccessCounter : MonoBehaviour
{
    [Header("UI显示")]
    [SerializeField] private TextMeshProUGUI successCountText;

    [Header("节拍控制器")]
    [SerializeField] private List<RhythmKeyControllerBase> rhythmControllers = new List<RhythmKeyControllerBase>();

    // 存储每个控制器的成功次数
    private Dictionary<RhythmKeyControllerBase, int> controllerSuccessCounts = new Dictionary<RhythmKeyControllerBase, int>();
    
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
                controllerSuccessCounts[controller] = 0;
                lastPausedStates[i] = IsControllerPaused(controller); // 记录初始状态
                Debug.Log($"[PauseSuccessCounter] 初始化控制器: {controller.keyConfigPrefix}");
            }
        }

        UpdateDisplay();
    }

    void Update()
    {
        for (int i = 0; i < rhythmControllers.Count; i++)
        {
            var controller = rhythmControllers[i];
            if (controller != null)
            {
                bool currentPaused = IsControllerPaused(controller);
                bool lastPaused = lastPausedStates[i];

                // 如果控制器处于暂停状态，更新成功计数
                if (currentPaused)
                {
                    int currentSuccess = GetConsecutiveSuccessCount(controller);
                    if (currentSuccess != controllerSuccessCounts[controller])
                    {
                        controllerSuccessCounts[controller] = currentSuccess;
                        UpdateDisplay();
                        Debug.Log($"[PauseSuccessCounter] {controller.keyConfigPrefix} 暂停成功次数: {currentSuccess}");
                    }
                }
                else
                {
                    // 如果从暂停状态恢复，重置计数
                    if (lastPaused && !currentPaused)
                    {
                        controllerSuccessCounts[controller] = 0;
                        UpdateDisplay();
                        Debug.Log($"[PauseSuccessCounter] {controller.keyConfigPrefix} 恢复，重置计数");
                    }
                }

                // 更新状态
                lastPausedStates[i] = currentPaused;
            }
        }
    }

    /// <summary>
    /// 检测控制器是否处于暂停状态
    /// </summary>
    private bool IsControllerPaused(RhythmKeyControllerBase controller)
    {
        return controller.isPaused;
    }

    /// <summary>
    /// 获取控制器的连续成功次数
    /// </summary>
    private int GetConsecutiveSuccessCount(RhythmKeyControllerBase controller)
    {
        return controller.consecutiveSuccessOnFailedKey;
    }

    /// <summary>
    /// 获取需要的成功次数
    /// </summary>
    private int GetRequiredSuccessCount(RhythmKeyControllerBase controller)
    {
        return controller.needConsecutiveSuccessToResume;
    }

    private void UpdateDisplay()
    {
        // 查找当前暂停的控制器
        RhythmKeyControllerBase pausedController = FindPausedController();
        
        if (successCountText != null)
        {
            if (pausedController != null)
            {
                int currentSuccess = controllerSuccessCounts[pausedController];
                int requiredSuccess = GetRequiredSuccessCount(pausedController);
                successCountText.text = $"{currentSuccess}/{requiredSuccess}";
            }
            else
            {
                successCountText.text = "0/4"; // 默认显示
            }
        }
    }

    /// <summary>
    /// 查找当前处于暂停状态的控制器
    /// </summary>
    private RhythmKeyControllerBase FindPausedController()
    {
        foreach (var controller in rhythmControllers)
        {
            if (controller != null && IsControllerPaused(controller))
            {
                return controller;
            }
        }
        return null;
    }

    /// <summary>
    /// 添加控制器到监控列表
    /// </summary>
    public void AddController(RhythmKeyControllerBase controller)
    {
        if (controller != null && !rhythmControllers.Contains(controller))
        {
            rhythmControllers.Add(controller);
            controllerSuccessCounts[controller] = 0;
            Debug.Log($"[PauseSuccessCounter] 添加控制器: {controller.keyConfigPrefix}");
        }
    }

    /// <summary>
    /// 从监控列表移除控制器
    /// </summary>
    public void RemoveController(RhythmKeyControllerBase controller)
    {
        if (rhythmControllers.Contains(controller))
        {
            rhythmControllers.Remove(controller);
            controllerSuccessCounts.Remove(controller);
            UpdateDisplay();
            Debug.Log($"[PauseSuccessCounter] 移除控制器: {controller.keyConfigPrefix}");
        }
    }

    /// <summary>
    /// 获取指定控制器的成功次数
    /// </summary>
    public int GetControllerSuccessCount(RhythmKeyControllerBase controller)
    {
        return controllerSuccessCounts.ContainsKey(controller) ? controllerSuccessCounts[controller] : 0;
    }
}