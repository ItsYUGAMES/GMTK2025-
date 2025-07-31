using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    [Header("Sprite Prefabs")]
    public GameObject keyA_Prefab;
    public GameObject keyD_Prefab;
    public GameObject longPressKeyPrefab;       // 长按键的Prefab
    public Vector3 keyASpawnPosition;
    public Vector3 keyDSpawnPosition;

    private SpriteRenderer keyA_SpriteRenderer;
    private SpriteRenderer keyD_SpriteRenderer;
    private SpriteRenderer longPressKey_SpriteRenderer; // 长按键的渲染器

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

    [Header("Long Press Settings")]
    public bool enableLongPress = true;         // 启用长按功能
    public KeyCode longPressKey = KeyCode.Space; // 专门的长按键
    public float longPressDuration = 2.0f;      // 长按持续时间
    public float longPressFrequency = 0.3f;     // 长按节拍出现频率（0-1之间）
    public Color longPressColor = Color.blue;   // 长按按键颜色
    public GameObject progressCirclePrefab;     // 进度圆的Prefab
    public Vector3 longPressKeyPosition = new Vector3(0, -2, 0); // 长按键显示位置

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

    // 长按相关变量
    private bool isLongPressBeat = false;       // 当前节拍是否是长按
    private bool isLongPressing = false;        // 是否正在长按
    private float longPressStartTime = 0f;      // 长按开始时间
    private GameObject currentProgressCircle;   // 当前的进度圆
    private UnityEngine.UI.Image progressFill;  // 进度填充组件

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

        // 实例化长按键视觉
        if (enableLongPress && longPressKeyPrefab != null)
        {
            GameObject longPressObj = Instantiate(longPressKeyPrefab, longPressKeyPosition, Quaternion.identity);
            longPressKey_SpriteRenderer = longPressObj.GetComponent<SpriteRenderer>();
            if (longPressKey_SpriteRenderer == null) Debug.LogError("长按键 Prefab 没有 SpriteRenderer 组件！");
        }
    }

    void Start()
    {
        gameStartTime = Time.time;
        SetAllKeysColor(normalKeyColor);
        StartNextBeat();
        Debug.Log($"节奏游戏开始！{primaryKey}-{secondaryKey}交替，长按键: {longPressKey}，无限循环模式。");
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
        // 随机决定是否是长按节拍
        if (enableLongPress && Random.value < longPressFrequency)
        {
            isLongPressBeat = true;
            expectedKey = longPressKey; // 长按节拍使用专门的长按键
        }
        else
        {
            isLongPressBeat = false;
            // 普通节拍：主副按键交替
            expectedKey = (beatCounter % 2 == 0) ? primaryKey : secondaryKey;
        }

        currentBeatStartTime = Time.time;
        waitingForInput = true;

        string beatType = isLongPressBeat ? "长按" : "普通";
        Debug.Log($"节拍 {beatCounter}: {beatType}节拍，期望按键 {expectedKey}，开始时间 {currentBeatStartTime:F2}s");

        // 根据节拍类型高亮对应按键
        if (isLongPressBeat)
        {
            // 高亮长按键
            SetLongPressKeyColor(longPressColor);
            // 立即创建进度圆指示器，但不开始填充
            CreateProgressIndicator();
        }
        else
        {
            // 高亮普通按键
            if (expectedKey == primaryKey)
            {
                SetKeyColor(KeyCode.A, highlightKeyColor);
            }
            else
            {
                SetKeyColor(KeyCode.D, highlightKeyColor);
            }
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

        // 长按节拍的处理
        if (isLongPressBeat)
        {
            // 如果正在长按，检查长按是否完成
            if (isLongPressing)
            {
                UpdateLongPressProgress();

                // 检查长按是否完成
                float longPressElapsed = Time.time - longPressStartTime;
                if (longPressElapsed >= longPressDuration)
                {
                    OnLongPressComplete();
                    return;
                }
            }

            // 如果超过成功窗口还没开始长按，视为错过
            if (!isLongPressing && elapsed > successWindow)
            {
                Debug.LogWarning($"错过长按节拍！耗时: {elapsed:F2}s");
                OnBeatMissed();
            }
        }
        else
        {
            // 普通节拍的处理
            if (elapsed > successWindow)
            {
                Debug.LogWarning($"错过节拍！耗时: {elapsed:F2}s");
                OnBeatMissed();
            }
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

        // 检查长按键
        if (enableLongPress)
        {
            if (Input.GetKeyDown(longPressKey))
            {
                OnKeyPressed(longPressKey);
            }
            else if (Input.GetKeyUp(longPressKey))
            {
                OnKeyReleased(longPressKey);
            }
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
            if (isLongPressBeat)
            {
                // 开始长按
                StartLongPress();
            }
            else
            {
                // 普通按键成功
                string performance = GetPerformanceRating(responseTime);
                Debug.Log($"✅ {performance} 成功！按键: {pressedKey}, 反应时间: {responseTime:F3}s");
                OnBeatSuccess();
            }
        }
        else
        {
            // 按错了键
            Debug.LogWarning($"❌ 按错了！期望 {expectedKey}，按下了 {pressedKey}");
            OnBeatFailed();
        }
    }

    /// <summary>
    /// 按键被抬起时的处理
    /// </summary>
    void OnKeyReleased(KeyCode releasedKey)
    {
        if (isLongPressing && releasedKey == longPressKey)
        {
            // 长按提前结束
            float longPressElapsed = Time.time - longPressStartTime;
            Debug.LogWarning($"❌ 长按提前结束！持续时间: {longPressElapsed:F2}s / {longPressDuration:F2}s");
            OnLongPressFailed();
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
    /// 开始长按（改进版）
    /// </summary>
    void StartLongPress()
    {
        isLongPressing = true;
        longPressStartTime = Time.time;

        Debug.Log($"🎯 开始长按 {expectedKey}！需要持续 {longPressDuration:F1}秒");

        // 如果还没有进度圆，现在创建
        if (currentProgressCircle == null)
        {
            CreateDefaultProgressIndicator();
        }

        // 确保进度填充组件存在
        if (progressFill == null)
        {
            progressFill = currentProgressCircle.GetComponentInChildren<UnityEngine.UI.Image>();
        }

        // 重置进度
        if (progressFill != null)
        {
            progressFill.fillAmount = 0f;
            progressFill.color = longPressColor;
        }
    }

    /// <summary>
    /// 创建进度指示器（在按键按下前显示）
    /// </summary>
    void CreateProgressIndicator()
    {
        if (progressCirclePrefab != null)
        {
            CreateAdvancedProgressCircle();
            return;
        }

        CreateDefaultProgressIndicator();
    }

    /// <summary>
    /// 创建默认进度指示器（改进版）
    /// </summary>
    void CreateDefaultProgressIndicator()
    {
        // 创建进度指示器GameObject
        currentProgressCircle = new GameObject("ProgressIndicator");
        currentProgressCircle.transform.position = longPressKeyPosition;

        // 添加Canvas
        Canvas canvas = currentProgressCircle.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = mainCamera;
        canvas.sortingOrder = 100; // 确保显示在最前面

        // 设置Canvas缩放
        RectTransform canvasRect = currentProgressCircle.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1, 1);
        currentProgressCircle.transform.localScale = Vector3.one * 0.01f; // 适当缩放

        // 创建外环指示器（背景环）
        GameObject ringObj = new GameObject("BackgroundRing");
        ringObj.transform.SetParent(currentProgressCircle.transform);

        UnityEngine.UI.Image ringImage = ringObj.AddComponent<UnityEngine.UI.Image>();
        ringImage.color = new Color(0.3f, 0.3f, 0.3f, 0.8f); // 深灰色背景环
        ringImage.type = UnityEngine.UI.Image.Type.Filled;
        ringImage.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
        ringImage.fillAmount = 1f; // 完整的背景环

        RectTransform ringRect = ringObj.GetComponent<RectTransform>();
        ringRect.sizeDelta = new Vector2(100, 100);
        ringRect.anchoredPosition = Vector2.zero;

        // 创建内部填充圆（进度填充）
        GameObject fillObj = new GameObject("ProgressFill");
        fillObj.transform.SetParent(currentProgressCircle.transform);

        progressFill = fillObj.AddComponent<UnityEngine.UI.Image>();
        progressFill.type = UnityEngine.UI.Image.Type.Filled;
        progressFill.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
        progressFill.fillOrigin = 0; // 从顶部开始填充
        progressFill.fillClockwise = true; // 顺时针填充
        progressFill.fillAmount = 0f; // 初始为空
        progressFill.color = longPressColor;

        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.sizeDelta = new Vector2(90, 90); // 稍小一点，形成环形效果
        fillRect.anchoredPosition = Vector2.zero;

        // 添加中心文字提示（可选）
        GameObject textObj = new GameObject("HintText");
        textObj.transform.SetParent(currentProgressCircle.transform);

        UnityEngine.UI.Text hintText = textObj.AddComponent<UnityEngine.UI.Text>();
        hintText.text = "HOLD";
        hintText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        hintText.fontSize = 12;
        hintText.color = Color.white;
        hintText.alignment = TextAnchor.MiddleCenter;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(80, 20);
        textRect.anchoredPosition = Vector2.zero;

        Debug.Log("💡 长按指示器已显示！按住空格键开始填充");
    }

    /// <summary>
    /// 创建更高级的进度圆（如果你有Prefab的话）
    /// </summary>
    void CreateAdvancedProgressCircle()
    {
        // 使用预制体
        currentProgressCircle = Instantiate(progressCirclePrefab, longPressKeyPosition, Quaternion.identity);

        // 获取所有Image组件
        UnityEngine.UI.Image[] images = currentProgressCircle.GetComponentsInChildren<UnityEngine.UI.Image>();

        foreach (var img in images)
        {
            // 找到进度填充组件（通过名称或标签）
            if (img.name.Contains("Fill") || img.name.Contains("Progress"))
            {
                progressFill = img;
                progressFill.type = UnityEngine.UI.Image.Type.Filled;
                progressFill.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
                progressFill.fillAmount = 0f;
                break;
            }
        }

        // 如果没找到填充组件，使用第一个Image
        if (progressFill == null && images.Length > 0)
        {
            progressFill = images[0];
            progressFill.type = UnityEngine.UI.Image.Type.Filled;
            progressFill.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
            progressFill.fillAmount = 0f;
        }
    }

    /// <summary>
    /// 更新长按进度（改进版 - 这是核心填充方法！）
    /// </summary>
    void UpdateLongPressProgress()
    {
        if (!isLongPressing || progressFill == null) return;

        float elapsed = Time.time - longPressStartTime;
        float progress = Mathf.Clamp01(elapsed / longPressDuration);

        // 🎯 核心填充代码 - 这里实现圆形逐渐填充！
        progressFill.fillAmount = progress;

        // 添加颜色渐变效果
        Color startColor = longPressColor;
        Color endColor = successKeyColor;
        progressFill.color = Color.Lerp(startColor, endColor, progress);

        // 可选：添加缩放效果
        if (currentProgressCircle != null)
        {
            float scale = 0.01f + (progress * 0.002f); // 轻微的缩放效果
            currentProgressCircle.transform.localScale = Vector3.one * scale;
        }

        // 可选：添加旋转效果
        if (currentProgressCircle != null)
        {
            float rotation = progress * 360f * 0.1f; // 轻微旋转
            currentProgressCircle.transform.rotation = Quaternion.Euler(0, 0, rotation);
        }

        // 调试信息
        if (progress > 0.1f && progress < 0.9f && Time.frameCount % 30 == 0) // 每30帧打印一次
        {
            Debug.Log($"🔄 长按进度: {progress:P1} ({elapsed:F1}s / {longPressDuration:F1}s)");
        }
    }

    /// <summary>
    /// 长按完成
    /// </summary>
    void OnLongPressComplete()
    {
        Debug.Log($"🎉 长按成功完成！持续时间: {longPressDuration:F2}s");

        isLongPressing = false;
        DestroyProgressCircle();

        ShowFeedback(expectedKey, successKeyColor);
        OnBeatSuccess();
    }

    /// <summary>
    /// 长按失败
    /// </summary>
    void OnLongPressFailed()
    {
        isLongPressing = false;
        DestroyProgressCircle();

        ShowFeedback(expectedKey, missKeyColor);
        StartSlowMotion();

        // 等待间隔后开始下一个节拍
        StartCoroutine(WaitForNextBeat());
    }

    /// <summary>
    /// 销毁进度圆
    /// </summary>
    void DestroyProgressCircle()
    {
        if (currentProgressCircle != null)
        {
            Destroy(currentProgressCircle);
            currentProgressCircle = null;
        }
        progressFill = null;
    }

    /// <summary>
    /// 节拍成功
    /// </summary>
    void OnBeatSuccess()
    {
        waitingForInput = false;
        isLongPressBeat = false; // 重置长按状态
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
        isLongPressBeat = false; // 重置长按状态
        DestroyProgressCircle(); // 清理进度圆
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
        isLongPressBeat = false; // 重置长按状态
        DestroyProgressCircle(); // 清理进度圆
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
        else if (key == longPressKey && longPressKey_SpriteRenderer != null)
        {
            StartCoroutine(ShowColorFeedback(longPressKey_SpriteRenderer, color));
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
        if (longPressKey_SpriteRenderer != null)
        {
            longPressKey_SpriteRenderer.color = color;
        }
    }

    /// <summary>
    /// 设置长按键颜色
    /// </summary>
    void SetLongPressKeyColor(Color color)
    {
        if (longPressKey_SpriteRenderer != null)
        {
            longPressKey_SpriteRenderer.color = color;
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

    /// <summary>
    /// 重新开始游戏
    /// </summary>
    [ContextMenu("重新开始")]
    public void RestartGame()
    {
        // 停止所有协程
        StopAllCoroutines();

        // 重置协程引用
        aKeyColorCoroutine = null;
        dKeyColorCoroutine = null;
        blinkCoroutine = null;

        // 重置长按状态
        isLongPressBeat = false;
        isLongPressing = false;
        DestroyProgressCircle();

        beatCounter = 0;
        waitingForInput = false;

        // 重置慢动作状态
        if (inSlowMotion)
        {
            Time.timeScale = normalTimeScale;
            inSlowMotion = false;
            slowMotionTimer = 0f;

            // 恢复摄像机颜色
            if (mainCamera != null)
            {
                mainCamera.backgroundColor = originalCameraColor;
            }

            // 关闭UI指示器
            if (slowMotionIndicator != null)
            {
                slowMotionIndicator.SetActive(false);
            }
        }

        SetAllKeysColor(normalKeyColor);

        gameStartTime = Time.time;
        StartNextBeat();

        Debug.Log("游戏重新开始！");
    }

    /// <summary>
    /// 暂停/继续游戏
    /// </summary>
    [ContextMenu("暂停/继续")]
    public void TogglePause()
    {
        if (Time.timeScale == 0)
        {
            Time.timeScale = inSlowMotion ? slowMotionTimeScale : normalTimeScale;
            Debug.Log("游戏继续");
        }
        else
        {
            Time.timeScale = 0;
            Debug.Log("游戏暂停");
        }
    }

    /// <summary>
    /// 添加粒子效果（可选增强功能）
    /// </summary>
    void AddProgressParticles()
    {
        if (currentProgressCircle == null) return;

        GameObject particleObj = new GameObject("ProgressParticles");
        particleObj.transform.SetParent(currentProgressCircle.transform);
        particleObj.transform.localPosition = Vector3.zero;

        ParticleSystem particles = particleObj.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.startColor = longPressColor;
        main.startSize = 0.1f;
        main.startLifetime = 1f;
        main.maxParticles = 20;

        var emission = particles.emission;
        emission.rateOverTime = 10f;

        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.5f;
    }

    /// <summary>
    /// 获取当前游戏统计信息
    /// </summary>
    public void LogGameStats()
    {
        float gameTime = Time.time - gameStartTime;
        Debug.Log($"📊 游戏统计 - 运行时间: {gameTime:F1}s, 总节拍数: {beatCounter}, 当前状态: {(waitingForInput ? "等待输入" : "处理中")}");
    }

    /// <summary>
    /// 调整游戏难度
    /// </summary>
    [ContextMenu("提高难度")]
    public void IncreaseDifficulty()
    {
        beatInterval = Mathf.Max(0.3f, beatInterval - 0.1f);
        successWindow = Mathf.Max(0.1f, successWindow - 0.05f);
        longPressFrequency = Mathf.Min(0.7f, longPressFrequency + 0.1f);

        Debug.Log($"🔥 难度提升！节拍间隔: {beatInterval:F1}s, 成功窗口: {successWindow:F1}s, 长按频率: {longPressFrequency:P0}");
    }

    /// <summary>
    /// 降低游戏难度
    /// </summary>
    [ContextMenu("降低难度")]
    public void DecreaseDifficulty()
    {
        beatInterval = Mathf.Min(3.0f, beatInterval + 0.1f);
        successWindow = Mathf.Min(1.0f, successWindow + 0.05f);
        longPressFrequency = Mathf.Max(0.1f, longPressFrequency - 0.1f);

        Debug.Log($"😌 难度降低！节拍间隔: {beatInterval:F1}s, 成功窗口: {successWindow:F1}s, 长按频率: {longPressFrequency:P0}");
    }

    /// <summary>
    /// 调试信息 - 显示当前状态
    /// </summary>
    void OnGUI()
    {
        if (!Application.isPlaying) return;

        // 在屏幕左上角显示调试信息
        GUI.color = Color.white;
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));

        GUILayout.Label($"节拍计数: {beatCounter}");
        GUILayout.Label($"等待输入: {waitingForInput}");
        GUILayout.Label($"期望按键: {expectedKey}");
        GUILayout.Label($"长按节拍: {isLongPressBeat}");
        GUILayout.Label($"正在长按: {isLongPressing}");
        GUILayout.Label($"慢动作: {inSlowMotion}");

        if (isLongPressing)
        {
            float elapsed = Time.time - longPressStartTime;
            float progress = elapsed / longPressDuration;
            GUILayout.Label($"长按进度: {progress:P1}");
        }

        GUILayout.EndArea();
    }
}