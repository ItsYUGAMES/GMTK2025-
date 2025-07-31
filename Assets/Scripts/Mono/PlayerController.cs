using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour // 脚本名已改为 PlayerController
{
    [Header("Sprite Prefabs")]
    public GameObject keyA_Prefab; // 拖拽你的 A 键 Sprite Prefab 到这里
    public GameObject keyD_Prefab; // 拖拽你的 D 键 Sprite Prefab 到这里
    public Vector3 keyASpawnPosition; // A 键 Sprite 生成位置
    public Vector3 keyDSpawnPosition; // D 键 Sprite 生成位置

    private SpriteRenderer keyA_SpriteRenderer;
    private SpriteRenderer keyD_SpriteRenderer;

    [Header("Visual Settings")]
    public Color normalKeyColor = Color.gray;
    public Color highlightKeyColor = Color.white;
    public Color successKeyColor = Color.green;
    public Color missKeyColor = Color.red;
    public float feedbackDisplayDuration = 0.1f; // 反馈颜色显示的时长

    [Header("Rhythm Settings")]
    public List<RhythmBeat> rhythmSequence;
    public float beatDetectionWindow = 0.2f;
    public float beatDisplayLeadTime = 1.0f;

    [Header("Slow Motion Settings")]
    public float slowMotionDuration = 2f;
    public float slowMotionTimeScale = 0.2f;
    private float normalTimeScale;
    private float slowMotionTimer = 0f;
    private bool inSlowMotion = false;

    private float songStartTime;
    private int currentBeatIndex = 0;

    // 用于管理按键颜色反馈的协程引用
    private Coroutine aKeyColorCoroutine;
    private Coroutine dKeyColorCoroutine;

    void Awake()
    {
        normalTimeScale = Time.timeScale;

        // 实例化 Sprite Prefab 并获取它们的 SpriteRenderer 组件
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
        songStartTime = Time.time;
        SetKeyColor(KeyCode.A, normalKeyColor); // 初始化 A 键颜色
        SetKeyColor(KeyCode.D, normalKeyColor); // 初始化 D 键颜色

        if (rhythmSequence.Count == 0) Debug.LogWarning("节奏序列为空！请在Inspector中添加节奏点。");
    }

    void Update()
    {
        HandleSlowMotion();
        ProcessRhythmBeats();
        HandlePlayerInput();
    }

    void HandleSlowMotion()
    {
        if (inSlowMotion)
        {
            slowMotionTimer += Time.unscaledDeltaTime;
            if (slowMotionTimer >= slowMotionDuration)
            {
                Time.timeScale = normalTimeScale;
                inSlowMotion = false;
                slowMotionTimer = 0f;
                Debug.Log("时间恢复正常。");
            }
        }
    }

    void ProcessRhythmBeats()
    {
        if (currentBeatIndex >= rhythmSequence.Count) return;

        RhythmBeat currentBeat = rhythmSequence[currentBeatIndex];
        float currentTime = Time.time - songStartTime;

        if (!currentBeat.isProcessed && currentTime >= currentBeat.time - beatDisplayLeadTime && currentTime < currentBeat.time + beatDetectionWindow)
        {
            SetKeyColor(currentBeat.requiredKey, highlightKeyColor); // 高亮按键
        }
        else if (!currentBeat.isProcessed && currentTime >= currentBeat.time + beatDetectionWindow)
        {
            Debug.LogWarning($"节奏点 {currentBeatIndex} ({currentBeat.requiredKey}) 错过！");
            ShowTemporaryFeedback(currentBeat.requiredKey, missKeyColor); // 显示错过反馈
            currentBeat.isProcessed = true;
            currentBeatIndex++;
        }
    }

    void HandlePlayerInput()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            CheckBeatHit(KeyCode.A);
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            CheckBeatHit(KeyCode.D);
        }
    }

    void CheckBeatHit(KeyCode pressedKey)
    {
        if (currentBeatIndex >= rhythmSequence.Count)
        {
            ShowTemporaryFeedback(pressedKey, missKeyColor); // 视为错误按下
            return;
        }

        RhythmBeat currentBeat = rhythmSequence[currentBeatIndex];
        float currentTime = Time.time - songStartTime;

        if (currentTime >= currentBeat.time - beatDetectionWindow && currentTime <= currentBeat.time + beatDetectionWindow)
        {
            if (pressedKey == currentBeat.requiredKey)
            {
                Debug.Log($"成功命中节奏点 {currentBeatIndex} ({pressedKey})！");
                currentBeat.isHit = true;
                currentBeat.isProcessed = true;
                ShowTemporaryFeedback(pressedKey, successKeyColor); // 显示成功反馈
                currentBeatIndex++;
            }
            else
            {
                Debug.LogWarning($"按键错误！期望 {currentBeat.requiredKey}，按下了 {pressedKey}");
                ShowTemporaryFeedback(pressedKey, missKeyColor); // 显示错误反馈
                StartSlowMotion();
            }
        }
        else
        {
            Debug.LogWarning($"按键时机错误！按下了 {pressedKey}，但不在有效窗口内。");
            ShowTemporaryFeedback(pressedKey, missKeyColor); // 显示错误反馈
            StartSlowMotion();
        }
    }

    /// <summary>
    /// 设置指定按键的颜色。
    /// </summary>
    void SetKeyColor(KeyCode key, Color color)
    {
        if (key == KeyCode.A && keyA_SpriteRenderer != null)
        {
            // 停止可能正在运行的 A 键颜色协程
            if (aKeyColorCoroutine != null) StopCoroutine(aKeyColorCoroutine);
            keyA_SpriteRenderer.color = color;
        }
        else if (key == KeyCode.D && keyD_SpriteRenderer != null)
        {
            // 停止可能正在运行的 D 键颜色协程
            if (dKeyColorCoroutine != null) StopCoroutine(dKeyColorCoroutine);
            keyD_SpriteRenderer.color = color;
        }
    }

    /// <summary>
    /// 显示短暂的颜色反馈，然后恢复正常颜色。
    /// </summary>
    void ShowTemporaryFeedback(KeyCode key, Color feedbackColor)
    {
        if (key == KeyCode.A)
        {
            // 停止之前的协程以避免冲突
            if (aKeyColorCoroutine != null) StopCoroutine(aKeyColorCoroutine);
            aKeyColorCoroutine = StartCoroutine(ChangeKeyColorTemporarily(keyA_SpriteRenderer, feedbackColor));
        }
        else if (key == KeyCode.D)
        {
            if (dKeyColorCoroutine != null) StopCoroutine(dKeyColorCoroutine);
            dKeyColorCoroutine = StartCoroutine(ChangeKeyColorTemporarily(keyD_SpriteRenderer, feedbackColor));
        }
    }

    /// <summary>
    /// 协程：短暂改变 SpriteRenderer 的颜色，然后恢复正常。
    /// </summary>
    IEnumerator ChangeKeyColorTemporarily(SpriteRenderer targetRenderer, Color tempColor)
    {
        if (targetRenderer == null) yield break; // 防止空引用

        Color originalColor = targetRenderer.color; // 记录当前颜色
        targetRenderer.color = tempColor; // 立即设置为反馈颜色

        yield return new WaitForSeconds(feedbackDisplayDuration); // 等待指定时长

        targetRenderer.color = normalKeyColor; // 恢复正常颜色
        // 可选：在这里清空协程引用，但由于是临时协程，不清空也影响不大
        // 如果是 aKeyColorCoroutine 或 dKeyColorCoroutine，在 ShowTemporaryFeedback 中启动前停止并更新即可
    }

    void StartSlowMotion()
    {
        if (!inSlowMotion)
        {
            Time.timeScale = slowMotionTimeScale;
            inSlowMotion = true;
            slowMotionTimer = 0f;
            Debug.Log("触发慢放！");
            // TODO: 播放慢放音效、屏幕特效等
        }
    }

    public void StartNewRhythmSequence(List<RhythmBeat> newSequence)
    {
        rhythmSequence = newSequence;
        currentBeatIndex = 0;
        songStartTime = Time.time;
        SetKeyColor(KeyCode.A, normalKeyColor); // 重置 A 键颜色
        SetKeyColor(KeyCode.D, normalKeyColor); // 重置 D 键颜色
        Time.timeScale = normalTimeScale;
        inSlowMotion = false;
        slowMotionTimer = 0f;
        Debug.Log("新的节奏序列已启动。");
    }
}