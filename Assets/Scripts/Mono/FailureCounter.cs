using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// ͳ����Ϸ��δ��ͣʱ��ʧ�ܴ���
/// </summary>
public class FailureCounter : MonoBehaviour
{
    [Header("UI��ʾ")]
    [SerializeField] private Text failureCountText;
    [SerializeField] private string displayPrefix = "ʧ�ܴ���: ";

    [Header("����������")]
    [SerializeField] private List<RhythmKeyControllerBase> rhythmControllers = new List<RhythmKeyControllerBase>();

    private int totalFailureCount = 0;
    private Dictionary<RhythmKeyControllerBase, bool> controllerPauseStates = new Dictionary<RhythmKeyControllerBase, bool>();

    void Start()
    {
        // ��ʼ��������״̬
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
        // ����ÿ����������ʧ�ܺ���ͣ״̬
        foreach (var controller in rhythmControllers)
        {
            if (controller == null) continue;

            // ��ȡ��ǰ����������ͣ״̬
            bool currentlyPaused = IsControllerPaused(controller);

            // ����Ƿ�ӷ���ͣ״̬��Ϊ��ͣ״̬����ʾ������ʧ�ܣ�
            if (!controllerPauseStates[controller] && currentlyPaused)
            {
                // ʧ�ܷ��������Ӽ���
                totalFailureCount++;
                UpdateDisplay();
                Debug.Log($"[FailureCounter] {controller.keyConfigPrefix} ʧ�ܣ���ʧ�ܴ���: {totalFailureCount}");
            }

            // ����״̬
            controllerPauseStates[controller] = currentlyPaused;
        }
    }

    /// <summary>
    /// ���������Ƿ�����ͣ״̬
    /// </summary>
    private bool IsControllerPaused(RhythmKeyControllerBase controller)
    {
        // ͨ�������ȡ˽���ֶ� isInPauseForFailure
        var fieldInfo = typeof(RhythmKeyControllerBase).GetField("isInPauseForFailure",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (fieldInfo != null)
        {
            return (bool)fieldInfo.GetValue(controller);
        }

        return false;
    }

    /// <summary>
    /// ����UI��ʾ
    /// </summary>
    private void UpdateDisplay()
    {
        if (failureCountText != null)
        {
            failureCountText.text = displayPrefix + totalFailureCount;
        }
    }

    /// <summary>
    /// ����ʧ�ܼ���
    /// </summary>
    public void ResetCount()
    {
        totalFailureCount = 0;
        UpdateDisplay();
    }

    /// <summary>
    /// ��ȡ��ǰʧ�ܴ���
    /// </summary>
    public int GetFailureCount()
    {
        return totalFailureCount;
    }

    /// <summary>
    /// �ֶ���ӿ�����
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
    /// �Ƴ�������
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