using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LongPressController : MonoBehaviour
{
    [Header("UI 组件")]
    public Image dashedCircle;           // 虚线圆
    public Image fillCircle;             // 填充圆

    [Header("判定设置")]
    public KeyCode longPressKey = KeyCode.Space;
    public float fillDuration = 2f;      // 完成长按所需时间
    public float resetDuration = 2f;     // 回退动画时间
    public float windowDuration = 0.3f;  // 窗口期时间

    [Header("颜色设置")]
    public Color fillNormalColor = Color.white;                       // 填充圈正常
    public Color fillWindowColor = Color.yellow;                      // 填充圈窗口期
    public Color dashedNormalColor = new Color32(0x60, 0x51, 0xFF, 0xFF); // 虚线圈正常（6051FF）
    public Color dashedWindowColor = Color.yellow;                    // 虚线圈窗口期

    [Header("判定统计/流程")]
    public int successToPass = 3; // 多少次成功判定通关
    public int failToLose = 2;    // 多少次失败GameOver

    private float pressStartTime;
    private float fillAmount = 0f;
    private bool isFilling = false;
    private bool isResetting = false;
    private bool isInWindow = false;
    private int successCount = 0;
    private int failCount = 0;
    private bool isGameEnded = false;

    void Start()
    {
        // 初始化颜色和UI
        if (fillCircle != null)
        {
            fillCircle.fillAmount = 0f;
            fillCircle.color = fillNormalColor;
        }
        if (dashedCircle != null)
        {
            dashedCircle.color = dashedNormalColor;
            dashedCircle.gameObject.SetActive(true);
        }
    }

    void Update()
    {
        if (isGameEnded) return;
        HandleInput();
        UpdateFillProgress();
    }

    void HandleInput()
    {
        if (isFilling || isResetting || isInWindow) return;

        // 开始长按
        if (Input.GetKeyDown(longPressKey))
        {
            isFilling = true;
            pressStartTime = Time.time;
            fillAmount = 0f;
            if (fillCircle != null)
            {
                fillCircle.fillAmount = 0f;
                fillCircle.color = fillNormalColor;
            }
        }
    }

    void UpdateFillProgress()
    {
        if (!isFilling) return;

        // 正在长按
        if (Input.GetKey(longPressKey))
        {
            float held = Time.time - pressStartTime;
            fillAmount = Mathf.Clamp01(held / fillDuration);
            if (fillCircle != null)
                fillCircle.fillAmount = fillAmount;

            // 长按完成
            if (fillAmount >= 1f)
            {
                isFilling = false;
                fillCircle.fillAmount = 1f;
                OnLongPressSuccess();
            }
        }
        else // 中途松开
        {
            isFilling = false;
            fillAmount = 0f;
            if (fillCircle != null)
                fillCircle.fillAmount = 0f;
            OnLongPressFail();
        }
    }

    void OnLongPressSuccess()
    {
        successCount++;
        Debug.Log($"长按判定成功（累计{successCount}）");

        if (successCount >= successToPass)
        {
            OnGameSuccess();
            return;
        }
        StartCoroutine(ResetAndWindowSequence());
    }

    void OnLongPressFail()
    {
        failCount++;
        Debug.Log($"长按判定失败（累计{failCount}）");

        if (failCount >= failToLose)
        {
            OnGameFail();
            return;
        }
        StartCoroutine(ResetAndWindowSequence());
    }

    // 回退+窗口协程
    IEnumerator ResetAndWindowSequence()
    {
        // --- 回退动画 ---
        isResetting = true;
        float startFill = fillAmount;
        float t = 0f;
        while (t < resetDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / resetDuration);
            float v = Mathf.Lerp(startFill, 0f, p);
            if (fillCircle != null)
                fillCircle.fillAmount = v;
            yield return null;
        }
        if (fillCircle != null) fillCircle.fillAmount = 0f;
        isResetting = false;

        // --- 窗口期 ---
        isInWindow = true;
        if (fillCircle != null) fillCircle.color = fillWindowColor;
        if (dashedCircle != null) dashedCircle.color = dashedWindowColor;
        yield return new WaitForSeconds(windowDuration);
        isInWindow = false;
        if (fillCircle != null) fillCircle.color = fillNormalColor;
        if (dashedCircle != null) dashedCircle.color = dashedNormalColor;
    }

    void OnGameSuccess()
    {
        isGameEnded = true;
        Debug.Log(" 通关成功！");
        // 这里可加奖励、通关UI等
    }

    void OnGameFail()
    {
        isGameEnded = true;
        Debug.Log(" 游戏失败！");
        // 这里可加GameOver界面、重试等
    }

    // 公共接口，可用于UI/其它逻辑调用
    public bool IsInProgress() => isFilling || isResetting || isInWindow;
    public float GetFillProgress() => fillAmount;
    public bool IsInWindowPeriod() => isInWindow;
    public bool IsResetting() => isResetting;

    // 重置接口
    public void ResetAll()
    {
        StopAllCoroutines();
        isFilling = false;
        isResetting = false;
        isInWindow = false;
        isGameEnded = false;
        fillAmount = 0f;
        successCount = 0;
        failCount = 0;
        if (fillCircle != null)
        {
            fillCircle.fillAmount = 0f;
            fillCircle.color = fillNormalColor;
        }
        if (dashedCircle != null)
        {
            dashedCircle.color = dashedNormalColor;
        }
    }
}
