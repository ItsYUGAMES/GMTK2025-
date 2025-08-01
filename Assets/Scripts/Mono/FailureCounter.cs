using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 统计游戏中未暂停时的失败次数
/// </summary>
public class FailureCounter : MonoBehaviour
{
    [Header("UI显示")]
    [SerializeField] private Text failureCountText;
    [SerializeField] private string displayPrefix = "失败次数: ";

    [Header("控制器引用")]
    [SerializeField] private List<RhythmKeyControllerBase> rhythmControllers = new List<RhythmKeyControllerBase>();

    private int totalFailureCount = 0;
    private Dictionary<RhythmKeyControllerBase, bool> controllerPauseStates = new Dictionary<RhythmKeyControllerBase, bool>();

    void Start()
    {
        // 初始化控制器状态
        foreach (var controller in rhythmControllers)
        {
            if (controller != null)
            {
                controllerPauseStates[controller] = false;
            }
        }

        UpdateDisplay();
    }

    void Update()
    {
        // 监听每个控制器的失败和暂停状态
        foreach (var controller in rhythmControllers)
        {
            if (controller == null) continue;

            // 获取当前控制器的暂停状态
            bool currentlyPaused = IsControllerPaused(controller);

            // 检查是否从非暂停状态变为暂停状态（表示发生了失败）
            if (!controllerPauseStates[controller] && currentlyPaused)
            {
                // 失败发生，增加计数
                totalFailureCount++;
                UpdateDisplay();
                Debug.Log($"[FailureCounter] {controller.keyConfigPrefix} 失败，总失败次数: {totalFailureCount}");
            }

            // 更新状态
            controllerPauseStates[controller] = currentlyPaused;
        }
    }

    /// <summary>
    /// 检查控制器是否处于暂停状态
    /// </summary>
    private bool IsControllerPaused(RhythmKeyControllerBase controller)
    {
        // 通过反射获取私有字段 isInPauseForFailure
        var fieldInfo = typeof(RhythmKeyControllerBase).GetField("isInPauseForFailure",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (fieldInfo != null)
        {
            return (bool)fieldInfo.GetValue(controller);
        }

        return false;
    }

    /// <summary>
    /// 更新UI显示
    /// </summary>
    private void UpdateDisplay()
    {
        if (failureCountText != null)
        {
            failureCountText.text = displayPrefix + totalFailureCount;
        }
    }

    /// <summary>
    /// 重置失败计数
    /// </summary>
    public void ResetCount()
    {
        totalFailureCount = 0;
        UpdateDisplay();
    }

    /// <summary>
    /// 获取当前失败次数
    /// </summary>
    public int GetFailureCount()
    {
        return totalFailureCount;
    }

    /// <summary>
    /// 手动添加控制器
    /// </summary>
    public void AddController(RhythmKeyControllerBase controller)
    {
        if (controller != null && !rhythmControllers.Contains(controller))
        {
            rhythmControllers.Add(controller);
            controllerPauseStates[controller] = false;
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
            controllerPauseStates.Remove(controller);
        }
    }
}