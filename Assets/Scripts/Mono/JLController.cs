using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class JLController : MonoBehaviour
{
    [Header("J/L Key Settings")]
    public KeyCode primaryKey = KeyCode.J;      // 主按键
    public KeyCode secondaryKey = KeyCode.L;    // 副按键

    [Header("Rhythm Settings")]
    public float beatInterval = 1.0f;           // 节拍间隔
    public float successWindow = 0.4f;          // 成功按键的时间窗口
    public float highlightDuration = 0.8f;     // 高亮显示的持续时间
    public bool infiniteLoop = true;            // 无限循环模式

    [Header("Visual Settings")]
    public Color normalKeyColor = Color.blue;
    public Color highlightKeyColor = Color.cyan;
    public Color successKeyColor = Color.green;
    public Color missKeyColor = Color.red;
    public float feedbackDisplayDuration = 0.5f;

    [Header("Sprite Prefabs")]
    public GameObject keyJ_Prefab;
    public GameObject keyL_Prefab;
    public Vector3 keyJSpawnPosition;
    public Vector3 keyLSpawnPosition;

    [Header("Slow Motion Settings")]
    public float slowMotionTimeScale = 0.3f;   // 慢放时的时间倍率
    public float slowMotionDuration = 1.5f;    // 慢放持续时间
    public Color slowMotionTintColor = Color.red; // 慢放时的屏幕色调

    [Header("Slow Motion Visual")]
    public GameObject slowMotionIndicator;      // 慢放UI指示器

    // 私有变量
    private SpriteRenderer keyJ_SpriteRenderer;
    private SpriteRenderer keyL_SpriteRenderer;
    private float gameStartTime;
    private int beatCounter = 0;
    private bool waitingForInput = false;
    private KeyCode expectedKey;
    private float currentBeatStartTime;
    private Coroutine jKeyColorCoroutine;
    private Coroutine lKeyColorCoroutine;

    // 慢动作相关
    private bool inSlowMotion = false;
    private float slowMotionTimer = 0f;
    private float normalTimeScale = 1f;
    private Camera mainCamera;
    private Color originalCameraColor;
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

        // 实例化J/L按键视觉
        if (keyJ_Prefab != null)
        {
            GameObject jObj = Instantiate(keyJ_Prefab, keyJSpawnPosition, Quaternion.identity);
            keyJ_SpriteRenderer = jObj.GetComponent<SpriteRenderer>();
            if (keyJ_SpriteRenderer == null) Debug.LogError("J 键 Prefab 没有 SpriteRenderer 组件！");
        }
        if (keyL_Prefab != null)
        {
            GameObject lObj = Instantiate(keyL_Prefab, keyLSpawnPosition, Quaternion.identity);
            keyL_SpriteRenderer = lObj.GetComponent<SpriteRenderer>();
            if (keyL_SpriteRenderer == null) Debug.LogError("L 键 Prefab 没有 SpriteRenderer 组件！");
        }
    }

    void Start()
    {
        gameStartTime = Time.time;
        SetAllKeysColor(normalKeyColor);
        StartNextBeat();
        Debug.Log($"JL节奏游戏开始！{primaryKey}-{secondaryKey}交替，无限循环模式。");
    }

    void Update()
    {
        HandleSlowMotion();
        CheckBeatTiming();
        HandlePlayerInput();
    }

    /// <summary>
    /// 处理玩家输入
    /// </summary>
    void HandlePlayerInput()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            OnKeyPressed(KeyCode.J);
        }
        else if (Input.GetKeyDown(KeyCode.L))
        {
            OnKeyPressed(KeyCode.L);
        }
    }

    /// <summary>
    /// 开始下一个节拍
    /// </summary>
    void StartNextBeat()
    {
        expectedKey = (beatCounter % 2 == 0) ? primaryKey : secondaryKey;
        currentBeatStartTime = Time.time;
        waitingForInput = true;

        Debug.Log($"JL节拍 {beatCounter}: 期望按键 {expectedKey}，开始时间 {currentBeatStartTime:F2}s");

        if (expectedKey == primaryKey)
        {
            SetKeyColor(KeyCode.J, highlightKeyColor);
        }
        else
        {
            SetKeyColor(KeyCode.L, highlightKeyColor);
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

        if (elapsed > successWindow)
        {
            Debug.LogWarning($"错过JL节拍！耗时: {elapsed:F2}s");
            OnBeatMissed();
        }
    }

    /// <summary>
    /// 按键被按下时的处理
    /// </summary>
    void OnKeyPressed(KeyCode pressedKey)
    {
        if (!waitingForInput)
        {
            Debug.LogWarning($"JL不在等待输入状态，按下了 {pressedKey}");
            ShowFeedback(pressedKey, missKeyColor);
            return;
        }

        float responseTime = Time.time - currentBeatStartTime;

        if (pressedKey == expectedKey)
        {
            string performance = GetPerformanceRating(responseTime);
            Debug.Log($"✅ JL {performance} 成功！按键: {pressedKey}, 反应时间: {responseTime:F3}s");
            OnBeatSuccess();
        }
        else
        {
            Debug.LogWarning($"❌ JL按错了！期望 {expectedKey}，按下了 {pressedKey}");
            OnBeatFailed();
        }
    }

    /// <summary>
    /// 节拍成功
    /// </summary>
    void OnBeatSuccess()
    {
        waitingForInput = false;
        ShowFeedback(expectedKey, successKeyColor);
        StartCoroutine(WaitForNextBeat());
    }

    /// <summary>
    /// 节拍失败
    /// </summary>
    void OnBeatFailed()
    {
        waitingForInput = false;
        ShowFeedback(expectedKey, missKeyColor);
        StartSlowMotion();
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
        StartCoroutine(WaitForNextBeat());
    }

    /// <summary>
    /// 等待下一个节拍
    /// </summary>
    IEnumerator WaitForNextBeat()
    {
        yield return new WaitForSeconds(0.1f);
        SetAllKeysColor(normalKeyColor);
        yield return new WaitForSeconds(beatInterval - 0.1f);

        if (infiniteLoop)
        {
            StartNextBeat();
        }
        else
        {
            Debug.Log("JL节奏序列结束！");
        }
    }

    /// <summary>
    /// 获取表现评级
    /// </summary>
    string GetPerformanceRating(float responseTime)
    {
        if (responseTime < 0.1f) return "Perfect";
        if (responseTime < 0.2f) return "Great";
        if (responseTime < 0.3f) return "Good";
        return "OK";
    }

    /// <summary>
    /// 显示按键反馈
    /// </summary>
    void ShowFeedback(KeyCode key, Color color)
    {
        if (key == primaryKey)
        {
            if (jKeyColorCoroutine != null) StopCoroutine(jKeyColorCoroutine);
            jKeyColorCoroutine = StartCoroutine(ShowColorFeedback(keyJ_SpriteRenderer, color));
        }
        else if (key == secondaryKey)
        {
            if (lKeyColorCoroutine != null) StopCoroutine(lKeyColorCoroutine);
            lKeyColorCoroutine = StartCoroutine(ShowColorFeedback(keyL_SpriteRenderer, color));
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
        if (key == KeyCode.J && keyJ_SpriteRenderer != null)
        {
            if (jKeyColorCoroutine != null) StopCoroutine(jKeyColorCoroutine);
            keyJ_SpriteRenderer.color = color;
        }
        else if (key == KeyCode.L && keyL_SpriteRenderer != null)
        {
            if (lKeyColorCoroutine != null) StopCoroutine(lKeyColorCoroutine);
            keyL_SpriteRenderer.color = color;
        }
    }

    /// <summary>
    /// 设置所有按键颜色
    /// </summary>
    void SetAllKeysColor(Color color)
    {
        if (keyJ_SpriteRenderer != null)
        {
            if (jKeyColorCoroutine != null) StopCoroutine(jKeyColorCoroutine);
            keyJ_SpriteRenderer.color = color;
        }
        if (keyL_SpriteRenderer != null)
        {
            if (lKeyColorCoroutine != null) StopCoroutine(lKeyColorCoroutine);
            keyL_SpriteRenderer.color = color;
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

            StartSlowMotionVisuals();
            Debug.Log("🐌 JL触发慢动作！时间变慢...");
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

        EndSlowMotionVisuals();
        Debug.Log("⚡ JL慢动作结束！时间恢复正常");
    }

    /// <summary>
    /// 启动慢放视觉效果
    /// </summary>
    void StartSlowMotionVisuals()
    {
        if (slowMotionIndicator != null)
        {
            slowMotionIndicator.SetActive(true);
        }

        if (mainCamera != null)
        {
            StartCoroutine(LerpCameraColor(originalCameraColor, slowMotionTintColor, 0.3f));
        }

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
        if (slowMotionIndicator != null)
        {
            slowMotionIndicator.SetActive(false);
        }

        if (mainCamera != null)
        {
            StartCoroutine(LerpCameraColor(mainCamera.backgroundColor, originalCameraColor, 0.3f));
        }

        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        SetAllKeysColor(normalKeyColor);
    }

    /// <summary>
    /// 更新慢放视觉效果
    /// </summary>
    void UpdateSlowMotionVisuals(float progress)
    {
        if (mainCamera != null)
        {
            float intensity = 1.0f - (progress * 0.5f);
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
            SetAllKeysColor(Color.red);
            yield return new WaitForSecondsRealtime(0.2f);

            SetAllKeysColor(normalKeyColor);
            yield return new WaitForSecondsRealtime(0.2f);
        }
    }
}