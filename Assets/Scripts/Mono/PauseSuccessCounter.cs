using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// ͳ����ͣ�������ɹ��Ĵ���
/// </summary>
public class PauseSuccessCounter : MonoBehaviour
{
    [Header("UI��ʾ")]
    [SerializeField] private Text successCountText;
    [SerializeField] private string displayPrefix = "�����ɹ�: ";
    [SerializeField] private bool showMaxRequired = true; // �Ƿ���ʾ��Ҫ��������

    [Header("����������")]
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
        // ���ҵ�ǰ������ͣ״̬�Ŀ�����
        RhythmKeyControllerBase pausedController = FindPausedController();

        if (pausedController != null)
        {
            // ������µ���ͣ�����������ü���
            if (currentPausedController != pausedController)
            {
                currentPausedController = pausedController;
                currentSuccessCount = 0;
                requiredSuccessCount = GetRequiredSuccessCount(pausedController);
            }

            // ��ȡ��ǰ�������ɹ�����
            int consecutiveSuccess = GetConsecutiveSuccessCount(pausedController);

            // ���¼���
            if (consecutiveSuccess != currentSuccessCount)
            {
                currentSuccessCount = consecutiveSuccess;
                UpdateDisplay();

                Debug.Log($"[PauseSuccessCounter] {pausedController.keyConfigPrefix} �����ɹ�: {currentSuccessCount}/{requiredSuccessCount}");

                // ����Ƿ񼴽��ָ�
                if (currentSuccessCount >= requiredSuccessCount)
                {
                    Debug.Log($"[PauseSuccessCounter] {pausedController.keyConfigPrefix} �����ָ���Ϸ��");
                }
            }
        }
        else
        {
            // û�п�����������ͣ״̬
            if (currentPausedController != null)
            {
                currentPausedController = null;
                currentSuccessCount = 0;
                UpdateDisplay();
            }
        }
    }

    /// <summary>
    /// ���ҵ�ǰ������ͣ״̬�Ŀ�����
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
    /// ���������Ƿ�����ͣ״̬
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
    /// ��ȡ�������������ɹ�����
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
    /// ��ȡ�ָ�����ĳɹ�����
    /// </summary>
    private int GetRequiredSuccessCount(RhythmKeyControllerBase controller)
    {
        // needConsecutiveSuccessToResume �ǹ����ֶ�
        return controller.needConsecutiveSuccessToResume;
    }

    /// <summary>
    /// ����UI��ʾ
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
    /// ��ȡ��ǰ�����ɹ�����
    /// </summary>
    public int GetCurrentSuccessCount()
    {
        return currentSuccessCount;
    }

    /// <summary>
    /// ��ȡ��ǰ��ͣ�Ŀ�����
    /// </summary>
    public RhythmKeyControllerBase GetCurrentPausedController()
    {
        return currentPausedController;
    }

    /// <summary>
    /// �ֶ���ӿ�����
    /// </summary>
    public void AddController(RhythmKeyControllerBase controller)
    {
        if (controller != null && !rhythmControllers.Contains(controller))
        {
            rhythmControllers.Add(controller);
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
        }
    }
}