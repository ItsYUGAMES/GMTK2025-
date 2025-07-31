using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    [Header("Sprite Prefabs")]
    public GameObject keyA_Prefab;
    public GameObject keyD_Prefab;
    public Vector3 keyASpawnPosition;
    public Vector3 keyDSpawnPosition;

    private SpriteRenderer keyA_SpriteRenderer;
    private SpriteRenderer keyD_SpriteRenderer;

    [Header("Visual Settings")]
    public Color normalKeyColor = Color.gray;
    public Color highlightKeyColor = Color.white;
    public Color successKeyColor = Color.green;
    public Color missKeyColor = Color.red;
    public float feedbackDisplayDuration = 0.2f;

    [Header("Rhythm Settings")]
    public float beatInterval = 1.0f;           // 节拍间隔（秒）
    public float successWindow = 0.4f;          // 成功按键的时间窗口
    public float highlightDuration = 0.8f;      // 高亮显示的持续时间
    public bool infiniteLoop = true;            // 无限循环模式

    [Header("Slow Motion Settings")]
    public float slowMotionDuration = 1.5f;
    public float slowMotionTimeScale = 0.3f;
    public Color slowMotionTintColor = new Color(0.8f, 0.8f, 1.0f, 0.7f); // 蓝色调
    private float normalTimeScale;
    private float slowMotionTimer = 0f;
    private bool inSlowMotion = false;

    [Header("Slow Motion UI")]
    public GameObject slowMotionIndicator; // 可选的UI指示器

    [Header("Key Settings")]
    public KeyCode primaryKey = KeyCode.A;      // 主按键
    public KeyCode secondaryKey = KeyCode.D;    // 副按键

    // 摄像机相关
    private Camera mainCamera;
    private Color originalCameraColor;

    // 节拍系统变量
    private float gameStartTime;
    private int beatCounter = 0;
    private bool waitingForInput = false;
    private KeyCode expectedKey;
    private float currentBeatStartTime;

    // 协程引用
    private Coroutine aKeyColorCoroutine;
    private Coroutine dKeyColorCoroutine;
    private Coroutine blinkCoroutine;

    void Awake()
    {
        normalTimeScale = Time.timeScale;

        // 获取主摄像机
        mainCamera = Camera.main;
        if (mainCamera == null) mainCamera = FindObjectOfType<Camera>();
        if (mainCamera != null)
        {
            originalCameraColor = mainCamera.backgroundColor;
        }

        // 实例化按键视觉
        if (keyA_Prefab != null)
        {
            GameObject aObj = Instantiate(keyA_Prefab, keyASpawnPosition, Quaternion.identity);
            keyA_SpriteRenderer = aObj.GetComponent<SpriteRenderer>();
            if (keyA_SpriteRenderer == null) Debug.LogError("A 键 Prefab 没有 SpriteRenderer 组件！");
        }
        if (keyD_Prefab != null)
        {
            GameObject dObj = Instantiate(keyD_Prefab, keyDSpawnPosition, Quaternion.identity);
            keyD_SpriteRenderer = dObj.GetComponent<SpriteRenderer>();
            if (keyD_SpriteRenderer == null) Debug.LogError("D 键 Prefab 没有 SpriteRenderer 组件！");
        }
    }

    void Start()
    {
        gameStartTime = Time.time;
        SetAllKeysColor(normalKeyColor);
        StartNextBeat();
        Debug.Log($"节奏游戏开始！{primaryKey}-{secondaryKey}交替，无限循环模式。");
    }

    void Update()
    {
        HandleSlowMotion();
        CheckBeatTiming();
        HandlePlayerInput();
    }

    /// <summary>
    /// 开始下一个节拍
    /// </summary>
    void StartNextBeat()
    {
        // 普通节拍：主副按键交替
        expectedKey = (beatCounter % 2 == 0) ? primaryKey : secondaryKey;

        currentBeatStartTime = Time.time;
        waitingForInput = true;

        string beatType = "普通";
        Debug.Log($"节拍 {beatCounter}: {beatType}节拍，期望按键 {expectedKey}，开始时间 {currentBeatStartTime:F2}s");

        // 根据节拍类型高亮对应按键
        if (expectedKey == primaryKey)
        {
            SetKeyColor(KeyCode.A, highlightKeyColor);
        }
        else
        {
            SetKeyColor(KeyCode.D, highlightKeyColor);
        }

        beatCounter++;
    }

    /// <summary>
    /// 检查节拍时机
    /// </summary>
    void CheckBeatTiming()
    {
        if (!waitingForInput) return;

        float elapsed = Time.time - currentBeatStartTime;

        // 普通节拍的处理
        if (elapsed > successWindow)
        {
            Debug.LogWarning($"错过节拍！耗时: {elapsed:F2}s");
            OnBeatMissed();
        }
    }

    /// <summary>
    /// 处理玩家输入
    /// </summary>
    void HandlePlayerInput()
    {
        // 检查普通按键按下
        if (Input.GetKeyDown(primaryKey))
        {
            OnKeyPressed(primaryKey);
        }
        else if (Input.GetKeyDown(secondaryKey))
        {
            OnKeyPressed(secondaryKey);
        }
    }

    /// <summary>
    /// 按键被按下时的处理
    /// </summary>
    void OnKeyPressed(KeyCode pressedKey)
    {
        if (!waitingForInput)
        {
            Debug.LogWarning($"不在等待输入状态，按下了 {pressedKey}");
            ShowFeedback(pressedKey, missKeyColor);
            return;
        }

        float responseTime = Time.time - currentBeatStartTime;

        if (pressedKey == expectedKey)
        {
            // 普通按键成功
            string performance = GetPerformanceRating(responseTime);
            Debug.Log($"✅ {performance} 成功！按键: {pressedKey}, 反应时间: {responseTime:F3}s");
            OnBeatSuccess();
        }
        else
        {
            // 按错了键
            Debug.LogWarning($"❌ 按错了！期望 {expectedKey}，按下了 {pressedKey}");
            OnBeatFailed();
        }
    }

    /// <summary>
    /// 根据反应时间评价表现
    /// </summary>
    string GetPerformanceRating(float responseTime)
    {
        if (responseTime < 0.1f) return "闪电般！⚡";
        else if (responseTime < 0.2f) return "完美！⭐⭐⭐";
        else if (responseTime < 0.3f) return "很好！⭐⭐";
        else return "不错！⭐";
    }

    /// <summary>
    /// 节拍成功
    /// </summary>
    void OnBeatSuccess()
    {
        waitingForInput = false;
        ShowFeedback(expectedKey, successKeyColor);

        // 等待间隔后开始下一个节拍
        StartCoroutine(WaitForNextBeat());
    }

    /// <summary>
    /// 节拍失败（按错键）
    /// </summary>
    void OnBeatFailed()
    {
        waitingForInput = false;
        ShowFeedback(expectedKey, missKeyColor);
        StartSlowMotion();

        // 等待间隔后开始下一个节拍
        StartCoroutine(WaitForNextBeat());
    }

    /// <summary>
    /// 错过节拍
    /// </summary>
    void OnBeatMissed()
    {
        waitingForInput = false;
        ShowFeedback(expectedKey, missKeyColor);
        StartSlowMotion();

        // 等待间隔后开始下一个节拍
        StartCoroutine(WaitForNextBeat());
    }

    /// <summary>
    /// 等待下一个节拍
    /// </summary>
    IEnumerator WaitForNextBeat()
    {
        // 重置所有按键颜色
        yield return new WaitForSeconds(0.1f);
        SetAllKeysColor(normalKeyColor);

        // 等待节拍间隔
        yield return new WaitForSeconds(beatInterval - 0.1f);

        // 开始下一个节拍（如果是无限模式）
        if (infiniteLoop)
        {
            StartNextBeat();
        }
        else
        {
            Debug.Log("节奏序列结束！");
        }
    }

    /// <summary>
    /// 显示按键反馈
    /// </summary>
    void ShowFeedback(KeyCode key, Color color)
    {
        // 根据按键类型显示反馈
        if (key == primaryKey)
        {
            if (aKeyColorCoroutine != null) StopCoroutine(aKeyColorCoroutine);
            aKeyColorCoroutine = StartCoroutine(ShowColorFeedback(keyA_SpriteRenderer, color));
        }
        else if (key == secondaryKey)
        {
            if (dKeyColorCoroutine != null) StopCoroutine(dKeyColorCoroutine);
            dKeyColorCoroutine = StartCoroutine(ShowColorFeedback(keyD_SpriteRenderer, color));
        }
    }

    /// <summary>
    /// 颜色反馈协程
    /// </summary>
    IEnumerator ShowColorFeedback(SpriteRenderer renderer, Color feedbackColor)
    {
        if (renderer == null) yield break;

        renderer.color = feedbackColor;
        yield return new WaitForSeconds(feedbackDisplayDuration);
        renderer.color = normalKeyColor;
    }

    /// <summary>
    /// 设置单个按键颜色
    /// </summary>
    void SetKeyColor(KeyCode key, Color color)
    {
        if (key == KeyCode.A && keyA_SpriteRenderer != null)
        {
            if (aKeyColorCoroutine != null) StopCoroutine(aKeyColorCoroutine);
            keyA_SpriteRenderer.color = color;
        }
        else if (key == KeyCode.D && keyD_SpriteRenderer != null)
        {
            if (dKeyColorCoroutine != null) StopCoroutine(dKeyColorCoroutine);
            keyD_SpriteRenderer.color = color;
        }
    }

    /// <summary>
    /// 设置所有按键颜色
    /// </summary>
    void SetAllKeysColor(Color color)
    {
        if (keyA_SpriteRenderer != null)
        {
            if (aKeyColorCoroutine != null) StopCoroutine(aKeyColorCoroutine);
            keyA_SpriteRenderer.color = color;
        }
        if (keyD_SpriteRenderer != null)
        {
            if (dKeyColorCoroutine != null) StopCoroutine(dKeyColorCoroutine);
            keyD_SpriteRenderer.color = color;
        }
    }

    /// <summary>
    /// 处理慢动作
    /// </summary>
    void HandleSlowMotion()
    {
        if (inSlowMotion)
        {
            slowMotionTimer += Time.unscaledDeltaTime;

            // 显示慢放进度
            float progress = slowMotionTimer / slowMotionDuration;
            UpdateSlowMotionVisuals(progress);

            if (slowMotionTimer >= slowMotionDuration)
            {
                EndSlowMotion();
            }
        }
    }

    /// <summary>
    /// 开始慢动作
    /// </summary>
    void StartSlowMotion()
    {
        if (!inSlowMotion)
        {
            Time.timeScale = slowMotionTimeScale;
            inSlowMotion = true;
            slowMotionTimer = 0f;

            // 启动慢放视觉效果
            StartSlowMotionVisuals();

            Debug.Log("🐌 触发慢动作！时间变慢...");
        }
    }

    /// <summary>
    /// 结束慢动作
    /// </summary>
    void EndSlowMotion()
    {
        Time.timeScale = normalTimeScale;
        inSlowMotion = false;
        slowMotionTimer = 0f;

        // 结束慢放视觉效果
        EndSlowMotionVisuals();

        Debug.Log("⚡ 慢动作结束！时间恢复正常");
    }

    /// <summary>
    /// 启动慢放视觉效果
    /// </summary>
    void StartSlowMotionVisuals()
    {
        // 启用UI指示器
        if (slowMotionIndicator != null)
        {
            slowMotionIndicator.SetActive(true);
        }

        // 改变摄像机背景色
        if (mainCamera != null)
        {
            StartCoroutine(LerpCameraColor(originalCameraColor, slowMotionTintColor, 0.3f));
        }

        // 让所有按键闪烁提示
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
        }
        blinkCoroutine = StartCoroutine(BlinkAllKeys());
    }

    /// <summary>
    /// 结束慢放视觉效果
    /// </summary>
    void EndSlowMotionVisuals()
    {
        // 关闭UI指示器
        if (slowMotionIndicator != null)
        {
            slowMotionIndicator.SetActive(false);
        }

        // 恢复摄像机背景色
        if (mainCamera != null)
        {
            StartCoroutine(LerpCameraColor(mainCamera.backgroundColor, originalCameraColor, 0.3f));
        }

        // 停止按键闪烁
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        // 立即恢复按键正常颜色
        SetAllKeysColor(normalKeyColor);
    }

    /// <summary>
    /// 更新慢放视觉效果（显示进度）
    /// </summary>
    void UpdateSlowMotionVisuals(float progress)
    {
        // 可以在这里添加进度条或其他动态效果
        // 例如：改变屏幕色调的强度
        if (mainCamera != null)
        {
            float intensity = 1.0f - (progress * 0.5f); // 随时间减弱效果
            Color currentTint = Color.Lerp(originalCameraColor, slowMotionTintColor, intensity);
            mainCamera.backgroundColor = currentTint;
        }
    }

    /// <summary>
    /// 渐变摄像机背景色
    /// </summary>
    IEnumerator LerpCameraColor(Color fromColor, Color toColor, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            if (mainCamera != null)
            {
                mainCamera.backgroundColor = Color.Lerp(fromColor, toColor, t);
            }
            yield return null;
        }
        if (mainCamera != null)
        {
            mainCamera.backgroundColor = toColor;
        }
    }

    /// <summary>
    /// 让所有按键闪烁
    /// </summary>
    IEnumerator BlinkAllKeys()
    {
        while (inSlowMotion)
        {
            // 闪烁红色
            SetAllKeysColor(Color.red);
            yield return new WaitForSecondsRealtime(0.2f);

            // 恢复正常色
            SetAllKeysColor(normalKeyColor);
            yield return new WaitForSecondsRealtime(0.2f);
        }
    }
}
