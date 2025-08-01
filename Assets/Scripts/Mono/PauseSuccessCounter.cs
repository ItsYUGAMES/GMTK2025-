using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 统计暂停后连续成功的次数
/// </summary>
public class PauseSuccessCounter : MonoBehaviour
{
    [Header("UI显示")]
    [SerializeField] private Text successCountText;
    [SerializeField] private string displayPrefix = "连续成功: ";
    [SerializeField] private bool showMaxRequired = true; // 是否显示需要的最大次数

    [Header("控制器引用")]
    [SerializeField] private List<RhythmKeyControllerBase> rhythmControllers = new List<RhythmKeyControllerBase>();

    private RhythmKeyControllerBase currentPausedController = null;
    private int currentSuccessCount = 0;
    private int requiredSuccessCount = 0;

    void Start()
    {
        UpdateDisplay();
    }

    void Update()
    {
        // 查找当前处于暂停状态的控制器
        RhythmKeyControllerBase pausedController = FindPausedController();

        if (pausedController != null)
        {
            // 如果是新的暂停控制器，重置计数
            if (currentPausedController != pausedController)
            {
                currentPausedController = pausedController;
                currentSuccessCount = 0;
                requiredSuccessCount = GetRequiredSuccessCount(pausedController);
            }

            // 获取当前的连续成功次数
            int consecutiveSuccess = GetConsecutiveSuccessCount(pausedController);

            // 更新计数
            if (consecutiveSuccess != currentSuccessCount)
            {
                currentSuccessCount = consecutiveSuccess;
                UpdateDisplay();

                Debug.Log($"[PauseSuccessCounter] {pausedController.keyConfigPrefix} 连续成功: {currentSuccessCount}/{requiredSuccessCount}");

                // 检查是否即将恢复
                if (currentSuccessCount >= requiredSuccessCount)
                {
                    Debug.Log($"[PauseSuccessCounter] {pausedController.keyConfigPrefix} 即将恢复游戏！");
                }
            }
        }
        else
        {
            // 没有控制器处于暂停状态
            if (currentPausedController != null)
            {
                currentPausedController = null;
                currentSuccessCount = 0;
                UpdateDisplay();
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
            if (controller == null) continue;

            if (IsControllerPaused(controller))
            {
                return controller;
            }
        }
        return null;
    }

    /// <summary>
    /// 检查控制器是否处于暂停状态
    /// </summary>
    private bool IsControllerPaused(RhythmKeyControllerBase controller)
    {
        var fieldInfo = typeof(RhythmKeyControllerBase).GetField("isInPauseForFailure",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (fieldInfo != null)
        {
            return (bool)fieldInfo.GetValue(controller);
        }

        return false;
    }

    /// <summary>
    /// 获取控制器的连续成功次数
    /// </summary>
    private int GetConsecutiveSuccessCount(RhythmKeyControllerBase controller)
    {
        var fieldInfo = typeof(RhythmKeyControllerBase).GetField("consecutiveSuccessOnFailedKey",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (fieldInfo != null)
        {
            return (int)fieldInfo.GetValue(controller);
        }

        return 0;
    }

    /// <summary>
    /// 获取恢复所需的成功次数
    /// </summary>
    private int GetRequiredSuccessCount(RhythmKeyControllerBase controller)
    {
        // needConsecutiveSuccessToResume 是公开字段
        return controller.needConsecutiveSuccessToResume;
    }

    /// <summary>
    /// 更新UI显示
    /// </summary>
    private void UpdateDisplay()
    {
        if (successCountText != null)
        {
            if (currentPausedController != null && showMaxRequired)
            {
                successCountText.text = $"{displayPrefix}{currentSuccessCount}/{requiredSuccessCount}";
            }
            else if (currentPausedController != null)
            {
                successCountText.text = displayPrefix + currentSuccessCount;
            }
            else
            {
                successCountText.text = displayPrefix + "0";
            }
        }
    }

    /// <summary>
    /// 获取当前连续成功次数
    /// </summary>
    public int GetCurrentSuccessCount()
    {
        return currentSuccessCount;
    }

    /// <summary>
    /// 获取当前暂停的控制器
    /// </summary>
    public RhythmKeyControllerBase GetCurrentPausedController()
    {
        return currentPausedController;
    }

    /// <summary>
    /// 手动添加控制器
    /// </summary>
    public void AddController(RhythmKeyControllerBase controller)
    {
        if (controller != null && !rhythmControllers.Contains(controller))
        {
            rhythmControllers.Add(controller);
        }
    }

    /// <summary>
    /// 移除控制器
    /// </summary>
    public void RemoveController(RhythmKeyControllerBase controller)
    {
        if (controller != null && rhythmControllers.Contains(controller))
        {
            rhythmControllers.Remove(controller);
        }
    }
}