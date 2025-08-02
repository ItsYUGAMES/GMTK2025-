using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class LongPressController : MonoBehaviour
{
    [Header("UI 组件")]
    public Image dashedCircle;           // 虚线圆
    public Image fillCircle;             // 填充圆
    public Image image3;                 // 失败时显示的图片

    [Header("判定设置")]
    public KeyCode longPressKey = KeyCode.Space;
    public float fillDuration = 2f;      // 完成长按所需时间
    public float resetDuration = 2f;     // 回退动画时间
    public float windowDuration = 0.3f;  // 窗口期时间
    public float failDisplayDuration = 0.3f; // 失败图片显示时间

    [Header("判定统计/流程")]
    public int failToLose = 2;    // 多少次失败GameOver

    [Header("自动游戏设置")]
    public float autoPressDuration = 1f;  // 自动长按持续时间

    [Header("脚本暂停设置")]
    public List<MonoBehaviour> scriptsToSuspend = new List<MonoBehaviour>(); // 需要暂停的脚本列表

    private float pressStartTime;
    private float fillAmount = 0f;
    private bool isFilling = false;
    private bool isResetting = false;
    private bool isInWindow = false;
    private bool isInFailState = false;  // 新增：是否处于失败状态（图3）
    private int successCount = 0;
    private int failCount = 0;
    private bool isGameEnded = false;
    private bool isAutoPlaying = false;
    private Coroutine autoPlayCoroutine;
    private List<bool> originalEnabledStates = new List<bool>(); // 保存脚本原始启用状态
    
    // 颜色设置
    private Color originalDashedColor;
    private Color yellowColor = Color.yellow;

    void Start()
    {
        // 初始化UI
        if (fillCircle != null)
        {
            fillCircle.fillAmount = 0f;
        }
        if (dashedCircle != null)
        {
            dashedCircle.gameObject.SetActive(true);
            originalDashedColor = dashedCircle.color; // 保存原始颜色
        }
        if (image3 != null)
        {
            image3.gameObject.SetActive(false);
        }

        // 保存脚本原始状态
        SaveOriginalScriptStates();

        // 检查是否启用自动游戏
        CheckAutoPlayStatus();
    }

    void SaveOriginalScriptStates()
    {
        originalEnabledStates.Clear();
        foreach (var script in scriptsToSuspend)
        {
            if (script != null)
            {
                originalEnabledStates.Add(script.enabled);
            }
        }
    }

    void SuspendScripts()
    {
        foreach (var script in scriptsToSuspend)
        {
            if (script != null)
            {
                script.enabled = false;
            }
        }
        Debug.Log("脚本已暂停");
    }

    void ResumeScripts()
    {
        for (int i = 0; i < scriptsToSuspend.Count && i < originalEnabledStates.Count; i++)
        {
            if (scriptsToSuspend[i] != null)
            {
                scriptsToSuspend[i].enabled = originalEnabledStates[i];
            }
        }
        Debug.Log("脚本已恢复");
    }

    void Update()
    {
        if (isGameEnded) return;
        HandleInput();
        UpdateFillProgress();
    }

    void CheckAutoPlayStatus()
    {
        if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.IsAutoPlayActive())
        {
            StartAutoPlay();
        }
    }

    void StartAutoPlay()
    {
        if (!isAutoPlaying && !isGameEnded)
        {
            isAutoPlaying = true;
            autoPlayCoroutine = StartCoroutine(AutoPlayCoroutine());
            Debug.Log("自动游戏模式启动");
        }
    }

    void StopAutoPlay()
    {
        if (autoPlayCoroutine != null)
        {
            StopCoroutine(autoPlayCoroutine);
            autoPlayCoroutine = null;
        }
        isAutoPlaying = false;
    }

    IEnumerator AutoPlayCoroutine()
    {
        while (!isGameEnded && isAutoPlaying)
        {
            // 等待当前操作完成
            while (isFilling || isResetting || isInWindow || isInFailState)
            {
                yield return null;
            }

            if (isGameEnded) break;

            // 开始自动长按
            yield return StartCoroutine(AutoLongPress());

            // 等待一小段时间再进行下一次
            yield return new WaitForSeconds(0.5f);
        }
    }

    IEnumerator AutoLongPress()
    {
        // 模拟开始长按
        isFilling = true;
        pressStartTime = Time.time;
        fillAmount = 0f;

        if (fillCircle != null)
        {
            fillCircle.fillAmount = 0f;
        }
        
        // 长按开始时变黄
        SetDashedCircleYellow();

        // 自动填充进度条
        float elapsedTime = 0f;
        while (elapsedTime < autoPressDuration && isFilling)
        {
            elapsedTime += Time.deltaTime;
            fillAmount = Mathf.Clamp01(elapsedTime / autoPressDuration);

            if (fillCircle != null)
                fillCircle.fillAmount = fillAmount;

            yield return null;
        }

        // 确保完成
        if (isFilling)
        {
            fillAmount = 1f;
            if (fillCircle != null)
                fillCircle.fillAmount = 1f;

            isFilling = false;
            
            // 长按结束时恢复颜色
            RestoreDashedCircleColor();
            
            OnLongPressSuccess();  // 自动游戏必定成功
        }
    }

    void HandleInput()
    {
        // 如果是自动游戏模式，忽略手动输入
        if (isAutoPlaying) return;

        // 如果image3正在显示，忽略输入
        if (image3 != null && image3.gameObject.activeInHierarchy) return;

        if (isFilling || isResetting || isInWindow) return;

        // 如果处于失败状态，只允许长按来恢复
        if (isInFailState)
        {
            if (Input.GetKeyDown(longPressKey))
            {
                StartCoroutine(RecoveryLongPress());
            }
            return;
        }

        // 开始长按
        if (Input.GetKeyDown(longPressKey))
        {
            isFilling = true;
            pressStartTime = Time.time;
            fillAmount = 0f;
            if (fillCircle != null)
            {
                fillCircle.fillAmount = 0f;
            }

            // 长按开始时变黄
            SetDashedCircleYellow();
        }
    }

    IEnumerator RecoveryLongPress()
    {
        // 恢复长按判定
        isFilling = true;
        pressStartTime = Time.time;
        fillAmount = 0f;

        if (fillCircle != null)
        {
            fillCircle.fillAmount = 0f;
        }
        
        // 长按开始时变黄
        SetDashedCircleYellow();

        while (isFilling)
        {
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
                    isInFailState = false;
                    
                    // 长按结束时恢复颜色
                    RestoreDashedCircleColor();
                    
                    ResumeScripts(); // 恢复脚本运行
                    OnLongPressSuccess();
                    yield break;
                }
            }
            else
            {
                // 松开了按键，重置并保持失败状态
                isFilling = false;
                fillAmount = 0f;
                if (fillCircle != null)
                {
                    fillCircle.fillAmount = 0f;
                }
                
                // 长按中断时恢复颜色
                RestoreDashedCircleColor();
                
                yield break;
            }
            yield return null;
        }
    }

    void UpdateFillProgress()
    {
        // 如果是自动游戏模式或处于失败状态，不处理手动输入
        if (isAutoPlaying || !isFilling || isInFailState) return;

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
                
                // 长按结束时恢复颜色
                RestoreDashedCircleColor();
                
                OnLongPressSuccess();
            }
        }
        else // 中途松开
        {
            isFilling = false;
            fillAmount = 0f;
            if (fillCircle != null)
                fillCircle.fillAmount = 0f;
                
            // 长按中断时恢复颜色
            RestoreDashedCircleColor();
            
            OnLongPressFail();
        }
    }

    void SetDashedCircleYellow()
    {
        if (dashedCircle != null)
        {
            dashedCircle.color = yellowColor;
        }
    }

    void RestoreDashedCircleColor()
    {
        if (dashedCircle != null)
        {
            dashedCircle.color = originalDashedColor;
        }
    }

    void OnLongPressSuccess()
    {
        successCount++;
        Debug.Log($"长按判定成功（累计{successCount}）");

        // 直接进入下一轮循环，不再检查成功次数
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

        // 显示失败图片0.3秒后再进入回退和窗口序列
        StartCoroutine(ShowFailImageAndContinue());
    }

    IEnumerator ShowFailImageAndContinue()
    {
        // 显示失败图片
        if (image3 != null)
        {
            image3.gameObject.SetActive(true);
        }

        // 等待0.3秒
        yield return new WaitForSeconds(failDisplayDuration);

        // 隐藏失败图片
        if (image3 != null)
        {
            image3.gameObject.SetActive(false);
        }

        // 继续进入回退和窗口序列
        StartCoroutine(ResetAndWindowSequence());
    }
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

    // --- 窗口期判定 ---
    isInWindow = true;

    // 窗口期开始时虚线圆变黄，填充圆保持0
    SetDashedCircleYellow();
    if (fillCircle != null) fillCircle.fillAmount = 0f;

    bool windowSuccess = false;
    float windowStartTime = Time.time;

    // 自动游戏模式下直接成功
    if (isAutoPlaying)
    {
        yield return new WaitForSeconds(windowDuration * 0.3f);
        windowSuccess = true;
        if (fillCircle != null)
            fillCircle.fillAmount = 1f;
    }
    else
    {
        // 窗口期内等待玩家长按判定
        while (Time.time - windowStartTime < windowDuration && !windowSuccess)
        {
            if (Input.GetKeyDown(longPressKey))
            {
                float pressStart = Time.time;
                bool pressingDown = true;

                while (pressingDown && Time.time - pressStart < fillDuration)
                {
                    if (Input.GetKey(longPressKey))
                    {
                        float progress = (Time.time - pressStart) / fillDuration;
                        if (fillCircle != null)
                            fillCircle.fillAmount = progress;

                        if (progress >= 1f)
                        {
                            windowSuccess = true;
                            break;
                        }
                    }
                    else
                    {
                        pressingDown = false;
                        break;
                    }
                    yield return null;
                }

                if (!windowSuccess && pressingDown)
                {
                    windowSuccess = true;
                    if (fillCircle != null)
                        fillCircle.fillAmount = 1f;
                }
                break;
            }
            yield return null;
        }
    }

    isInWindow = false;

    // 根据窗口期结果处理
    if (windowSuccess)
    {
        // 窗口期成功，重置fillAmount并恢复虚线圆颜色
        if (fillCircle != null) fillCircle.fillAmount = 0f;
        RestoreDashedCircleColor();
    }
    else
    {
        // 窗口期超时，显示失败图片然后进入失败状态
        StartCoroutine(ShowFailImageAndEnterFailState());
    }
}

IEnumerator ShowFailImageAndEnterFailState()
{
    // 显示失败图片
    if (image3 != null)
    {
        image3.gameObject.SetActive(true);
    }

    // 等待0.3秒
    yield return new WaitForSeconds(failDisplayDuration);

    // 隐藏失败图片
    if (image3 != null)
    {
        image3.gameObject.SetActive(false);
    }

    // 进入失败状态
    isInFailState = true;
    if (fillCircle != null) fillCircle.fillAmount = 0f;
    // 保持虚线圆为黄色，表示失败状态
    SuspendScripts();
    Debug.Log("窗口期超时，进入失败状态，请长按恢复");
}

    void OnGameFail()
    {
        isGameEnded = true;
        StopAutoPlay();
        ResumeScripts();
        Debug.Log("游戏失败！");
    }

    public bool IsInProgress() => isFilling || isResetting || isInWindow || isInFailState;
    public float GetFillProgress() => fillAmount;
    public bool IsInWindowPeriod() => isInWindow;
    public bool IsResetting() => isResetting;
    public bool IsInFailState() => isInFailState;

    public void ResetAll()
    {
        StopAllCoroutines();
        StopAutoPlay();
        isFilling = false;
        isResetting = false;
        isInWindow = false;
        isInFailState = false;
        isGameEnded = false;
        fillAmount = 0f;
        successCount = 0;
        failCount = 0;
        if (fillCircle != null)
        {
            fillCircle.fillAmount = 0f;
        }
        if (dashedCircle != null)
        {
            dashedCircle.fillAmount = 1f;
        }
        if (image3 != null)
        {
            image3.gameObject.SetActive(false);
        }

        // 恢复原始颜色
        RestoreDashedCircleColor();
        
        ResumeScripts();
        CheckAutoPlayStatus();
    }
}