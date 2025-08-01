using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartManager : MonoBehaviour
{
    [Header("按键配置")]
    public KeyCode primaryKey = KeyCode.A;
    public KeyCode secondaryKey = KeyCode.D;

    [Header("判定设置")]
    public int requiredSuccessCount = 4;
    public float beatInterval = 1.0f;

    [Header("视觉反馈")]
    public SpriteRenderer primaryKeyRenderer;
    public SpriteRenderer secondaryKeyRenderer;
    public Color normalColor = Color.white;
    public Color highlightColor = Color.yellow;
    public Color successColor = Color.green;

    private int currentSuccessCount = 0;
    private bool isWaitingForInput = false;
    private bool isCompleted = false;
    private KeyCode currentExpectedKey;
    private List<MonoBehaviour> pausedBehaviors = new List<MonoBehaviour>();

    void Start()
    {
        PauseAllOtherScripts();
        StartCoroutine(StartSequence());
    }

    void Update()
    {
        if (isCompleted || !isWaitingForInput) return;

        if (Input.GetKeyDown(currentExpectedKey))
        {
            OnKeyPressed();
        }
    }

    private void PauseAllOtherScripts()
    {
        pausedBehaviors.Clear();
        foreach (var mb in FindObjectsOfType<MonoBehaviour>())
        {
            if (mb == this || mb is PauseManager)
                continue;
            if (!mb.enabled)
                continue;
            pausedBehaviors.Add(mb);
            mb.enabled = false;
        }
        Debug.Log($"已暂停 {pausedBehaviors.Count} 个脚本");
    }

    private void ResumeAllOtherScripts()
    {
        foreach (var mb in pausedBehaviors)
            if (mb != null) mb.enabled = true;
        pausedBehaviors.Clear();
        Debug.Log("已恢复所有脚本");
    }

    private IEnumerator StartSequence()
    {
        yield return new WaitForSeconds(0.5f);

        while (currentSuccessCount < requiredSuccessCount && !isCompleted)
        {
            // 交替设置期望按键
            currentExpectedKey = (currentSuccessCount % 2 == 0) ? primaryKey : secondaryKey;
            yield return StartCoroutine(WaitForBeat());
        }

        if (currentSuccessCount >= requiredSuccessCount)
        {
            OnSequenceCompleted();
        }
    }

    private IEnumerator WaitForBeat()
    {
        // 设置所有按键为正常颜色
        SetAllKeysColor(normalColor);
        
        // 高亮当前期望的按键
        SetKeyColor(currentExpectedKey, highlightColor);

        isWaitingForInput = true;

        // 等待玩家输入或超时
        float timeLeft = beatInterval;
        while (timeLeft > 0 && isWaitingForInput)
        {
            timeLeft -= Time.deltaTime;
            yield return null;
        }

        // 如果超时未按键，重置计数
        if (isWaitingForInput)
        {
            OnBeatMissed();
        }

        isWaitingForInput = false;

        // 如果按键成功，显示绿色反馈
        if (currentSuccessCount > 0)
        {
            SetKeyColor(currentExpectedKey, successColor);
            yield return new WaitForSeconds(0.1f);
        }

        // 恢复按键颜色
        SetAllKeysColor(normalColor);
        yield return new WaitForSeconds(0.2f);
    }

    private void OnKeyPressed()
    {
        if (!isWaitingForInput) return;

        isWaitingForInput = false;
        currentSuccessCount++;

        Debug.Log($"成功判定 {currentSuccessCount}/{requiredSuccessCount} - 按键: {currentExpectedKey}");
    }

    private void OnBeatMissed()
    {
        currentSuccessCount = 0;
        Debug.Log("判定失败，重新开始");
    }

    private void SetKeyColor(KeyCode key, Color color)
    {
        if (key == primaryKey && primaryKeyRenderer != null)
            primaryKeyRenderer.color = color;
        else if (key == secondaryKey && secondaryKeyRenderer != null)
            secondaryKeyRenderer.color = color;
    }

    private void SetAllKeysColor(Color color)
    {
        if (primaryKeyRenderer != null)
            primaryKeyRenderer.color = color;
        if (secondaryKeyRenderer != null)
            secondaryKeyRenderer.color = color;
    }

    private void OnSequenceCompleted()
    {
        isCompleted = true;
        Debug.Log("开始序列完成，启动游戏");

        ResumeAllOtherScripts();

        var controllers = FindObjectsOfType<RhythmKeyControllerBase>();
        foreach (var controller in controllers)
        {
            controller.StartRhythm();
        }

        gameObject.SetActive(false);
    }
}