using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SpaceLongPressRadialFill : MonoBehaviour
{
    [Header("UI 组件")]
    public Image dashedCircle;      // 虚线圆形图片
    public Image fillCircle;        // 实线填充圆形图片

    [Header("长按设置")]
    public float fillDuration = 2f;  // 长按填充时间
    public float resetDuration = 2f; // 回退动画时间
    public float windowDuration = 0.3f; // 重置后的窗口期时间

    [Header("按键设置")]
    public KeyCode longPressKey = KeyCode.Space; // 长按键

    [Header("颜色设置")]
    public Color normalColor = Color.white; // 正常颜色
    public Color windowColor = Color.yellow; // 窗口期颜色

    private bool isFilling = false;
    private bool isResetting = false; // 是否在回退中
    private bool isInWindow = false; // 是否在窗口期
    private float pressStartTime;
    private float fillAmount = 0f;

    private void Start()
    {
        // 初始化UI状态
        if (fillCircle != null)
        {
            fillCircle.fillAmount = 0f;
            fillCircle.color = normalColor;
        }

        // 确保虚线圆可见，实线圆初始为空
        if (dashedCircle != null)
        {
            dashedCircle.gameObject.SetActive(true);
        }
    }

    private void Update()
    {
        HandleInput();
        UpdateFillProgress();
    }

    private void HandleInput()
    {
        // 检测空格键按下 - 只有在非忙碌状态下才能开始
        if (Input.GetKeyDown(longPressKey) && !isFilling && !isResetting && !isInWindow)
        {
            StartFilling();
        }

        // 检测空格键松开
        if (Input.GetKeyUp(longPressKey) && isFilling)
        {
            StopFilling();
        }
    }

    private void UpdateFillProgress()
    {
        if (isFilling)
        {
            float timeHeld = Time.time - pressStartTime;
            fillAmount = Mathf.Clamp01(timeHeld / fillDuration);

            if (fillCircle != null)
            {
                fillCircle.fillAmount = fillAmount;
            }

            // 检查是否填充完成
            if (fillAmount >= 1f)
            {
                OnFillComplete();
            }
        }
    }

    private void StartFilling()
    {
        isFilling = true;
        pressStartTime = Time.time;
        fillAmount = 0f;

        if (fillCircle != null)
        {
            fillCircle.fillAmount = 0f;
            fillCircle.color = normalColor;
            fillCircle.gameObject.SetActive(true);
        }

        Debug.Log("开始长按填充");
    }

    private void StopFilling()
    {
        if (!isFilling) return;

        isFilling = false;
        fillAmount = 0f;

        if (fillCircle != null)
        {
            fillCircle.fillAmount = 0f;
        }

        Debug.Log("停止填充，重置进度");
    }

    private void OnFillComplete()
    {
        isFilling = false;

        Debug.Log("填充完成！开始回退动画");

        // 在这里添加填充完成时的逻辑
        OnActionTriggered();

        // 直接开始回退动画，不需要窗口期
        StartCoroutine(ResetAnimation());
    }

    private IEnumerator ResetAnimation()
    {
        isResetting = true;
        float startFillAmount = fillAmount;
        float elapsedTime = 0f;

        Debug.Log("开始回退动画，从 " + startFillAmount + " 回退到 0");

        while (elapsedTime < resetDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / resetDuration;

            // 使用平滑曲线让回退更自然
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
            fillAmount = Mathf.Lerp(startFillAmount, 0f, smoothProgress);

            if (fillCircle != null)
            {
                fillCircle.fillAmount = fillAmount;
            }

            yield return null;
        }

        // 确保完全重置
        fillAmount = 0f;
        if (fillCircle != null)
        {
            fillCircle.fillAmount = 0f;
        }

        isResetting = false;
        Debug.Log("回退动画完成，进入窗口期");

        // 回退完成后开始窗口期
        yield return StartCoroutine(WindowPeriod());
    }

    private IEnumerator WindowPeriod()
    {
        isInWindow = true;

        // 设置窗口期颜色为黄色
        if (fillCircle != null)
        {
            fillCircle.color = windowColor;
        }

        Debug.Log("进入0.3秒窗口期（黄色）");

        // 等待窗口期时间
        yield return new WaitForSeconds(windowDuration);

        // 窗口期结束，恢复正常状态
        isInWindow = false;

        if (fillCircle != null)
        {
            fillCircle.color = normalColor;
        }

        Debug.Log("窗口期结束，可以重新开始长按");
    }

    private void OnActionTriggered()
    {
        Debug.Log("长按动作触发！");
        // 在这里添加你需要执行的动作
        // 例如：触发某个事件、播放音效、执行特定功能等
    }

    // 公共方法：获取当前状态
    public bool IsInProgress()
    {
        return isFilling || isResetting || isInWindow;
    }

    public float GetFillProgress()
    {
        return fillAmount;
    }

    public bool IsInWindowPeriod()
    {
        return isInWindow;
    }

    public bool IsResetting()
    {
        return isResetting;
    }

    // 公共方法：手动重置
    public void ResetProgress()
    {
        StopAllCoroutines();
        isFilling = false;
        isResetting = false;
        isInWindow = false;
        fillAmount = 0f;

        if (fillCircle != null)
        {
            fillCircle.fillAmount = 0f;
            fillCircle.color = normalColor;
        }
    }
}